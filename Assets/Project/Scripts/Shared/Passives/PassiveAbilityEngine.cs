using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Passives
{
    public class PassiveAbilityEngine
    {
        public IReadOnlyList<HeroPassiveRuntimeState> States => _states;


        private HeroPassiveRuntimeState[] _states = Array.Empty<HeroPassiveRuntimeState>();

        
        public void Initialize(IReadOnlyList<HeroPassiveSetup> setups)
        {
            if (null == setups || setups.Count == 0)
            {
                _states = Array.Empty<HeroPassiveRuntimeState>();
                return;
            }

            var states = new List<HeroPassiveRuntimeState>(setups.Count);
            for (var i = 0; i < setups.Count; i++)
            {
                var setup = setups[i];
                if (false == setup.Definition.IsConfigured)
                    continue;

                states.Add(new HeroPassiveRuntimeState(
                    setup.Side,
                    setup.SlotIndex,
                    setup.SlotKind,
                    setup.Definition));
            }

            _states = states.ToArray();
        }

        public bool ProcessActivationConditionEvent(ActivationConditionEvent e, bool sourceHasActiveBuff = false)
        {
            if (e.Kind == ActivationConditionKind.None || e.Amount <= 0)
                return false;

            var changed = false;
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                if (false == state.CanActivateAgain)
                    continue;

                if (sourceHasActiveBuff && false == state.Definition.CanActivateWhileActive)
                    continue;

                if (false == TryAddConditionProgress(state, e, out var nextState))
                    continue;

                while (nextState.CanActivateAgain && IsConditionGroupSatisfied(nextState))
                {
                    nextState = ConsumeSatisfiedConditionProgress(nextState);
                    nextState = nextState.WithActivatedAndProgress(CopyConditionProgress(nextState));
                }

                _states[i] = nextState;
                changed = true;
            }

            return changed;
        }

        public bool ResetActivationConditionProgress(ActivationConditionKind kind, BattleSide side)
        {
            if (kind == ActivationConditionKind.None)
                return false;

            var changed = false;
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                if (state.Side != side)
                    continue;

                var conditions = state.Definition.ActivationConditions.Conditions;
                for (var conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                {
                    if (conditions[conditionIndex].Kind != kind || state.GetConditionProgress(conditionIndex) == 0f)
                        continue;

                    state = state.WithConditionProgress(conditionIndex, 0f);
                    changed = true;
                }

                _states[i] = state;
            }

            return changed;
        }

        private static bool TryAddConditionProgress(HeroPassiveRuntimeState state, ActivationConditionEvent e,
            out HeroPassiveRuntimeState nextState)
        {
            nextState = state;
            var changed = false;
            var conditions = state.Definition.ActivationConditions.Conditions;
            for (var conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
            {
                var condition = conditions[conditionIndex];
                if (false == ActivationConditionRules.Matches(condition, e, state.Side, state.SlotIndex,
                        state.SlotKind))
                    continue;

                nextState = nextState.WithConditionProgress(conditionIndex,
                    nextState.GetConditionProgress(conditionIndex) + e.Amount);
                changed = true;
            }

            return changed;
        }

        private static bool IsConditionGroupSatisfied(HeroPassiveRuntimeState state)
        {
            var group = state.Definition.ActivationConditions;
            var conditions = group.Conditions;
            if (conditions.Count == 0)
                return false;

            for (var i = 0; i < conditions.Count; i++)
            {
                var satisfied = state.GetConditionProgress(i) >= conditions[i].RequiredCount;
                if (group.Operator == ActivationConditionGroupOperator.Any && satisfied)
                    return true;

                if (group.Operator == ActivationConditionGroupOperator.All && false == satisfied)
                    return false;
            }

            return group.Operator == ActivationConditionGroupOperator.All;
        }

        private static HeroPassiveRuntimeState ConsumeSatisfiedConditionProgress(HeroPassiveRuntimeState state)
        {
            var group = state.Definition.ActivationConditions;
            var conditions = group.Conditions;

            if (group.Operator == ActivationConditionGroupOperator.All)
            {
                for (var i = 0; i < conditions.Count; i++)
                    state = ConsumeConditionProgress(state, i, conditions[i].RequiredCount);

                return state;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                if (state.GetConditionProgress(i) < conditions[i].RequiredCount)
                    continue;

                return ConsumeConditionProgress(state, i, conditions[i].RequiredCount);
            }

            return state;
        }

        private static HeroPassiveRuntimeState ConsumeConditionProgress(HeroPassiveRuntimeState state,
            int conditionIndex, int amount)
        {
            return state.WithConditionProgress(conditionIndex,
                state.GetConditionProgress(conditionIndex) - amount);
        }

        private static float[] CopyConditionProgress(HeroPassiveRuntimeState state)
        {
            var result = new float[state.ConditionCount];
            for (var i = 0; i < result.Length; i++)
                result[i] = state.GetConditionProgress(i);

            return result;
        }

        public bool DisableOwner(BattleSide side, int slotIndex)
        {
            var changed = false;
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                if (state.Side != side || state.SlotIndex != slotIndex || state.IsDisabled)
                    continue;

                _states[i] = state.WithDisabled();
                changed = true;
            }

            return changed;
        }
    }
}