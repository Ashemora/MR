using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public class UnitStateService : IUnitStateService
    {
        private const int SlotCount = 4;


        private readonly BattleSetup _battleSetup;
        private readonly IHeroService _heroService;
        private readonly IAvatarService _avatarService;


        public UnitStateService(BattleSetup battleSetup, IHeroService heroService, IAvatarService avatarService)
        {
            _battleSetup = battleSetup;
            _heroService = heroService;
            _avatarService = avatarService;
        }


        public bool TryGetUnit(UnitDescriptor unit, out UnitRuntimeState state)
        {
            if (unit.Kind == UnitKind.Avatar)
                return TryGetAvatar(unit, out state);

            return TryGetHero(unit, out state);
        }

        private bool TryGetAvatar(UnitDescriptor unit, out UnitRuntimeState state)
        {
            var avatar = _avatarService.GetAvatar(unit.Side);
            if (false == _battleSetup.TryGetUnit(unit, out var setup))
            {
                state = default;
                return false;
            }

            var abilityType = setup.IsAssigned ? setup.Unit.ActionType : unit.ActionType;
            state = new UnitRuntimeState(
                UnitDescriptor.Avatar(unit.Side, abilityType),
                setup.IsAssigned,
                avatar.IsAlive,
                avatar.CurrentHP,
                avatar.MaxHP,
                setup.BaseAbilityPower,
                setup.BaseActivationEnergyCost,
                TileKind.None);

            return true;
        }

        private bool TryGetHero(UnitDescriptor unit, out UnitRuntimeState state)
        {
            state = default;
            if (unit.SlotIndex is < 0 or >= SlotCount)
                return false;

            var slots = _heroService.GetSlots(unit.Side);
            if (unit.SlotIndex >= slots.Count)
                return false;

            var slot = slots[unit.SlotIndex];
            if (false == _battleSetup.TryGetUnit(unit, out var setup))
                return false;

            var abilityType = slot.IsAssigned ? slot.ActionType : unit.ActionType;
            state = new UnitRuntimeState(
                UnitDescriptor.Hero(unit.Side, unit.SlotIndex, abilityType),
                slot.IsAssigned,
                slot.IsAlive,
                slot.CurrentHP,
                slot.MaxHP,
                slot.ActionValue,
                slot.ActivationEnergyCost,
                slot.SlotKind);

            return true;
        }
    }
}