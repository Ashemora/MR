namespace Project.Scripts.Shared.Passives
{
    public readonly struct PassiveEffectEntryDefinition
    {
        public UnitTargetingDefinition EffectRecipients { get; }
        public BuffDefinition Buff { get; }
        public bool IsConfigured => Buff.IsConfigured;


        public PassiveEffectEntryDefinition(UnitTargetingDefinition effectRecipients, BuffDefinition buff)
        {
            EffectRecipients = effectRecipients;
            Buff = buff;
        }
    }
}