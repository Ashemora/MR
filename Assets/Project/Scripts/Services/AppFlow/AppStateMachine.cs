using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Constants;
using Project.Scripts.Gameplay.UI.Loading;
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
        private readonly IBattleSessionProvider _battleSessionProvider;
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
#if DEV
        private readonly IDevMatchOverrideService _devMatchOverride;
#endif


        public AppStateMachine(ISceneLoadingService sceneLoadingService,
            IBattleSessionProvider battleSessionProvider, UIService uiService, UIConfig uiConfig
#if DEV
            , IDevMatchOverrideService devMatchOverride
#endif
        )
        {
            _sceneLoadingService = sceneLoadingService;
            _battleSessionProvider = battleSessionProvider;
            _uiService = uiService;
            _uiConfig = uiConfig;
#if DEV
            _devMatchOverride = devMatchOverride;
#endif
        }

        public async UniTask EnterLobbyAsync(ILoadingPresenter loadingPresenter = null, int activationDelayMilliseconds = 0)
        {
            Current = AppState.Lobby;
            _battleSessionProvider.Clear();
            _uiService.CloseAll();
#if DEV
            _uiService.CleanupDevGameplayButtons();
#endif
            await _sceneLoadingService.LoadSceneAsync(SceneNames.Lobby, loadingPresenter, activationDelayMilliseconds);
        }

        public async UniTask StartBattleAsync()
        {
            Current = AppState.LoadingGameplay;
            _uiService.CloseAll();
#if DEV
            _uiService.CleanupDevGameplayButtons();
#endif

            var playerSeed = Random.Range(int.MinValue, int.MaxValue);
            var opponentSeed = Random.Range(int.MinValue, int.MaxValue);
#if DEV
            if (_devMatchOverride.PlayerSeedOverride.HasValue)
                playerSeed = _devMatchOverride.PlayerSeedOverride.Value;
            if (_devMatchOverride.OpponentSeedOverride.HasValue)
                opponentSeed = _devMatchOverride.OpponentSeedOverride.Value;

            Debug.Log($"[Battle] player(mode={_devMatchOverride.PlayerMode} seed={playerSeed}) " +
                      $"opponent(mode={_devMatchOverride.OpponentMode} seed={opponentSeed}) " +
                      $"strength={_devMatchOverride.GetStrengthDisplayName(_devMatchOverride.StrengthIndex)}");
#endif
            _battleSessionProvider.SetCurrent(new BattleSession(playerSeed, opponentSeed));

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
#if DEV
            _uiService.CleanupDevGameplayButtons();
#endif

            _uiService.RegisterView<GameplayLoadingView>(_uiConfig.GameplayLoadingViewPrefab, UILayer.System);
            var loadingView = await _uiService.Show<GameplayLoadingView, GameplayLoadingViewModel>(
                new GameplayLoadingViewModel());

            await _sceneLoadingService.LoadSceneAsync(SceneNames.Lobby, loadingView);
            _uiService.Close<GameplayLoadingView>();
        }
    }
}