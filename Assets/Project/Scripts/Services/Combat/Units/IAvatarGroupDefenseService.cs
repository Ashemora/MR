using Project.Scripts.Shared.GroupDefense;
using R3;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IAvatarGroupDefenseService
    {
        ReadOnlyReactiveProperty<AvatarDefenseSnapshot> PlayerDefense { get; }
        ReadOnlyReactiveProperty<AvatarDefenseSnapshot> EnemyDefense { get; }
        bool IsExposed(BattleSide side);
        AvatarDefenseSnapshot GetSnapshot(BattleSide side);
    }
}