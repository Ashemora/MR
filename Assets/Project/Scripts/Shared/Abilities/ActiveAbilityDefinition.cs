using System;
using System.Collections.Generic;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct ActiveAbilityDefinition
    {
        public string DisplayName { get; }
        public int ActivationEnergyCost { get; }
        public float ActivationCooldownSeconds { get; }
        public IReadOnlyList<AbilityEffectEntryDefinition> EffectEntries =>
            _effectEntries ?? Array.Empty<AbilityEffectEntryDefinition>();
        public bool IsConfigured => HasConfiguredEffectEntries();


        private readonly AbilityEffectEntryDefinition[] _effectEntries;


        public ActiveAbilityDefinition(string displayName, int activationEnergyCost, float activationCooldownSeconds,
            IReadOnlyList<AbilityEffectEntryDefinition> effectEntries)
        {
            DisplayName = displayName ?? string.Empty;
            ActivationEnergyCost = activationEnergyCost < 0 ? 0 : activationEnergyCost;
            ActivationCooldownSeconds = activationCooldownSeconds < 0f ? 0f : activationCooldownSeconds;
            _effectEntries = AbilityDefinitionCopy.CopyConfiguredEffectEntries(effectEntries);
        }

        private bool HasConfiguredEffectEntries()
        {
            if (_effectEntries != null)
                for (var i = 0; i < _effectEntries.Length; i++)
                    if (_effectEntries[i].IsConfigured)
                        return true;

            return false;
        }
    }
}
