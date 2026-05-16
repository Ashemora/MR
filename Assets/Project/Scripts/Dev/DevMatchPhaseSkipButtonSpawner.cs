#if DEV
using Project.Scripts.Services.Board;
using Project.Scripts.Services.UISystem;
using UnityEngine;

namespace Project.Scripts.Dev
{
    public class DevMatchPhaseSkipButtonSpawner
    {
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
            if (_view)
                return;

            if (!prefab)
            {
                Debug.LogError("[DevMatchPhaseSkip] Button prefab is not assigned.");
                return;
            }

            var parent = _uiService.GetLayerRoot(UILayer.Main, SafeAreaMode.ForceIgnore);
            var instance = Object.Instantiate(prefab, parent);
            instance.name = "DevMatchPhaseSkipButton";

            _view = instance.GetComponent<DevMatchPhaseSkipButtonView>();
            if (!_view)
                _view = instance.AddComponent<DevMatchPhaseSkipButtonView>();

            _view.Init(_skipService, _boardBoundsProvider);
        }
    }
}
#endif