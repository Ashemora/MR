#if DEV
using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Services.UISystem;
using Project.Scripts.UI.Dev;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Dev
{
    public class DevLobbyBattleOptionsButtonSpawner
    {
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
        private readonly IDevMatchOverrideService _devMatchOverride;
        private GameObject _buttonObject;
        private Button _button;
        private IDisposable _closeSubscription;
        private bool _isOptionsOpen;


        public DevLobbyBattleOptionsButtonSpawner(UIService uiService, UIConfig uiConfig,
            IDevMatchOverrideService devMatchOverride)
        {
            _uiService = uiService;
            _uiConfig = uiConfig;
            _devMatchOverride = devMatchOverride;
        }

        public void Spawn()
        {
            if (_buttonObject)
                return;

            var prefab = _uiConfig.DevBattleOptionsButtonPrefab;
            if (!prefab)
            {
                Debug.LogError("[DevBattleOptions] Button prefab is not assigned.");
                return;
            }

            var parent = _uiService.GetLayerRoot(UILayer.Popup, SafeAreaMode.ForceApply);
            _buttonObject = UnityEngine.Object.Instantiate(prefab, parent);
            _buttonObject.name = "DevBattleOptionsButton";

            _button = _buttonObject.GetComponent<Button>();
            if (!_button)
            {
                Debug.LogError("[DevBattleOptions] Button prefab has no Button component.");
                UnityEngine.Object.Destroy(_buttonObject);
                _buttonObject = null;
                return;
            }

            _button.onClick.AddListener(OpenOptions);
        }

        public void Despawn()
        {
            _closeSubscription?.Dispose();
            _closeSubscription = null;

            if (_button)
                _button.onClick.RemoveListener(OpenOptions);
            if (_buttonObject)
                UnityEngine.Object.Destroy(_buttonObject);

            _button = null;
            _buttonObject = null;
            _isOptionsOpen = false;
        }


        private void OpenOptions()
        {
            if (_isOptionsOpen)
                return;

            if (!_uiConfig.DevBattleOptionsViewPrefab)
            {
                Debug.LogError("[DevBattleOptions] View prefab is not assigned.");
                return;
            }

            _isOptionsOpen = true;
            OpenOptionsAsync().Forget();
        }

        private async UniTaskVoid OpenOptionsAsync()
        {
            _uiService.RegisterView<DevBattleOptionsView>(_uiConfig.DevBattleOptionsViewPrefab, UILayer.Popup);

            var viewModel = new DevBattleOptionsViewModel(_devMatchOverride);
            _closeSubscription = viewModel.CloseRequested
                .Take(1)
                .Subscribe(_ => CloseOptionsAsync().Forget());

            await _uiService.Show<DevBattleOptionsView, DevBattleOptionsViewModel>(viewModel);
        }

        private async UniTaskVoid CloseOptionsAsync()
        {
            _closeSubscription?.Dispose();
            _closeSubscription = null;

            await _uiService.Hide<DevBattleOptionsView>();
            _uiService.Close<DevBattleOptionsView>();
            _isOptionsOpen = false;
        }
    }
}
#endif