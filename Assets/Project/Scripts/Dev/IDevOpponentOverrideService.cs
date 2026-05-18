#if DEV
namespace Project.Scripts.Dev
{
    public interface IDevOpponentOverrideService
    {
        DevOpponentMode Mode { get; }
        int StrengthIndex { get; }
        int? OpponentSeedOverride { get; }
        int StrengthCount { get; }

        string GetStrengthDisplayName(int index);
        void SetMode(DevOpponentMode mode);
        void SetStrengthIndex(int index);
        void SetOpponentSeedOverride(int? seed);
        void Save();
        string GetBuildBlockReason();
        bool TryBuildOpponent(int opponentSeed, out DevOpponentSelection selection);
    }
}
#endif