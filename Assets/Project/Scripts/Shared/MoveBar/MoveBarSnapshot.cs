namespace Project.Scripts.Shared.MoveBar
{
    public readonly struct MoveBarSnapshot
    {
        public readonly int CurrentMoves;
        public readonly float FillProgress;
        public readonly bool IsAtMax;


        public MoveBarSnapshot(int currentMoves, float fillProgress, bool isAtMax)
        {
            CurrentMoves = currentMoves;
            FillProgress = fillProgress;
            IsAtMax = isAtMax;
        }
    }
}