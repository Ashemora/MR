using System.Collections.Generic;
using Project.Scripts.Shared.ActivationConditions;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct PassiveAbilityDefinition
    {
        public string DisplayName { get; }
        public ActivationConditionGroupDefinition ActivationConditions { get; }
        public DirectActionDefinition DirectAction { get; }
        public IReadOnlyList<BuffEntryDefinition> BuffEntries =>
            _buffEntries ?? System.Array.Empty<BuffEntryDefinition>();
        public bool CanActivateWhileActive { get; }
        public int MaxActivations { get; }
        public bool IsConfigured => ActivationConditions.IsConfigured
                                    && (DirectAction.IsConfigured || HasConfiguredBuffEntries());


        private readonly BuffEntryDefinition[] _buffEntries;


        public PassiveAbilityDefinition(string displayName, ActivationConditionGroupDefinition activationConditions,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries,
            bool canActivateWhileActive, int maxActivations)
        {
            DisplayName = displayName ?? string.Empty;
            ActivationConditions = activationConditions;
            DirectAction = directAction;
            _buffEntries = AbilityDefinitionCopy.CopyConfiguredBuffEntries(buffEntries);
            CanActivateWhileActive = canActivateWhileActive;
            MaxActivations = maxActivations < 0 ? 0 : maxActivations;
        }

        private bool HasConfiguredBuffEntries()
        {
            if (_buffEntries != null)
                for (var i = 0; i < _buffEntries.Length; i++)
                    if (_buffEntries[i].IsConfigured)
                        return true;

            return false;
        }
    }
}