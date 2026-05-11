using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityEffectApplicationService
    {
        AbilityEffectApplicationResult Apply(UnitDescriptor source, UnitDescriptor selectedTarget,
            IReadOnlyList<AbilityEffectEntryDefinition> entries, TileKind sourceSlotKind, int currentRound,
            BattlePhaseKind currentPhase, long occurredAtTick, int applicationIndexOffset = 0, bool isRepeat = false);
    }
}