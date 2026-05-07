using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Bot;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Rules;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Services.Bot
{
    public class BotOpponentService : IBotOpponentService, IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IHeroService _heroService;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IEnemyAvatarChargeService _enemyChargeService;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IBattleEconomyModifierService _battleEconomyModifier;
        private readonly INextAttackBuffService _nextAttackBuffService;
        private readonly IAbilityRepeatModifierService _abilityRepeatModifierService;
        private readonly IAbilityAdditionalTargetModifierService _abilityAdditionalTargetModifierService;
        private readonly IAbilityApplicationService _abilityApplicationService;
        private readonly IBattleClock _battleClock;
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
            IEnemyAvatarChargeService enemyChargeService,
            IPlayerStateService playerState,
            IEnemyStateService enemyState,
            IAvatarGroupDefenseService groupDefense,
            IBattleEconomyModifierService battleEconomyModifier,
            INextAttackBuffService nextAttackBuffService,
            IAbilityRepeatModifierService abilityRepeatModifierService,
            IAbilityAdditionalTargetModifierService abilityAdditionalTargetModifierService,
            IAbilityApplicationService abilityApplicationService,
            IBattleClock battleClock,
            BotConfig botConfig,
            SlotLayoutConfig slotLayoutConfig)
        {
            _eventBus = eventBus;
            _heroService = heroService;
            _gameStateService = gameStateService;
            _battleFlowService = battleFlowService;
            _battleSideEnergyService = battleSideEnergyService;
            _enemyChargeService = enemyChargeService;
            _playerState = playerState;
            _enemyState = enemyState;
            _groupDefense = groupDefense;
            _battleEconomyModifier = battleEconomyModifier;
            _nextAttackBuffService = nextAttackBuffService;
            _abilityRepeatModifierService = abilityRepeatModifierService;
            _abilityAdditionalTargetModifierService = abilityAdditionalTargetModifierService;
            _abilityApplicationService = abilityApplicationService;
            _battleClock = battleClock;
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

                if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Match)
                    continue;

                var generatedEnergy = GenerateSimulatedMatchEnergy();
                _battleSideEnergyService.AddEnergy(BattleSide.Enemy, generatedEnergy);

                if (_enemyChargeService.IsReady && false == _dischargeScheduled)
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

            var abilityPower = _enemyChargeService.AbilityPower;

            if (_enemyChargeService.AbilityType == HeroActionType.DealDamage)
            {
                if (!_groupDefense.IsExposed(BattleSide.Player))
                {
                    var playerSlots = _heroService.GetSlots(BattleSide.Player);
                    var targetIdx = _engine.PickGroupBreakTarget(
                        playerSlots,
                        _slotLayoutConfig.Group1SlotIndices,
                        _slotLayoutConfig.Group2SlotIndices);

                    if (targetIdx < 0)
                        return;

                    if (!_enemyChargeService.TryRelease())
                        return;

                    abilityPower = GetAvatarActionValueWithNextAttackBuff(abilityPower);

                    var source = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.DealDamage);
                    var target = UnitDescriptor.Hero(BattleSide.Player, targetIdx, HeroActionType.DealDamage);
                    _abilityApplicationService.Apply(source, target, HeroActionType.DealDamage, abilityPower,
                        0, _battleClock.CurrentTick);
                    PublishAbilityExecuted(source, target, HeroActionType.DealDamage, abilityPower);
                }
                else
                {
                    if (!_enemyChargeService.TryRelease())
                        return;

                    abilityPower = GetAvatarActionValueWithNextAttackBuff(abilityPower);

                    var source = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.DealDamage);
                    var target = UnitDescriptor.Avatar(BattleSide.Player, HeroActionType.DealDamage);
                    _abilityApplicationService.Apply(source, target, HeroActionType.DealDamage, abilityPower,
                        0, _battleClock.CurrentTick);
                    PublishAbilityExecuted(source, target, HeroActionType.DealDamage, abilityPower);
                }
            }
            else
                DischargeAvatarHeal(abilityPower);
        }

        private void DischargeAvatarHeal(int abilityPower)
        {
            if (!_groupDefense.IsExposed(BattleSide.Enemy))
            {
                var enemySlots = _heroService.GetSlots(BattleSide.Enemy);
                var targetIdx = _engine.PickWeakestGroupHero(
                    enemySlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0)
                    return;

                if (!_enemyChargeService.TryRelease())
                    return;

                var source = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, HeroActionType.HealAlly);
                _abilityApplicationService.Apply(source, target, HeroActionType.HealAlly, abilityPower,
                    0, _battleClock.CurrentTick);
                PublishAbilityExecuted(source, target, HeroActionType.HealAlly, abilityPower);
            }
            else if (_enemyState.CurrentHP < _enemyState.MaxHP)
            {
                if (false == _enemyChargeService.TryRelease())
                    return;

                var source = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.HealAlly);
                var target = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.HealAlly);
                _abilityApplicationService.Apply(source, target, HeroActionType.HealAlly, abilityPower,
                    0, _battleClock.CurrentTick);
                PublishAbilityExecuted(source, target, HeroActionType.HealAlly, abilityPower);
            }
            else
            {
                var enemySlots = _heroService.GetSlots(BattleSide.Enemy);
                var targetIdx = _engine.PickMostWoundedHero(enemySlots);
                if (targetIdx < 0)
                    return;

                if (false == _enemyChargeService.TryRelease())
                    return;

                var source = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, HeroActionType.HealAlly);
                _abilityApplicationService.Apply(source, target, HeroActionType.HealAlly, abilityPower,
                    0, _battleClock.CurrentTick);
                PublishAbilityExecuted(source, target, HeroActionType.HealAlly, abilityPower);
            }
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

                if (false == _heroService.CanActivate(BattleSide.Enemy, pickedIndex) || _heroActivationPending[pickedIndex])
                    continue;

                if (updatedSlot.ActionType == HeroActionType.HealAlly)
                {
                    bool hasHealTarget;

                    if (!_groupDefense.IsExposed(BattleSide.Enemy))
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
                        hasHealTarget = _enemyState.CurrentHP < _enemyState.MaxHP
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

            if (false == _heroService.CanActivate(BattleSide.Enemy, slotIndex))
                return;

            if (slot.ActionType == HeroActionType.DealDamage)
                ActivateHeroDamage(slotIndex, enemySlots);
            else
                ActivateHeroHeal(slotIndex, enemySlots);
        }

        private void ActivateHeroDamage(int slotIndex, IReadOnlyList<HeroSlotState> enemySlots)
        {
            if (!_groupDefense.IsExposed(BattleSide.Player))
            {
                var playerSlots = _heroService.GetSlots(BattleSide.Player);
                var targetIdx = _engine.PickGroupBreakTarget(
                    playerSlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0)
                    return;

                if (!_heroService.TryDischargeHero(BattleSide.Enemy, slotIndex, out _, out var damageValue))
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, HeroActionType.DealDamage);
                var target = UnitDescriptor.Hero(BattleSide.Player, targetIdx, HeroActionType.DealDamage);
                ApplyHeroAbility(source, target, HeroActionType.DealDamage, damageValue);
            }
            else
            {
                if (!_heroService.TryDischargeHero(BattleSide.Enemy, slotIndex, out _, out var damageValue))
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, HeroActionType.DealDamage);
                var target = UnitDescriptor.Avatar(BattleSide.Player, HeroActionType.DealDamage);
                ApplyHeroAbility(source, target, HeroActionType.DealDamage, damageValue);
            }
        }

        private void ActivateHeroHeal(int slotIndex, IReadOnlyList<HeroSlotState> enemySlots)
        {
            if (!_groupDefense.IsExposed(BattleSide.Enemy))
            {
                var targetIdx = _engine.PickWeakestGroupHero(
                    enemySlots,
                    _slotLayoutConfig.Group1SlotIndices,
                    _slotLayoutConfig.Group2SlotIndices);

                if (targetIdx < 0 || targetIdx == slotIndex)
                    return;

                if (!_heroService.TryDischargeHero(BattleSide.Enemy, slotIndex, out _, out var healValue))
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, HeroActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, HeroActionType.HealAlly);
                ApplyHeroAbility(source, target, HeroActionType.HealAlly, healValue);
            }
            else if (_enemyState.CurrentHP < _enemyState.MaxHP)
            {
                if (!_heroService.TryDischargeHero(BattleSide.Enemy, slotIndex, out _, out var healValue))
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, HeroActionType.HealAlly);
                var target = UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.HealAlly);
                ApplyHeroAbility(source, target, HeroActionType.HealAlly, healValue);
            }
            else
            {
                var targetIdx = _engine.PickMostWoundedHero(enemySlots);
                if (targetIdx < 0 || targetIdx == slotIndex)
                    return;

                if (!_heroService.TryDischargeHero(BattleSide.Enemy, slotIndex, out _, out var healValue))
                    return;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex, HeroActionType.HealAlly);
                var target = UnitDescriptor.Hero(BattleSide.Enemy, targetIdx, HeroActionType.HealAlly);
                ApplyHeroAbility(source, target, HeroActionType.HealAlly, healValue);
            }
        }

        private void ApplyHeroAbility(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType,
            int value)
        {
            var occurredAtTick = _battleClock.CurrentTick;
            var additionalTargets = SelectAdditionalTargets(source, target, actionType);
            _abilityApplicationService.Apply(source, target, actionType, value, GetRepeatCount(source),
                occurredAtTick);
            _abilityApplicationService.ApplyAdditionalTargets(source, additionalTargets, actionType, value,
                occurredAtTick);
            PublishAbilityExecuted(source, target, actionType, value);
        }

        private void PublishAbilityExecuted(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType,
            int value)
        {
            _eventBus.Publish(new AbilityExecutedEvent(source, target, actionType, value, _battleClock.CurrentTick));
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
            if (_cts != null)
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

        private int GetAvatarActionValueWithNextAttackBuff(int baseActionValue)
        {
            if (_enemyChargeService.AbilityType != HeroActionType.DealDamage)
                return baseActionValue;

            var source = UnitDescriptor.Avatar(BattleSide.Enemy, _enemyChargeService.AbilityType);
            
            return baseActionValue + _nextAttackBuffService.Consume(source);
        }

        private int GetRepeatCount(UnitDescriptor source)
        {
            return source.Kind == UnitKind.Hero ? _abilityRepeatModifierService.GetRepeatCount(source) : 0;
        }

        private int GetAdditionalTargetCount(UnitDescriptor source)
        {
            return source.Kind == UnitKind.Hero
                ? _abilityAdditionalTargetModifierService.GetAdditionalTargetCount(source)
                : 0;
        }

        private List<UnitDescriptor> SelectAdditionalTargets(UnitDescriptor source, UnitDescriptor primaryTarget,
            HeroActionType actionType)
        {
            return AbilityAdditionalTargetRules.SelectTargets(source, primaryTarget, actionType,
                GetAdditionalTargetCount(source), CollectTargetCandidates());
        }

        private List<AbilityTargetCandidate> CollectTargetCandidates()
        {
            var result = new List<AbilityTargetCandidate>(10)
            {
                new(UnitDescriptor.Avatar(BattleSide.Player, HeroActionType.DealDamage),
                    _playerState.CurrentHP, _playerState.MaxHP, _playerState.CurrentHP > 0,
                    _groupDefense.IsExposed(BattleSide.Player)),
                new(UnitDescriptor.Avatar(BattleSide.Enemy, HeroActionType.DealDamage),
                    _enemyState.CurrentHP, _enemyState.MaxHP, _enemyState.CurrentHP > 0,
                    _groupDefense.IsExposed(BattleSide.Enemy))
            };

            AddHeroTargetCandidates(result, BattleSide.Player);
            AddHeroTargetCandidates(result, BattleSide.Enemy);

            return result;
        }

        private void AddHeroTargetCandidates(List<AbilityTargetCandidate> result, BattleSide side)
        {
            var slots = _heroService.GetSlots(side);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                result.Add(new AbilityTargetCandidate(UnitDescriptor.Hero(side, i, slot.ActionType),
                    slot.CurrentHP, slot.MaxHP, slot is { IsAssigned: true, IsAlive: true }, true));
            }
        }
    }
}