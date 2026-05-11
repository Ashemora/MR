namespace Project.Scripts.Shared.MoveBar
{
    public interface IMoveBarEngine
    {
        MoveBarSnapshot Snapshot { get; }
        void Initialize(MoveBarSettings settings);
        bool Tick(float deltaTime);
        bool TryConsume();
    }
}