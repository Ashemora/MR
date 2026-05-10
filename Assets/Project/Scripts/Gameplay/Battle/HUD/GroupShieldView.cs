using DG.Tweening;
using Project.Scripts.Configs.Battle.Visuals;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class GroupShieldView : MonoBehaviour
    {
        private CompositeDisposable _disposables;
        private Vector3 _originalScale;
        private Tween _pulseTween;


        private void OnDestroy()
        {
            _pulseTween?.Kill();
            _disposables?.Dispose();
        }


        public void Bind(Observable<bool> isGroupAlive)
        {
            _originalScale = transform.localScale;
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            isGroupAlive
                .Subscribe(alive =>
                {
                    if (false == alive)
                        StopPulse();

                    gameObject.SetActive(alive);
                })
                .AddTo(_disposables);
        }

        public void StartPulse(ShieldPulseConfig config)
        {
            if (null == config || false == gameObject.activeSelf)
                return;

            _pulseTween?.Kill();
            transform.localScale = _originalScale;

            var peak = _originalScale * config.ScaleMultiplier;
            var half = config.Duration * 0.5f;

            _pulseTween = DOTween.Sequence()
                .Append(transform.DOScale(peak, half).SetEase(config.Ease))
                .Append(transform.DOScale(_originalScale, half).SetEase(config.Ease))
                .AppendInterval(config.Interval)
                .SetLoops(-1, LoopType.Restart)
                .OnKill(() =>
                {
                    if (this)
                        transform.localScale = _originalScale;

                    _pulseTween = null;
                });
        }

        public void StopPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
        }
    }
}