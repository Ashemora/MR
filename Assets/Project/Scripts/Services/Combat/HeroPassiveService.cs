using System;
using System.Collections.Generic;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleFlow;
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
        private readonly LevelConfig _levelConfig;
        private readonly SlotLayoutConfig _slotLayoutConfig;
        private readonly IBuffService _buffService;
        private readonly IHeroService _heroService;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly PassiveAbilityEngine _engine = new();
        private IDisposable _heroDefeatedSubscription;
        private IDisposable _heroActivatedSubscription;
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


        public HeroPassiveService(EventBus eventBus, LevelConfig levelConfig, SlotLayoutConfig slotLayoutConfig,
            IBuffService buffService, IHeroService heroService, IPlayerStateService playerState, IEnemyStateService enemyState)
        {
            _eventBus = eventBus;
            _levelConfig = levelConfig;
            _slotLayoutConfig = slotLayoutConfig;
            _buffService = buffService;
            _heroService = heroService;
            _playerState = playerState;
            _enemyState = enemyState;
        }


        public void Start()
        {
            InitializePassives();
            _heroDefeatedSubscription = _eventBus.Subscribe<HeroDefeatedEvent>(OnHeroDefeated);
            _heroActivatedSubscription = _eventBus.Subscribe<HeroActivatedEvent>(OnHeroActivated);
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
            _heroActivatedSubscription?.Dispose();
            _heroActivatedSubscription = null;
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
            AddSidePassives(setups, BattleSide.Player, _levelConfig.PlayerHeroes);
            AddSidePassives(setups, BattleSide.Enemy, _levelConfig.EnemyHeroes);
            _engine.Initialize(setups);
            _pendingActivationCounts = new int[_engine.States.Count];
        }

        private void AddSidePassives(List<HeroPassiveSetup> setups, BattleSide side, HeroConfig[] heroes)
        {
            if (null == heroes)
                return;

            for (var slotIndex = 0; slotIndex < SlotCount && slotIndex < heroes.Length; slotIndex++)
            {
                var hero = heroes[slotIndex];
                if (!hero)
                    continue;

                var passiveConfigs = hero.PassiveAbilities;
                if (null == passiveConfigs || passiveConfigs.Length == 0)
                    continue;

                var slotKind = GetSlotKind(slotIndex);
                for (var passiveIndex = 0; passiveIndex < passiveConfigs.Length; passiveIndex++)
                {
                    var passiveConfig = passiveConfigs[passiveIndex];
                    if (!passiveConfig)
                        continue;

                    var definition = passiveConfig.ToDefinition();
                    if (false == definition.IsConfigured)
                        continue;

                    setups.Add(new HeroPassiveSetup(side, slotIndex, slotKind, definition));
                }
            }
        }

        private TileKind GetSlotKind(int slotIndex)
        {
            var slotKinds = _slotLayoutConfig.HeroSlotKinds;
            
            return null != slotKinds && slotIndex >= 0 && slotIndex < slotKinds.Length
                ? slotKinds[slotIndex] : TileKind.None;
        }

        private void OnHeroDefeated(HeroDefeatedEvent e)
        {
            var owner = UnitDescriptor.Hero(e.Side, e.SlotIndex, GetSourceActionType(e.Side, e.SlotIndex));
            var passiveDisabled = _engine.DisableOwner(e.Side, e.SlotIndex);
            var buffsChanged = _buffService.RemoveByUnit(owner);

            if (passiveDisabled)
            {
                ClearPendingActivations(e.Side, e.SlotIndex);
                _eventBus.Publish(new HeroPassiveDisabledEvent(e.Side, e.SlotIndex));
            }

            if (buffsChanged)
            {
                _eventBus.Publish(new BuffsChangedEvent());
                PublishAllAbilityStatsChanged();
                RefreshAllSlotKindPassiveStates();
                return;
            }

            if (passiveDisabled)
                RefreshSlotKindPassiveState(e.Side, e.SlotIndex);
        }

        private void RemoveBuffsForUnit(UnitDescriptor unit)
        {
            if (false == _buffService.RemoveByUnit(unit))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            RefreshAllSlotKindPassiveStates();
        }

        private void OnHeroActivated(HeroActivatedEvent e)
        {
            if (_currentPhase != BattlePhaseKind.Hero)
                return;

            AddProgressAndPublishActivations(CreateAbilityActivatedEvent(e.Side, e.SlotIndex));
        }

        private void OnAbilityExecuted(AbilityExecutedEvent e)
        {
            if (_currentPhase != BattlePhaseKind.Hero || e.Source.Kind != UnitKind.Hero)
                return;

            AddProgressAndPublishActivations(CreateAbilityActivatedEvent(e.Source.Side, e.Source.SlotIndex));
        }

        private void OnBattleSideEnergyAdded(BattleSideEnergyAddedEvent e)
        {
            if (_currentPhase is not (BattlePhaseKind.Match or BattlePhaseKind.PendingHero))
                return;

            AddProgressAndQueueActivations(new ActivationConditionEvent(
                ActivationConditionKind.MatchEnergyCollected,
                e.Side,
                e.Amount));
        }

        private void OnBattleSideMatchesCollected(BattleSideMatchesCollectedEvent e)
        {
            if (_currentPhase is not (BattlePhaseKind.Match or BattlePhaseKind.PendingHero))
                return;

            AddProgressAndQueueActivations(new ActivationConditionEvent(
                ActivationConditionKind.MatchesCollected,
                e.Side,
                e.Count));
            AddProgressAndQueueActivations(new ActivationConditionEvent(
                ActivationConditionKind.SlotKindMatchesCollected,
                e.Side,
                e.Count,
                e.TileKind));
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

            AddProgressAndQueueActivations(new ActivationConditionEvent(
                conditionKind,
                e.Side,
                e.Count));
        }

        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            _currentPhase = e.Phase;

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
                _engine.ResetActivationConditionProgress(ActivationConditionKind.AbilityActivated, BattleSide.Player);
                _engine.ResetActivationConditionProgress(ActivationConditionKind.AbilityActivated, BattleSide.Enemy);
                PublishPendingActivations();
            }
        }

        private void OnBattleFlowRoundChanged(BattleFlowRoundChangedEvent e)
        {
            _currentRound = e.CurrentRound;
            if (false == _buffService.ExpireRoundLimitedBuffs(_currentRound))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            RefreshAllSlotKindPassiveStates();
        }

        private ActivationConditionEvent CreateAbilityActivatedEvent(BattleSide side, int slotIndex)
        {
            return new ActivationConditionEvent(ActivationConditionKind.AbilityActivated,
                UnitDescriptor.Hero(side, slotIndex, GetSourceActionType(side, slotIndex)));
        }

        private void AddProgressAndPublishActivations(ActivationConditionEvent e)
        {
            var activationCounts = CaptureActivationCounts();
            var sourceHasActiveBuff = _buffService.HasBuffFromSource(e.Source);
            if (false == _engine.ProcessActivationConditionEvent(e, sourceHasActiveBuff))
                return;

            PublishNewActivations(activationCounts);
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
            var entries = state.Definition.EffectEntries;
            var buffsChanged = false;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, CollectCandidates());
                for (var j = 0; j < targets.Count; j++)
                {
                    if (_buffService.AddBuff(source, targets[j], state.SlotKind, entry.Buff, _currentRound))
                    {
                        buffsChanged = true;
                        PublishAbilityStatsChanged(targets[j]);
                    }
                }
            }

            if (buffsChanged)
                _eventBus.Publish(new BuffsChangedEvent());
        }

        private void PublishHeroAbilityStatsChanged(BattleSide side, int slotIndex)
        {
            var heroConfig = GetHeroConfig(side, slotIndex);
            if (!heroConfig)
                return;

            _eventBus.Publish(new HeroAbilityStatsChangedEvent(side, slotIndex,
                GetActivationEnergyCost(side, slotIndex, heroConfig.ActivationEnergyCost),
                GetAbilityPower(side, slotIndex, heroConfig.AbilityPower)));
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

        private void PublishAbilityStatsChanged(UnitDescriptor target)
        {
            if (target.Kind == UnitKind.Hero)
            {
                PublishHeroAbilityStatsChanged(target.Side, target.SlotIndex);
                return;
            }

            PublishAvatarAbilityPowerChanged(target.Side);
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
            var config = side == BattleSide.Player
                ? _levelConfig.PlayerAvatarConfig
                : _levelConfig.EnemyAvatarConfig;
            if (!config)
                return;

            var target = UnitDescriptor.Avatar(side, config.AbilityType);
            _eventBus.Publish(new AvatarAbilityPowerChangedEvent(side, GetAbilityPower(target, config.AbilityPower)));
        }

        private void RefreshAllSlotKindPassiveStates()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                RefreshSlotKindPassiveState(BattleSide.Player, i);
                RefreshSlotKindPassiveState(BattleSide.Enemy, i);
            }
        }

        private HeroConfig GetHeroConfig(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return null;

            var heroes = side == BattleSide.Player
                ? _levelConfig.PlayerHeroes
                : _levelConfig.EnemyHeroes;

            return null != heroes && slotIndex < heroes.Length ? heroes[slotIndex] : null;
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

        private List<UnitTargetCandidate> CollectCandidates()
        {
            var playerAvatarActionType = _levelConfig.PlayerAvatarConfig.AbilityType;
            var enemyAvatarActionType = _levelConfig.EnemyAvatarConfig.AbilityType;
            var result = new List<UnitTargetCandidate>(10)
            {
                new(UnitDescriptor.Avatar(BattleSide.Player, playerAvatarActionType),
                    _playerState.CurrentHP,
                    _playerState.MaxHP,
                    _playerState.CurrentHP > 0),
                new(UnitDescriptor.Avatar(BattleSide.Enemy, enemyAvatarActionType),
                    _enemyState.CurrentHP,
                    _enemyState.MaxHP,
                    _enemyState.CurrentHP > 0)
            };

            AddHeroCandidates(result, BattleSide.Player);
            AddHeroCandidates(result, BattleSide.Enemy);
            
            return result;
        }

        private void AddHeroCandidates(List<UnitTargetCandidate> result, BattleSide side)
        {
            var slots = _heroService.GetSlots(side);
            for (var i = 0; i < SlotCount && i < slots.Count; i++)
            {
                var slot = slots[i];

                result.Add(new UnitTargetCandidate(UnitDescriptor.Hero(side, i, slot.ActionType), 
                    slot.CurrentHP, slot.MaxHP, slot is { IsAssigned: true, IsAlive: true }));
            }
        }

        private HeroActionType GetSourceActionType(BattleSide side, int slotIndex)
        {
            var hero = GetHeroConfig(side, slotIndex);
            
            return hero ? hero.AbilityType : HeroActionType.DealDamage;
        }

        private UnitDescriptor GetAvatarUnit(BattleSide side)
        {
            var config = side == BattleSide.Player
                ? _levelConfig.PlayerAvatarConfig
                : _levelConfig.EnemyAvatarConfig;

            return UnitDescriptor.Avatar(side, config ? config.AbilityType : HeroActionType.DealDamage);
        }

        private static int GetSideIndex(BattleSide side)
        {
            return side == BattleSide.Player ? 0 : 1;
        }
    }
}