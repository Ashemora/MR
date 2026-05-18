using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Constants;
using Project.Scripts.Gameplay.UI.Loading;
using Project.Scripts.Services.Progression;
using Project.Scripts.Services.SceneLoading;
using Project.Scripts.Services.UISystem;
using Project.Scripts.Shared.Match;
using UnityEngine;
#if DEV
using Project.Scripts.Dev;
#endif

namespace Project.Scripts.Services.AppFlow
{
    public class AppStateMachine : IAppStateMachine
    {
        public AppState Current { get; private set; } = AppState.Boot;
        
        
        private readonly ISceneLoadingService _sceneLoadingService;
        private readonly ILevelProgressionService _levelProgressionService;
        private readonly IBattleSessionProvider _battleSessionProvider;
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
#if DEV
        private readonly IDevOpponentOverrideService _devOpponentOverride;
#endif


        public AppStateMachine(ISceneLoadingService sceneLoadingService, ILevelProgressionService levelProgressionService,
            IBattleSessionProvider battleSessionProvider, UIService uiService, UIConfig uiConfig
#if DEV
            , IDevOpponentOverrideService devOpponentOverride
#endif
        )
        {
            _sceneLoadingService = sceneLoadingService;
            _levelProgressionService = levelProgressionService;
            _battleSessionProvider = battleSessionProvider;
            _uiService = uiService;
            _uiConfig = uiConfig;
#if DEV
            _devOpponentOverride = devOpponentOverride;
#endif
        }

        public async UniTask EnterLobbyAsync(ILoadingPresenter loadingPresenter = null, int activationDelayMilliseconds = 0)
        {
            Current = AppState.Lobby;
            _battleSessionProvider.Clear();
            _uiService.CloseAll();
            await _sceneLoadingService.LoadSceneAsync(SceneNames.Lobby, loadingPresenter, activationDelayMilliseconds);
        }

        public async UniTask StartBattleAsync()
        {
            Current = AppState.LoadingGameplay;
            _uiService.CloseAll();

            var opponentSeed = Random.Range(int.MinValue, int.MaxValue);
#if DEV
            if (_devOpponentOverride.OpponentSeedOverride.HasValue)
                opponentSeed = _devOpponentOverride.OpponentSeedOverride.Value;

            var strength = _devOpponentOverride.Mode == DevOpponentMode.Random
                ? _devOpponentOverride.GetStrengthDisplayName(_devOpponentOverride.StrengthIndex)
                : "-";
            Debug.Log($"[Battle] opponentSeed={opponentSeed} mode={_devOpponentOverride.Mode} strength={strength}");
#endif
            _battleSessionProvider.SetCurrent(new BattleSession(
                _levelProgressionService.CurrentLevelId,
                opponentSeed));

            _uiService.RegisterView<GameplayLoadingView>(_uiConfig.GameplayLoadingViewPrefab, UILayer.System);
            var loadingView = await _uiService.Show<GameplayLoadingView, GameplayLoadingViewModel>(
                new GameplayLoadingViewModel());

            await _sceneLoadingService.LoadSceneAsync(SceneNames.GamePlay, loadingView);
            Current = AppState.Gameplay;
            _uiService.Close<GameplayLoadingView>();
        }

        public async UniTask ReturnToLobbyAsync()
        {
            Current = AppState.Lobby;
            _battleSessionProvider.Clear();
            _uiService.CloseAll();

            _uiService.RegisterView<GameplayLoadingView>(_uiConfig.GameplayLoadingViewPrefab, UILayer.System);
            var loadingView = await _uiService.Show<GameplayLoadingView, GameplayLoadingViewModel>(
                new GameplayLoadingViewModel());

            await _sceneLoadingService.LoadSceneAsync(SceneNames.Lobby, loadingView);
            _uiService.Close<GameplayLoadingView>();
        }
    }
}