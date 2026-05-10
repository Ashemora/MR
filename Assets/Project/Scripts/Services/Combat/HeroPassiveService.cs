using System;
using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Tiles;
using VContainer.Unity;

namespace Project.Scripts.Services.Combat
{
    public class HeroPassiveService : IHeroPassiveService, IStartable, IDisposable
    {
        private const int SlotCount = 4;
        
        
        public IReadOnlyList<HeroPassiveRuntimeState> States => _engine.States;


        private readonly EventBus _eventBus;
        private readonly BattleSetup _battleSetup;
        private readonly IBuffService _buffService;
        private readonly IAbilityEffectApplicationService _abilityEffectApplicationService;
        private readonly IHeroService _heroService;
        private readonly IBattleClock _battleClock;
        private readonly PassiveAbilityEngine _engine = new();
        private IDisposable _heroDefeatedSubscription;
        private IDisposable _heroResurrectedSubscription;
        private IDisposable _abilityExecutedSubscription;
        private IDisposable _energyAddedSubscription;
        private IDisposable _matchesCollectedSubscription;
        private IDisposable _specialTileUsedSubscription;
        private IDisposable _phaseChangedSubscription;
        private IDisposable _roundChangedSubscription;
        private IDisposable _playerDefeatedSubscription;
        private IDisposable _enemyDefeatedSubscription;
        private BattlePhaseKind _currentPhase = BattlePhaseKind.Match;
        private int _currentRound = 1;
        private int[] _pendingActivationCounts = Array.Empty<int>();
        private readonly bool[,] _slotKindPassiveStates = new bool[2, SlotCount];


        public HeroPassiveService(EventBus eventBus, BattleSetup battleSetup,
            IBuffService buffService, IAbilityEffectApplicationService abilityEffectApplicationService,
            IHeroService heroService, IBattleClock battleClock)
        {
            _eventBus = eventBus;
            _battleSetup = battleSetup;
            _buffService = buffService;
            _abilityEffectApplicationService = abilityEffectApplicationService;
            _heroService = heroService;
            _battleClock = battleClock;
        }


        public void Start()
        {
            InitializePassives();
            _heroDefeatedSubscription = _eventBus.Subscribe<HeroDefeatedEvent>(OnHeroDefeated);
            _heroResurrectedSubscription = _eventBus.Subscribe<HeroResurrectedEvent>(OnHeroResurrected);
            _abilityExecutedSubscription = _eventBus.Subscribe<AbilityExecutedEvent>(OnAbilityExecuted);
            _energyAddedSubscription = _eventBus.Subscribe<BattleSideEnergyAddedEvent>(OnBattleSideEnergyAdded);
            _matchesCollectedSubscription = _eventBus.Subscribe<BattleSideMatchesCollectedEvent>(OnBattleSideMatchesCollected);
            _specialTileUsedSubscription = _eventBus.Subscribe<BattleSideSpecialTileUsedEvent>(OnBattleSideSpecialTileUsed);
            _phaseChangedSubscription = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);
            _roundChangedSubscription = _eventBus.Subscribe<BattleFlowRoundChangedEvent>(OnBattleFlowRoundChanged);
            _playerDefeatedSubscription = _eventBus.Subscribe<PlayerDefeatedEvent>(_ => RemoveBuffsForUnit(GetAvatarUnit(BattleSide.Player)));
            _enemyDefeatedSubscription = _eventBus.Subscribe<EnemyDefeatedEvent>(_ => RemoveBuffsForUnit(GetAvatarUnit(BattleSide.Enemy)));
        }

        public void Dispose()
        {
            _heroDefeatedSubscription?.Dispose();
            _heroDefeatedSubscription = null;
            _heroResurrectedSubscription?.Dispose();
            _heroResurrectedSubscription = null;
            _abilityExecutedSubscription?.Dispose();
            _abilityExecutedSubscription = null;
            _energyAddedSubscription?.Dispose();
            _energyAddedSubscription = null;
            _matchesCollectedSubscription?.Dispose();
            _matchesCollectedSubscription = null;
            _specialTileUsedSubscription?.Dispose();
            _specialTileUsedSubscription = null;
            _phaseChangedSubscription?.Dispose();
            _phaseChangedSubscription = null;
            _roundChangedSubscription?.Dispose();
            _roundChangedSubscription = null;
            _playerDefeatedSubscription?.Dispose();
            _playerDefeatedSubscription = null;
            _enemyDefeatedSubscription?.Dispose();
            _enemyDefeatedSubscription = null;
        }

