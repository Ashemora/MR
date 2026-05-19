#if DEV
using System;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Battle.Units;
using UnityEngine;

namespace Project.Scripts.Dev
{
    public class DevMatchOverrideService : IDevMatchOverrideService
    {
        private const string PrefsKeyPlayerMode = "Dev.Match.Player.Mode";
        private const string PrefsKeyPlayerDeckIndex = "Dev.Match.Player.DeckIndex";
        private const string PrefsKeyPlayerSeedHas = "Dev.Match.Player.Seed.Has";
        private const string PrefsKeyPlayerSeedValue = "Dev.Match.Player.Seed.Value";
        private const string PrefsKeyOpponentMode = "Dev.Match.Opponent.Mode";
        private const string PrefsKeyOpponentDeckIndex = "Dev.Match.Opponent.DeckIndex";
        private const string PrefsKeyOpponentSeedHas = "Dev.Match.Opponent.Seed.Has";
        private const string PrefsKeyOpponentSeedValue = "Dev.Match.Opponent.Seed.Value";
        private const string PrefsKeyStrengthMode = "Dev.Match.Strength.Mode";
        private const string PrefsKeyStrengthIndex = "Dev.Match.StrengthIndex";
        private const string PrefsKeyStrategyMode = "Dev.Match.Strategy.Mode";
        private const string PrefsKeyStrategyIndex = "Dev.Match.Strategy.Index";
        private const string PrefsKeySkipFillsBotEnergy = "Dev.Match.SkipFillsBotEnergy";


        public DevSideMode PlayerMode { get; private set; }
        public int PlayerDeckIndex { get; private set; }
        public int? PlayerSeedOverride { get; private set; }
        public DevSideMode OpponentMode { get; private set; }
        public int OpponentDeckIndex { get; private set; }
        public int? OpponentSeedOverride { get; private set; }
        public DevBotSelectionMode StrengthMode { get; private set; }
        public int StrengthIndex { get; private set; }
        public int StrengthCount => _catalog?.BotStrengths?.Length ?? 0;
        public DevBotSelectionMode StrategyMode { get; private set; }
        public int StrategyIndex { get; private set; }
        public int StrategyCount => _catalog?.BotStrategies?.Length ?? 0;
        public int DeckCount => _catalog?.Decks?.Length ?? 0;
        public bool SkipFillsBotEnergy { get; private set; }


        private readonly DevUnitCatalogConfig _catalog;
        private readonly DebugConfig _debugConfig;


        public DevMatchOverrideService(DevUnitCatalogConfig catalog, DebugConfig debugConfig)
        {
            _catalog = catalog;
            _debugConfig = debugConfig;
            PlayerMode = ClampMode(PlayerPrefs.GetInt(PrefsKeyPlayerMode, (int)DevSideMode.PickDeck));
            PlayerDeckIndex = ClampDeckIndex(PlayerPrefs.GetInt(PrefsKeyPlayerDeckIndex, 0));
            PlayerSeedOverride = PlayerPrefs.GetInt(PrefsKeyPlayerSeedHas, 0) != 0
                ? PlayerPrefs.GetInt(PrefsKeyPlayerSeedValue, 0)
                : null;
            OpponentMode = ClampMode(PlayerPrefs.GetInt(PrefsKeyOpponentMode, (int)DevSideMode.PickDeck));
            OpponentDeckIndex = ClampDeckIndex(PlayerPrefs.GetInt(PrefsKeyOpponentDeckIndex, 0));
            OpponentSeedOverride = PlayerPrefs.GetInt(PrefsKeyOpponentSeedHas, 0) != 0
                ? PlayerPrefs.GetInt(PrefsKeyOpponentSeedValue, 0)
                : null;
            StrengthMode = ClampSelectionMode(PlayerPrefs.GetInt(PrefsKeyStrengthMode,
                (int)DevBotSelectionMode.Pick));
            StrengthIndex = ClampStrengthIndex(PlayerPrefs.GetInt(PrefsKeyStrengthIndex, 0));
            StrategyMode = ClampSelectionMode(PlayerPrefs.GetInt(PrefsKeyStrategyMode,
                (int)DevBotSelectionMode.Pick));
            StrategyIndex = ClampStrategyIndex(PlayerPrefs.GetInt(PrefsKeyStrategyIndex, 0));
            SkipFillsBotEnergy = PlayerPrefs.GetInt(PrefsKeySkipFillsBotEnergy, 0) != 0;
        }


