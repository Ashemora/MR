using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Rules;

using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Energy;

namespace Project.Scripts.Services.Combat.Units
{
    public class UnitAbilityActivationService : IUnitAbilityActivationService
    {
        private readonly IUnitStateService _unitStateService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IUnitActivationCooldownService _unitActivationCooldownService;
        private readonly IAbilityPowerModifierService _abilityPowerModifierService;
        private readonly IHeroAbilityModifierService _heroAbilityModifierService;
        private readonly INextAttackBuffService _nextAttackBuffService;
        private readonly INextActivationBuffService _nextActivationBuffService;
        private readonly EventBus _eventBus;


        public UnitAbilityActivationService(
            IUnitStateService unitStateService,
            IBattleSideEnergyService battleSideEnergyService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IUnitActivationCooldownService unitActivationCooldownService,
            IAbilityPowerModifierService abilityPowerModifierService,
            IHeroAbilityModifierService heroAbilityModifierService,
            INextAttackBuffService nextAttackBuffService,
            INextActivationBuffService nextActivationBuffService,
            EventBus eventBus)
        {
            _unitStateService = unitStateService;
            _battleSideEnergyService = battleSideEnergyService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _unitActivationCooldownService = unitActivationCooldownService;
            _abilityPowerModifierService = abilityPowerModifierService;
            _heroAbilityModifierService = heroAbilityModifierService;
            _nextAttackBuffService = nextAttackBuffService;
            _nextActivationBuffService = nextActivationBuffService;
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

            var actionValue = GetActionValueWithNextAttackBuffPreview(unitState.Unit, unitState.Unit.ActionType,
                _abilityPowerModifierService.GetAbilityPower(unitState.Unit, unitState.BaseAbilityPower));
            state = new UnitAbilityActivationState(unitState.Unit.ActionType, actionValue, unitState.IsAlive);
            
            return true;
        }

        private bool TryCommitAvatar(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AvatarActivation).IsAllowed)
                return false;

            if (false == _unitStateService.TryGetUnit(source, out var unitState) || false == CanActivateAvatar(unitState))
                return false;

            if (false == _battleSideEnergyService.TrySpend(unitState.Unit.Side, unitState.BaseActivationEnergyCost))
                return false;

            _unitActivationCooldownService.StartCooldown(unitState.Unit);
            var actionValue = GetActionValueWithNextAttackBuff(unitState.Unit, unitState.Unit.ActionType,
                _abilityPowerModifierService.GetAbilityPower(unitState.Unit, unitState.BaseAbilityPower));
            state = new UnitAbilityActivationState(unitState.Unit.ActionType, actionValue, unitState.IsAlive);
            
            return true;
        }

        private bool TryPreviewHero(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _unitStateService.TryGetUnit(source, out var unitState))
                return false;

            if (false == CanActivateHero(unitState))
                return false;

            var actionValue = GetActionValueWithNextAttackBuffPreview(unitState.Unit, unitState.Unit.ActionType,
                GetHeroAbilityPower(unitState));
            state = new UnitAbilityActivationState(unitState.Unit.ActionType, actionValue, unitState.IsAlive);
            
            return true;
        }

        private bool TryCommitHero(UnitDescriptor source, out UnitAbilityActivationState state)
        {
            state = default;
            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return false;

            if (false == _unitStateService.TryGetUnit(source, out var unitState) || false == CanActivateHero(unitState))
                return false;

            var activationEnergyCost = GetHeroActivationEnergyCost(unitState);
            if (false == _battleSideEnergyService.TrySpend(unitState.Unit.Side, activationEnergyCost))
                return false;

            _unitActivationCooldownService.StartCooldown(unitState.Unit);
            var actionValue = GetActionValueWithNextAttackBuff(unitState.Unit, unitState.Unit.ActionType,
                GetHeroAbilityPower(unitState));
            ConsumeNextActivationBuffs(unitState);
            state = new UnitAbilityActivationState(unitState.Unit.ActionType, actionValue, unitState.IsAlive);
            
            return true;
        }

        private bool CanActivateAvatar(UnitRuntimeState state)
        {
            if (false == state.IsAssigned || false == state.IsAlive)
                return false;

            if (_unitActivationCooldownService.IsOnCooldown(state.Unit))
                return false;

            return _battleSideEnergyService.CanSpend(state.Unit.Side, state.BaseActivationEnergyCost);
        }

        private bool CanActivateHero(UnitRuntimeState state)
        {
            if (false == state.IsAssigned || false == state.IsAlive)
                return false;

            if (state.Unit.ActionType == HeroActionType.HealAlly && false == HasHeroHealTarget(state))
                return false;

            if (_unitActivationCooldownService.IsOnCooldown(state.Unit))
                return false;

            return _battleSideEnergyService.CanSpend(state.Unit.Side, GetHeroActivationEnergyCost(state));
        }

        private bool HasHeroHealTarget(UnitRuntimeState source)
        {
            if (_unitStateService.TryGetUnit(UnitDescriptor.Avatar(source.Unit.Side, HeroActionType.HealAlly),
                    out var avatar) && false == avatar.IsHpFull)
                return true;

            for (var i = 0; i < 4; i++)
            {
                if (i == source.Unit.SlotIndex)
                    continue;

                if (false == _unitStateService.TryGetUnit(UnitDescriptor.Hero(source.Unit.Side, i, HeroActionType.HealAlly),
                        out var target))
                    continue;

                if (target is { IsAssigned: true, IsAlive: true, MaxHP: > 0, IsHpFull: false })
                    return true;
            }

            return false;
        }

        private int GetHeroActivationEnergyCost(UnitRuntimeState state)
        {
            return _heroAbilityModifierService.GetActivationEnergyCost(state.Unit.Side, state.Unit.SlotIndex,
                state.BaseActivationEnergyCost);
        }

        private int GetHeroAbilityPower(UnitRuntimeState state)
        {
            return _heroAbilityModifierService.GetAbilityPower(state.Unit.Side, state.Unit.SlotIndex,
                state.BaseAbilityPower);
        }

        private void ConsumeNextActivationBuffs(UnitRuntimeState state)
        {
            if (false == _nextActivationBuffService.Consume(state.Unit))
                return;

            _eventBus.Publish(new BuffsChangedEvent());
            _eventBus.Publish(new HeroAbilityStatsChangedEvent(state.Unit.Side, state.Unit.SlotIndex,
                GetHeroActivationEnergyCost(state), GetHeroAbilityPower(state)));
        }

        private int GetActionValueWithNextAttackBuffPreview(UnitDescriptor source, HeroActionType actionType,
            int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            return baseActionValue + _nextAttackBuffService.Get(source);
        }

        private int GetActionValueWithNextAttackBuff(UnitDescriptor source, HeroActionType actionType,
            int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            return baseActionValue + _nextAttackBuffService.Consume(source);
        }
    }
}