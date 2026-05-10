using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
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


        private readonly IUnitAbilityActivationService _unitAbilityActivationService;
        private readonly IUnitStateService _unitStateService;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IAbilityRepeatModifierService _abilityRepeatModifierService;
        private readonly IAbilityAdditionalTargetModifierService _abilityAdditionalTargetModifierService;
        private readonly IAbilityEffectApplicationService _abilityEffectApplicationService;
        private readonly EventBus _eventBus;
        private readonly IBattleClock _battleClock;
        private readonly BattleSetup _battleSetup;


        public AbilityExecutionService(
            IUnitAbilityActivationService unitAbilityActivationService,
            IUnitStateService unitStateService,
            IAvatarGroupDefenseService groupDefense,
            IGameStateService gameStateService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IAbilityRepeatModifierService abilityRepeatModifierService,
            IAbilityAdditionalTargetModifierService abilityAdditionalTargetModifierService,
            IAbilityEffectApplicationService abilityEffectApplicationService,
            EventBus eventBus,
            IBattleClock battleClock,
            BattleSetup battleSetup)
        {
            _unitAbilityActivationService = unitAbilityActivationService;
            _unitStateService = unitStateService;
            _groupDefense = groupDefense;
            _gameStateService = gameStateService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _abilityRepeatModifierService = abilityRepeatModifierService;
            _abilityAdditionalTargetModifierService = abilityAdditionalTargetModifierService;
            _abilityEffectApplicationService = abilityEffectApplicationService;
            _eventBus = eventBus;
            _battleClock = battleClock;
            _battleSetup = battleSetup;
        }

        public void Execute(UnitDescriptor source, UnitDescriptor target)
        {
            if (TryExecute(source, target, out var result))
                PublishAbilityExecutionResultEvents(result);
        }

        public bool TryExecute(UnitDescriptor source, UnitDescriptor target, out AbilityExecutionResult result)
        {
            result = default;
            if (false == _gameStateService.IsPlaying)
                return false;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return false;

            if (false == _unitAbilityActivationService.TryPreview(source, out var sourceState))
                return false;

            if (sourceState.ActionValue <= 0)
                return false;

            var previewEntries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_battleSetup, source,
                sourceState.ActionType, sourceState.ActionValue);

            if (false == TryGetTargetState(target, out var isTargetAlive, out var isTargetHpFull, out var isTargetExposed))
                return false;

            if (false == AbilityTargetRules.IsTargetValid(sourceState.ActionType, sourceState.IsAlive, isTargetAlive,
                    isTargetHpFull, isTargetExposed))
                return false;

            if (false == AbilityTargetRules.IsTargetAllowedByDirectEntries(source, target, previewEntries,
                    CollectUnitTargetCandidates()))
                return false;

            var additionalTargets = SelectAdditionalTargets(source, target, sourceState.ActionType, previewEntries);

            if (false == _unitAbilityActivationService.TryCommit(source, out var committedState))
                return false;

            if (committedState.ActionType != sourceState.ActionType || committedState.ActionValue != sourceState.ActionValue)
                return false;

            var occurredAtTick = _battleClock.CurrentTick;
            var directApplications = new List<AbilityExecutionApplicationResult>();
            var abilityStatsChanges = new List<AbilityStatsChangeResult>();
            var buffsChanged = false;
            ApplyPrimaryTarget(source, target, committedState.ActionType, committedState.ActionValue,
                GetRepeatCount(source), occurredAtTick, directApplications, abilityStatsChanges, ref buffsChanged);
            ApplyAdditionalTargets(source, additionalTargets, committedState.ActionType, committedState.ActionValue,
                occurredAtTick, directApplications, abilityStatsChanges, ref buffsChanged);
            result = new AbilityExecutionResult(true, source, target, committedState.ActionType,
                committedState.ActionValue, occurredAtTick, buffsChanged, directApplications, abilityStatsChanges);
            
            return true;
        }

        private void ApplyPrimaryTarget(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType,
            int actionValue, int repeatCount, long occurredAtTick,
            List<AbilityExecutionApplicationResult> directApplications,
            List<AbilityStatsChangeResult> abilityStatsChanges, ref bool buffsChanged)
        {
            var entries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_battleSetup, source, actionType,
                actionValue);
            var totalApplications = 1 + (repeatCount < 0 ? 0 : repeatCount);
            for (var applicationIndex = 0; applicationIndex < totalApplications; applicationIndex++)
            {
                var result = _abilityEffectApplicationService.Apply(source, target, entries, GetSourceSlotKind(source),
                    0, BattlePhaseKind.Hero, occurredAtTick, applicationIndex, applicationIndex > 0);
                AppendApplicationResult(result, applicationIndex * RepeatApplicationDelaySeconds, directApplications,
                    abilityStatsChanges, ref buffsChanged);
                if (false == result.WasChanged)
                    break;
            }
        }

        private void ApplyAdditionalTargets(UnitDescriptor source, IReadOnlyList<UnitDescriptor> targets,
            HeroActionType actionType, int actionValue, long occurredAtTick,
            List<AbilityExecutionApplicationResult> directApplications,
            List<AbilityStatsChangeResult> abilityStatsChanges, ref bool buffsChanged)
        {
            if (targets == null || targets.Count == 0)
                return;

            var entries = AbilityRuntimeDefinitionResolver.CreateCommittedEntries(_battleSetup, source, actionType,
                actionValue);
            for (var i = 0; i < targets.Count; i++)
            {
                var result = _abilityEffectApplicationService.Apply(source, targets[i], entries, GetSourceSlotKind(source),
                    0, BattlePhaseKind.Hero, occurredAtTick, i + 1, true);
                AppendApplicationResult(result, 0f, directApplications, abilityStatsChanges, ref buffsChanged);
            }
        }

        private static void AppendApplicationResult(AbilityEffectApplicationResult result, float presentationDelaySeconds,
            List<AbilityExecutionApplicationResult> directApplications,
            List<AbilityStatsChangeResult> abilityStatsChanges, ref bool buffsChanged)
        {
            if (result.BuffApplicationCount > 0)
                buffsChanged = true;

            var statsChanges = result.AbilityStatsChanges;
            for (var i = 0; i < statsChanges.Count; i++)
                abilityStatsChanges.Add(statsChanges[i]);

            var applications = result.DirectApplications;
            for (var i = 0; i < applications.Count; i++)
                directApplications.Add(new AbilityExecutionApplicationResult(applications[i], presentationDelaySeconds));
        }

        private void PublishAbilityExecutionResultEvents(AbilityExecutionResult result)
        {
            if (result.BuffsChanged)
                _eventBus.Publish(new BuffsChangedEvent());

            PublishAbilityStatsChangedEvents(result);

            var directApplications = result.DirectApplications;
            for (var i = 0; i < directApplications.Count; i++)
            {
                var application = directApplications[i].Application;
                _eventBus.Publish(new AbilityApplicationEvent(application.Source, application.Target,
                    application.ActionType, application.Value, application.ApplicationIndex, application.IsRepeat,
                    directApplications[i].PresentationDelaySeconds, application.OccurredAtTick));
            }

            _eventBus.Publish(new AbilityExecutedEvent(result.Source, result.PrimaryTarget, result.ActionType,
                result.ActionValue, result.OccurredAtTick));
        }

        private void PublishAbilityStatsChangedEvents(AbilityExecutionResult result)
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

        private bool TryGetTargetState(UnitDescriptor target, out bool isAlive, out bool isHpFull, out bool isExposed)
        {
            isAlive = false;
            isHpFull = false;
            isExposed = true;

            if (false == _unitStateService.TryGetUnit(target, out var state))
                return false;

            if (false == state.IsAssigned)
                return false;

            isAlive = state.IsAlive;
            isHpFull = state.IsHpFull;
            isExposed = target.Kind != UnitKind.Avatar || _groupDefense.IsExposed(target.Side);
            
            return true;
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
            return _unitStateService.TryGetUnit(source, out var state)
                ? state.SlotKind
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
            var result = new List<AbilityTargetCandidate>(10);
            AddAvatarTargetCandidate(result, BattleSide.Player);
            AddAvatarTargetCandidate(result, BattleSide.Enemy);
            AddHeroTargetCandidates(result, BattleSide.Player);
            AddHeroTargetCandidates(result, BattleSide.Enemy);

            return result;
        }

        private void AddAvatarTargetCandidate(List<AbilityTargetCandidate> result, BattleSide side)
        {
            if (false == _unitStateService.TryGetUnit(UnitDescriptor.Avatar(side, HeroActionType.DealDamage),
                    out var state))
                return;

            result.Add(new AbilityTargetCandidate(state.Unit, state.CurrentHP, state.MaxHP,
                state.IsAssigned && state.IsAlive, _groupDefense.IsExposed(side)));
        }

        private void AddHeroTargetCandidates(List<AbilityTargetCandidate> result, BattleSide side)
        {
            for (var i = 0; i < 4; i++)
            {
                if (false == _unitStateService.TryGetUnit(UnitDescriptor.Hero(side, i, HeroActionType.DealDamage),
                        out var state))
                    continue;

                result.Add(new AbilityTargetCandidate(state.Unit, state.CurrentHP, state.MaxHP,
                    state.IsAssigned && state.IsAlive, true));
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
