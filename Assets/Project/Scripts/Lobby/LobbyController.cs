using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Lobby.Options;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.Audio.Settings;
using Project.Scripts.Services.UISystem;
using R3;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Lobby
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private LobbyView _view;


        private IAppStateMachine _appStateMachine;
        private UIService _uiService;
        private UIConfig _uiConfig;
        private IAudioSettingsService _audioSettings;

        private readonly CompositeDisposable _optionsDisposables = new();
        private bool _isOptionsOpen;


        private void OnDestroy()
        {
            _optionsDisposables.Dispose();
        }


        [Inject]
        public void Construct(IAppStateMachine appStateMachine, UIService uiService, UIConfig uiConfig,
            IAudioSettingsService audioSettings)
        {
            _appStateMachine = appStateMachine;
            _uiService = uiService;
            _uiConfig = uiConfig;
            _audioSettings = audioSettings;
        }


        private void Start()
        {
            if (_uiConfig.OptionsViewPrefab)
                _uiService.RegisterView<OptionsView>(_uiConfig.OptionsViewPrefab, UILayer.Popup);

            _view.Bind(StartBattle, OpenOptions);
        }


        private void StartBattle()
        {
            _appStateMachine.StartBattleAsync().Forget();
        }

        private void OpenOptions()
        {
            if (_isOptionsOpen)
                return;
            
            if (!_uiConfig.OptionsViewPrefab)
            {
                Debug.LogError("OptionsViewPrefab is not assigned in UIConfig");
                return;
            }

            _isOptionsOpen = true;
            OpenOptionsAsync().Forget();
        }

        private async UniTaskVoid OpenOptionsAsync()
        {
            var viewModel = new OptionsViewModel(_audioSettings);

            viewModel.CloseRequested
                .Take(1)
                .Subscribe(_ => CloseOptionsAsync().Forget())
                .AddTo(_optionsDisposables);

            await _uiService.Show<OptionsView, OptionsViewModel>(viewModel);
        }

        private async UniTaskVoid CloseOptionsAsync()
        {
            await _uiService.Hide<OptionsView>();
            _uiService.Close<OptionsView>();
            _isOptionsOpen = false;
        }
    }
}