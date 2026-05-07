using System.Collections.Generic;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.GroupDefense;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Events
{
    public readonly struct MoveUsedEvent
    {

    }

    public readonly struct EnemyHPChangedEvent
    {
        public int Current { get; }
        public int Max { get; }
        public bool Silent { get; }


        public EnemyHPChangedEvent(int current, int max, bool silent = false)
        {
            Current = current;
            Max = max;
            Silent = silent;
        }
    }

    public readonly struct EnemyDefeatedEvent
    {

    }

    public readonly struct EnergyGeneratedEvent
    {
        public BattleSide Side { get; }
        public EnergyGainBreakdown Breakdown { get; }
        public IReadOnlyDictionary<TileKind, float> EnergyByKind => Breakdown.TotalEnergyByKind;


        public EnergyGeneratedEvent(IReadOnlyDictionary<TileKind, float> energyByKind)
            : this(BattleSide.Player, energyByKind)
        {
        }

        public EnergyGeneratedEvent(BattleSide side, IReadOnlyDictionary<TileKind, float> energyByKind)
            : this(side, new EnergyGainBreakdown(energyByKind))
        {
            
        }

        public EnergyGeneratedEvent(BattleSide side, EnergyGainBreakdown breakdown)
        {
            Side = side;
            Breakdown = breakdown;
        }
    }

    public readonly struct BattleSideEnergyAddedEvent
    {
        public BattleSide Side { get; }
        public float Amount { get; }


        public BattleSideEnergyAddedEvent(BattleSide side, float amount)
        {
            Side = side;
            Amount = amount < 0f ? 0f : amount;
        }
    }

    public readonly struct BattleSideMatchesCollectedEvent
    {
        public BattleSide Side { get; }
        public TileKind TileKind { get; }
        public int Count { get; }
        public long OccurredAtTick { get; }


        public BattleSideMatchesCollectedEvent(BattleSide side, TileKind tileKind, int count,
            long occurredAtTick = 0)
        {
            Side = side;
            TileKind = tileKind;
            Count = count < 0 ? 0 : count;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct BattleSideSpecialTileUsedEvent
    {
        public BattleSide Side { get; }
        public TileKind TileKind { get; }
        public int Count { get; }


        public BattleSideSpecialTileUsedEvent(BattleSide side, TileKind tileKind, int count = 1)
        {
            Side = side;
            TileKind = tileKind;
            Count = count < 0 ? 0 : count;
        }
    }

    public readonly struct PlayerHPChangedEvent
    {
        public int Current { get; }
        public int Max { get; }
        public bool Silent { get; }


        public PlayerHPChangedEvent(int current, int max, bool silent = false)
        {
            Current = current;
            Max = max;
            Silent = silent;
        }
    }

    public readonly struct HeroHPChangedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public int Current { get; }
        public int Max { get; }
        public bool Silent { get; }


        public HeroHPChangedEvent(BattleSide side, int slotIndex, int current, int max, bool silent = false)
        {
            Side = side;
            SlotIndex = slotIndex;
            Current = current;
            Max = max;
            Silent = silent;
        }
    }

    public readonly struct HeroDefeatedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public TileKind SlotKind { get; }
        public long OccurredAtTick { get; }


        public HeroDefeatedEvent(BattleSide side, int slotIndex, TileKind slotKind, long occurredAtTick = 0)
        {
            Side = side;
            SlotIndex = slotIndex;
            SlotKind = slotKind;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct HeroResurrectedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public int RestoredHP { get; }
        public long OccurredAtTick { get; }


        public HeroResurrectedEvent(BattleSide side, int slotIndex, int restoredHP, long occurredAtTick = 0)
        {
            Side = side;
            SlotIndex = slotIndex;
            RestoredHP = restoredHP < 0 ? 0 : restoredHP;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct HeroActivatedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public HeroActionType ActionType { get; }
        public int ActionValue { get; }
        public long OccurredAtTick { get; }


        public HeroActivatedEvent(BattleSide side, int slotIndex, HeroActionType actionType, int actionValue,
            long occurredAtTick = 0)
        {
            Side = side;
            SlotIndex = slotIndex;
            ActionType = actionType;
            ActionValue = actionValue;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct HeroPassiveActivatedEvent
    {
        public HeroPassiveRuntimeState State { get; }


        public HeroPassiveActivatedEvent(HeroPassiveRuntimeState state)
        {
            State = state;
        }
    }

    public readonly struct HeroPassiveDisabledEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }


        public HeroPassiveDisabledEvent(BattleSide side, int slotIndex)
        {
            Side = side;
            SlotIndex = slotIndex;
        }
    }

    public readonly struct HeroPassiveExpiredEvent
    {
        public HeroPassiveRuntimeState State { get; }


        public HeroPassiveExpiredEvent(HeroPassiveRuntimeState state)
        {
            State = state;
        }
    }

    public readonly struct BuffsChangedEvent
    {
    }

    public readonly struct HeroSlotKindPassiveStateChangedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public bool IsActive { get; }


        public HeroSlotKindPassiveStateChangedEvent(BattleSide side, int slotIndex, bool isActive)
        {
            Side = side;
            SlotIndex = slotIndex;
            IsActive = isActive;
        }
    }

    public readonly struct HeroAbilityStatsChangedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public int ActivationEnergyCost { get; }
        public int AbilityPower { get; }


        public HeroAbilityStatsChangedEvent(BattleSide side, int slotIndex, int activationEnergyCost, int abilityPower)
        {
            Side = side;
            SlotIndex = slotIndex;
            ActivationEnergyCost = activationEnergyCost;
            AbilityPower = abilityPower;
        }
    }

    public readonly struct AvatarAbilityPowerChangedEvent
    {
        public BattleSide Side { get; }
        public int AbilityPower { get; }


        public AvatarAbilityPowerChangedEvent(BattleSide side, int abilityPower)
        {
            Side = side;
            AbilityPower = abilityPower;
        }
    }

    public readonly struct HeroCooldownChangedEvent
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public float RemainingSeconds { get; }
        public float DurationSeconds { get; }


        public HeroCooldownChangedEvent(BattleSide side, int slotIndex, float remainingSeconds, float durationSeconds)
        {
            Side = side;
            SlotIndex = slotIndex;
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct PlayerDefeatedEvent
    {

    }

    public readonly struct BattleSideEnergyChangedEvent
    {
        public BattleSide Side { get; }
        public int Current { get; }
        public int EnergyCap { get; }


        public BattleSideEnergyChangedEvent(BattleSide side, int current, int energyCap)
        {
            Side = side;
            Current = current;
            EnergyCap = energyCap;
        }
    }

    public readonly struct EnemyAvatarActivatedEvent
    {
        public HeroActionType ActionType { get; }
        public int ActionValue { get; }


        public EnemyAvatarActivatedEvent(HeroActionType actionType, int actionValue)
        {
            ActionType = actionType;
            ActionValue = actionValue;
        }
    }

    public readonly struct AvatarCooldownChangedEvent
    {
        public BattleSide Side { get; }
        public float RemainingSeconds { get; }
        public float DurationSeconds { get; }


        public AvatarCooldownChangedEvent(BattleSide side, float remainingSeconds, float durationSeconds)
        {
            Side = side;
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct AbilityExecutedEvent
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public HeroActionType ActionType { get; }
        public int Value { get; }
        public long OccurredAtTick { get; }


        public AbilityExecutedEvent(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType, int value,
            long occurredAtTick = 0)
        {
            Source = source;
            Target = target;
            ActionType = actionType;
            Value = value;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct AbilityApplicationEvent
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public HeroActionType ActionType { get; }
        public int Value { get; }
        public int ApplicationIndex { get; }
        public bool IsRepeat { get; }
        public float PresentationDelaySeconds { get; }
        public long OccurredAtTick { get; }


        public AbilityApplicationEvent(UnitDescriptor source, UnitDescriptor target, HeroActionType actionType,
            int value, int applicationIndex, bool isRepeat, float presentationDelaySeconds, long occurredAtTick = 0)
        {
            Source = source;
            Target = target;
            ActionType = actionType;
            Value = value;
            ApplicationIndex = applicationIndex < 0 ? 0 : applicationIndex;
            IsRepeat = isRepeat;
            PresentationDelaySeconds = presentationDelaySeconds < 0f ? 0f : presentationDelaySeconds;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }

    public readonly struct AvatarExposedEvent
    {
        public BattleSide Side { get; }
        public HeroGroupId DestroyedGroupId { get; }


        public AvatarExposedEvent(BattleSide side, HeroGroupId destroyedGroupId)
        {
            Side = side;
            DestroyedGroupId = destroyedGroupId;
        }
    }

    public readonly struct GameResultEvent
    {
        public BattleSide Winner { get; }
        public bool IsFlawless { get; }


        public GameResultEvent(BattleSide winner, bool isFlawless)
        {
            Winner = winner;
            IsFlawless = isFlawless;
        }
    }

    public readonly struct BattleFlowRoundChangedEvent
    {
        public int CurrentRound { get; }
        public int TotalRounds { get; }


        public BattleFlowRoundChangedEvent(int currentRound, int totalRounds)
        {
            CurrentRound = currentRound;
            TotalRounds = totalRounds;
        }
    }

    public readonly struct BattleFlowPhaseChangedEvent
    {
        public BattlePhaseKind Phase { get; }
        public int CurrentRound { get; }
        public int TotalRounds { get; }
        public EnergyCarryoverMode EnergyCarryoverMode { get; }


        public BattleFlowPhaseChangedEvent(
            BattlePhaseKind phase,
            int currentRound,
            int totalRounds,
            EnergyCarryoverMode energyCarryoverMode)
        {
            Phase = phase;
            CurrentRound = currentRound;
            TotalRounds = totalRounds;
            EnergyCarryoverMode = energyCarryoverMode;
        }
    }

    public readonly struct BattleFlowTimerChangedEvent
    {
        public BattlePhaseKind Phase { get; }
        public float TimeRemaining { get; }


        public BattleFlowTimerChangedEvent(BattlePhaseKind phase, float timeRemaining)
        {
            Phase = phase;
            TimeRemaining = timeRemaining;
        }
    }

    public readonly struct BattleFlowCountdownTickEvent
    {
        public BattlePhaseKind Phase { get; }
        public int SecondsRemaining { get; }


        public BattleFlowCountdownTickEvent(BattlePhaseKind phase, int secondsRemaining)
        {
            Phase = phase;
            SecondsRemaining = secondsRemaining;
        }
    }

    public readonly struct BattlePrePhaseStartedEvent
    {
        public BattlePhaseKind UpcomingPhase { get; }


        public BattlePrePhaseStartedEvent(BattlePhaseKind upcomingPhase)
        {
            UpcomingPhase = upcomingPhase;
        }
    }

    public readonly struct BattlePrePhaseEndedEvent
    {
        public BattlePhaseKind NextPhase { get; }


        public BattlePrePhaseEndedEvent(BattlePhaseKind nextPhase)
        {
            NextPhase = nextPhase;
        }
    }

    public readonly struct BurndownStartedEvent
    {

    }

    public readonly struct EscalationModifiersAppliedEvent
    {
        public float CascadeEnergyMultiplier { get; }


        public EscalationModifiersAppliedEvent(float cascadeEnergyMultiplier)
        {
            CascadeEnergyMultiplier = cascadeEnergyMultiplier;
        }
    }

    public readonly struct AutoEnergyTickEvent
    {
        public float EnergyAmount { get; }


        public AutoEnergyTickEvent(float energyAmount)
        {
            EnergyAmount = energyAmount;
        }
    }
}