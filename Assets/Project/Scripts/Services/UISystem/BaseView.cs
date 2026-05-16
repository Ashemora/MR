using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;

namespace Project.Scripts.Services.UISystem
{
    public abstract class BaseView<TViewModel> : MonoBehaviour, IView where TViewModel : BaseViewModel
    {
        private const float PumpStrength = 0.12f;
        private const float PumpDuration = 0.35f;
        private const int PumpVibrato = 1;
        private const float PumpElasticity = 0.5f;


        public TViewModel ViewModel { get; private set; }
        public bool IsVisible { get; private set; }


        public virtual SafeAreaMode SafeAreaMode => SafeAreaMode.Inherit;

        protected virtual bool EnablePumpAnimation => false;

        protected CompositeDisposable Disposables { get; } = new();


        private Tween _pumpTween;


        private void OnDestroy()
        {
            _pumpTween?.Kill();
            Disposables.Dispose();
        }


        public async UniTask InitializeAsync(TViewModel viewModel)
        {
            ViewModel = viewModel;
            await ViewModel.InitializeAsync();
            await OnBindViewModel();
        }

        public async UniTask ShowAsync()
        {
            if (IsVisible)
                return;

            gameObject.SetActive(true);
            IsVisible = true;

            if (EnablePumpAnimation)
                PlayPumpAnimation();

            await OnShow();
        }

        public async UniTask HideAsync()
        {
            if (false == IsVisible)
                return;

            _pumpTween?.Kill();

            await OnHide();

            IsVisible = false;
            gameObject.SetActive(false);
        }

        public void Close()
        {
            _pumpTween?.Kill();
            Disposables.Dispose();

            if (null != ViewModel)
                ViewModel.Cleanup();

            OnClose();
            Destroy(gameObject);
        }


        protected virtual UniTask OnBindViewModel()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnShow()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnHide()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnClose()
        {
        }


        private void PlayPumpAnimation()
        {
            _pumpTween?.Kill();
            transform.localScale = Vector3.one;
            _pumpTween = transform.DOPunchScale(Vector3.one * PumpStrength, PumpDuration, PumpVibrato, PumpElasticity);
        }
    }
}