using UnityEngine;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Targeting
{
    public interface ITargetable
    {
        UnitDescriptor Descriptor { get; }
        UnitActionType ActionType { get; }
        bool IsReadySource { get; }
        Bounds WorldBounds { get; }
        bool IsValidTarget(UnitDescriptor source, UnitActionType sourceActionType);
        void SetSourceHighlight(bool active);
        void SetTargetHighlight(bool active, UnitActionType actionType);
    }
}