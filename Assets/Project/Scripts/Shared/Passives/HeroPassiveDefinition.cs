using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct HeroPassiveDefinition
    {
        public string DisplayName { get; }
        public ActivationConditionGroupDefinition ActivationConditions { get; }
        public IReadOnlyList<AbilityEffectEntryDefinition> AbilityEffectEntries =>
            _abilityEffectEntries ?? Array.Empty<AbilityEffectEntryDefinition>();
        public bool CanActivateWhileActive { get; }
        public int MaxActivations { get; }
        public bool IsConfigured => ActivationConditions.IsConfigured && HasConfiguredAbilityEffects();


        private readonly AbilityEffectEntryDefinition[] _abilityEffectEntries;


        public HeroPassiveDefinition(string displayName, ActivationConditionGroupDefinition activationConditions,
            IReadOnlyList<AbilityEffectEntryDefinition> abilityEffectEntries, bool canActivateWhileActive,
            int maxActivations)
        {
            DisplayName = displayName ?? string.Empty;
            ActivationConditions = activationConditions;
            _abilityEffectEntries = CopyConfiguredAbilityEffects(abilityEffectEntries);
            CanActivateWhileActive = canActivateWhileActive;
            MaxActivations = maxActivations < 0 ? 0 : maxActivations;
        }

        private bool HasConfiguredAbilityEffects()
        {
            if (_abilityEffectEntries != null)
                for (var i = 0; i < _abilityEffectEntries.Length; i++)
                    if (_abilityEffectEntries[i].IsConfigured)
                        return true;

            return false;
        }
        private static AbilityEffectEntryDefinition[] CopyConfiguredAbilityEffects(
            IReadOnlyList<AbilityEffectEntryDefinition> effects)
        {
            if (null == effects || effects.Count == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new List<AbilityEffectEntryDefinition>(effects.Count);
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.IsConfigured)
                    result.Add(effect);
            }

            return result.ToArray();
        }
    }
}