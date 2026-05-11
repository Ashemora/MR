using System.Collections.Generic;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Buffs;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Buffs
{
    public interface IBuffService
    {
        IReadOnlyList<BuffRuntimeState> Buffs { get; }
        bool Tick(float deltaTime);
        bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound, BattlePhaseKind currentPhase, float durationSeconds = 0f);
        bool RemoveByUnit(UnitDescriptor unit);
        bool ExpireUntilEndOfNextMainPhaseBuffs(BattlePhaseKind previousPhase, BattlePhaseKind nextPhase);
        bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind);
        bool HasBuffFromSource(UnitDescriptor source);
    }
}