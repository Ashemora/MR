namespace Project.Scripts.Shared.Match
{
    public sealed class BattleSession
    {
        public BattleSession(int playerSeed, int opponentSeed)
        {
            PlayerSeed = playerSeed;
            OpponentSeed = opponentSeed;
        }


        public int PlayerSeed { get; }
        public int OpponentSeed { get; }
    }
}