using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Lobby
{
    public class LobbyView : MonoBehaviour
    {
        [Tooltip("Button that starts a battle from the lobby")]
        [SerializeField] private Button _startButton;


        private Action _startAction;


        private void OnDestroy()
        {
            _startButton?.onClick.RemoveListener(OnStartClicked);
        }

        public void Bind(Action startAction)
        {
            _startAction = startAction;
            _startButton.onClick.RemoveListener(OnStartClicked);
            _startButton.onClick.AddListener(OnStartClicked);
        }


        private void OnStartClicked()
        {
            _startAction?.Invoke();
        }
    }
}