        private void InitializePassives()
        {
            var setups = new List<HeroPassiveSetup>();
            AddSidePassives(setups, BattleSide.Player);
            AddSidePassives(setups, BattleSide.Enemy);
            _engine.Initialize(setups, _battleClock.TickRate);
            _pendingActivationCounts = new int[_engine.States.Count];
        }

        private void AddSidePassives(List<HeroPassiveSetup> setups, BattleSide side)
        {
            for (var slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                var hero = _battleSetup.GetHero(side, slotIndex);
                if (false == hero.IsAssigned)
                    continue;

                var passiveDefinitions = hero.PassiveAbilities;
                if (passiveDefinitions.Count == 0)
                    continue;

                for (var passiveIndex = 0; passiveIndex < passiveDefinitions.Count; passiveIndex++)
                {
                    var definition = passiveDefinitions[passiveIndex];
                    if (false == definition.IsConfigured)
                        continue;

                    setups.Add(new HeroPassiveSetup(side, slotIndex, hero.SlotKind, definition));
                }
            }
        }

        private void OnHeroDefeated(HeroDefeatedEvent e)
        {
            AddProgressAndPublishActivations(new ActivationConditionEvent(ActivationConditionKind.EnemyHeroDefeatsInTimeWindow,
                UnitDescriptor.Hero(e.Side, e.SlotIndex, GetSourceActionType(e.Side, e.SlotIndex)),
                occurredAtTick: ResolveOccurredAtTick(e.OccurredAtTick)));

            _engine.ResetOwnerProgress(e.Side, e.SlotIndex);
            var passiveDisabled = _engine.DisableOwner(e.Side, e.SlotIndex);
            if (passiveDisabled)
            {
                ClearPendingActivations(e.Side, e.SlotIndex);
                _eventBus.Publish(new HeroPassiveDisabledEvent(e.Side, e.SlotIndex));
            }

            var buffsChanged = RunOwnerBuffCleanup(e.Side, e.SlotIndex);
            if (false == buffsChanged && passiveDisabled)
                RefreshSlotKindPassiveState(e.Side, e.SlotIndex);
        }

        private void OnHeroResurrected(HeroResurrectedEvent e)
        {
            _engine.ResetOwnerProgress(e.Side, e.SlotIndex);
            ClearPendingActivations(e.Side, e.SlotIndex);
            RunOwnerBuffCleanup(e.Side, e.SlotIndex);
        }

        private bool RunOwnerBuffCleanup(BattleSide side, int slotIndex)
        {
            var owner = UnitDescriptor.Hero(side, slotIndex, GetSourceActionType(side, slotIndex));
            if (false == _buffService.RemoveByUnit(owner))
                return false;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            RefreshAllSlotKindPassiveStates();

            return true;
        }

        private void RemoveBuffsForUnit(UnitDescriptor unit)
        {
            if (false == _buffService.RemoveByUnit(unit))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            RefreshAllSlotKindPassiveStates();
        }

        private void OnAbilityExecuted(AbilityExecutedEvent e)
        {
            if (_currentPhase != BattlePhaseKind.Hero)
                return;

            if (e.Source.Kind == UnitKind.Hero)
            {
                AddHeroAbilityActivationProgressAndPublish(e.Source, e.OccurredAtTick);
                return;
            }

            if (e.Source.Kind == UnitKind.Avatar)
                AddUnitActivationProgressAndPublish(e.Source, e.OccurredAtTick);
        }

        private void OnBattleSideEnergyAdded(BattleSideEnergyAddedEvent e)
        {
            if (_currentPhase is not (BattlePhaseKind.Match or BattlePhaseKind.PendingHero))
                return;

            AddProgressAndQueueActivations(new ActivationConditionEvent(ActivationConditionKind.MatchEnergyCollected,
                e.Side, e.Amount));
        }

        private void OnBattleSideMatchesCollected(BattleSideMatchesCollectedEvent e)
        {
            if (_currentPhase is not (BattlePhaseKind.Match or BattlePhaseKind.PendingHero))
                return;

            var occurredAtTick = ResolveOccurredAtTick(e.OccurredAtTick);
            AddProgressAndQueueActivations(new ActivationConditionEvent(ActivationConditionKind.MatchesCollected,
                e.Side, e.Count));
            AddProgressAndQueueActivations(new ActivationConditionEvent(ActivationConditionKind.SlotKindMatchesCollected,
                e.Side, e.Count, e.TileKind));
            AddProgressAndQueueActivations(new ActivationConditionEvent(ActivationConditionKind.SlotKindMatchesInTimeWindow,
                e.Side, e.Count, e.TileKind, occurredAtTick));
        }

