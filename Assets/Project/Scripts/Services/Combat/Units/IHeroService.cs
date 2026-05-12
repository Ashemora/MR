using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IHeroService
    {
        IReadOnlyList<HeroSlotState> GetSlots(BattleSide side);
        void ApplyDamageToHero(BattleSide side, int slotIndex, int amount, bool silent = false);
        void ApplyHealToHero(BattleSide side, int slotIndex, int amount);
        bool TryResurrectHero(BattleSide side, int slotIndex, int restoredHP, out int actualRestoredHP,
            long occurredAtTick = 0);
    }
}