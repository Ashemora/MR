using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Board;
using UnityEngine;

namespace Project.Scripts.Configs.Levels
{
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
        [Tooltip("Дека противника для этого уровня")]
        [SerializeField] private UnitDeckConfig _opponentUnitDeck;

        [Header("Bot")]
        [Tooltip("Настройки бота для этого уровня; null означает отсутствие бота (зарезервировано для реального PvP)")]
        [SerializeField] private BotConfig _botConfig;


        public int LevelId => _levelId;
        public TileConfig[] RegularTiles => _regularTiles;
        public TileConfig[] SpecialTiles => _specialTiles;
        public UnitDeckConfig OpponentUnitDeck => _opponentUnitDeck;
        public BotConfig BotConfig => _botConfig;
    }
}