#if DEV
using System;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle.Units;
using UnityEngine;

namespace Project.Scripts.Dev
{
    public class DevOpponentOverrideService : IDevOpponentOverrideService
    {
        private const string PrefsKeyMode = "Dev.Opponent.Mode";
        private const string PrefsKeyStrengthIndex = "Dev.Opponent.StrengthIndex";
        private const string PrefsKeyOpponentSeedHas = "Dev.Opponent.Seed.Has";
        private const string PrefsKeyOpponentSeedValue = "Dev.Opponent.Seed.Value";


        public DevOpponentMode Mode { get; private set; }
        public int StrengthIndex { get; private set; }
        public int? OpponentSeedOverride { get; private set; }
        public int StrengthCount => _catalog?.BotStrengths?.Length ?? 0;
        
        
        private readonly DevUnitCatalogConfig _catalog;
        private readonly DebugConfig _debugConfig;


        public DevOpponentOverrideService(DevUnitCatalogConfig catalog, DebugConfig debugConfig)
        {
            _catalog = catalog;
            _debugConfig = debugConfig;
            Mode = (DevOpponentMode)PlayerPrefs.GetInt(PrefsKeyMode, (int)DevOpponentMode.Config);
            StrengthIndex = ClampStrengthIndex(PlayerPrefs.GetInt(PrefsKeyStrengthIndex, 0));
            OpponentSeedOverride = PlayerPrefs.GetInt(PrefsKeyOpponentSeedHas, 0) != 0
                ? PlayerPrefs.GetInt(PrefsKeyOpponentSeedValue, 0)
                : null;
        }


        public string GetStrengthDisplayName(int index)
        {
            if (!_catalog || _catalog.BotStrengths == null)
                return string.Empty;

            if (index < 0 || index >= _catalog.BotStrengths.Length)
                return string.Empty;

            return _catalog.BotStrengths[index].DisplayName;
        }

        public void SetMode(DevOpponentMode mode)
        {
            Mode = Enum.IsDefined(typeof(DevOpponentMode), mode) ? mode : DevOpponentMode.Config;
        }

        public void SetStrengthIndex(int index)
        {
            StrengthIndex = ClampStrengthIndex(index);
        }

        public void SetOpponentSeedOverride(int? seed)
        {
            OpponentSeedOverride = seed;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(PrefsKeyMode, (int)Mode);
            PlayerPrefs.SetInt(PrefsKeyStrengthIndex, StrengthIndex);
            PlayerPrefs.SetInt(PrefsKeyOpponentSeedHas, OpponentSeedOverride.HasValue ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeyOpponentSeedValue, OpponentSeedOverride.GetValueOrDefault());
            PlayerPrefs.Save();
            if (_debugConfig.LogDevOpponentOptions)
            {
                Debug.Log($"[DevOpponent] Saved mode={Mode} strength={GetStrengthDisplayName(StrengthIndex)} " +
                          $"opponentSeed={(OpponentSeedOverride.HasValue ? OpponentSeedOverride.Value.ToString() : "random")}");
            }
        }

        public string GetBuildBlockReason()
        {
            if (Mode != DevOpponentMode.Random)
                return $"mode is {Mode}";

            if (!_catalog)
                return "DevUnitCatalogConfig is not assigned";

            if (_catalog.Avatars == null || _catalog.Avatars.Length == 0)
                return "avatar pool is empty";

            if (_catalog.Heroes == null || _catalog.Heroes.Length == 0)
                return "hero pool is empty";

            if (_catalog.BotStrengths == null || _catalog.BotStrengths.Length == 0)
                return "bot strengths are empty";

            return string.Empty;
        }

        public bool TryBuildOpponent(int opponentSeed, out DevOpponentSelection selection)
        {
            selection = default;
            if (false == string.IsNullOrEmpty(GetBuildBlockReason()))
                return false;

            var random = new System.Random(opponentSeed);
            var avatar = _catalog.Avatars[random.Next(_catalog.Avatars.Length)];
            var heroes = new HeroConfig[4];
            for (var i = 0; i < heroes.Length; i++)
                heroes[i] = _catalog.Heroes[random.Next(_catalog.Heroes.Length)];

            var strengthIndex = ClampStrengthIndex(StrengthIndex);
            var botConfig = _catalog.BotStrengths[strengthIndex].Config;
            selection = new DevOpponentSelection(avatar, heroes, botConfig);

            return true;
        }


        private int ClampStrengthIndex(int index)
        {
            var count = StrengthCount;
            return count > 0
                ? Mathf.Clamp(index, 0, count - 1)
                : 0;
        }
    }
}
#endif