        private void OnBattleSideSpecialTileUsed(BattleSideSpecialTileUsedEvent e)
        {
            var conditionKind = e.TileKind switch
            {
                TileKind.LineRuneH or TileKind.LineRuneV => ActivationConditionKind.LineRuneUsed,
                TileKind.Bomb => ActivationConditionKind.BombUsed,
                TileKind.Storm => ActivationConditionKind.StormUsed,
                _ => ActivationConditionKind.None
            };

            if (conditionKind == ActivationConditionKind.None)
                return;

            AddProgressAndQueueActivations(new ActivationConditionEvent(conditionKind, e.Side, e.Count));
        }

        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            var previousPhase = _currentPhase;
            _currentPhase = e.Phase;
            ResetTimeWindowProgress();
            ExpireUntilEndOfNextMainPhaseBuffs(previousPhase, e.Phase);

            if (e.Phase == BattlePhaseKind.Match)
            {
                ClearPendingActivations();
                _engine.ResetActivationConditionProgress(ActivationConditionKind.MatchEnergyCollected, BattleSide.Player);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.MatchEnergyCollected, BattleSide.Enemy);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.MatchesCollected, BattleSide.Player);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.MatchesCollected, BattleSide.Enemy);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.SlotKindMatchesCollected, BattleSide.Player);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.SlotKindMatchesCollected, BattleSide.Enemy);
                
                return;
            }

            if (e.Phase == BattlePhaseKind.Hero)
            {
                PublishPendingActivations();
            }
        }

        private void ExpireUntilEndOfNextMainPhaseBuffs(BattlePhaseKind previousPhase, BattlePhaseKind nextPhase)
        {
            if (false == _buffService.ExpireUntilEndOfNextMainPhaseBuffs(previousPhase, nextPhase))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            RefreshAllSlotKindPassiveStates();
        }

        private void ResetTimeWindowProgress()
        {
            _engine.ResetActivationConditionProgress(ActivationConditionKind.UnitActivationsInTimeWindow,
                BattleSide.Player);
            _engine.ResetActivationConditionProgress(ActivationConditionKind.UnitActivationsInTimeWindow,
                BattleSide.Enemy);
            _engine.ResetActivationConditionProgress(ActivationConditionKind.SlotKindMatchesInTimeWindow,
                BattleSide.Player);
            _engine.ResetActivationConditionProgress(ActivationConditionKind.SlotKindMatchesInTimeWindow,
                BattleSide.Enemy);
            _engine.ResetActivationConditionProgress(ActivationConditionKind.EnemyHeroDefeatsInTimeWindow,
                BattleSide.Player);
            _engine.ResetActivationConditionProgress(ActivationConditionKind.EnemyHeroDefeatsInTimeWindow,
                BattleSide.Enemy);
        }

        private void OnBattleFlowRoundChanged(BattleFlowRoundChangedEvent e)
        {
            _currentRound = e.CurrentRound;
        }

        private ActivationConditionEvent CreateUnitActivationEvent(ActivationConditionKind kind,
            UnitDescriptor source, long occurredAtTick)
        {
            return new ActivationConditionEvent(kind, source, occurredAtTick: ResolveOccurredAtTick(occurredAtTick));
        }

        private void AddHeroAbilityActivationProgressAndPublish(UnitDescriptor source, long occurredAtTick)
        {
            var activationCounts = CaptureActivationCounts();
            var resolvedTick = ResolveOccurredAtTick(occurredAtTick);
            var changed = _engine.ProcessActivationConditionEvent(
                CreateUnitActivationEvent(ActivationConditionKind.UnitActivationsInTimeWindow, source, resolvedTick),
                HasActiveBuffForPassiveOwner);

            if (changed)
                PublishNewActivations(activationCounts);
        }

        private void AddUnitActivationProgressAndPublish(UnitDescriptor source, long occurredAtTick)
        {
            AddProgressAndPublishActivations(CreateUnitActivationEvent(ActivationConditionKind.UnitActivationsInTimeWindow,
                source, occurredAtTick));
        }

        private void AddProgressAndPublishActivations(ActivationConditionEvent e)
        {
            var activationCounts = CaptureActivationCounts();
            if (false == _engine.ProcessActivationConditionEvent(e, HasActiveBuffForPassiveOwner))
                return;

            PublishNewActivations(activationCounts);
        }

        private bool HasActiveBuffForPassiveOwner(HeroPassiveRuntimeState state)
        {
            return _buffService.HasBuffFromSource(UnitDescriptor.Hero(state.Side, state.SlotIndex, GetSourceActionType(state.Side, state.SlotIndex)));
        }

        private long ResolveOccurredAtTick(long occurredAtTick)
        {
            return occurredAtTick > 0 ? occurredAtTick : _battleClock.CurrentTick;
        }

        private void AddProgressAndQueueActivations(ActivationConditionEvent e)
        {
            var activationCounts = CaptureActivationCounts();
            if (false == _engine.ProcessActivationConditionEvent(e))
                return;

            QueueNewActivations(activationCounts);
        }

        private int[] CaptureActivationCounts()
        {
            var states = _engine.States;
            var result = new int[states.Count];
            for (var i = 0; i < states.Count; i++)
                result[i] = states[i].TotalActivationCount;
            
            return result;
        }

        private void PublishNewActivations(int[] previousActivationCounts)
        {
            var states = _engine.States;
            for (var i = 0; i < states.Count && i < previousActivationCounts.Length; i++)
            {
                var activationDelta = states[i].TotalActivationCount - previousActivationCounts[i];
                if (activationDelta <= 0)
                    continue;

                PublishActivationRepeated(states[i], activationDelta);
                RefreshSlotKindPassiveState(states[i].Side, states[i].SlotIndex);
            }
        }

        private void QueueNewActivations(int[] previousActivationCounts)
        {
            var states = _engine.States;
            EnsurePendingActivationCapacity(states.Count);
            for (var i = 0; i < states.Count && i < previousActivationCounts.Length; i++)
            {
                var activationDelta = states[i].TotalActivationCount - previousActivationCounts[i];
                if (activationDelta > 0)
                    _pendingActivationCounts[i] += activationDelta;
            }
        }

        private void PublishPendingActivations()
        {
            var states = _engine.States;
            EnsurePendingActivationCapacity(states.Count);
            for (var i = 0; i < states.Count; i++)
            {
                var activationCount = _pendingActivationCounts[i];
                _pendingActivationCounts[i] = 0;
                if (activationCount <= 0 || states[i].IsDisabled)
                    continue;

                PublishActivationRepeated(states[i], activationCount);
                RefreshSlotKindPassiveState(states[i].Side, states[i].SlotIndex);
            }
        }

        private void PublishActivationRepeated(HeroPassiveRuntimeState state, int activationCount)
        {
            for (var i = 0; i < activationCount; i++)
            {
                _eventBus.Publish(new HeroPassiveActivatedEvent(state));
                ApplyPassiveEffects(state);
            }
        }

        private void ClearPendingActivations()
        {
            Array.Clear(_pendingActivationCounts, 0, _pendingActivationCounts.Length);
        }

        private void ClearPendingActivations(BattleSide side, int slotIndex)
        {
            var states = _engine.States;
            EnsurePendingActivationCapacity(states.Count);
            for (var i = 0; i < states.Count; i++)
                if (states[i].Side == side && states[i].SlotIndex == slotIndex)
                    _pendingActivationCounts[i] = 0;
        }

        private void EnsurePendingActivationCapacity(int stateCount)
        {
            if (_pendingActivationCounts.Length == stateCount)
                return;

            Array.Resize(ref _pendingActivationCounts, stateCount);
        }

        private void ApplyPassiveEffects(HeroPassiveRuntimeState state)
        {
            var source = UnitDescriptor.Hero(state.Side, state.SlotIndex, GetSourceActionType(state.Side, state.SlotIndex));
            var result = _abilityEffectApplicationService.Apply(source, default, state.Definition.EffectEntries,
                state.SlotKind, _currentRound, _currentPhase, _battleClock.CurrentTick);
            PublishAbilityApplicationResultEvents(result);
        }

        private void PublishAbilityApplicationResultEvents(AbilityEffectApplicationResult result)
        {
            if (result.BuffApplicationCount > 0)
                _eventBus.Publish(new BuffsChangedEvent());

            PublishAbilityStatsChangedEvents(result);
            
            var directApplications = result.DirectApplications;
            for (var i = 0; i < directApplications.Count; i++)
            {
                var application = directApplications[i];
                _eventBus.Publish(new AbilityApplicationEvent(application.Source, application.Target,
                    application.ActionType, application.Value, application.ApplicationIndex, application.IsRepeat,
                    0f, application.OccurredAtTick));
            }
        }

        private void PublishAbilityStatsChangedEvents(AbilityEffectApplicationResult result)
        {
            var statsChanges = result.AbilityStatsChanges;
            for (var i = 0; i < statsChanges.Count; i++)
            {
                var change = statsChanges[i];
                if (change.Target.Kind == UnitKind.Hero)
                {
                    _eventBus.Publish(new HeroAbilityStatsChangedEvent(change.Target.Side, change.Target.SlotIndex,
                        change.ActivationEnergyCost, change.AbilityPower));
                    continue;
                }

                _eventBus.Publish(new AvatarAbilityPowerChangedEvent(change.Target.Side, change.AbilityPower));
            }
        }

        private void PublishHeroAbilityStatsChanged(BattleSide side, int slotIndex)
        {
            var unit = _battleSetup.GetHero(side, slotIndex);
            if (false == unit.IsAssigned)
                return;

            _eventBus.Publish(new HeroAbilityStatsChangedEvent(side, slotIndex,
                GetActivationEnergyCost(side, slotIndex, unit.BaseActivationEnergyCost),
                GetAbilityPower(side, slotIndex, unit.BaseAbilityPower)));
        }

        private int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost)
        {
            return (_buffService as IHeroAbilityModifierService)?.GetActivationEnergyCost(side, slotIndex, baseCost)
                   ?? baseCost;
        }

        private int GetAbilityPower(BattleSide side, int slotIndex, int basePower)
        {
            return (_buffService as IAbilityPowerModifierService)
                       ?.GetAbilityPower(UnitDescriptor.Hero(side, slotIndex, GetSourceActionType(side, slotIndex)), basePower)
                   ?? basePower;
        }

        private int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return (_buffService as IAbilityPowerModifierService)?.GetAbilityPower(target, basePower)
                   ?? basePower;
        }

        private void PublishAllAbilityStatsChanged()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                PublishHeroAbilityStatsChanged(BattleSide.Player, i);
                PublishHeroAbilityStatsChanged(BattleSide.Enemy, i);
            }

            PublishAvatarAbilityPowerChanged(BattleSide.Player);
            PublishAvatarAbilityPowerChanged(BattleSide.Enemy);
        }

        private void PublishAvatarAbilityPowerChanged(BattleSide side)
        {
            var unit = side == BattleSide.Player ? _battleSetup.PlayerAvatar : _battleSetup.EnemyAvatar;
            if (false == unit.IsAssigned)
                return;

            _eventBus.Publish(new AvatarAbilityPowerChangedEvent(side,
                GetAbilityPower(unit.Unit, unit.BaseAbilityPower)));
        }

        private void RefreshAllSlotKindPassiveStates()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                RefreshSlotKindPassiveState(BattleSide.Player, i);
                RefreshSlotKindPassiveState(BattleSide.Enemy, i);
            }
        }

        private void RefreshSlotKindPassiveState(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return;

            var sideIndex = GetSideIndex(side);
            var active = HasActiveSlotKindPassive(side, slotIndex);
            if (_slotKindPassiveStates[sideIndex, slotIndex] == active)
                return;

            _slotKindPassiveStates[sideIndex, slotIndex] = active;
            _eventBus.Publish(new HeroSlotKindPassiveStateChangedEvent(side, slotIndex, active));
        }

        private bool HasActiveSlotKindPassive(BattleSide side, int slotIndex)
        {
            var states = _engine.States;
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state.Side != side || state.SlotIndex != slotIndex)
                    continue;

                if (_buffService.HasMatchEnergyBuff(side, state.SlotKind))
                    return true;
            }

            return false;
        }

        private HeroActionType GetSourceActionType(BattleSide side, int slotIndex)
        {
            var hero = _battleSetup.GetHero(side, slotIndex);
            
            return hero.IsAssigned ? hero.Unit.ActionType : HeroActionType.DealDamage;
        }

        private UnitDescriptor GetAvatarUnit(BattleSide side)
        {
            var avatar = side == BattleSide.Player ? _battleSetup.PlayerAvatar : _battleSetup.EnemyAvatar;
            
            return avatar.IsAssigned ? avatar.Unit : UnitDescriptor.Avatar(side, HeroActionType.DealDamage);
        }

        private static int GetSideIndex(BattleSide side)
        {
            return side == BattleSide.Player ? 0 : 1;
        }
    }
}