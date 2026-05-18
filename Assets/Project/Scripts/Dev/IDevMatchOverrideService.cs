#if DEV
using Project.Scripts.Configs.Battle.Units;

namespace Project.Scripts.Dev
{
    public interface IDevMatchOverrideService
    {
        DevSideMode PlayerMode { get; }
        int PlayerDeckIndex { get; }
        int? PlayerSeedOverride { get; }
        DevSideMode OpponentMode { get; }
        int OpponentDeckIndex { get; }
        int? OpponentSeedOverride { get; }
        int StrengthIndex { get; }
        int StrengthCount { get; }
        int DeckCount { get; }

        string GetStrengthDisplayName(int index);
        string GetDeckDisplayName(int index);
        void SetPlayerMode(DevSideMode mode);
        void SetPlayerDeckIndex(int index);
        void SetPlayerSeedOverride(int? seed);
        void SetOpponentMode(DevSideMode mode);
        void SetOpponentDeckIndex(int index);
        void SetOpponentSeedOverride(int? seed);
        void SetStrengthIndex(int index);
        void Save();
        string GetRandomBuildBlockReason();
        bool TryBuildRandomDeck(int seed, out DevDeckSelection selection);
        bool TryGetPickedDeck(int index, out UnitDeckConfig deck);
    }
}
#endif