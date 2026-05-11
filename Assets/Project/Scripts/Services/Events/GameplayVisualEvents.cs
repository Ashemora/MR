using System.Collections.Generic;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Events
{
    public readonly struct MatchPlayedEvent
    {
        public int CascadeIndex { get; }


        public MatchPlayedEvent(int cascadeIndex)
        {
            CascadeIndex = cascadeIndex;
        }
    }

    public readonly struct BombActivatedEvent
    {

    }

    public readonly struct MoveCountChangedEvent
    {
        public int MovesUsed { get; }


        public MoveCountChangedEvent(int movesUsed)
        {
            MovesUsed = movesUsed;
        }
    }

    public readonly struct EnergyGeneratedVisualEvent
    {
        public IReadOnlyDictionary<TileKind, SharedVector3> SourceByKind { get; }


        public EnergyGeneratedVisualEvent(IReadOnlyDictionary<TileKind, SharedVector3> sourceByKind)
        {
            SourceByKind = sourceByKind;
        }
    }

    public readonly struct EnemyAttackEvent
    {
        public int Damage { get; }


        public EnemyAttackEvent(int damage)
        {
            Damage = damage;
        }
    }

    public readonly struct SwapRejectedEvent
    {

    }

    public readonly struct MoveBarChangedEvent
    {
        public int CurrentMoves { get; }
        public float FillProgress { get; }
        public bool IsAtMax { get; }


        public MoveBarChangedEvent(int currentMoves, float fillProgress, bool isAtMax)
        {
            CurrentMoves = currentMoves;
            FillProgress = fillProgress;
            IsAtMax = isAtMax;
        }
    }

    public readonly struct BattleEscalationReachedEvent
    {
        public float TimeRemaining { get; }


        public BattleEscalationReachedEvent(float timeRemaining)
        {
            TimeRemaining = timeRemaining;
        }
    }

    public readonly struct BurndownDrainTargetChangedEvent
    {
        public BattleSide Side { get; }
        public int TargetIndex { get; }


        public BurndownDrainTargetChangedEvent(BattleSide side, int targetIndex)
        {
            Side = side;
            TargetIndex = targetIndex;
        }
    }
}