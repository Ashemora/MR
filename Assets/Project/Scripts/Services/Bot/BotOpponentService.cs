using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Bot;
using R3;
using UnityEngine;
using VContainer.Unity;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Bot
{
    public class BotOpponentService : IBotOpponentService, IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IHeroService _heroService;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IUnitAbilityActivationService _unitAbilityActivationService;
        private readonly IAbilityExecutionService _abilityExecutionService;
        private readonly IAvatarService _avatarService;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IBattleEconomyModifierService _battleEconomyModifier;
        private readonly BotConfig _botConfig;
        private readonly SlotLayoutConfig _slotLayoutConfig;

        private BotDecisionEngine _engine;
        private CancellationTokenSource _cts;
        private IDisposable _stateSub;
        private IDisposable _phaseSub;
        private readonly bool[] _heroActivationPending = new bool[4];
        private bool _dischargeScheduled;


        public BotOpponentService(
            EventBus eventBus,
            IHeroService heroService,
            IGameStateService gameStateService,
            IBattleFlowService battleFlowService,
            IBattleSideEnergyService battleSideEnergyService,
            IUnitAbilityActivationService unitAbilityActivationService,
            IAbilityExecutionService abilityExecutionService,
            IAvatarService avatarService,
            IAvatarGroupDefenseService groupDefense,
            IBattleEconomyModifierService battleEconomyModifier,
            BotConfig botConfig,
            SlotLayoutConfig slotLayoutConfig)
        {
            _eventBus = eventBus;
            _heroService = heroService;
            _gameStateService = gameStateService;
            _battleFlowService = battleFlowService;
            _battleSideEnergyService = battleSideEnergyService;
            _unitAbilityActivationService = unitAbilityActivationService;
            _abilityExecutionService = abilityExecutionService;
            _avatarService = avatarService;
            _groupDefense = groupDefense;
            _battleEconomyModifier = battleEconomyModifier;
            _botConfig = botConfig;
            _slotLayoutConfig = slotLayoutConfig;
        }


        public void Start()
        {
            if (false == _botConfig.Enabled)
                return;

            _engine = new BotDecisionEngine(_botConfig.ToSettings(), UnityEngine.Random.Range(0, int.MaxValue));

            _stateSub = _gameStateService.State
                .Where(s => s != GameState.Playing)
                .Take(1)
                .Subscribe(_ => StopLoops());
            _phaseSub = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);

            if (_battleFlowService.IsInitialized)
                SyncLoopState(_battleFlowService.Snapshot.Phase);
        }

        public void Dispose()
        {
            StopLoops();
            _stateSub?.Dispose();
            _stateSub = null;
            _phaseSub?.Dispose();
            _phaseSub = null;
        }


        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            SyncLoopState(e.Phase);
        }

        private async UniTaskVoid RunEnemyChargeLoop(CancellationToken ct)
        {
            while (false == ct.IsCancellationRequested)
            {
                var interval = _botConfig.MatchEnergyTickInterval / _battleEconomyModifier.AutoEnergyIntervalMultiplier;
                var cancelled = await UniTask
                    .Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled || false == _gameStateService.IsPlaying)
                    return;

                var phase = _battleFlowService.Snapshot.Phase;
                if (phase == BattlePhaseKind.Match)
                {
                    var generatedEnergy = GenerateSimulatedMatchEnergy();
                    _battleSideEnergyService.AddEnergy(BattleSide.Enemy, generatedEnergy);
                }

                if (phase == BattlePhaseKind.Hero && false == _dischargeScheduled
                                                   && TryPreviewEnemyAvatar(out _))
                {
                    _dischargeScheduled = true;
                    ScheduleDischarge(ct).Forget();
                }
            }
        }

        private float RollCascadeMultiplier()
        {
            var roll = UnityEngine.Random.value;
            if (roll < _botConfig.GreatCascadeChance)
                return _botConfig.GreatCascadeMultiplier;
            if (roll < _botConfig.GoodCascadeChance)
                return _botConfig.GoodCascadeMultiplier;
            
            return 1f;
        }

        private int GenerateSimulatedMatchEnergy()
        {
            var variation = 1f + UnityEngine.Random.Range(-_botConfig.CascadeVariation, _botConfig.CascadeVariation);
            var baseEnergy = _botConfig.BaseMatchEnergyPerTick * variation * RollCascadeMultiplier();
            
            return Mathf.Max(1, Mathf.RoundToInt(baseEnergy));
        }

        private async UniTaskVoid ScheduleDischarge(CancellationToken ct)
        {
            var delay = _engine.GenerateAvatarActivationDelay();

            var cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct)
                .SuppressCancellationThrow();

            _dischargeScheduled = false;

            if (cancelled || false == _gameStateService.IsPlaying)
                return;

            if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                return;

            if (false == TryPreviewEnemyAvatar(out var preview))
                return;

            if (preview.ActionType == UnitActionType.DealDamage)
            {
                if (false == _groupDefense.IsExposed(BattleSide.Player))
                {
                    var playerSlots = _heroService.GetSlots(BattleSide.Player);
                    var targetIdx = _engine.PickGroupBreakTarget(
                        playerSlots,
                        _slotLayoutConfig.Group1SlotIndices,
                        _slotLayoutConfig.Group2SlotIndices);

                    if (targetIdx < 0)
                        return;

                    ExecuteEnemyAvatarAbility(preview.ActionType,
                        UnitDescriptor.Hero(BattleSide.Player, targetIdx, preview.ActionType));
                }
                else
                {
                    ExecuteEnemyAvatarAbility(preview.ActionType,
                        UnitDescriptor.Avatar(BattleSide.Player, preview.ActionType));
                }
            }
            else
                DischargeAvatarHeal(preview);
        }

        private void DischargeAvatarHeal(UnitAbilityActivationState preview)
        {
            if (false == _groupDefense.IsExposed(BattleSide.Enemy))
            {
                var enemySlots = _heroService.GetSlots(BattleSide.Enemy);
                var targetIdx = _engine.PickWeakestGroupHero(
                    enemySlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0)
                    return;

                ExecuteEnemyAvatarAbility(preview.ActionType,
                    UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, preview.ActionType));
            }
            else if (false == _avatarService.IsHpFull(BattleSide.Enemy))
            {
                ExecuteEnemyAvatarAbility(preview.ActionType,
                    UnitDescriptor.Avatar(BattleSide.Enemy, preview.ActionType));
            }
            else
            {
                var enemySlots = _heroService.GetSlots(BattleSide.Enemy);
                var targetIdx = _engine.PickMostWoundedHero(enemySlots);
                if (targetIdx < 0)
                    return;

                ExecuteEnemyAvatarAbility(preview.ActionType,
                    UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, preview.ActionType));
            }
        }

        private bool TryPreviewEnemyAvatar(out UnitAbilityActivationState preview)
        {
            return _unitAbilityActivationService.TryPreview(
                UnitDescriptor.Avatar(BattleSide.Enemy, UnitActionType.DealDamage), out preview);
        }

        private void ExecuteEnemyAvatarAbility(UnitActionType actionType, UnitDescriptor target)
        {
            _abilityExecutionService.Execute(UnitDescriptor.Avatar(BattleSide.Enemy, actionType), target);
        }

        private async UniTaskVoid RunHeroEnergyLoop(CancellationToken ct)
        {
            while (false == ct.IsCancellationRequested)
            {
                var interval = _botConfig.HeroActivationCheckInterval / _battleEconomyModifier.AutoEnergyIntervalMultiplier;
                var cancelled = await UniTask
                    .Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled || false == _gameStateService.IsPlaying)
                    return;

                if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                    continue;

                var slots = _heroService.GetSlots(BattleSide.Enemy);
                var pickedIndex = _engine.PickRandomAssignedSlot(slots);

                if (pickedIndex < 0)
                    continue;

                var currentEnemySlots = _heroService.GetSlots(BattleSide.Enemy);
                var updatedSlot = currentEnemySlots[pickedIndex];

                var source = UnitDescriptor.Hero(BattleSide.Enemy, pickedIndex, updatedSlot.ActionType);
                if (false == _unitAbilityActivationService.TryPreview(source, out _) || _heroActivationPending[pickedIndex])
                    continue;

                if (updatedSlot.ActionType == UnitActionType.HealAlly)
                {
                    bool hasHealTarget;

                    if (false == _groupDefense.IsExposed(BattleSide.Enemy))
                    {
                        var t = _engine.PickWeakestGroupHero(
                            currentEnemySlots,
                            _slotLayoutConfig.Group1SlotIndices,
                            _slotLayoutConfig.Group2SlotIndices);
                        hasHealTarget = t >= 0 && t != pickedIndex;
                    }
                    else
                    {
                        var t = _engine.PickMostWoundedHero(currentEnemySlots);
                        hasHealTarget = false == _avatarService.IsHpFull(BattleSide.Enemy)
                            || (t >= 0 && t != pickedIndex);
                    }

                    if (!hasHealTarget)
                        continue;
                }

                _heroActivationPending[pickedIndex] = true;
                ActivateWithDelay(pickedIndex, ct).Forget();
            }
        }

        private async UniTaskVoid ActivateWithDelay(int slotIndex, CancellationToken ct)
        {
            var delay = _engine.GenerateDelay(
                _botConfig.MinHeroActivationDelay,
                _botConfig.MaxHeroActivationDelay);

            var cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct)
                .SuppressCancellationThrow();

            _heroActivationPending[slotIndex] = false;

            if (cancelled || false == _gameStateService.IsPlaying)
                return;

            if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                return;

            var enemySlots = _heroService.GetSlots(BattleSide.Enemy);
            var slot = enemySlots[slotIndex];

            var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, slot.ActionType);
            if (false == _unitAbilityActivationService.TryPreview(source, out _))
                return;

            if (slot.ActionType == UnitActionType.DealDamage)
                ActivateHeroDamage(slotIndex, enemySlots);
            else if (slot.ActionType == UnitActionType.SupportAlly)
                ActivateHeroSupport(slotIndex);
            else
                ActivateHeroHeal(slotIndex, enemySlots);
        }

        private void ActivateHeroSupport(int slotIndex)
        {
            var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.SupportAlly);
            _abilityExecutionService.Execute(source, source);
        }

        private void ActivateHeroDamage(int slotIndex, IReadOnlyList<HeroSlotState> enemySlots)
        {
            if (false == _groupDefense.IsExposed(BattleSide.Player))
            {
                var playerSlots = _heroService.GetSlots(BattleSide.Player);
                var targetIdx = _engine.PickGroupBreakTarget(
                    playerSlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0)
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.DealDamage);
                var target = UnitDescriptor.Hero(BattleSide.Player, targetIdx, UnitActionType.DealDamage);
                _abilityExecutionService.Execute(source, target);
            }
            else
            {
                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.DealDamage);
                var target = UnitDescriptor.Avatar(BattleSide.Player, UnitActionType.DealDamage);
                _abilityExecutionService.Execute(source, target);
            }
        }

        private void ActivateHeroHeal(int slotIndex, IReadOnlyList<HeroSlotState> enemySlots)
        {
            if (false == _groupDefense.IsExposed(BattleSide.Enemy))
            {
                var targetIdx = _engine.PickWeakestGroupHero(
                    enemySlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0 || targetIdx == slotIndex)
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, UnitActionType.HealAlly);
                _abilityExecutionService.Execute(source, target);
            }
            else if (false == _avatarService.IsHpFull(BattleSide.Enemy))
            {
                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.HealAlly);
                var target = UnitDescriptor.Avatar(BattleSide.Enemy, UnitActionType.HealAlly);
                _abilityExecutionService.Execute(source, target);
            }
            else
            {
                var targetIdx = _engine.PickMostWoundedHero(enemySlots);
                if (targetIdx < 0 || targetIdx == slotIndex)
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, UnitActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, UnitActionType.HealAlly);
                _abilityExecutionService.Execute(source, target);
            }
        }

        private void StopLoops()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ResetPendingActions();
        }

        private void SyncLoopState(BattlePhaseKind phase)
        {
            if (false == _gameStateService.IsPlaying)
            {
                StopLoops();
                return;
            }

            if (phase is BattlePhaseKind.Match or BattlePhaseKind.Hero)
            {
                EnsureLoopsRunning();
                return;
            }

            StopLoops();
        }

        private void EnsureLoopsRunning()
        {
            if (null != _cts)
                return;

            _cts = new CancellationTokenSource();
            RunEnemyChargeLoop(_cts.Token).Forget();
            RunHeroEnergyLoop(_cts.Token).Forget();
        }

        private void ResetPendingActions()
        {
            Array.Clear(_heroActivationPending, 0, _heroActivationPending.Length);
            _dischargeScheduled = false;
        }
    }
}