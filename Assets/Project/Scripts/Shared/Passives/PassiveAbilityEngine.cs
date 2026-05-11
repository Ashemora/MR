using System;
using System.Collections.Generic;
using Project.Scripts.Shared.ActivationConditions;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Passives
{
    public class PassiveAbilityEngine
    {
        private const int DefaultTickRate = 30;


        public IReadOnlyList<HeroPassiveRuntimeState> States => _states;


        private HeroPassiveRuntimeState[] _states = Array.Empty<HeroPassiveRuntimeState>();
        private int _tickRate = DefaultTickRate;

        
        public void Initialize(IReadOnlyList<HeroPassiveSetup> setups, int tickRate = DefaultTickRate)
        {
            _tickRate = tickRate < 1 ? DefaultTickRate : tickRate;
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
            Func<HeroPassiveRuntimeState, bool> hasActiveBuff = sourceHasActiveBuff ? _ => true : null;
            
            return ProcessActivationConditionEvent(e, hasActiveBuff);
        }

        public bool ProcessActivationConditionEvent(ActivationConditionEvent e, Func<HeroPassiveRuntimeState, bool> hasActiveBuff)
        {
            if (e.Kind == ActivationConditionKind.None || e.Amount <= 0)
                return false;

            var changed = false;
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                if (false == state.CanActivateAgain)
                    continue;

                if (hasActiveBuff?.Invoke(state) == true && false == state.Definition.CanActivateWhileActive)
                    continue;

                if (false == TryAddConditionProgress(state, e, _tickRate, out var nextState))
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

                    state = state.WithConditionProgress(conditionIndex, 0f)
                        .WithConditionOccurrenceTicksConsumed(conditionIndex, int.MaxValue);
                    changed = true;
                }

                _states[i] = state;
            }

            return changed;
        }

        private static bool TryAddConditionProgress(HeroPassiveRuntimeState state, ActivationConditionEvent e,
            int tickRate, out HeroPassiveRuntimeState nextState)
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

                nextState = UsesTickWindow(condition)
                    ? nextState.WithConditionOccurrenceTicksAdded(conditionIndex, e.OccurredAtTick,
                        ToWindowTicks(condition.WindowSeconds, tickRate), ToOccurrenceCount(e.Amount))
                    : nextState.WithConditionProgress(conditionIndex,
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
            if (UsesTickWindow(state.Definition.ActivationConditions.Conditions[conditionIndex]))
                return state
                    .WithConditionProgress(conditionIndex, 0f)
                    .WithConditionOccurrenceTicksConsumed(conditionIndex, int.MaxValue);

            return state.WithConditionProgress(conditionIndex,
                state.GetConditionProgress(conditionIndex) - amount);
        }

        private static bool UsesTickWindow(ActivationConditionDefinition condition)
        {
            return condition.WindowSeconds > 0f;
        }

        private static int ToWindowTicks(float windowSeconds, int tickRate)
        {
            if (windowSeconds <= 0f || tickRate <= 0)
                return 0;

            return (int)Math.Ceiling(windowSeconds * tickRate);
        }

        private static int ToOccurrenceCount(float amount)
        {
            return amount <= 0f ? 0 : (int)Math.Floor(amount);
        }

        private static float[] CopyConditionProgress(HeroPassiveRuntimeState state)
        {
            var result = new float[state.ConditionCount];
            for (var i = 0; i < result.Length; i++)
                result[i] = state.GetConditionProgress(i);

            return result;
        }

        public bool ResetOwnerProgress(BattleSide side, int slotIndex)
        {
            var changed = false;
            for (var i = 0; i < _states.Length; i++)
            {
                var state = _states[i];
                if (state.Side != side || state.SlotIndex != slotIndex)
                    continue;

                var conditions = state.Definition.ActivationConditions.Conditions;
                var stateChanged = false;
                for (var conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                {
                    if (state.GetConditionProgress(conditionIndex) == 0f)
                        continue;

                    state = state.WithConditionProgress(conditionIndex, 0f)
                        .WithConditionOccurrenceTicksConsumed(conditionIndex, int.MaxValue);
                    stateChanged = true;
                }

                if (false == stateChanged)
                    continue;

                _states[i] = state;
                changed = true;
            }

            return changed;
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