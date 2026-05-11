using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct HeroPassiveRuntimeState
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public TileKind SlotKind { get; }
        public PassiveAbilityDefinition Definition { get; }
        public bool IsDisabled { get; }
        public int TotalActivationCount { get; }
        public int ConditionCount => _conditionProgress?.Length ?? 0;

        public bool CanActivateAgain => false == IsDisabled
                                        && Definition.IsConfigured
                                        && (Definition.MaxActivations == 0 || TotalActivationCount < Definition.MaxActivations);


        private readonly float[] _conditionProgress;
        private readonly long[][] _conditionOccurrenceTicks;


        public HeroPassiveRuntimeState(BattleSide side, int slotIndex, TileKind slotKind, 
            PassiveAbilityDefinition definition, bool isDisabled = false, int totalActivationCount = 0, 
            float[] conditionProgress = null, long[][] conditionOccurrenceTicks = null)
        {
            Side = side;
            SlotIndex = slotIndex;
            SlotKind = slotKind;
            Definition = definition;
            IsDisabled = isDisabled;
            TotalActivationCount = totalActivationCount < 0 ? 0 : totalActivationCount;
            _conditionProgress = CopyConditionProgress(definition, conditionProgress);
            _conditionOccurrenceTicks = CopyConditionOccurrenceTicks(definition, conditionOccurrenceTicks);
        }

        public float GetConditionProgress(int conditionIndex)
        {
            return null != _conditionProgress && conditionIndex >= 0 && conditionIndex < _conditionProgress.Length
                ? _conditionProgress[conditionIndex]
                : 0f;
        }

        public HeroPassiveRuntimeState WithConditionProgress(int conditionIndex, float progress)
        {
            if (null == _conditionProgress || conditionIndex < 0 || conditionIndex >= _conditionProgress.Length)
                return this;

            var nextProgress = CopyConditionProgress(Definition, _conditionProgress);
            nextProgress[conditionIndex] = progress < 0 ? 0 : progress;

            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, IsDisabled,
                TotalActivationCount, nextProgress, _conditionOccurrenceTicks);
        }

        public HeroPassiveRuntimeState WithConditionOccurrenceTicksAdded(int conditionIndex, long occurredAtTick,
            int windowTicks, int occurrenceCount)
        {
            if (null == _conditionOccurrenceTicks || conditionIndex < 0 ||
                conditionIndex >= _conditionOccurrenceTicks.Length || windowTicks <= 0 || occurrenceCount <= 0)
                return this;

            var nextOccurrences = CopyConditionOccurrenceTicks(Definition, _conditionOccurrenceTicks);
            var existing = nextOccurrences[conditionIndex] ?? System.Array.Empty<long>();
            var minTick = occurredAtTick - windowTicks;
            var keptCount = 0;
            for (var i = 0; i < existing.Length; i++)
                if (existing[i] >= minTick)
                    keptCount++;

            var ticks = new long[keptCount + occurrenceCount];
            var targetIndex = 0;
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] < minTick)
                    continue;

                ticks[targetIndex] = existing[i];
                targetIndex++;
            }

            var nextTick = occurredAtTick < 0 ? 0 : occurredAtTick;
            for (var i = 0; i < occurrenceCount; i++)
                ticks[targetIndex + i] = nextTick;
            nextOccurrences[conditionIndex] = ticks;

            var nextProgress = CopyConditionProgress(Definition, _conditionProgress);
            nextProgress[conditionIndex] = ticks.Length;

            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, IsDisabled,
                TotalActivationCount, nextProgress, nextOccurrences);
        }

        public HeroPassiveRuntimeState WithConditionOccurrenceTicksConsumed(int conditionIndex, int amount)
        {
            if (null == _conditionOccurrenceTicks || conditionIndex < 0 ||
                conditionIndex >= _conditionOccurrenceTicks.Length || amount <= 0)
                return this;

            var existing = _conditionOccurrenceTicks[conditionIndex] ?? System.Array.Empty<long>();
            var remainingCount = existing.Length - amount;
            if (remainingCount < 0)
                remainingCount = 0;

            var ticks = new long[remainingCount];
            for (var i = 0; i < remainingCount; i++)
                ticks[i] = existing[i + amount];

            var nextOccurrences = CopyConditionOccurrenceTicks(Definition, _conditionOccurrenceTicks);
            nextOccurrences[conditionIndex] = ticks;

            var nextProgress = CopyConditionProgress(Definition, _conditionProgress);
            nextProgress[conditionIndex] = ticks.Length;

            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, IsDisabled,
                TotalActivationCount, nextProgress, nextOccurrences);
        }

        public HeroPassiveRuntimeState WithActivated()
        {
            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, IsDisabled,
                TotalActivationCount + 1, _conditionProgress, _conditionOccurrenceTicks);
        }

        public HeroPassiveRuntimeState WithActivatedAndProgress(float[] conditionProgress)
        {
            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, IsDisabled,
                TotalActivationCount + 1, conditionProgress, _conditionOccurrenceTicks);
        }

        public HeroPassiveRuntimeState WithDisabled()
        {
            return new HeroPassiveRuntimeState(Side, SlotIndex, SlotKind, Definition, true,
                TotalActivationCount, _conditionProgress, _conditionOccurrenceTicks);
        }

        private static float[] CopyConditionProgress(PassiveAbilityDefinition definition, float[] source)
        {
            var conditions = definition.ActivationConditions.Conditions;
            if (conditions.Count == 0)
                return System.Array.Empty<float>();

            var result = new float[conditions.Count];
            if (null == source)
                return result;

            for (var i = 0; i < result.Length && i < source.Length; i++)
                result[i] = source[i] < 0f ? 0f : source[i];

            return result;
        }

        private static long[][] CopyConditionOccurrenceTicks(PassiveAbilityDefinition definition, long[][] source)
        {
            var conditions = definition.ActivationConditions.Conditions;
            if (conditions.Count == 0)
                return System.Array.Empty<long[]>();

            var result = new long[conditions.Count][];
            for (var i = 0; i < result.Length; i++)
            {
                if (null == source || i >= source.Length || null == source[i] || source[i].Length == 0)
                {
                    result[i] = System.Array.Empty<long>();
                    continue;
                }

                result[i] = new long[source[i].Length];
                for (var j = 0; j < result[i].Length; j++)
                    result[i][j] = source[i][j] < 0 ? 0 : source[i][j];
            }

            return result;
        }
    }
}