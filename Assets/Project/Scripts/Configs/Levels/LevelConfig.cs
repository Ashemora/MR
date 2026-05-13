using System.Collections.Generic;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Board;
using Project.Scripts.Utils.Buttons;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Scripts.Configs.Levels
{
    public enum LevelGoalType
    {
        DamageBased
    }

    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level")]
        [Tooltip("Уникальный числовой идентификатор уровня, используется для поиска в LevelDatabase")]
        [SerializeField] private int _levelId = 1;

        [Header("Board")]
        [Tooltip("Типы тайлов, доступные для обычных совпадений на этой доске")]
        [SerializeField] private TileConfig[] _regularTiles;

        [Tooltip("Типы тайлов, доступные как специальные (улучшенные) тайлы на этой доске")]
        [SerializeField] private TileConfig[] _specialTiles;

        [Header("Combat")]
        [Tooltip("Тип условия победы - DamageBased означает, что игрок побеждает, снизив HP врага до нуля")]
        [SerializeField] private LevelGoalType _goalType = LevelGoalType.DamageBased;

        // TODO: replace with player loadout from lobby
        [Space(5)]
        [Tooltip("Конфиг аватара игрока для этого уровня")]
        [SerializeField] private AvatarConfig _playerAvatarConfig;

        // TODO: replace with opponent data from matchmaking
        [Tooltip("Конфиг аватара противника для этого уровня")]
        [SerializeField] private AvatarConfig _enemyAvatarConfig;

        // TODO: replace with lobby loadout when PvP is implemented
        [Header("Heroes (temporary - will be replaced by lobby loadout)")]
        [Tooltip("Четыре конфига героев для стороны игрока (пустой слот = null)")]
        [SerializeField] private HeroConfig[] _playerHeroes = new HeroConfig[4];

        [Tooltip("Редактор: якорь для кнопки подбора команды; в рантайме не используется")]
        [Button(nameof(RandomPlayerTeam), ButtonPosition.Above, false)]
        [SerializeField] private bool _editorPlayerRandomTeamButton;

        [Tooltip("Четыре конфига героев для стороны врага; используется когда BotConfig.RandomHeroSelection = false")]
        [SerializeField] private HeroConfig[] _enemyHeroes = new HeroConfig[4];

        [Tooltip("Редактор: якорь для кнопки подбора команды; в рантайме не используется")]
        [Button(nameof(RandomEnemyTeam), ButtonPosition.Above, false)]
        [SerializeField] private bool _editorEnemyRandomTeamButton;

        // TODO: replace with matchmaking opponent data when lobby is implemented
        [Header("Bot (temporary - will be replaced by lobby opponent)")]
        [Tooltip("Настройки бота для этого уровня; null означает отсутствие бота (зарезервировано для реального PvP)")]
        [SerializeField] private BotConfig _botConfig;


        public int LevelId => _levelId;
        public TileConfig[] RegularTiles => _regularTiles;
        public TileConfig[] SpecialTiles => _specialTiles;
        public LevelGoalType GoalType => _goalType;
        public AvatarConfig PlayerAvatarConfig => _playerAvatarConfig;
        public AvatarConfig EnemyAvatarConfig => _enemyAvatarConfig;
        public HeroConfig[] PlayerHeroes => _playerHeroes;
        public HeroConfig[] EnemyHeroes => _enemyHeroes;
        public BotConfig BotConfig => _botConfig;

        private void RandomPlayerTeam()
        {
#if UNITY_EDITOR
            ApplyRandomHeroFill(ref _playerHeroes);
#endif
        }

        private void RandomEnemyTeam()
        {
#if UNITY_EDITOR
            ApplyRandomHeroFill(ref _enemyHeroes);
#endif
        }

#if UNITY_EDITOR
        private const string HeroConfigsEditorFolder = "Assets/Project/Configs/Battle/Heroes";

        private static void ApplyRandomHeroFill(ref HeroConfig[] slots)
        {
            var heroes = LoadAllHeroConfigsInEditorFolder();
            if (heroes.Length == 0)
                return;

            if (slots == null || slots.Length != 4)
                System.Array.Resize(ref slots, 4);

            for (var i = 0; i < 4; i++)
                slots[i] = heroes[Random.Range(0, heroes.Length)];
        }

        private static HeroConfig[] LoadAllHeroConfigsInEditorFolder()
        {
            var guids = AssetDatabase.FindAssets("t:HeroConfig", new[] { HeroConfigsEditorFolder });
            if (guids == null || guids.Length == 0)
                return System.Array.Empty<HeroConfig>();

            var list = new List<HeroConfig>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var hero = AssetDatabase.LoadAssetAtPath<HeroConfig>(path);
                if (hero)
                    list.Add(hero);
            }

            return list.Count == 0 ? System.Array.Empty<HeroConfig>() : list.ToArray();
        }
#endif
    }
}