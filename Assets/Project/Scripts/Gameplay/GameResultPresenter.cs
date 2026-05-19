using Cysharp.Threading.Tasks;
using Project.Scripts.Gameplay.Battle;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.UI.Windows;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.UISystem;

namespace Project.Scripts.Gameplay
{
    public class GameResultPresenter
    {
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
        private readonly IMoveCounterService _moveCounter;
        private readonly IAppStateMachine _appStateMachine;
        private readonly EffectiveBotConfigProvider _effectiveBotConfigProvider;


        public GameResultPresenter(UIService uiService, UIConfig uiConfig, IMoveCounterService moveCounter,
            IAppStateMachine appStateMachine, EffectiveBotConfigProvider effectiveBotConfigProvider)
        {
            _uiService = uiService;
            _uiConfig = uiConfig;
            _moveCounter = moveCounter;
            _appStateMachine = appStateMachine;
            _effectiveBotConfigProvider = effectiveBotConfigProvider;
        }


        public void Initialize()
        {
            _uiService.RegisterView<WinView>(_uiConfig.WinViewPrefab, UILayer.Popup);
            _uiService.RegisterView<LoseView>(_uiConfig.LoseViewPrefab, UILayer.Popup);
        }

        public async UniTask ShowWin(bool isFlawless)
        {
            var bot = _effectiveBotConfigProvider.BotStrengthConfig;
            var viewModel = new WinViewModel(_moveCounter, _appStateMachine,
                bot ? bot.OpponentName : string.Empty, isFlawless,
                () => _uiService.Close<WinView>());
            await _uiService.Show<WinView, WinViewModel>(viewModel);
        }

        public async UniTask ShowLose()
        {
            var bot = _effectiveBotConfigProvider.BotStrengthConfig;
            var viewModel = new LoseViewModel(_moveCounter, _appStateMachine,
                bot ? bot.OpponentName : string.Empty,
                () => _uiService.Close<LoseView>());
            await _uiService.Show<LoseView, LoseViewModel>(viewModel);
        }
    }
}