using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Bot
{
    public sealed class BotDecisionEngine
    {
        private readonly BotSettings _settings;
        private readonly System.Random _rng;


        public BotDecisionEngine(BotSettings settings, int seed)
        {
            _settings = settings;
            _rng = new System.Random(seed);
        }

        public float GenerateDelay(float min, float max)
        {
            return (float)(_rng.NextDouble() * (max - min) + min);
        }

        public float GenerateAvatarActivationDelay()
        {
            return GenerateDelay(_settings.MinAvatarActivationDelay, _settings.MaxAvatarActivationDelay);
        }

        public int PickMostWoundedHero(IReadOnlyList<HeroSlotState> slots)
        {
            var bestIndex = -1;
            var lowestFraction = 1f;

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (false == slot.IsAssigned || false == slot.IsAlive || slot.MaxHP <= 0)
                    continue;

                var fraction = (float)slot.CurrentHP / slot.MaxHP;
                if (fraction >= 1f)
                    continue;

                if (fraction < lowestFraction)
                {
                    lowestFraction = fraction;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public int PickRandomAssignedSlot(IReadOnlyList<HeroSlotState> slots)
        {
            var eligibleCount = 0;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i].CanAccumulateEnergy)
                    eligibleCount++;

            if (eligibleCount == 0)
                return -1;

            var pick = _rng.Next(eligibleCount);
            var count = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                if (false == slots[i].CanAccumulateEnergy)
                    continue;
                if (count == pick)
                    return i;
                count++;
            }

            return -1;
        }

        public int PickGroupBreakTarget(IReadOnlyList<HeroSlotState> playerSlots, int[] group1Indices, 
            int[] group2Indices)
        {
            var g1Alive = CountAliveInGroup(playerSlots, group1Indices);
            var g2Alive = CountAliveInGroup(playerSlots, group2Indices);

            if (g1Alive == 0 && g2Alive == 0)
                return -1;

            int[] chosenGroup;

            if (g1Alive == 0)
                chosenGroup = group2Indices;
            else if (g2Alive == 0)
                chosenGroup = group1Indices;
            else if (g1Alive < g2Alive)
                chosenGroup = group1Indices;
            else if (g2Alive < g1Alive)
                chosenGroup = group2Indices;
            else
            {
                var g1HP = SumHPInGroup(playerSlots, group1Indices);
                var g2HP = SumHPInGroup(playerSlots, group2Indices);
                chosenGroup = g1HP <= g2HP ? group1Indices : group2Indices;
            }

            return PickLowestHPInGroup(playerSlots, chosenGroup);
        }

        public int PickWeakestGroupHero(IReadOnlyList<HeroSlotState> enemySlots, int[] group1Indices,
            int[] group2Indices)
        {
            var g1Alive = CountAliveInGroup(enemySlots, group1Indices);
            var g2Alive = CountAliveInGroup(enemySlots, group2Indices);

            if (g1Alive == 0 && g2Alive == 0)
                return -1;

            int[] primaryGroup;
            int[] secondaryGroup;

            if (g1Alive == 0)
            {
                primaryGroup = group2Indices;
                secondaryGroup = group1Indices;
            }
            else if (g2Alive == 0)
            {
                primaryGroup = group1Indices;
                secondaryGroup = group2Indices;
            }
            else
            {
                var g1HP = SumHPInGroup(enemySlots, group1Indices);
                var g2HP = SumHPInGroup(enemySlots, group2Indices);
                if (g1HP <= g2HP)
                {
                    primaryGroup = group1Indices;
                    secondaryGroup = group2Indices;
                }
                else
                {
                    primaryGroup = group2Indices;
                    secondaryGroup = group1Indices;
                }
            }

            var target = PickMostWoundedInGroup(enemySlots, primaryGroup);
            if (target >= 0)
                return target;

            return PickMostWoundedInGroup(enemySlots, secondaryGroup);
        }

        private static int CountAliveInGroup(IReadOnlyList<HeroSlotState> slots, int[] groupIndices)
        {
            var count = 0;
            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx >= 0 && idx < slots.Count && slots[idx].IsAssigned && slots[idx].IsAlive)
                    count++;
            }

            return count;
        }

        private static int SumHPInGroup(IReadOnlyList<HeroSlotState> slots, int[] groupIndices)
        {
            var sum = 0;
            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx >= 0 && idx < slots.Count && slots[idx].IsAssigned && slots[idx].IsAlive)
                    sum += slots[idx].CurrentHP;
            }

            return sum;
        }

        private static int PickLowestHPInGroup(IReadOnlyList<HeroSlotState> slots, int[] groupIndices)
        {
            var bestIndex = -1;
            var lowestHP = int.MaxValue;

            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx < 0 || idx >= slots.Count)
                    continue;

                var slot = slots[idx];
                if (!slot.IsAssigned || !slot.IsAlive)
                    continue;

                if (slot.CurrentHP < lowestHP)
                {
                    lowestHP = slot.CurrentHP;
                    bestIndex = idx;
                }
            }

            return bestIndex;
        }

        private static int PickMostWoundedInGroup(IReadOnlyList<HeroSlotState> slots, int[] groupIndices)
        {
            var bestIndex = -1;
            var lowestFraction = 1f;

            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx < 0 || idx >= slots.Count)
                    continue;

                var slot = slots[idx];
                if (!slot.IsAssigned || !slot.IsAlive || slot.MaxHP <= 0)
                    continue;

                var fraction = (float)slot.CurrentHP / slot.MaxHP;
                if (fraction >= 1f)
                    continue;

                if (fraction < lowestFraction)
                {
                    lowestFraction = fraction;
                    bestIndex = idx;
                }
            }

            return bestIndex;
        }
    }
}