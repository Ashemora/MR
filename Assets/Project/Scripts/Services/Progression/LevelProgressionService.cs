using Project.Scripts.Configs.Levels;

namespace Project.Scripts.Services.Progression
{
    public class LevelProgressionService : ILevelProgressionService
    {
        public int CurrentLevelId { get; private set; } = 1;


        private readonly LevelDatabase _levelDatabase;


        public LevelProgressionService(LevelDatabase levelDatabase)
        {
            _levelDatabase = levelDatabase;
        }

        public void Advance()
        {
            CurrentLevelId = _levelDatabase.GetNextId(CurrentLevelId);
        }
    }
}