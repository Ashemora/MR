namespace Project.Scripts.Shared.Match
{
    public sealed class BattleSession
    {
        public BattleSession(int levelId, int seed)
        {
            LevelId = levelId;
            Seed = seed;
        }


        public int LevelId { get; }
        public int Seed { get; }
    }
}