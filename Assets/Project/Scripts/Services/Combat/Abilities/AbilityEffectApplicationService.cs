using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.BattleFlow;
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
            IReadOnlyList<AbilityEffectEntryDefinition> entries, TileKind sourceSlotKind, int currentRound,
            BattlePhaseKind currentPhase, long occurredAtTick, int applicationIndexOffset = 0, bool isRepeat = false)
        {
            if (entries == null || entries.Count == 0)
                return default;

            var directApplications = new List<AbilityDirectApplicationResult>();
            var abilityStatsChanges = new List<AbilityStatsChangeResult>();
            var buffApplicationCount = 0;
            var candidates = CollectCandidates();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (false == entry.IsConfigured)
                    continue;

                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, selectedTarget, candidates);
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var target = targets[targetIndex];
                    ApplyDirectActions(source, target, entry.DirectActions, entry.IgnoresAvatarGroupDefense,
                        occurredAtTick, applicationIndexOffset, isRepeat, directApplications);
                    buffApplicationCount += ApplyBuffApplications(source, target, sourceSlotKind, entry.BuffApplications,
                        currentRound, currentPhase, abilityStatsChanges);
                }
            }

            return new AbilityEffectApplicationResult(buffApplicationCount, directApplications, abilityStatsChanges);
        }

        private void ApplyDirectActions(UnitDescriptor source, UnitDescriptor target,
            IReadOnlyList<DirectActionDefinition> actions, bool ignoresAvatarGroupDefense, long occurredAtTick,
            int applicationIndexOffset, bool isRepeat, List<AbilityDirectApplicationResult> directApplications)
        {
            if (null == actions || actions.Count == 0)
                return;

            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (false == action.IsConfigured || false == CanApplyDirectAction(action, target, ignoresAvatarGroupDefense))
                    continue;

                if (ApplyDirectAction(target, action))
                    directApplications.Add(new AbilityDirectApplicationResult(source, target, UnitActionTypeMapping.FromDirectActionKind(action.Kind),
                        action.Value, applicationIndexOffset + i, isRepeat, occurredAtTick));
            }
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

        private bool ApplyDirectAction(UnitDescriptor target, DirectActionDefinition action)
        {
            if (action.Kind == DirectActionKind.Damage)
            {
                ApplyDamage(target, action.Value);
                return true;
            }

            if (action.Kind == DirectActionKind.Heal)
            {
                ApplyHeal(target, action.Value);
                return true;
            }

            return false;
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

        private bool CanApplyDirectAction(DirectActionDefinition action, UnitDescriptor target,
            bool ignoresAvatarGroupDefense)
        {
            if (false == TryGetTargetState(target, ignoresAvatarGroupDefense, out var isAlive, out var isHpFull,
                    out var isExposed))
                return false;

            return AbilityTargetRules.IsTargetValid(UnitActionTypeMapping.FromDirectActionKind(action.Kind), true, isAlive, isHpFull, isExposed);
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
            if (false == _unitStateService.TryGetUnit(UnitDescriptor.Avatar(side, UnitActionType.DealDamage),
                    out var state))
                return;

            result.Add(new UnitTargetCandidate(state.Unit, state.CurrentHP, state.MaxHP,
                state.IsAssigned && state.IsAlive));
        }

        private void AddHeroCandidates(List<UnitTargetCandidate> result, BattleSide side)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (false == _unitStateService.TryGetUnit(UnitDescriptor.Hero(side, i, UnitActionType.DealDamage),
                        out var state))
                    continue;

                result.Add(new UnitTargetCandidate(state.Unit, state.CurrentHP, state.MaxHP,
                    state.IsAssigned && state.IsAlive));
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

            change = new AbilityStatsChangeResult(unit.Unit, 0, GetAbilityPower(unit.Unit, unit.BaseAbilityPower));
            return true;
        }

        private int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost)
        {
            return (_buffService as IHeroAbilityModifierService)?.GetActivationEnergyCost(side, slotIndex, baseCost)
                   ?? baseCost;
        }

        private int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return (_buffService as IAbilityPowerModifierService)?.GetAbilityPower(target, basePower)
                   ?? basePower;
        }
    }
}
