using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IUnitActivationCooldownService
    {
        void Tick(float deltaTime);
        bool IsOnCooldown(UnitDescriptor unit);
        bool IsHeroOnCooldown(BattleSide side, int slotIndex);
        bool IsAvatarOnCooldown(BattleSide side);
        void StartCooldown(UnitDescriptor unit);
        void StartHeroCooldown(BattleSide side, int slotIndex);
        void StartAvatarCooldown(BattleSide side);
    }
}