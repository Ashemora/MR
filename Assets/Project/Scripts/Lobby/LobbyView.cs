using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Lobby
{
    public class LobbyView : MonoBehaviour
    {
        [Tooltip("Button that starts a battle from the lobby")]
        [SerializeField] private Button _startButton;

        [Tooltip("Button that opens the options window")]
        [SerializeField] private Button _optionsButton;

        [Tooltip("DEV-only button that opens the dev battle options window. Disabled by default; activated under #if DEV.")]
        [SerializeField] private GameObject _devBattleOptionsButton;


        private Action _startAction;
        private Action _optionsAction;


        public GameObject DevBattleOptionsButton => _devBattleOptionsButton;


        private void Awake()
        {
#if DEV
            if (_devBattleOptionsButton)
                _devBattleOptionsButton.SetActive(true);
#endif
        }

        private void OnDestroy()
        {
            if (_startButton)
                _startButton.onClick.RemoveListener(OnStartClicked);
            if (_optionsButton)
                _optionsButton.onClick.RemoveListener(OnOptionsClicked);
        }

        public void Bind(Action startAction, Action optionsAction)
        {
            _startAction = startAction;
            _optionsAction = optionsAction;

            _startButton.onClick.RemoveListener(OnStartClicked);
            _startButton.onClick.AddListener(OnStartClicked);

            if (_optionsButton)
            {
                _optionsButton.onClick.RemoveListener(OnOptionsClicked);
                _optionsButton.onClick.AddListener(OnOptionsClicked);
            }
        }


        private void OnStartClicked()
        {
            _startAction?.Invoke();
        }

        private void OnOptionsClicked()
        {
            _optionsAction?.Invoke();
        }
    }
}