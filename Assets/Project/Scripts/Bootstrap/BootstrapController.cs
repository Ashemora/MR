using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.UI;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Bootstrap
{
    public class BootstrapController : MonoBehaviour
    {
        [Tooltip("UI экрана загрузки, отображаемый во время bootstrap")]
        [SerializeField] private LoadingScreen _loadingScreen;

        [Tooltip("Задержка в секундах перед появлением прогресс-бара")]
        [SerializeField] private float _initialDelaySeconds = 0.1f;

        [Tooltip("Задержка в секундах после полной загрузки сцены перед её активацией")]
        [SerializeField] private float _finalLoadingDelaySeconds = 0.3f;

        
        private IAppStateMachine _appStateMachine;


        [Inject]
        public void Construct(IAppStateMachine appStateMachine)
        {
            _appStateMachine = appStateMachine;
        }
        

        private async void Start()
        {
            try
            {
                await StartAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Critical error during bootstrap: {ex}");
            }
        }

        private async UniTask StartAsync()
        {
            _loadingScreen.Show();

            await UniTask.Delay((int)(_initialDelaySeconds * 1000));

            await _appStateMachine.EnterLobbyAsync(_loadingScreen, (int)(_finalLoadingDelaySeconds * 1000));
        }
    }
}