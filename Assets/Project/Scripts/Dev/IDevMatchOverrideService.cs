#if DEV
using Project.Scripts.Configs.Battle.Bot;
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
        DevBotSelectionMode StrengthMode { get; }
        int StrengthIndex { get; }
        int StrengthCount { get; }
        DevBotSelectionMode StrategyMode { get; }
        int StrategyIndex { get; }
        int StrategyCount { get; }
        int DeckCount { get; }
        bool SkipFillsBotEnergy { get; }

        string GetStrengthDisplayName(int index);
        string GetStrategyDisplayName(int index);
        string GetDeckDisplayName(int index);
        void SetPlayerMode(DevSideMode mode);
        void SetPlayerDeckIndex(int index);
        void SetPlayerSeedOverride(int? seed);
        void SetOpponentMode(DevSideMode mode);
        void SetOpponentDeckIndex(int index);
        void SetOpponentSeedOverride(int? seed);
        void SetStrengthMode(DevBotSelectionMode mode);
        void SetStrengthIndex(int index);
        void SetStrategyMode(DevBotSelectionMode mode);
        void SetStrategyIndex(int index);
        void SetSkipFillsBotEnergy(bool value);
        void Save();
        string GetRandomBuildBlockReason();
        bool TryBuildRandomDeck(int seed, out DevDeckSelection selection);
        bool TryGetPickedDeck(int index, out UnitDeckConfig deck);
        bool TryGetPickedStrength(int index, out BotStrengthConfig strength);
        bool TryPickRandomStrength(int seed, out BotStrengthConfig strength);
        bool TryGetPickedStrategy(int index, out BotStrategyConfig strategy);
        bool TryPickRandomStrategy(int seed, out BotStrategyConfig strategy);
    }
}
#endif