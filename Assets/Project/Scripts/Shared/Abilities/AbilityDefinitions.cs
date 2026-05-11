using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Passives;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct AbilityEffectEntryDefinition
    {
        public UnitTargetingDefinition Targeting { get; }
        public IReadOnlyList<DirectActionDefinition> DirectActions =>
            _directActions ?? Array.Empty<DirectActionDefinition>();
        public IReadOnlyList<BuffApplicationDefinition> BuffApplications =>
            _buffApplications ?? Array.Empty<BuffApplicationDefinition>();
        public bool IsConfigured => HasConfiguredDirectActions() || HasConfiguredBuffApplications();


        private readonly DirectActionDefinition[] _directActions;
        private readonly BuffApplicationDefinition[] _buffApplications;


        public AbilityEffectEntryDefinition(UnitTargetingDefinition targeting,
            IReadOnlyList<DirectActionDefinition> directActions,
            IReadOnlyList<BuffApplicationDefinition> buffApplications)
        {
            Targeting = targeting;
            _directActions = CopyConfiguredDirectActions(directActions);
            _buffApplications = CopyConfiguredBuffApplications(buffApplications);
        }

        private bool HasConfiguredDirectActions()
        {
            if (null != _directActions)
                for (var i = 0; i < _directActions.Length; i++)
                    if (_directActions[i].IsConfigured)
                        return true;

            return false;
        }

        private bool HasConfiguredBuffApplications()
        {
            if (null != _buffApplications)
                for (var i = 0; i < _buffApplications.Length; i++)
                    if (_buffApplications[i].IsConfigured)
                        return true;

            return false;
        }

        private static DirectActionDefinition[] CopyConfiguredDirectActions(IReadOnlyList<DirectActionDefinition> directActions)
        {
            if (null == directActions || directActions.Count == 0)
                return Array.Empty<DirectActionDefinition>();

            var result = new List<DirectActionDefinition>(directActions.Count);
            for (var i = 0; i < directActions.Count; i++)
            {
                var action = directActions[i];
                if (action.IsConfigured)
                    result.Add(action);
            }

            return result.ToArray();
        }

        private static BuffApplicationDefinition[] CopyConfiguredBuffApplications(IReadOnlyList<BuffApplicationDefinition> buffApplications)
        {
            if (null == buffApplications || buffApplications.Count == 0)
                return Array.Empty<BuffApplicationDefinition>();

            var result = new List<BuffApplicationDefinition>(buffApplications.Count);
            for (var i = 0; i < buffApplications.Count; i++)
            {
                var application = buffApplications[i];
                if (application.IsConfigured)
                    result.Add(application);
            }

            return result.ToArray();
        }
    }

    public readonly struct DirectActionDefinition
    {
        public DirectActionKind Kind { get; }
        public int Value { get; }
        public bool IsConfigured => Kind != DirectActionKind.None && Value > 0;


        public DirectActionDefinition(DirectActionKind kind, int value)
        {
            Kind = kind;
            Value = value < 0 ? 0 : value;
        }
    }

    public readonly struct BuffApplicationDefinition
    {
        public BuffDefinition Buff { get; }
        public float DurationSeconds { get; }
        public bool IsConfigured => Buff.IsConfigured;


        public BuffApplicationDefinition(BuffDefinition buff, float durationSeconds = 0f)
        {
            Buff = buff;
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
        }
    }

    public enum DirectActionKind
    {
        None,
        Damage,
        Heal,
        Resurrect
    }

    internal static class AbilityDefinitionCopy
    {
        public static AbilityEffectEntryDefinition[] CopyConfiguredEffectEntries(IReadOnlyList<AbilityEffectEntryDefinition> effectEntries)
        {
            if (null == effectEntries || effectEntries.Count == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new List<AbilityEffectEntryDefinition>(effectEntries.Count);
            for (var i = 0; i < effectEntries.Count; i++)
            {
                var entry = effectEntries[i];
                if (entry.IsConfigured)
                    result.Add(entry);
            }

            return result.ToArray();
        }
    }
}