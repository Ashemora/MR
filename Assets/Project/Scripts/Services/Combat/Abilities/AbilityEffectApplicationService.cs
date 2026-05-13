using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Buffs;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public class AbilityEffectApplicationService : IAbilityEffectApplicationService
    {
        private const int SlotCount = 4;


        private readonly BattleSetup _battleSetup;
        private readonly IUnitStateService _unitStateService;
        private readonly IHeroService _heroService;
        private readonly IAvatarService _avatarService;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IBuffService _buffService;


        public AbilityEffectApplicationService(BattleSetup battleSetup, IUnitStateService unitStateService,
            IHeroService heroService,
            IAvatarService avatarService, IAvatarGroupDefenseService groupDefense,
            IBuffService buffService)
        {
            _battleSetup = battleSetup;
            _unitStateService = unitStateService;
            _heroService = heroService;
            _avatarService = avatarService;
            _groupDefense = groupDefense;
            _buffService = buffService;
        }


        public AbilityEffectApplicationResult Apply(UnitDescriptor source, UnitDescriptor selectedTarget,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries,
            TileKind sourceSlotKind, int currentRound, BattlePhaseKind currentPhase, long occurredAtTick,
            int applicationIndex = 0, bool isRepeat = false, int nextAttackBonus = 0)
        {
            var directApplications = new List<AbilityDirectApplicationResult>();
            var abilityStatsChanges = new List<AbilityStatsChangeResult>();
            var buffApplicationCount = 0;
            var candidates = CollectCandidates();

            ApplyDirectAction(source, selectedTarget, directAction, candidates, occurredAtTick, applicationIndex,
                isRepeat, nextAttackBonus, directApplications);
            buffApplicationCount += ApplyBuffEntries(source, selectedTarget, buffEntries, sourceSlotKind,
                currentRound, currentPhase, candidates, abilityStatsChanges);

            return new AbilityEffectApplicationResult(buffApplicationCount, directApplications, abilityStatsChanges);
        }

        private void ApplyDirectAction(UnitDescriptor source, UnitDescriptor selectedTarget,
            DirectActionDefinition action, IReadOnlyList<UnitTargetCandidate> candidates, long occurredAtTick,
            int applicationIndex, bool isRepeat, int nextAttackBonus,
            List<AbilityDirectApplicationResult> directApplications)
        {
            if (false == action.IsConfigured)
                return;

            var targets = action.Kind == DirectActionKind.Resurrect
                ? UnitTargetingRules.SelectTargetsIncludingUnavailable(action.Targeting, source, selectedTarget,
                    candidates)
                : UnitTargetingRules.SelectTargets(action.Targeting, source, selectedTarget, candidates);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (false == CanApplyDirectAction(action, target))
                    continue;

                var finalValue = ExecuteDirectAction(source, target, action, nextAttackBonus, occurredAtTick);
                if (finalValue > 0)
                    directApplications.Add(new AbilityDirectApplicationResult(source, target,
                        UnitActionTypeMapping.FromDirectActionKind(action.Kind),
                        finalValue, applicationIndex, isRepeat, occurredAtTick));
            }
        }

        private int ApplyBuffEntries(UnitDescriptor source, UnitDescriptor selectedTarget,
            IReadOnlyList<BuffEntryDefinition> buffEntries, TileKind sourceSlotKind, int currentRound,
            BattlePhaseKind currentPhase, IReadOnlyList<UnitTargetCandidate> candidates,
            List<AbilityStatsChangeResult> abilityStatsChanges)
        {
            if (null == buffEntries || buffEntries.Count == 0)
                return 0;

            var appliedCount = 0;
            for (var i = 0; i < buffEntries.Count; i++)
            {
                var entry = buffEntries[i];
                if (false == entry.IsConfigured)
                    continue;

                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, selectedTarget, candidates);
                for (var t = 0; t < targets.Count; t++)
                    appliedCount += ApplyBuffApplications(source, targets[t], sourceSlotKind, entry.BuffApplications,
                        currentRound, currentPhase, abilityStatsChanges);
            }

            return appliedCount;
        }

        private int ApplyBuffApplications(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            IReadOnlyList<BuffApplicationDefinition> applications, int currentRound, BattlePhaseKind currentPhase,
            List<AbilityStatsChangeResult> abilityStatsChanges)
        {
            if (null == applications || applications.Count == 0)
                return 0;

            var appliedCount = 0;
            for (var i = 0; i < applications.Count; i++)
            {
                var application = applications[i];
                if (false == application.IsConfigured)
                    continue;

                if (false == CanApplyBuffApplication(application.Buff, target))
                    continue;

                if (_buffService.AddBuff(source, target, sourceSlotKind, application.Buff, currentRound, currentPhase,
                        application.DurationSeconds))
                {
                    if (TryCreateAbilityStatsChange(target, out var change))
                        abilityStatsChanges.Add(change);
                    appliedCount++;
                }
            }

            return appliedCount;
        }

        private bool CanApplyBuffApplication(BuffDefinition buff, UnitDescriptor target)
        {
            if (buff.Kind != BuffKind.Shield)
                return true;

            return TryGetTargetState(target, true, out var isAlive, out _, out _) && isAlive;
        }

        private int ExecuteDirectAction(UnitDescriptor source, UnitDescriptor target, DirectActionDefinition action,
            int nextAttackBonus, long occurredAtTick)
        {
            if (action.Kind == DirectActionKind.Damage)
            {
                var value = GetAbilityPower(source, action.Value) + (nextAttackBonus < 0 ? 0 : nextAttackBonus);
                if (value <= 0)
                    return 0;

                ApplyDamage(target, value);
                return value;
            }

            if (action.Kind == DirectActionKind.Heal)
            {
                var value = GetAbilityPower(source, action.Value);
                if (value <= 0)
                    return 0;

                ApplyHeal(target, value);
                return value;
            }

            if (action.Kind == DirectActionKind.Resurrect)
            {
                if (target.Kind != UnitKind.Hero)
                    return 0;

                var value = ResolveResurrectionHP(target, action);
                if (value <= 0)
                    return 0;

                return _heroService.TryResurrectHero(target.Side, target.SlotIndex, value, out var restoredHP,
                    occurredAtTick)
                    ? restoredHP
                    : 0;
            }

            return 0;
        }

        private void ApplyDamage(UnitDescriptor target, int value)
        {
            if (target.Kind == UnitKind.Avatar)
            {
                _avatarService.ApplyDamage(target.Side, value);
                return;
            }

            _heroService.ApplyDamageToHero(target.Side, target.SlotIndex, value);
        }

        private void ApplyHeal(UnitDescriptor target, int value)
        {
            if (target.Kind == UnitKind.Avatar)
            {
                _avatarService.ApplyHeal(target.Side, value);
                return;
            }

            _heroService.ApplyHealToHero(target.Side, target.SlotIndex, value);
        }

        private bool CanApplyDirectAction(DirectActionDefinition action, UnitDescriptor target)
        {
            if (false == TryGetTargetState(target, action.IgnoresAvatarGroupDefense, out var isAlive, out var isHpFull,
                    out var isExposed))
                return false;

            if (action.Kind == DirectActionKind.Resurrect && target.Kind != UnitKind.Hero)
                return false;

            return AbilityTargetRules.IsActionRecipientValid(action.Kind, isAlive, isHpFull, isExposed);
        }

        private int ResolveResurrectionHP(UnitDescriptor target, DirectActionDefinition action)
        {
            if (false == _unitStateService.TryGetUnit(target, out var state))
                return 0;

            return BuffRules.ResolveAdditiveValue(action.Operation, action.Value, state.MaxHP);
        }

        private bool TryGetTargetState(UnitDescriptor target, bool ignoresAvatarGroupDefense, out bool isAlive,
            out bool isHpFull, out bool isExposed)
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
            isExposed = target.Kind != UnitKind.Avatar || ignoresAvatarGroupDefense || _groupDefense.IsExposed(target.Side);

            return true;
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

        private bool TryCreateAbilityStatsChange(UnitDescriptor target, out AbilityStatsChangeResult change)
        {
            change = default;
            if (target.Kind == UnitKind.Hero)
                return TryCreateHeroAbilityStatsChange(target.Side, target.SlotIndex, out change);

            return TryCreateAvatarAbilityPowerChange(target.Side, out change);
        }

        private bool TryCreateHeroAbilityStatsChange(BattleSide side, int slotIndex, out AbilityStatsChangeResult change)
        {
            change = default;
            var unit = _battleSetup.GetHero(side, slotIndex);
            if (false == unit.IsAssigned)
                return false;

            change = new AbilityStatsChangeResult(unit.Unit,
                GetActivationEnergyCost(side, slotIndex, unit.BaseActivationEnergyCost),
                GetAbilityPower(unit.Unit, unit.BaseAbilityPower));
            return true;
        }

        private bool TryCreateAvatarAbilityPowerChange(BattleSide side, out AbilityStatsChangeResult change)
        {
            change = default;
            var unit = side == BattleSide.Player ? _battleSetup.PlayerAvatar : _battleSetup.EnemyAvatar;
            if (false == unit.IsAssigned)
                return false;

            change = new AbilityStatsChangeResult(unit.Unit,
                GetActivationEnergyCost(unit.Unit, unit.BaseActivationEnergyCost),
                GetAbilityPower(unit.Unit, unit.BaseAbilityPower));
            return true;
        }

        private int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost)
        {
            return GetActivationEnergyCost(UnitDescriptor.Hero(side, slotIndex), baseCost);
        }

        private int GetActivationEnergyCost(UnitDescriptor unit, int baseCost)
        {
            return (_buffService as IHeroAbilityModifierService)?.GetActivationEnergyCost(unit, baseCost) ?? baseCost;
        }

        private int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return (_buffService as IAbilityPowerModifierService)?.GetAbilityPower(target, basePower)
                   ?? basePower;
        }
    }
}