        public string GetStrengthDisplayName(int index)
        {
            if (!_catalog || _catalog.BotStrengths == null)
                return string.Empty;

            if (index < 0 || index >= _catalog.BotStrengths.Length)
                return string.Empty;

            return _catalog.BotStrengths[index].DisplayName;
        }

        public string GetStrategyDisplayName(int index)
        {
            if (!_catalog || _catalog.BotStrategies == null)
                return string.Empty;

            if (index < 0 || index >= _catalog.BotStrategies.Length)
                return string.Empty;

            return _catalog.BotStrategies[index].DisplayName;
        }

        public string GetDeckDisplayName(int index)
        {
            if (!_catalog || _catalog.Decks == null)
                return string.Empty;

            if (index < 0 || index >= _catalog.Decks.Length)
                return string.Empty;

            var deck = _catalog.Decks[index];
            if (!deck)
                return string.Empty;

            return string.IsNullOrEmpty(deck.DisplayName) ? deck.name : deck.DisplayName;
        }

        public void SetPlayerMode(DevSideMode mode)
        {
            PlayerMode = ClampMode((int)mode);
        }

        public void SetPlayerDeckIndex(int index)
        {
            PlayerDeckIndex = ClampDeckIndex(index);
        }

        public void SetPlayerSeedOverride(int? seed)
        {
            PlayerSeedOverride = seed;
        }

        public void SetOpponentMode(DevSideMode mode)
        {
            OpponentMode = ClampMode((int)mode);
        }

        public void SetOpponentDeckIndex(int index)
        {
            OpponentDeckIndex = ClampDeckIndex(index);
        }

        public void SetOpponentSeedOverride(int? seed)
        {
            OpponentSeedOverride = seed;
        }

        public void SetStrengthMode(DevBotSelectionMode mode)
        {
            StrengthMode = ClampSelectionMode((int)mode);
        }

        public void SetStrengthIndex(int index)
        {
            StrengthIndex = ClampStrengthIndex(index);
        }

        public void SetStrategyMode(DevBotSelectionMode mode)
        {
            StrategyMode = ClampSelectionMode((int)mode);
        }

        public void SetStrategyIndex(int index)
        {
            StrategyIndex = ClampStrategyIndex(index);
        }

        public void SetSkipFillsBotEnergy(bool value)
        {
            SkipFillsBotEnergy = value;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(PrefsKeyPlayerMode, (int)PlayerMode);
            PlayerPrefs.SetInt(PrefsKeyPlayerDeckIndex, PlayerDeckIndex);
            PlayerPrefs.SetInt(PrefsKeyPlayerSeedHas, PlayerSeedOverride.HasValue ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeyPlayerSeedValue, PlayerSeedOverride.GetValueOrDefault());
            PlayerPrefs.SetInt(PrefsKeyOpponentMode, (int)OpponentMode);
            PlayerPrefs.SetInt(PrefsKeyOpponentDeckIndex, OpponentDeckIndex);
            PlayerPrefs.SetInt(PrefsKeyOpponentSeedHas, OpponentSeedOverride.HasValue ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeyOpponentSeedValue, OpponentSeedOverride.GetValueOrDefault());
            PlayerPrefs.SetInt(PrefsKeyStrengthMode, (int)StrengthMode);
            PlayerPrefs.SetInt(PrefsKeyStrengthIndex, StrengthIndex);
            PlayerPrefs.SetInt(PrefsKeyStrategyMode, (int)StrategyMode);
            PlayerPrefs.SetInt(PrefsKeyStrategyIndex, StrategyIndex);
            PlayerPrefs.SetInt(PrefsKeySkipFillsBotEnergy, SkipFillsBotEnergy ? 1 : 0);
            PlayerPrefs.Save();
            if (_debugConfig.LogDevOpponentOptions)
            {
                Debug.Log($"[DevMatch] Saved player={PlayerMode}/deck={GetDeckDisplayName(PlayerDeckIndex)}/" +
                          $"seed={SeedLabel(PlayerSeedOverride)} opponent={OpponentMode}/deck={GetDeckDisplayName(OpponentDeckIndex)}/" +
                          $"seed={SeedLabel(OpponentSeedOverride)} strength={StrengthMode}/{GetStrengthDisplayName(StrengthIndex)} " +
                          $"strategy={StrategyMode}/{GetStrategyDisplayName(StrategyIndex)}");
            }
        }

