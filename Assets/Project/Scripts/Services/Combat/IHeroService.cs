using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IHeroService
    {
        IReadOnlyList<HeroSlotState> GetSlots(BattleSide side);
        void ApplyDamageToHero(BattleSide side, int slotIndex, int amount, bool silent = false);
        void ApplyHealToHero(BattleSide side, int slotIndex, int amount);
    }
}