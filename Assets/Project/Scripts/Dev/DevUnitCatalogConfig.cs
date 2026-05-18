#if DEV
using System.Collections.Generic;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Utils.Buttons;
using UnityEngine;

namespace Project.Scripts.Dev
{
    [CreateAssetMenu(fileName = "DevUnitCatalogConfig", menuName = "Configs/Dev/Unit Catalog (DEV)")]
    public class DevUnitCatalogConfig : ScriptableObject
    {
#if UNITY_EDITOR
        private const string AvatarsFolderPath = "Assets/Project/Configs/Battle/Avatars";
        private const string HeroesFolderPath = "Assets/Project/Configs/Battle/Heroes";
        private const string BotStrengthsFolderPath = "Assets/Project/Configs/Battle/Bots";
        private const string DecksFolderPath = "Assets/Project/Configs/Battle/Decks";
#endif

        [Header("Pools for random opponent generation")]
        [SerializeField] private AvatarConfig[] _avatars;

        [Button(nameof(FillAvatarsFromFolder), drawField: false)]
        [SerializeField] private bool _fillAvatarsFromFolderButton;

        [SerializeField] private HeroConfig[] _heroes;

        [Button(nameof(FillHeroesFromFolder), drawField: false)]
        [SerializeField] private bool _fillHeroesFromFolderButton;

        [Header("Bot strength presets")]
        [SerializeField] private BotStrengthEntry[] _botStrengths;

        [Button(nameof(FillBotStrengthsFromFolder), drawField: false)]
        [SerializeField] private bool _fillBotStrengthsFromFolderButton;

        [Header("Picked-deck catalog")]
        [SerializeField] private UnitDeckConfig[] _decks;

        [Button(nameof(FillDecksFromFolder), drawField: false)]
        [SerializeField] private bool _fillDecksFromFolderButton;


        public AvatarConfig[] Avatars => _avatars;
        public HeroConfig[] Heroes => _heroes;
        public BotStrengthEntry[] BotStrengths => _botStrengths;
        public UnitDeckConfig[] Decks => _decks;


        private void FillAvatarsFromFolder()
        {
#if UNITY_EDITOR
            _avatars = LoadAssetsFromFolder<AvatarConfig>(AvatarsFolderPath);
            SaveEditorChanges();
#else
            Debug.LogWarning("FillAvatarsFromFolder is only available in the Unity Editor.", this);
#endif
        }

        private void FillHeroesFromFolder()
        {
#if UNITY_EDITOR
            _heroes = LoadAssetsFromFolder<HeroConfig>(HeroesFolderPath);
            SaveEditorChanges();
#else
            Debug.LogWarning("FillHeroesFromFolder is only available in the Unity Editor.", this);
#endif
        }

        private void FillBotStrengthsFromFolder()
        {
#if UNITY_EDITOR
            var botConfigs = LoadAssetsFromFolder<BotConfig>(BotStrengthsFolderPath);
            var entries = new BotStrengthEntry[botConfigs.Length];
            for (var i = 0; i < botConfigs.Length; i++)
                entries[i] = new BotStrengthEntry(CreateBotStrengthDisplayName(botConfigs[i]), botConfigs[i]);

            _botStrengths = entries;
            SaveEditorChanges();
#else
            Debug.LogWarning("FillBotStrengthsFromFolder is only available in the Unity Editor.", this);
#endif
        }

        private void FillDecksFromFolder()
        {
#if UNITY_EDITOR
            _decks = LoadAssetsFromFolder<UnitDeckConfig>(DecksFolderPath);
            SaveEditorChanges();
#else
            Debug.LogWarning("FillDecksFromFolder is only available in the Unity Editor.", this);
#endif
        }

#if UNITY_EDITOR
        private static T[] LoadAssetsFromFolder<T>(string folderPath) where T : Object
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            var assets = new List<T>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset)
                    assets.Add(asset);
            }

            assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return assets.ToArray();
        }

        private static string CreateBotStrengthDisplayName(BotConfig config)
        {
            if (!config)
                return string.Empty;

            return config.name.StartsWith("Bot_")
                ? config.name.Substring("Bot_".Length)
                : config.name;
        }

        private void SaveEditorChanges()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }

    [System.Serializable]
    public struct BotStrengthEntry
    {
        [SerializeField] private string _displayName;
        [SerializeField] private BotConfig _config;


        public BotStrengthEntry(string displayName, BotConfig config)
        {
            _displayName = displayName;
            _config = config;
        }


        public string DisplayName => _displayName;
        public BotConfig Config => _config;
    }
}
#endif