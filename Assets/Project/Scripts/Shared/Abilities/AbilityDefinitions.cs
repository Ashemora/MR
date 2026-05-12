using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Buffs;
using Project.Scripts.Shared.Targeting;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct DirectActionDefinition
    {
        public DirectActionKind Kind { get; }
        public int Value { get; }
        public ValueModifierOperation Operation { get; }
        public UnitTargetingDefinition Targeting { get; }
        public bool IgnoresAvatarGroupDefense { get; }
        public bool IsConfigured => Kind != DirectActionKind.None && Value > 0;


        public DirectActionDefinition(DirectActionKind kind, int value, UnitTargetingDefinition targeting,
            bool ignoresAvatarGroupDefense, ValueModifierOperation operation = ValueModifierOperation.AddFlat)
        {
            Kind = kind;
            Value = value < 0 ? 0 : value;
            Operation = operation == ValueModifierOperation.None ? ValueModifierOperation.AddFlat : operation;
            Targeting = targeting;
            IgnoresAvatarGroupDefense = ignoresAvatarGroupDefense;
        }
    }

    public readonly struct BuffEntryDefinition
    {
        public UnitTargetingDefinition Targeting { get; }
        public IReadOnlyList<BuffApplicationDefinition> BuffApplications =>
            _buffApplications ?? Array.Empty<BuffApplicationDefinition>();
        public bool IsConfigured => HasConfiguredBuffApplications();


        private readonly BuffApplicationDefinition[] _buffApplications;


        public BuffEntryDefinition(UnitTargetingDefinition targeting,
            IReadOnlyList<BuffApplicationDefinition> buffApplications)
        {
            Targeting = targeting;
            _buffApplications = CopyConfiguredBuffApplications(buffApplications);
        }

        private bool HasConfiguredBuffApplications()
        {
            if (null != _buffApplications)
                for (var i = 0; i < _buffApplications.Length; i++)
                    if (_buffApplications[i].IsConfigured)
                        return true;

            return false;
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
        public static BuffEntryDefinition[] CopyConfiguredBuffEntries(IReadOnlyList<BuffEntryDefinition> buffEntries)
        {
            if (null == buffEntries || buffEntries.Count == 0)
                return Array.Empty<BuffEntryDefinition>();

            var result = new List<BuffEntryDefinition>(buffEntries.Count);
            for (var i = 0; i < buffEntries.Count; i++)
            {
                var entry = buffEntries[i];
                if (entry.IsConfigured)
                    result.Add(entry);
            }

            return result.ToArray();
        }
    }
}