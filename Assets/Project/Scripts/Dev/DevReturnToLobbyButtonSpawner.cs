#if DEV
using Project.Scripts.Services.Board;
using Project.Scripts.Services.UISystem;
using UnityEngine;

namespace Project.Scripts.Dev
{
    public class DevReturnToLobbyButtonSpawner
    {
        private readonly UIService _uiService;
        private readonly DevAbortBattleService _abortService;
        private readonly DevMatchPhaseSkipService _skipService;
        private readonly IBoardBoundsProvider _boardBoundsProvider;
        private DevReturnToLobbyButtonView _view;


        public DevReturnToLobbyButtonSpawner(UIService uiService, DevAbortBattleService abortService,
            DevMatchPhaseSkipService skipService, IBoardBoundsProvider boardBoundsProvider)
        {
            _uiService = uiService;
            _abortService = abortService;
            _skipService = skipService;
            _boardBoundsProvider = boardBoundsProvider;
        }


        public void Spawn(GameObject prefab)
        {
            if (_view)
                return;

            if (!prefab)
            {
                Debug.LogError("[DevAbortBattle] Button prefab is not assigned.");
                return;
            }

            var parent = _uiService.GetLayerRoot(UILayer.Main, SafeAreaMode.ForceIgnore);
            var instance = Object.Instantiate(prefab, parent);
            instance.name = "DevAbortBattleButton";

            _view = instance.GetComponent<DevReturnToLobbyButtonView>();
            if (!_view)
                _view = instance.AddComponent<DevReturnToLobbyButtonView>();

            _view.Init(_abortService, _skipService, _boardBoundsProvider);
        }
    }
}
#endif