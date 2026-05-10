using Project.Scripts.Shared.GroupDefense;
using Project.Scripts.Shared.Heroes;
using R3;

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