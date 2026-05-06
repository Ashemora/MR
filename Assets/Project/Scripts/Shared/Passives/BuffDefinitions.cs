using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct BuffDefinition
    {
        public BuffKind Kind { get; }
        public BuffModifierOperation Operation { get; }
        public float Value { get; }
        public BuffLifetimeKind LifetimeKind { get; }
        public int DurationRounds { get; }
        public BuffStackingMode StackingMode { get; }
        public bool IsConfigured => Kind != BuffKind.None;


        public BuffDefinition(BuffKind kind, BuffModifierOperation operation, float value, 
            BuffLifetimeKind lifetimeKind, int durationRounds, BuffStackingMode stackingMode)
        {
            Kind = kind;
            Operation = operation;
            Value = value;
            LifetimeKind = lifetimeKind;
            DurationRounds = durationRounds < 0 ? 0 : durationRounds;
            StackingMode = stackingMode;
        }
    }

    public readonly struct BuffRuntimeState
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public TileKind SourceSlotKind { get; }
        public BuffDefinition Definition { get; }
        public int StackCount { get; }
        public int ExpiresAtRound { get; }


        public BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, int currentRound)
        {
            Source = source;
            Target = target;
            SourceSlotKind = sourceSlotKind;
            Definition = definition;
            StackCount = stackCount < 1 ? 1 : stackCount;
            ExpiresAtRound = definition.LifetimeKind == BuffLifetimeKind.Rounds
                ? currentRound + definition.DurationRounds
                : 0;
        }

        public BuffRuntimeState WithStackAdded(int amount, int currentRound)
        {
            return new BuffRuntimeState(Source, Target, SourceSlotKind, Definition, StackCount + amount,
                currentRound);
        }
    }
    
    
    public enum BuffKind
    {
        None,
        ModifyAbilityPower,
        ModifyActivationEnergyCost,
        ModifyMatchEnergyBySlotKind,
        ModifySpecialTileActivationEnergy,
        ModifyBombRadius,
        NextAttackDamage
    }

    public enum BuffModifierOperation
    {
        None,
        AddFlat,
        AddPercent
    }

    public enum BuffLifetimeKind
    {
        Battle,
        Rounds,
        NextAttack
    }

    public enum BuffStackingMode
    {
        Stack,
        IgnoreNew
    }
}