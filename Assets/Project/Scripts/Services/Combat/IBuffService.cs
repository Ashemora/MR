using System.Collections.Generic;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Combat
{
    public interface IBuffService
    {
        IReadOnlyList<BuffRuntimeState> Buffs { get; }
        bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound, BattlePhaseKind currentPhase);
        bool RemoveByUnit(UnitDescriptor unit);
        bool ExpireUntilEndOfNextMainPhaseBuffs(BattlePhaseKind previousPhase, BattlePhaseKind nextPhase);
        bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind);
        bool HasBuffFromSource(UnitDescriptor source);
    }

    public interface IBombRadiusModifierService
    {
        int GetBombRadiusBonus(BattleSide side);
    }
}