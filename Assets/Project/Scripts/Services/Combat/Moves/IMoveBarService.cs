using Project.Scripts.Shared.MoveBar;

namespace Project.Scripts.Services.Combat.Moves
{
    public interface IMoveBarService
    {
        bool IsEnabled { get; }
        bool HasMoves { get; }
        MoveBarSnapshot GetSnapshot();
        void Initialize();
        void Tick(float deltaTime);
        bool TryConsume();
    }
}