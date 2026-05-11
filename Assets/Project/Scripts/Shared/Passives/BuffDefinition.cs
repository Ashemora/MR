namespace Project.Scripts.Shared.Passives
{
    public readonly struct BuffDefinition
    {
        public BuffKind Kind { get; }
        public BuffModifierOperation Operation { get; }
        public float Value { get; }
        public BuffLifetimeKind LifetimeKind { get; }
        public BuffStackingMode StackingMode { get; }
        public bool IsConfigured => Kind != BuffKind.None;


        public BuffDefinition(BuffKind kind, BuffModifierOperation operation, float value,
            BuffLifetimeKind lifetimeKind, BuffStackingMode stackingMode)
        {
            Kind = kind;
            Operation = operation;
            Value = value;
            LifetimeKind = lifetimeKind;
            StackingMode = stackingMode;
        }
    }
}