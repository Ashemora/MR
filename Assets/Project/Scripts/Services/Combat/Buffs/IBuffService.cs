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
        bool ExpireUntilEndOfRoundBuffs(int completedRound);
        bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind);
        bool HasBuffFromSource(UnitDescriptor source);
    }

    public interface IShieldService
    {
        ShieldSnapshot GetShield(UnitDescriptor target);
        ShieldAbsorptionResult AbsorbDamage(UnitDescriptor target, int damage, BattlePhaseKind currentPhase);
    }

    public interface IStunStatusService
    {
        StunStatusSnapshot GetStunStatus(UnitDescriptor target);
        bool IsStunned(UnitDescriptor target);
    }
}