#if DEV
using System;
using Project.Scripts.Configs;
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
        private const string PrefsKeyStrengthIndex = "Dev.Match.StrengthIndex";
        private const string PrefsKeySkipFillsBotEnergy = "Dev.Match.SkipFillsBotEnergy";


        public DevSideMode PlayerMode { get; private set; }
        public int PlayerDeckIndex { get; private set; }
        public int? PlayerSeedOverride { get; private set; }
        public DevSideMode OpponentMode { get; private set; }
        public int OpponentDeckIndex { get; private set; }
        public int? OpponentSeedOverride { get; private set; }
        public int StrengthIndex { get; private set; }
        public int StrengthCount => _catalog?.BotStrengths?.Length ?? 0;
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
            StrengthIndex = ClampStrengthIndex(PlayerPrefs.GetInt(PrefsKeyStrengthIndex, 0));
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

        public void SetStrengthIndex(int index)
        {
            StrengthIndex = ClampStrengthIndex(index);
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
            PlayerPrefs.SetInt(PrefsKeyStrengthIndex, StrengthIndex);
            PlayerPrefs.SetInt(PrefsKeySkipFillsBotEnergy, SkipFillsBotEnergy ? 1 : 0);
            PlayerPrefs.Save();
            if (_debugConfig.LogDevOpponentOptions)
            {
                Debug.Log($"[DevMatch] Saved player={PlayerMode}/deck={GetDeckDisplayName(PlayerDeckIndex)}/" +
                          $"seed={SeedLabel(PlayerSeedOverride)} opponent={OpponentMode}/deck={GetDeckDisplayName(OpponentDeckIndex)}/" +
                          $"seed={SeedLabel(OpponentSeedOverride)} strength={GetStrengthDisplayName(StrengthIndex)}");
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


        private static DevSideMode ClampMode(int raw)
        {
            return Enum.IsDefined(typeof(DevSideMode), raw) ? (DevSideMode)raw : DevSideMode.PickDeck;
        }

        private int ClampStrengthIndex(int index)
        {
            var count = StrengthCount;
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