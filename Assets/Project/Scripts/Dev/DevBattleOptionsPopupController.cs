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
    public class DevBattleOptionsPopupController
    {
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
        private readonly IDevMatchOverrideService _devMatchOverride;
        private Button _button;
        private IDisposable _closeSubscription;
        private bool _isOptionsOpen;


        public DevBattleOptionsPopupController(UIService uiService, UIConfig uiConfig,
            IDevMatchOverrideService devMatchOverride)
        {
            _uiService = uiService;
            _uiConfig = uiConfig;
            _devMatchOverride = devMatchOverride;
        }

        public void Bind(GameObject buttonObject)
        {
            if (!buttonObject)
            {
                Debug.LogError("[DevBattleOptions] Button GameObject is not assigned.");
                return;
            }

            _button = buttonObject.GetComponent<Button>();
            if (!_button)
            {
                Debug.LogError("[DevBattleOptions] Button GameObject has no Button component.");
                return;
            }

            _button.onClick.AddListener(OpenOptions);
        }

        public void Unbind()
        {
            _closeSubscription?.Dispose();
            _closeSubscription = null;

            if (_button)
                _button.onClick.RemoveListener(OpenOptions);

            _button = null;
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
