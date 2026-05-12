using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.BattleSetup
{
    public readonly struct BattleUnitSetup
    {
        public UnitDescriptor Unit { get; }
        public bool IsAssigned { get; }
        public int MaxHP { get; }
        public int BaseAbilityPower { get; }
        public int BaseActivationEnergyCost { get; }
        public float BaseActivationCooldownSeconds { get; }
        public ActiveAbilityDefinition ActiveAbility { get; }
        public UnitActionType ActionType => UnitActionTypeMapping.FromActiveAbility(ActiveAbility);
        public TileKind SlotKind { get; }
        public IReadOnlyList<PassiveAbilityDefinition> PassiveAbilities =>
            _passiveAbilities ?? Array.Empty<PassiveAbilityDefinition>();


        private readonly PassiveAbilityDefinition[] _passiveAbilities;


        public BattleUnitSetup(UnitDescriptor unit, bool isAssigned, int maxHP, int baseAbilityPower,
            int baseActivationEnergyCost, float baseActivationCooldownSeconds,
            ActiveAbilityDefinition activeAbility, TileKind slotKind,
            IReadOnlyList<PassiveAbilityDefinition> passiveAbilities = null)
        {
            Unit = unit;
            IsAssigned = isAssigned;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            BaseAbilityPower = baseAbilityPower < 0 ? 0 : baseAbilityPower;
            BaseActivationEnergyCost = baseActivationEnergyCost < 0 ? 0 : baseActivationEnergyCost;
            BaseActivationCooldownSeconds = baseActivationCooldownSeconds < 0f ? 0f : baseActivationCooldownSeconds;
            ActiveAbility = activeAbility;
            SlotKind = slotKind;
            _passiveAbilities = CopyPassives(passiveAbilities);
        }

        private static PassiveAbilityDefinition[] CopyPassives(IReadOnlyList<PassiveAbilityDefinition> passiveAbilities)
        {
            if (null == passiveAbilities || passiveAbilities.Count == 0)
                return Array.Empty<PassiveAbilityDefinition>();

            var result = new List<PassiveAbilityDefinition>(passiveAbilities.Count);
            for (var i = 0; i < passiveAbilities.Count; i++)
            {
                var passive = passiveAbilities[i];
                if (passive.IsConfigured)
                    result.Add(passive);
            }

            return result.ToArray();
        }
    }
}