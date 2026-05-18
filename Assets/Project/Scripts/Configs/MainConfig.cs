using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Configs.Battle.Energy;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Battle.Visuals;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Configs.UI;
using Project.Scripts.Services.Audio.AudioSystem.Configs;
using UnityEngine;
#if DEV
using Project.Scripts.Dev;
#endif

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(fileName = "MainConfig", menuName = "Configs/MainConfig")]
    public class MainConfig : ScriptableObject
    {
        [Tooltip("Музыкальные треки: привязка музыки")]
        [SerializeField] private AudioMusicConfig _audioMusicConfig;

        [Tooltip("Звуковые эффекты: привязка SFX-клипов")]
        [SerializeField] private AudioSFXConfig _audioSFXConfig;

        [Tooltip("Настройки ввода: чувствительность свайпов и касаний")]
        [SerializeField] private InputConfig _inputConfig;

        [Tooltip("Визуальные параметры игрового поля: отступы, рамка, маска и prefab тайла")]
        [SerializeField] private BoardConfig _boardConfig;

        [Tooltip("Правила сетки тайлов: размерность доски")]
        [SerializeField] private GridConfig _gridConfig;

        [Tooltip("Анимации поля: скорости падения, слияния и появления тайлов")]
        [SerializeField] private BoardAnimationConfig _boardAnimationConfig;

        [Tooltip("Анимации боя: длительности ударов, хилов и эффектов на юнитах")]
        [SerializeField] private BattleAnimationConfig _battleAnimationConfig;

        [Tooltip("Последовательности post-result презентации: шаги победы и поражения с настраиваемыми паузами")]
        [SerializeField] private GameResultSequenceConfig _gameResultSequenceConfig;

        [Tooltip("Формула энергии от матчей: множители за каскады, L/T-образные совпадения и мультиматчи")]
        [SerializeField] private CascadeEnergyConfig _cascadeEnergyConfig;

        [Tooltip("Специальные тайлы: пороги активации и параметры бомб, линий и шторма")]
        [SerializeField] private SpecialTileConfig _specialTileConfig;

        [Tooltip("База уровней: список всех уровней с их конфигами")]
        [SerializeField] private LevelDatabase _levelDatabase;

        [Tooltip("Боевые настройки игрока: базовая дека до появления выбора в лобби")]
        [SerializeField] private PlayerBattleConfig _playerBattleConfig;

        [Tooltip("Настройки UI: шрифты, цвета, анимации интерфейса")]
        [SerializeField] private UIConfig _uiConfig;

        [Tooltip("Объявления над доской: prefab, позиция и анимация обычных сообщений и countdown")]
        [SerializeField] private BoardAnnouncementConfig _boardAnnouncementConfig;

        [Tooltip("Полоска ходов: максимум ходов, правила пополнения и отображения")]
        [SerializeField] private MoveBarConfig _moveBarConfig;

        [Tooltip("Визуальные параметры боя: позиции и размеры слотов аватаров и героев")]
        [SerializeField] private BattleWorldLayoutConfig _battleWorldLayoutConfig;

        [Tooltip("Фазовые профили боевого поля: compressed/full snapshots, переходы и смещение доски в Hero-фазе")]
        [SerializeField] private BattleFieldLayoutConfig _battleFieldLayoutConfig;

        [Tooltip("Единые настройки экранной компоновки Gameplay: aspect, safe area, камера, зоны UI и world-content")]
        [SerializeField] private GameplayScreenLayoutConfig _gameplayScreenLayoutConfig;

        [Tooltip("Палитра цветов тайлов: цветовые значения для каждого типа тайла")]
        [SerializeField] private TileKindPaletteConfig _tileKindPaletteConfig;

        [Tooltip("Раскладка слотов: порядок цветов ячеек героев и аватара на поле")]
        [SerializeField] private SlotLayoutConfig _slotLayoutConfig;

        [Tooltip("Фазы боя: число раундов, длительности Match/Hero Phase и правило переноса энергии между раундами")]
        [SerializeField] private BattleFlowConfig _battleFlowConfig;

        [Tooltip("Фаза сжигания: урон и спец-анимации этого модуля")]
        [SerializeField] private BurndownConfig _burndownConfig;

        [Tooltip("Смерть юнитов: длительности анимаций и задержки при гибели героев и аватаров")]
        [SerializeField] private UnitDeathConfig _unitDeathConfig;

        [Tooltip("Автозаполнение энергии: интервал тиков и количество энергии за тик для всех юнитов")]
        [SerializeField] private AutoEnergyConfig _autoEnergyConfig;

        [Tooltip("Эскалация: множители параметров (скорость авто-энергии, энергия от каскадов), вступающие в силу при наступлении фазы эскалации")]
        [SerializeField] private EscalationConfig _escalationConfig;

        [Tooltip("Подсказки: таймер, подсветка и пульсация при бездействии игрока")]
        [SerializeField] private HintConfig _hintConfig;

        [Tooltip("Отладка: флаги логирования для разработчиков (не влияет на геймплей)")]
        [SerializeField] private DebugConfig _debugConfig;

#if DEV
        [Tooltip("DEV-only каталог для random opponent режима в лобби")]
        [SerializeField] private DevUnitCatalogConfig _devUnitCatalogConfig;
#endif


        public AudioMusicConfig AudioMusicConfig => _audioMusicConfig;
        public AudioSFXConfig AudioSFXConfig => _audioSFXConfig;
        public InputConfig InputConfig => _inputConfig;
        public BoardConfig BoardConfig => _boardConfig;
        public GridConfig GridConfig => _gridConfig;
        public BoardAnimationConfig BoardAnimationConfig => _boardAnimationConfig;
        public BattleAnimationConfig BattleAnimationConfig => _battleAnimationConfig;
        public GameResultSequenceConfig GameResultSequenceConfig => _gameResultSequenceConfig;
        public CascadeEnergyConfig CascadeEnergyConfig => _cascadeEnergyConfig;
        public SpecialTileConfig SpecialTileConfig => _specialTileConfig;
        public LevelDatabase LevelDatabase => _levelDatabase;
        public PlayerBattleConfig PlayerBattleConfig => _playerBattleConfig;
        public UIConfig UIConfig => _uiConfig;
        public BoardAnnouncementConfig BoardAnnouncementConfig => _boardAnnouncementConfig;
        public MoveBarConfig MoveBarConfig => _moveBarConfig;
        public BattleWorldLayoutConfig BattleWorldLayoutConfig => _battleWorldLayoutConfig;
        public BattleFieldLayoutConfig BattleFieldLayoutConfig => _battleFieldLayoutConfig;
        public GameplayScreenLayoutConfig GameplayScreenLayoutConfig => _gameplayScreenLayoutConfig;
        public TileKindPaletteConfig TileKindPaletteConfig => _tileKindPaletteConfig;
        public SlotLayoutConfig SlotLayoutConfig => _slotLayoutConfig;
        public BattleFlowConfig BattleFlowConfig => _battleFlowConfig;
        public BurndownConfig BurndownConfig => _burndownConfig;
        public UnitDeathConfig UnitDeathConfig => _unitDeathConfig;
        public AutoEnergyConfig AutoEnergyConfig => _autoEnergyConfig;
        public EscalationConfig EscalationConfig => _escalationConfig;
        public HintConfig HintConfig => _hintConfig;
        public DebugConfig DebugConfig => _debugConfig;
#if DEV
        public DevUnitCatalogConfig DevUnitCatalogConfig => _devUnitCatalogConfig;
#endif
    }
}