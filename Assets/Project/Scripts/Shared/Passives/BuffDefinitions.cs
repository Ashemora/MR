using Project.Scripts.Shared.BattleFlow;
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

    public readonly struct BuffRuntimeState
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public TileKind SourceSlotKind { get; }
        public BuffDefinition Definition { get; }
        public int StackCount { get; }
        public BattlePhaseKind ExpiresAfterMainPhase { get; }
        public float RemainingDurationSeconds { get; }
        public bool UsesDuration => RemainingDurationSeconds > 0f;


        public BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, int currentRound, BattlePhaseKind currentPhase,
            float durationSeconds = 0f)
            : this(source, target, sourceSlotKind, definition, stackCount,
                definition.LifetimeKind == BuffLifetimeKind.UntilEndOfNextMainPhase
                    ? GetNextMainPhase(currentPhase)
                    : default,
                durationSeconds)
        {
        }

        private BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, BattlePhaseKind expiresAfterMainPhase, float durationSeconds)
        {
            Source = source;
            Target = target;
            SourceSlotKind = sourceSlotKind;
            Definition = definition;
            StackCount = stackCount < 1 ? 1 : stackCount;
            ExpiresAfterMainPhase = expiresAfterMainPhase;
            RemainingDurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
        }

        public BuffRuntimeState WithStackAdded(int amount, int currentRound, BattlePhaseKind currentPhase,
            float durationSeconds = 0f)
        {
            var nextDuration = RemainingDurationSeconds;
            if (durationSeconds > nextDuration)
                nextDuration = durationSeconds;

            return new BuffRuntimeState(Source, Target, SourceSlotKind, Definition, StackCount + amount,
                currentRound, currentPhase, nextDuration);
        }

        public BuffRuntimeState WithDurationTicked(float deltaTime)
        {
            if (RemainingDurationSeconds <= 0f || deltaTime <= 0f)
                return this;

            var nextDuration = RemainingDurationSeconds - deltaTime;
            if (nextDuration < 0f)
                nextDuration = 0f;

            return new BuffRuntimeState(Source, Target, SourceSlotKind, Definition, StackCount,
                ExpiresAfterMainPhase, nextDuration);
        }

        private static BattlePhaseKind GetNextMainPhase(BattlePhaseKind currentPhase)
        {
            return currentPhase == BattlePhaseKind.Hero
                ? BattlePhaseKind.Match
                : BattlePhaseKind.Hero;
        }
    }
    
    
    public enum BuffKind
    {
        None,
        ModifyAbilityPower,
        ModifyActivationEnergyCost,
        ModifyActivationCooldown,
        ModifyMatchEnergyBySlotKind,
        ModifySpecialTileActivationEnergy,
        ModifyBombRadius,
        RepeatAbilityApplication,
        NextAttackDamage,
        ApplyAbilityToAdditionalTargets,
        ModifyLineRuneThickness,
        ResurrectOnDeath,
        Shield,
        Stun
    }

    public enum BuffModifierOperation
    {
        None,
        AddFlat,
        AddPercent
    }

    public enum BuffLifetimeKind
    {
        Battle = 0,
        NextAttack = 2,
        NextActivation = 3,
        UntilEndOfNextMainPhase = 4
    }

    public enum BuffStackingMode
    {
        Stack,
        IgnoreNew
    }
}