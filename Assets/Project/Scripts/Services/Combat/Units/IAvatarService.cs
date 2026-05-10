using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IAvatarService
    {
        AvatarState GetAvatar(BattleSide side);
        bool IsAlive(BattleSide side);
        bool IsHpFull(BattleSide side);
        void ApplyDamage(BattleSide side, int amount, bool silent = false);
        void ApplyHeal(BattleSide side, int amount);
        void ForceApplyDamage(BattleSide side, int amount, bool suppressDefeatedEvent = false);
    }
}
