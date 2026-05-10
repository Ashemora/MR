using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Progression;
using Project.Scripts.Services.UISystem;

namespace Project.Scripts.Gameplay
{
    public class GameResultPresenter
    {
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
        private readonly IMoveCounterService _moveCounter;
        private readonly ILevelProgressionService _progression;
        private readonly LevelConfig _levelConfig;


        public GameResultPresenter(
            UIService uiService,
            UIConfig uiConfig,
            IMoveCounterService moveCounter,
            ILevelProgressionService progression,
            LevelConfig levelConfig)
        {
            _uiService = uiService;
            _uiConfig = uiConfig;
            _moveCounter = moveCounter;
            _progression = progression;
            _levelConfig = levelConfig;
        }


        public void Initialize()
        {
            _uiService.RegisterView<WinView>(_uiConfig.WinViewPrefab, UILayer.Popup);
            _uiService.RegisterView<LoseView>(_uiConfig.LoseViewPrefab, UILayer.Popup);
        }

        public async UniTask ShowWin(bool isFlawless)
        {
            var bot = _levelConfig.BotConfig;
            var viewModel = new WinViewModel(_moveCounter, _progression,
                _levelConfig.LevelId,
                bot ? bot.OpponentName : string.Empty,
                isFlawless,
                () => _uiService.Close<WinView>());
            await _uiService.Show<WinView, WinViewModel>(viewModel);
        }

        public async UniTask ShowLose()
        {
            var bot = _levelConfig.BotConfig;
            var viewModel = new LoseViewModel(_moveCounter, _progression,
                _levelConfig.LevelId,
                bot ? bot.OpponentName : string.Empty,
                () => _uiService.Close<LoseView>());
            await _uiService.Show<LoseView, LoseViewModel>(viewModel);
        }
    }
}