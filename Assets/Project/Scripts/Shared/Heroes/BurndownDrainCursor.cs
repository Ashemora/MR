using System.Collections.Generic;

namespace Project.Scripts.Shared.Heroes
{
    public struct BurndownDrainCursor
    {
        public int TargetIndex => _targetIndex;
        public bool IsDrainingAvatar => _targetIndex < 0;
        
        
        private int _targetIndex;


        public void Initialize(IReadOnlyList<HeroSlotState> slots)
        {
            _targetIndex = FindFirstAlive(slots, 0);
        }

        public bool AdvanceIfDead(IReadOnlyList<HeroSlotState> slots)
        {
            if (_targetIndex < 0)
                return false;

            if (IsLiveTarget(slots, _targetIndex))
                return false;

            _targetIndex = FindFirstAlive(slots, _targetIndex + 1);
            
            return true;
        }


        private static bool IsLiveTarget(IReadOnlyList<HeroSlotState> slots, int index)
        {
            return slots[index].IsAssigned && slots[index].IsAlive;
        }

        private static int FindFirstAlive(IReadOnlyList<HeroSlotState> slots, int startFrom)
        {
            for (var i = startFrom; i < slots.Count; i++)
                if (IsLiveTarget(slots, i))
                    return i;

            return -1;
        }
    }
}