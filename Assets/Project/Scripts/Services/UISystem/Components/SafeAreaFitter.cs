using System;
using Project.Scripts.Services.SafeArea;
using R3;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Project.Scripts.Services.UISystem.Components
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Учитывать вырез сверху (нотч / dynamic island)")]
        [SerializeField] private bool _applyTop = true;

        [Tooltip("Учитывать вырез снизу (home indicator)")]
        [SerializeField] private bool _applyBottom = true;

        [Tooltip("Учитывать вырез слева")]
        [SerializeField] private bool _applyLeft = true;

        [Tooltip("Учитывать вырез справа")]
        [SerializeField] private bool _applyRight = true;


        private RectTransform _rectTransform;
        private ISafeAreaService _safeArea;
        private IDisposable _subscription;
        private bool _didWarnAboutLayout;


        [Inject]
        public void Construct(ISafeAreaService safeArea)
        {
            _safeArea = safeArea;
            ResubscribeIfActive();
        }


        private void Reset()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void Awake()
        {
            EnsureRectTransform();
            WarnIfLayoutIsNotStretch();
        }

        private void OnEnable()
        {
            EnsureRectTransform();
            ResubscribeIfActive();
            ApplyCurrentOrScreenSafeArea();
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnValidate()
        {
            EnsureRectTransform();
            ApplyEditorPreviewSafeArea();
        }


        private void ResubscribeIfActive()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (false == Application.isPlaying || null == _safeArea || null == _safeArea.Current
                || false == isActiveAndEnabled)
                return;

            _subscription = _safeArea.Current.Subscribe(Apply);
        }

        private void ApplyCurrentOrScreenSafeArea()
        {
            if (Application.isPlaying && null != _safeArea && null != _safeArea.Current)
            {
                Apply(_safeArea.Current.CurrentValue);
                return;
            }

            ApplyEditorPreviewSafeArea();
        }

        private void ApplyEditorPreviewSafeArea()
        {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                ApplyFullScreen();
                return;
            }
#endif

            if (Application.isPlaying)
            {
                ApplyScreenSafeArea();
                return;
            }

            ApplyFullScreen();
        }

        private void ApplyScreenSafeArea()
        {
            var screenSize = new Vector2(Screen.width, Screen.height);
            Apply(new SafeAreaInfo(Screen.safeArea, screenSize, Screen.orientation));
        }

        private void ApplyFullScreen()
        {
            Apply(new SafeAreaInfo(new Rect(0f, 0f, 1f, 1f), Vector2.one, Screen.orientation));
        }

        private void Apply(SafeAreaInfo info)
        {
            EnsureRectTransform();

            var min = new Vector2(
                _applyLeft ? info.AnchorMin.x : 0f,
                _applyBottom ? info.AnchorMin.y : 0f);
            var max = new Vector2(
                _applyRight ? info.AnchorMax.x : 1f,
                _applyTop ? info.AnchorMax.y : 1f);

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        private void EnsureRectTransform()
        {
            if (false == _rectTransform)
                _rectTransform = (RectTransform)transform;
        }

        private void WarnIfLayoutIsNotStretch()
        {
            if (_didWarnAboutLayout || false == _rectTransform)
                return;

            var isStretch = _rectTransform.anchorMin == Vector2.zero
                            && _rectTransform.anchorMax == Vector2.one
                            && _rectTransform.offsetMin == Vector2.zero
                            && _rectTransform.offsetMax == Vector2.zero;
            if (isStretch)
                return;

            _didWarnAboutLayout = true;
            Debug.LogWarning(
                $"{nameof(SafeAreaFitter)} expects a stretched RectTransform with zero offsets.",
                this);
        }
    }
}
