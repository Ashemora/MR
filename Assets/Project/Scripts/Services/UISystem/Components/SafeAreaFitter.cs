using System;
using Project.Scripts.Services.SafeArea;
using R3;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
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
#if UNITY_EDITOR
        private Rect _lastEditorSafeArea;
        private Vector2Int _lastEditorScreenSize;
        private ScreenOrientation _lastEditorOrientation;
        private bool _editorPollSubscribed;
#endif


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
            SubscribeEditorPoll();
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            UnsubscribeEditorPoll();
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
            if (PrefabStageUtility.GetCurrentPrefabStage())
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

#if UNITY_EDITOR
            ApplyScreenSafeArea();
#else
            ApplyFullScreen();
#endif
        }

        private void ApplyScreenSafeArea()
        {
            var screenSize = new Vector2(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            Apply(new SafeAreaInfo(UnityEngine.Device.Screen.safeArea, screenSize, UnityEngine.Device.Screen.orientation));
        }

        private void ApplyFullScreen()
        {
            Apply(new SafeAreaInfo(new Rect(0f, 0f, 1f, 1f), Vector2.one, UnityEngine.Device.Screen.orientation));
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

            var changed = _rectTransform.anchorMin != min
                          || _rectTransform.anchorMax != max
                          || _rectTransform.offsetMin != Vector2.zero
                          || _rectTransform.offsetMax != Vector2.zero;
            if (false == changed)
                return;

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

#if UNITY_EDITOR
            if (false == Application.isPlaying)
            {
                EditorUtility.SetDirty(_rectTransform);
                if (gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
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

        private void SubscribeEditorPoll()
        {
#if UNITY_EDITOR
            if (_editorPollSubscribed || Application.isPlaying)
                return;

            _lastEditorSafeArea = Screen.safeArea;
            _lastEditorScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastEditorOrientation = Screen.orientation;
            EditorApplication.update += OnEditorUpdate;
            _editorPollSubscribed = true;
#endif
        }

        private void UnsubscribeEditorPoll()
        {
#if UNITY_EDITOR
            if (false == _editorPollSubscribed)
                return;

            EditorApplication.update -= OnEditorUpdate;
            _editorPollSubscribed = false;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorUpdate()
        {
            if (Application.isPlaying)
                return;
            if (false == this || false == gameObject)
                return;
            if (false == isActiveAndEnabled)
                return;

            var safeArea = UnityEngine.Device.Screen.safeArea;
            var screenSize = new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            var orientation = UnityEngine.Device.Screen.orientation;

            if (safeArea == _lastEditorSafeArea
                && screenSize == _lastEditorScreenSize
                && orientation == _lastEditorOrientation)
                return;

            _lastEditorSafeArea = safeArea;
            _lastEditorScreenSize = screenSize;
            _lastEditorOrientation = orientation;

            ApplyEditorPreviewSafeArea();
        }
#endif
    }
}