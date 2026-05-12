using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public class UnitAbilityActivationService : IUnitAbilityActivationService
    {
        private const int SlotCount = 4;


        private readonly IUnitStateService _unitStateService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IUnitActivationCooldownService _unitActivationCooldownService;
        private readonly IStunStatusService _stunStatusService;
        private readonly IHeroAbilityModifierService _heroAbilityModifierService;
        private readonly IAbilityPowerModifierService _abilityPowerModifierService;
        private readonly INextActivationBuffService _nextActivationBuffService;
        private readonly BattleSetup _battleSetup;
        private readonly EventBus _eventBus;


        public UnitAbilityActivationService(
            IUnitStateService unitStateService,
            IBattleSideEnergyService battleSideEnergyService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IUnitActivationCooldownService unitActivationCooldownService,
            IStunStatusService stunStatusService,
            IHeroAbilityModifierService heroAbilityModifierService,
            IAbilityPowerModifierService abilityPowerModifierService,
            INextActivationBuffService nextActivationBuffService,
            BattleSetup battleSetup,
            EventBus eventBus)
        {
            _unitStateService = unitStateService;
            _battleSideEnergyService = battleSideEnergyService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _unitActivationCooldownService = unitActivationCooldownService;
            _stunStatusService = stunStatusService;
            _heroAbilityModifierService = heroAbilityModifierService;
            _abilityPowerModifierService = abilityPowerModifierService;
            _nextActivationBuffService = nextActivationBuffService;
            _battleSetup = battleSetup;
            _eventBus = eventBus;
        }


        public bool TryPreview(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (source.Kind == UnitKind.Avatar)
                return TryPreviewAvatar(source, out state);

            return TryPreviewHero(source, out state);
        }

        public bool TryCommit(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (source.Kind == UnitKind.Avatar)
                return TryCommitAvatar(source, out state);

            return TryCommitHero(source, out state);
        }

        private bool TryPreviewAvatar(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _unitStateService.TryGetUnit(source, out var unitState))
                return false;

            if (false == CanActivateAvatar(unitState))
                return false;

            state = new UnitAbilityActivationState(unitState.ActionType, unitState.IsAlive);

            return true;
        }

        private bool TryCommitAvatar(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AvatarActivation).IsAllowed)
                return false;

            if (false == _unitStateService.TryGetUnit(source, out var unitState) || false == CanActivateAvatar(unitState))
                return false;

            var activationEnergyCost = GetActivationEnergyCost(unitState);
            if (false == _battleSideEnergyService.TrySpend(unitState.Unit.Side, activationEnergyCost))
                return false;

            _unitActivationCooldownService.StartCooldown(unitState.Unit);
            ConsumeNextActivationBuffs(unitState);
            state = new UnitAbilityActivationState(unitState.ActionType, unitState.IsAlive);

            return true;
        }

        private bool TryPreviewHero(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _unitStateService.TryGetUnit(source, out var unitState))
                return false;

            if (false == CanActivateHero(unitState))
                return false;

            state = new UnitAbilityActivationState(unitState.ActionType, unitState.IsAlive);

            return true;
        }

        private bool TryCommitHero(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return false;

            if (false == _unitStateService.TryGetUnit(source, out var unitState) || false == CanActivateHero(unitState))
                return false;

            var activationEnergyCost = GetActivationEnergyCost(unitState);
            if (false == _battleSideEnergyService.TrySpend(unitState.Unit.Side, activationEnergyCost))
                return false;

            _unitActivationCooldownService.StartCooldown(unitState.Unit);
            ConsumeNextActivationBuffs(unitState);
            state = new UnitAbilityActivationState(unitState.ActionType, unitState.IsAlive);

            return true;
        }

        private bool CanActivateAvatar(UnitRuntimeState state)
        {
            if (false == state.IsAssigned || false == state.IsAlive)
                return false;

            if (_stunStatusService.IsStunned(state.Unit))
                return false;

            if (_unitActivationCooldownService.IsOnCooldown(state.Unit))
                return false;

            if (false == _battleSideEnergyService.CanSpend(state.Unit.Side, GetActivationEnergyCost(state)))
                return false;

            return HasAnyValidEffect(state);
        }

        private bool CanActivateHero(UnitRuntimeState state)
        {
            if (false == state.IsAssigned || false == state.IsAlive)
                return false;

            if (_stunStatusService.IsStunned(state.Unit))
                return false;

            if (_unitActivationCooldownService.IsOnCooldown(state.Unit))
                return false;

            if (false == _battleSideEnergyService.CanSpend(state.Unit.Side, GetActivationEnergyCost(state)))
                return false;

            return HasAnyValidEffect(state);
        }

        private bool HasAnyValidEffect(UnitRuntimeState state)
        {
            if (false == _battleSetup.TryGetUnit(state.Unit, out var setup))
                return false;

            return AbilityActivationRules.WouldProduceAnyEffect(state.Unit, setup.ActiveAbility.DirectAction,
                setup.ActiveAbility.BuffEntries, CollectCandidates());
        }

        private List<UnitTargetCandidate> CollectCandidates()
        {
            var result = new List<UnitTargetCandidate>(10);
            AddAvatarCandidate(result, BattleSide.Player);
            AddAvatarCandidate(result, BattleSide.Enemy);
            AddHeroCandidates(result, BattleSide.Player);
            AddHeroCandidates(result, BattleSide.Enemy);

            return result;
        }

        private void AddAvatarCandidate(List<UnitTargetCandidate> result, BattleSide side)
        {
            if (false == _unitStateService.TryGetUnit(UnitDescriptor.Avatar(side),
                    out var state))
                return;

            result.Add(new UnitTargetCandidate(state.Unit, state.ActionType, state.CurrentHP, state.MaxHP,
                state.IsAssigned && state.IsAlive, state.IsAssigned));
        }

        private void AddHeroCandidates(List<UnitTargetCandidate> result, BattleSide side)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (false == _unitStateService.TryGetUnit(UnitDescriptor.Hero(side, i),
                        out var state))
                    continue;

                result.Add(new UnitTargetCandidate(state.Unit, state.ActionType, state.CurrentHP, state.MaxHP,
                    state.IsAssigned && state.IsAlive, state.IsAssigned));
            }
        }

        private int GetActivationEnergyCost(UnitRuntimeState state)
        {
            return _heroAbilityModifierService.GetActivationEnergyCost(state.Unit, state.BaseActivationEnergyCost);
        }

        private int GetHeroAbilityPower(UnitRuntimeState state)
        {
            return _abilityPowerModifierService.GetAbilityPower(state.Unit, state.BaseAbilityPower);
        }

        private void ConsumeNextActivationBuffs(UnitRuntimeState state)
        {
            if (false == _nextActivationBuffService.Consume(state.Unit))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAbilityStatsChanged(state);
        }

        private void PublishAbilityStatsChanged(UnitRuntimeState state)
        {
            var activationEnergyCost = GetActivationEnergyCost(state);
            var abilityPower = GetHeroAbilityPower(state);
            if (state.Unit.Kind == UnitKind.Hero)
            {
                _eventBus.Publish(new HeroAbilityStatsChangedEvent(state.Unit.Side, state.Unit.SlotIndex,
                    activationEnergyCost, abilityPower));
                return;
            }

            _eventBus.Publish(new AvatarAbilityPowerChangedEvent(state.Unit.Side, activationEnergyCost, abilityPower));
        }
    }
}