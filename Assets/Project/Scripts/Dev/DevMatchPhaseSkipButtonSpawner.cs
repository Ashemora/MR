#if DEV
using System;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.UISystem;
using UnityEngine;

namespace Project.Scripts.Dev
{
    public class DevMatchPhaseSkipButtonSpawner : IDisposable
    {
        private const string ButtonName = "DevMatchPhaseSkipButton";
        private readonly UIService _uiService;
        private readonly DevMatchPhaseSkipService _skipService;
        private readonly IBoardBoundsProvider _boardBoundsProvider;
        private DevMatchPhaseSkipButtonView _view;


        public DevMatchPhaseSkipButtonSpawner(UIService uiService, DevMatchPhaseSkipService skipService,
            IBoardBoundsProvider boardBoundsProvider)
        {
            _uiService = uiService;
            _skipService = skipService;
            _boardBoundsProvider = boardBoundsProvider;
        }

        public void Spawn(GameObject prefab)
        {
            if (!prefab)
            {
                Debug.LogError("[DevMatchPhaseSkip] Button prefab is not assigned.");
                return;
            }

            var parent = _uiService.GetLayerRoot(UILayer.Main, SafeAreaMode.ForceIgnore);
            DevGameplayButtonCleanup.DestroyNamedButtons(parent, ButtonName);
            _view = null;
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = ButtonName;
            _view = instance.GetComponent<DevMatchPhaseSkipButtonView>();
            if (!_view)
                _view = instance.AddComponent<DevMatchPhaseSkipButtonView>();
            
            _view.Init(_skipService, _boardBoundsProvider);
        }

        public void Dispose()
        {
            if (!_view)
                return;

            UnityEngine.Object.Destroy(_view.gameObject);
            _view = null;
        }
    }
}
#endif