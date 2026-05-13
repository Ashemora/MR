using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Buffs
{
    public readonly struct BuffRuntimeState
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public TileKind SourceSlotKind { get; }
        public BuffDefinition Definition { get; }
        public int StackCount { get; }
        public BattlePhaseKind ExpiresAfterMainPhase { get; }
        public int ExpiresAfterRound { get; }
        public float DurationSeconds { get; }
        public float RemainingDurationSeconds { get; }
        public bool UsesDuration => RemainingDurationSeconds > 0f;
        public int ShieldCapacity { get; }
        public int ShieldRemaining { get; }
        public bool IsActiveShield => Definition.Kind == BuffKind.Shield && ShieldRemaining > 0;


        public BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, int currentRound, BattlePhaseKind currentPhase,
            float durationSeconds = 0f)
            : this(source, target, sourceSlotKind, definition, stackCount,
                definition.LifetimeKind == BuffLifetimeKind.UntilEndOfNextMainPhase
                    ? GetNextMainPhase(currentPhase)
                    : default,
                definition.LifetimeKind == BuffLifetimeKind.UntilEndOfRound
                    ? currentRound
                    : 0,
                durationSeconds,
                ResolveShieldCapacity(definition))
        {
        }

        private BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, BattlePhaseKind expiresAfterMainPhase, int expiresAfterRound,
            float durationSeconds, int shieldCapacity)
            : this(source, target, sourceSlotKind, definition, stackCount, expiresAfterMainPhase, expiresAfterRound,
                durationSeconds, durationSeconds, shieldCapacity, shieldCapacity)
        {
        }

        private BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, BattlePhaseKind expiresAfterMainPhase, int expiresAfterRound,
            float durationSeconds)
            : this(source, target, sourceSlotKind, definition, stackCount, expiresAfterMainPhase, expiresAfterRound,
                durationSeconds, durationSeconds, 0, 0)
        {
        }

        private BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, BattlePhaseKind expiresAfterMainPhase, int expiresAfterRound,
            float durationSeconds, float remainingDurationSeconds, int shieldCapacity, int shieldRemaining)
        {
            Source = source;
            Target = target;
            SourceSlotKind = sourceSlotKind;
            Definition = definition;
            StackCount = stackCount < 1 ? 1 : stackCount;
            ExpiresAfterMainPhase = expiresAfterMainPhase;
            ExpiresAfterRound = expiresAfterRound < 0 ? 0 : expiresAfterRound;
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
            RemainingDurationSeconds = remainingDurationSeconds < 0f ? 0f : remainingDurationSeconds;
            ShieldCapacity = shieldCapacity < 0 ? 0 : shieldCapacity;
            ShieldRemaining = ClampShieldRemaining(shieldRemaining, ShieldCapacity);
        }

        private BuffRuntimeState(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            BuffDefinition definition, int stackCount, BattlePhaseKind expiresAfterMainPhase, int expiresAfterRound,
            float durationSeconds, float remainingDurationSeconds)
        {
            Source = source;
            Target = target;
            SourceSlotKind = sourceSlotKind;
            Definition = definition;
            StackCount = stackCount < 1 ? 1 : stackCount;
            ExpiresAfterMainPhase = expiresAfterMainPhase;
            ExpiresAfterRound = expiresAfterRound < 0 ? 0 : expiresAfterRound;
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
            RemainingDurationSeconds = remainingDurationSeconds < 0f ? 0f : remainingDurationSeconds;
            ShieldCapacity = 0;
            ShieldRemaining = 0;
        }

        public BuffRuntimeState WithDurationTicked(float deltaTime)
        {
            if (RemainingDurationSeconds <= 0f || deltaTime <= 0f)
                return this;

            var nextDuration = RemainingDurationSeconds - deltaTime;
            if (nextDuration < 0f)
                nextDuration = 0f;

            return new BuffRuntimeState(Source, Target, SourceSlotKind, Definition, StackCount,
                ExpiresAfterMainPhase, ExpiresAfterRound, DurationSeconds, nextDuration, ShieldCapacity,
                ShieldRemaining);
        }

        public BuffRuntimeState WithShieldRemaining(int shieldRemaining)
        {
            if (Definition.Kind != BuffKind.Shield)
                return this;

            return new BuffRuntimeState(Source, Target, SourceSlotKind, Definition, StackCount,
                ExpiresAfterMainPhase, ExpiresAfterRound, DurationSeconds, RemainingDurationSeconds, ShieldCapacity,
                shieldRemaining);
        }

        private static BattlePhaseKind GetNextMainPhase(BattlePhaseKind currentPhase)
        {
            return currentPhase == BattlePhaseKind.Hero
                ? BattlePhaseKind.Match
                : BattlePhaseKind.Hero;
        }

        private static int ResolveShieldCapacity(BuffDefinition definition)
        {
            if (definition.Kind != BuffKind.Shield || definition.Value <= 0f)
                return 0;

            return BuffRules.ToDisplayInt(definition.Value);
        }

        private static int ClampShieldRemaining(int shieldRemaining, int shieldCapacity)
        {
            if (shieldRemaining <= 0 || shieldCapacity <= 0)
                return 0;

            return shieldRemaining > shieldCapacity ? shieldCapacity : shieldRemaining;
        }
    }
}