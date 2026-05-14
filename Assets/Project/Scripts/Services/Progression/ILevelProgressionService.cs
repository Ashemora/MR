namespace Project.Scripts.Services.Progression
{
    public interface ILevelProgressionService
    {
        int CurrentLevelId { get; }

        void Advance();
    }
}