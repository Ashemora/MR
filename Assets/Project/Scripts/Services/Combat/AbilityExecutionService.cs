using System;
using System.Collections.Generic;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Combat
{
    public class AbilityExecutionService : IAbilityExecutionService
    {
        private const float RepeatApplicationDelaySeconds = 0.2f;


        private readonly IPlayerAvatarChargeService _playerAvatarCharge;
        private readonly IHeroService _heroService;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IHeroAbilityModifierService _heroAbilityModifierService;
        private readonly INextAttackBuffService _nextAttackBuffService;
        private readonly IAbilityRepeatModifierService _abilityRepeatModifierService;
        private readonly IAbilityAdditionalTargetModifierService _abilityAdditionalTargetModifierService;
        private readonly IAbilityEffectApplicationService _abilityEffectApplicationService;
        private readonly EventBus _eventBus;
        private readonly IBattleClock _battleClock;
        private readonly LevelConfig _levelConfig;


        public AbilityExecutionService(
            IPlayerAvatarChargeService playerAvatarCharge,
            IHeroService heroService,
            IPlayerStateService playerState,
            IEnemyStateService enemyState,
            IAvatarGroupDefenseService groupDefense,
            IGameStateService gameStateService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IHeroAbilityModifierService heroAbilityModifierService,
            INextAttackBuffService nextAttackBuffService,
            IAbilityRepeatModifierService abilityRepeatModifierService,
            IAbilityAdditionalTargetModifierService abilityAdditionalTargetModifierService,
            IAbilityEffectApplicationService abilityEffectApplicationService,
            EventBus eventBus,
            IBattleClock battleClock,
            LevelConfig levelConfig)
        {
            _playerAvatarCharge = playerAvatarCharge;
            _heroService = heroService;
            _playerState = playerState;
            _enemyState = enemyState;
            _groupDefense = groupDefense;
            _gameStateService = gameStateService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _heroAbilityModifierService = heroAbilityModifierService;
            _nextAttackBuffService = nextAttackBuffService;
            _abilityRepeatModifierService = abilityRepeatModifierService;
            _abilityAdditionalTargetModifierService = abilityAdditionalTargetModifierService;
            _abilityEffectApplicationService = abilityEffectApplicationService;
            _eventBus = eventBus;
            _battleClock = battleClock;
            _levelConfig = levelConfig;
        }

        public void Execute(UnitDescriptor source, UnitDescriptor target)
        {
            if (false == _gameStateService.IsPlaying)
                return;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return;

            if (source.Side != BattleSide.Player)
                return;

            if (false == TryGetSourceState(source, out var sourceActionType, out var sourceActionValue, out var isSourceAlive))
                return;

            if (sourceActionValue <= 0)
                return;

            var previewEntries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_levelConfig, source,
                sourceActionType, sourceActionValue);

            if (false == TryGetTargetState(target, out var isTargetAlive, out var isTargetHpFull, out var isTargetExposed))
                return;

            if (false == AbilityTargetRules.IsTargetValid(sourceActionType, isSourceAlive, isTargetAlive,
                    isTargetHpFull, isTargetExposed))
                return;

            if (false == AbilityTargetRules.IsTargetAllowedByDirectEntries(source, target, previewEntries,
                    CollectUnitTargetCandidates()))
                return;

            var additionalTargets = SelectAdditionalTargets(source, target, sourceActionType, previewEntries);

            if (false == TryCommitSource(source, out var actionType, out var actionValue))
                return;

            if (actionType != sourceActionType || actionValue != sourceActionValue)
                return;

            ApplyPrimaryTarget(source, target, actionType, actionValue, GetRepeatCount(source));
            ApplyAdditionalTargets(source, additionalTargets, actionType, actionValue);
            _eventBus.Publish(new AbilityExecutedEvent(source, target, actionType, actionValue,
                _battleClock.CurrentTick));
        }

        private void ApplyPrimaryTarget(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType,
            int actionValue, int repeatCount)
        {
            var entries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_levelConfig, source, actionType,
                actionValue);
            var totalApplications = 1 + (repeatCount < 0 ? 0 : repeatCount);
            for (var applicationIndex = 0; applicationIndex < totalApplications; applicationIndex++)
            {
                var result = _abilityEffectApplicationService.Apply(source, target, entries, GetSourceSlotKind(source),
                    0, BattlePhaseKind.Hero, _battleClock.CurrentTick, applicationIndex, applicationIndex > 0,
                    applicationIndex * RepeatApplicationDelaySeconds);
                if (false == result.WasChanged)
                    break;
            }
        }

        private void ApplyAdditionalTargets(UnitDescriptor source, IReadOnlyList<UnitDescriptor> targets,
            HeroActionType actionType, int actionValue)
        {
            if (targets == null || targets.Count == 0)
                return;

            var entries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_levelConfig, source, actionType,
                actionValue);
            for (var i = 0; i < targets.Count; i++)
                _abilityEffectApplicationService.Apply(source, targets[i], entries, GetSourceSlotKind(source),
                    0, BattlePhaseKind.Hero, _battleClock.CurrentTick, i + 1, true, 0f);
        }

        private bool TryGetSourceState(UnitDescriptor source, out HeroActionType actionType, out int actionValue, out bool isAlive)
        {
            actionType = default;
            actionValue = 0;
            isAlive = false;

            if (source.Kind == UnitKind.Avatar)
            {
                if (source.Side != BattleSide.Player)
                    return false;

                isAlive = _playerState.CurrentHP > 0;
                if (false == isAlive || false == _playerAvatarCharge.IsReady)
                    return false;

                actionType = _playerAvatarCharge.AbilityType;
                actionValue = GetActionValueWithNextAttackBuffPreview(source, actionType, _playerAvatarCharge.AbilityPower);
                return true;
            }

            var slots = _heroService.GetSlots(source.Side);
            if (source.SlotIndex < 0 || source.SlotIndex >= slots.Count)
                return false;

            var slot = slots[source.SlotIndex];
            isAlive = slot.IsAlive;
            if (false == _heroService.CanActivate(source.Side, source.SlotIndex) || false == isAlive)
                return false;

            actionType = slot.ActionType;
            actionValue = GetActionValueWithNextAttackBuffPreview(source, actionType,
                _heroAbilityModifierService.GetAbilityPower(source.Side, source.SlotIndex, slot.ActionValue));
            
            return true;
        }

        private bool TryGetTargetState(UnitDescriptor target, out bool isAlive, out bool isHpFull, out bool isExposed)
        {
            isAlive = false;
            isHpFull = false;
            isExposed = true;

            if (target.Kind == UnitKind.Avatar)
            {
                if (target.Side == BattleSide.Player)
                {
                    isAlive = _playerState.CurrentHP > 0;
                    isHpFull = _playerState.CurrentHP >= _playerState.MaxHP;
                }
                else
                {
                    isAlive = _enemyState.CurrentHP > 0;
                    isHpFull = _enemyState.CurrentHP >= _enemyState.MaxHP;
                }

                isExposed = _groupDefense.IsExposed(target.Side);
                return true;
            }

            var slots = _heroService.GetSlots(target.Side);
            if (target.SlotIndex < 0 || target.SlotIndex >= slots.Count)
                return false;

            var slot = slots[target.SlotIndex];
            if (false == slot.IsAssigned)
                return false;

            isAlive = slot.IsAlive;
            isHpFull = slot.CurrentHP >= slot.MaxHP;
            
            return true;
        }

        private bool TryCommitSource(UnitDescriptor source, out HeroActionType actionType, out int actionValue)
        {
            actionType = default;
            actionValue = 0;

            if (source.Kind == UnitKind.Avatar)
            {
                if (false == _playerAvatarCharge.TryRelease())
                    return false;

                actionType = _playerAvatarCharge.AbilityType;
                actionValue = GetActionValueWithNextAttackBuff(source, actionType, _playerAvatarCharge.AbilityPower);
                
                return true;
            }

            return _heroService.TryDischargeHero(source.Side, source.SlotIndex, out actionType, out actionValue);
        }

        private int GetActionValueWithNextAttackBuffPreview(UnitDescriptor source, HeroActionType actionType, int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            return baseActionValue + _nextAttackBuffService.Get(source);
        }

        private int GetActionValueWithNextAttackBuff(UnitDescriptor source, HeroActionType actionType, int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

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

        private TileKind GetSourceSlotKind(UnitDescriptor source)
        {
            if (source.Kind != UnitKind.Hero)
                return TileKind.None;

            var slots = _heroService.GetSlots(source.Side);
            return source.SlotIndex >= 0 && source.SlotIndex < slots.Count
                ? slots[source.SlotIndex].SlotKind
                : TileKind.None;
        }

        private List<UnitDescriptor> SelectAdditionalTargets(UnitDescriptor source, UnitDescriptor primaryTarget,
            HeroActionType actionType, IReadOnlyList<AbilityEffectEntryDefinition> entries)
        {
            return AbilityAdditionalTargetRules.SelectTargets(source, primaryTarget, actionType,
                GetAdditionalTargetCount(source), CollectTargetCandidates(), entries);
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

        private List<UnitTargetCandidate> CollectUnitTargetCandidates()
        {
            var targetCandidates = CollectTargetCandidates();
            var result = new List<UnitTargetCandidate>(targetCandidates.Count);
            for (var i = 0; i < targetCandidates.Count; i++)
            {
                var candidate = targetCandidates[i];
                result.Add(new UnitTargetCandidate(candidate.Descriptor, candidate.CurrentHP, candidate.MaxHP,
                    candidate.IsAlive));
            }

            return result;
        }
    }
}