namespace Project.Scripts.Shared.MoveBar
{
    public readonly struct MoveBarSettings
    {
        public readonly int MaxMoves;
        public readonly float SecondsPerMove;
        public readonly int StartMoves;


        public MoveBarSettings(int maxMoves, float secondsPerMove, int startMoves)
        {
            MaxMoves = maxMoves;
            SecondsPerMove = secondsPerMove;
            StartMoves = startMoves;
        }
    }
}