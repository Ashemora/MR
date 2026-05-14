using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.SceneLoading;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI
{
    public class LoadingScreen : MonoBehaviour, ILoadingPresenter
    {
        [Tooltip("Корневой GameObject панели, отображаемый во время загрузки")]
        [SerializeField] private GameObject _loadingPanel;

        [Tooltip("Слайдер прогресса загрузки (0-1)")]
        [SerializeField] private Slider _progressBar;


        private IDisposable _subscription;

        
        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
        

        public void SubscribeToProgress(Observable<float> progressObservable)
        {
            _subscription = progressObservable.Subscribe(progress =>
            {
                _progressBar.value = progress;
            });
        }

        public void Show()
        {
            if (null == _loadingPanel || null == _progressBar)
                return;

            _loadingPanel.SetActive(true);
            _progressBar.value = 0;
        }

        public void ShowProgressBar()
        {
            if (null == _progressBar)
                return;

            _progressBar.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (null == _loadingPanel)
                return;

            _loadingPanel.SetActive(false);
        }

        public UniTask ShowAsync()
        {
            Show();
            ShowProgressBar();
            
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync()
        {
            Hide();
            
            return UniTask.CompletedTask;
        }

        public void SetProgress(float progress)
        {
            if (null == _progressBar)
                return;

            _progressBar.value = Mathf.Clamp01(progress);
        }
    }
}