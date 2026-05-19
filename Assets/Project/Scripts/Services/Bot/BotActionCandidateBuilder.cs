using System.Collections.Generic;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Bot;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Bot
{
    public sealed class BotActionCandidateBuilder : IBotActionCandidateBuilder
    {
        private readonly IUnitAbilityActivationService _unitAbilityActivationService;
        private readonly IUnitStateService _unitStateService;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly BattleSetup _battleSetup;
        private readonly SlotLayoutConfig _slotLayoutConfig;


        public BotActionCandidateBuilder(IUnitAbilityActivationService unitAbilityActivationService,
            IUnitStateService unitStateService, IAvatarGroupDefenseService groupDefense, BattleSetup battleSetup,
            SlotLayoutConfig slotLayoutConfig)
        {
            _unitAbilityActivationService = unitAbilityActivationService;
            _unitStateService = unitStateService;
            _groupDefense = groupDefense;
            _battleSetup = battleSetup;
            _slotLayoutConfig = slotLayoutConfig;
        }

        public void AddCandidatesForSource(UnitDescriptor source, List<BotActionCandidate> result)
        {
            if (null == result)
                return;

            if (false == _unitAbilityActivationService.TryPreview(source, out var preview))
                return;

            if (false == _battleSetup.TryGetUnit(source, out var setup))
                return;

            var unitCandidates = CollectUnitTargetCandidates();
            AddCandidateIfLegal(source, UnitDescriptor.Avatar(BattleSide.Player), preview, setup, unitCandidates, result);
            AddCandidateIfLegal(source, UnitDescriptor.Avatar(BattleSide.Enemy), preview, setup, unitCandidates, result);
            for (var i = 0; i < BattleSetup.HeroSlotCount; i++)
            {
                AddCandidateIfLegal(source, UnitDescriptor.Hero(BattleSide.Player, i), preview, setup,
                    unitCandidates, result);
                AddCandidateIfLegal(source, UnitDescriptor.Hero(BattleSide.Enemy, i), preview, setup,
                    unitCandidates, result);
            }
        }

        private void AddCandidateIfLegal(UnitDescriptor source, UnitDescriptor target,
            UnitAbilityActivationState preview, BattleUnitSetup setup, IReadOnlyList<UnitTargetCandidate> unitCandidates,
            List<BotActionCandidate> result)
        {
            if (false == TryGetTargetState(target, out var targetState, out var targetIsExposed))
                return;

            if (false == AbilityTargetRules.IsTargetValidForEffect(source, target, setup.ActiveAbility.DirectAction,
                    setup.ActiveAbility.BuffEntries, unitCandidates, preview.IsAlive, targetState.IsAlive,
                    targetState.IsHpFull, targetIsExposed))
                return;

            result.Add(new BotActionCandidate(source, target, preview.ActionType,
                setup.ActiveAbility.DirectAction.Kind, setup.ActiveAbility.DirectAction.Value,
                targetState.CurrentHP, targetState.MaxHP, targetState.IsAlive, targetIsExposed,
                WouldBreakDefense(target, setup.ActiveAbility.DirectAction.Value)));
        }

        private List<UnitTargetCandidate> CollectUnitTargetCandidates()
        {
            var result = new List<UnitTargetCandidate>(10);
            AddUnitTargetCandidate(result, UnitDescriptor.Avatar(BattleSide.Player));
            AddUnitTargetCandidate(result, UnitDescriptor.Avatar(BattleSide.Enemy));
            for (var i = 0; i < BattleSetup.HeroSlotCount; i++)
            {
                AddUnitTargetCandidate(result, UnitDescriptor.Hero(BattleSide.Player, i));
                AddUnitTargetCandidate(result, UnitDescriptor.Hero(BattleSide.Enemy, i));
            }

            return result;
        }

        private void AddUnitTargetCandidate(List<UnitTargetCandidate> result, UnitDescriptor unit)
        {
            if (false == _unitStateService.TryGetUnit(unit, out var state))
                return;

            result.Add(new UnitTargetCandidate(state.Unit, state.ActionType, state.CurrentHP, state.MaxHP,
                state.IsAssigned && state.IsAlive, state.IsAssigned));
        }

        private bool TryGetTargetState(UnitDescriptor target, out UnitRuntimeState state, out bool isExposed)
        {
            isExposed = true;
            if (false == _unitStateService.TryGetUnit(target, out state))
                return false;

            if (false == state.IsAssigned)
                return false;

            isExposed = target.Kind != UnitKind.Avatar || _groupDefense.IsExposed(target.Side);
            
            return true;
        }

        private bool WouldBreakDefense(UnitDescriptor target, int actionValue)
        {
            if (target.Side != BattleSide.Player || target.Kind != UnitKind.Hero || actionValue <= 0)
                return false;

            if (false == _unitStateService.TryGetUnit(target, out var state) || false == state.IsAlive)
                return false;

            if (state.CurrentHP > actionValue)
                return false;

            return IsLastAliveInGroup(target.SlotIndex, _slotLayoutConfig.Group1SlotIndices)
                   || IsLastAliveInGroup(target.SlotIndex, _slotLayoutConfig.Group2SlotIndices);
        }

        private bool IsLastAliveInGroup(int slotIndex, int[] groupIndices)
        {
            if (null == groupIndices)
                return false;

            var aliveCount = 0;
            var containsTarget = false;
            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx == slotIndex)
                    containsTarget = true;

                if (idx < 0 || idx >= BattleSetup.HeroSlotCount)
                    continue;

                if (_unitStateService.TryGetUnit(UnitDescriptor.Hero(BattleSide.Player, idx), out var state)
                    && state.IsAssigned && state.IsAlive)
                    aliveCount++;
            }

            return containsTarget && aliveCount == 1;
        }
    }
}