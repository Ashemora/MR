using System.Collections.Generic;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct ActiveAbilityDefinition
    {
        public string DisplayName { get; }
        public int ActivationEnergyCost { get; }
        public float ActivationCooldownSeconds { get; }
        public DirectActionDefinition DirectAction { get; }
        public IReadOnlyList<BuffEntryDefinition> BuffEntries =>
            _buffEntries ?? System.Array.Empty<BuffEntryDefinition>();
        public bool IsConfigured => DirectAction.IsConfigured || HasConfiguredBuffEntries();


        private readonly BuffEntryDefinition[] _buffEntries;


        public ActiveAbilityDefinition(string displayName, int activationEnergyCost, float activationCooldownSeconds,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries)
        {
            DisplayName = displayName ?? string.Empty;
            ActivationEnergyCost = activationEnergyCost < 0 ? 0 : activationEnergyCost;
            ActivationCooldownSeconds = activationCooldownSeconds < 0f ? 0f : activationCooldownSeconds;
            DirectAction = directAction;
            _buffEntries = AbilityDefinitionCopy.CopyConfiguredBuffEntries(buffEntries);
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