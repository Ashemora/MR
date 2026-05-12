using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
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
        private readonly INextAttackBuffService _nextAttackBuffService;
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
            INextAttackBuffService nextAttackBuffService,
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
            _nextAttackBuffService = nextAttackBuffService;
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

            var directAction = GetDirectAction(source);
            var buffEntries = GetBuffEntries(source);
            if (sourceState.ActionType != UnitActionType.SupportAlly && false == directAction.IsConfigured)
                return false;

            if (false == TryGetTargetState(target, out var isTargetAlive, out var isTargetHpFull, out var isTargetExposed))
                return false;

            if (false == AbilityTargetRules.IsTargetValidForEffect(source, target, directAction, buffEntries,
                    CollectUnitTargetCandidates(), sourceState.IsAlive, isTargetAlive, isTargetHpFull, isTargetExposed))
                return false;

            var additionalTargets = SelectAdditionalTargets(source, target, sourceState.ActionType,
                directAction, buffEntries);

            if (false == _unitAbilityActivationService.TryCommit(source, out var committedState))
                return false;

            if (committedState.ActionType != sourceState.ActionType)
                return false;

            var nextAttackBonus = directAction.Kind == DirectActionKind.Damage
                ? _nextAttackBuffService.Consume(source)
                : 0;
            var occurredAtTick = _battleClock.CurrentTick;
            var directApplications = new List<AbilityExecutionApplicationResult>();
            var abilityStatsChanges = new List<AbilityStatsChangeResult>();
            var buffsChanged = false;
            ApplyPrimaryTarget(source, target, directAction, buffEntries, GetRepeatCount(source), nextAttackBonus,
                occurredAtTick, directApplications, abilityStatsChanges, ref buffsChanged);
            ApplyAdditionalTargets(source, additionalTargets, directAction, buffEntries, nextAttackBonus,
                occurredAtTick, directApplications, abilityStatsChanges, ref buffsChanged);
            result = new AbilityExecutionResult(true, source, target, committedState.ActionType,
                occurredAtTick, buffsChanged, directApplications, abilityStatsChanges);

            return true;
        }

        private void ApplyPrimaryTarget(UnitDescriptor source, UnitDescriptor target, DirectActionDefinition directAction,
            IReadOnlyList<BuffEntryDefinition> buffEntries, int repeatCount, int nextAttackBonus, long occurredAtTick,
            List<AbilityExecutionApplicationResult> directApplications,
            List<AbilityStatsChangeResult> abilityStatsChanges, ref bool buffsChanged)
        {
            var totalApplications = 1 + (repeatCount < 0 ? 0 : repeatCount);
            for (var applicationIndex = 0; applicationIndex < totalApplications; applicationIndex++)
            {
                var result = _abilityEffectApplicationService.Apply(source, target, directAction, buffEntries,
                    GetSourceSlotKind(source), 0, BattlePhaseKind.Hero, occurredAtTick, applicationIndex,
                    applicationIndex > 0, nextAttackBonus);
                AppendApplicationResult(result, applicationIndex * RepeatApplicationDelaySeconds, directApplications,
                    abilityStatsChanges, ref buffsChanged);
                if (false == result.WasChanged)
                    break;
            }
        }

        private void ApplyAdditionalTargets(UnitDescriptor source, IReadOnlyList<UnitDescriptor> targets,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries, int nextAttackBonus,
            long occurredAtTick, List<AbilityExecutionApplicationResult> directApplications,
            List<AbilityStatsChangeResult> abilityStatsChanges, ref bool buffsChanged)
        {
            if (targets == null || targets.Count == 0)
                return;

            for (var i = 0; i < targets.Count; i++)
            {
                var result = _abilityEffectApplicationService.Apply(source, targets[i], directAction, buffEntries,
                    GetSourceSlotKind(source), 0, BattlePhaseKind.Hero, occurredAtTick, i + 1, true, nextAttackBonus);
                AppendApplicationResult(result, 0f, directApplications, abilityStatsChanges, ref buffsChanged);
            }
        }

        private DirectActionDefinition GetDirectAction(UnitDescriptor source)
        {
            return _battleSetup.TryGetUnit(source, out var unitSetup)
                ? unitSetup.ActiveAbility.DirectAction
                : default;
        }

        private IReadOnlyList<BuffEntryDefinition> GetBuffEntries(UnitDescriptor source)
        {
            return _battleSetup.TryGetUnit(source, out var unitSetup)
                ? unitSetup.ActiveAbility.BuffEntries
                : System.Array.Empty<BuffEntryDefinition>();
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
                result.OccurredAtTick));
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

                _eventBus.Publish(new AvatarAbilityPowerChangedEvent(change.Target.Side,
                    change.ActivationEnergyCost, change.AbilityPower));
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
            return _abilityRepeatModifierService.GetRepeatCount(source);
        }

        private int GetAdditionalTargetCount(UnitDescriptor source)
        {
            return _abilityAdditionalTargetModifierService.GetAdditionalTargetCount(source);
        }

        private TileKind GetSourceSlotKind(UnitDescriptor source)
        {
            return _unitStateService.TryGetUnit(source, out var state)
                ? state.SlotKind
                : TileKind.None;
        }

        private List<UnitDescriptor> SelectAdditionalTargets(UnitDescriptor source, UnitDescriptor primaryTarget,
            UnitActionType actionType, DirectActionDefinition directAction,
            IReadOnlyList<BuffEntryDefinition> buffEntries)
        {
            return AbilityAdditionalTargetRules.SelectTargets(source, primaryTarget, actionType,
                GetAdditionalTargetCount(source), CollectTargetCandidates(), directAction, buffEntries);
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
            if (false == _unitStateService.TryGetUnit(UnitDescriptor.Avatar(side),
                    out var state))
                return;

            result.Add(new AbilityTargetCandidate(state.Unit, state.ActionType, state.CurrentHP, state.MaxHP,
                state.IsAssigned && state.IsAlive, _groupDefense.IsExposed(side)));
        }

        private void AddHeroTargetCandidates(List<AbilityTargetCandidate> result, BattleSide side)
        {
            for (var i = 0; i < 4; i++)
            {
                if (false == _unitStateService.TryGetUnit(UnitDescriptor.Hero(side, i),
                        out var state))
                    continue;

                result.Add(new AbilityTargetCandidate(state.Unit, state.ActionType, state.CurrentHP, state.MaxHP,
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
                result.Add(new UnitTargetCandidate(candidate.Descriptor, candidate.ActionType, candidate.CurrentHP,
                    candidate.MaxHP, candidate.IsAlive));
            }

            return result;
        }
    }
}