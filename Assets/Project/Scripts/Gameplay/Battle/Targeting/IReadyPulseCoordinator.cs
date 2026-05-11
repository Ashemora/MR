using R3;

namespace Project.Scripts.Gameplay.Battle.Targeting
{
    public interface IReadyPulseCoordinator
    {
        Observable<float> Alpha { get; }
    }
}