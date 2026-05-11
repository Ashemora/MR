using System;
using System.Collections.Generic;
using Project.Scripts.Shared.ActivationConditions;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct PassiveAbilityDefinition
    {
        public string DisplayName { get; }
        public ActivationConditionGroupDefinition ActivationConditions { get; }
        public IReadOnlyList<AbilityEffectEntryDefinition> EffectEntries =>
            _effectEntries ?? Array.Empty<AbilityEffectEntryDefinition>();
        public bool CanActivateWhileActive { get; }
        public int MaxActivations { get; }
        public bool IsConfigured => ActivationConditions.IsConfigured && HasConfiguredEffectEntries();


        private readonly AbilityEffectEntryDefinition[] _effectEntries;


        public PassiveAbilityDefinition(string displayName, ActivationConditionGroupDefinition activationConditions,
            IReadOnlyList<AbilityEffectEntryDefinition> effectEntries, bool canActivateWhileActive, int maxActivations)
        {
            DisplayName = displayName ?? string.Empty;
            ActivationConditions = activationConditions;
            _effectEntries = AbilityDefinitionCopy.CopyConfiguredEffectEntries(effectEntries);
            CanActivateWhileActive = canActivateWhileActive;
            MaxActivations = maxActivations < 0 ? 0 : maxActivations;
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