        public string GetRandomBuildBlockReason()
        {
            if (!_catalog)
                return "DevUnitCatalogConfig is not assigned";

            if (_catalog.Avatars == null || _catalog.Avatars.Length == 0)
                return "avatar pool is empty";

            if (_catalog.Heroes == null || _catalog.Heroes.Length == 0)
                return "hero pool is empty";

            return string.Empty;
        }

        public bool TryBuildRandomDeck(int seed, out DevDeckSelection selection)
        {
            selection = default;
            if (false == string.IsNullOrEmpty(GetRandomBuildBlockReason()))
                return false;

            var random = new System.Random(seed);
            var avatar = _catalog.Avatars[random.Next(_catalog.Avatars.Length)];
            var heroes = new HeroConfig[4];
            for (var i = 0; i < heroes.Length; i++)
                heroes[i] = _catalog.Heroes[random.Next(_catalog.Heroes.Length)];

            selection = new DevDeckSelection(avatar, heroes);

            return true;
        }

        public bool TryGetPickedDeck(int index, out UnitDeckConfig deck)
        {
            deck = null;
            if (!_catalog || _catalog.Decks == null || _catalog.Decks.Length == 0)
                return false;

            if (index < 0 || index >= _catalog.Decks.Length)
                return false;

            deck = _catalog.Decks[index];

            return deck;
        }

        public bool TryGetPickedStrength(int index, out BotStrengthConfig strength)
        {
            strength = null;
            if (!_catalog || _catalog.BotStrengths == null || _catalog.BotStrengths.Length == 0)
                return false;

            if (index < 0 || index >= _catalog.BotStrengths.Length)
                return false;

            strength = _catalog.BotStrengths[index].Config;

            return strength;
        }

        public bool TryPickRandomStrength(int seed, out BotStrengthConfig strength)
        {
            strength = null;
            if (!_catalog || _catalog.BotStrengths == null || _catalog.BotStrengths.Length == 0)
                return false;

            var random = new System.Random(seed);
            strength = _catalog.BotStrengths[random.Next(_catalog.BotStrengths.Length)].Config;

            return strength;
        }

        public bool TryGetPickedStrategy(int index, out BotStrategyConfig strategy)
        {
            strategy = null;
            if (!_catalog || _catalog.BotStrategies == null || _catalog.BotStrategies.Length == 0)
                return false;

            if (index < 0 || index >= _catalog.BotStrategies.Length)
                return false;

            strategy = _catalog.BotStrategies[index].Config;

            return strategy;
        }

        public bool TryPickRandomStrategy(int seed, out BotStrategyConfig strategy)
        {
            strategy = null;
            if (!_catalog || _catalog.BotStrategies == null || _catalog.BotStrategies.Length == 0)
                return false;

            var random = new System.Random(seed);
            strategy = _catalog.BotStrategies[random.Next(_catalog.BotStrategies.Length)].Config;

            return strategy;
        }


        private static DevSideMode ClampMode(int raw)
        {
            return Enum.IsDefined(typeof(DevSideMode), raw) ? (DevSideMode)raw : DevSideMode.PickDeck;
        }

        private static DevBotSelectionMode ClampSelectionMode(int raw)
        {
            return Enum.IsDefined(typeof(DevBotSelectionMode), raw)
                ? (DevBotSelectionMode)raw
                : DevBotSelectionMode.Pick;
        }

        private int ClampStrengthIndex(int index)
        {
            var count = StrengthCount;
            
            return count > 0
                ? Mathf.Clamp(index, 0, count - 1)
                : 0;
        }

        private int ClampStrategyIndex(int index)
        {
            var count = StrategyCount;
            
            return count > 0
                ? Mathf.Clamp(index, 0, count - 1)
                : 0;
        }

        private int ClampDeckIndex(int index)
        {
            var count = DeckCount;
            
            return count > 0
                ? Mathf.Clamp(index, 0, count - 1)
                : 0;
        }

        private static string SeedLabel(int? seed)
        {
            return seed.HasValue ? seed.Value.ToString() : "random";
        }
    }
}
#endif