using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System;
#endif

namespace Project.Scripts.Configs.Gameplay
{
    [CreateAssetMenu(fileName = "GameplayScreenLayoutConfig", menuName = "Configs/Gameplay Screen Layout Config")]
    public class GameplayScreenLayoutConfig : ScriptableObject
    {
        [Header("Gameplay Frame")]
        [Tooltip("Ширина reference-разрешения Canvas/GamePlay в pixels.")]
        [Min(1f)]
        [SerializeField] private float _referenceResolutionWidth = 1080f;

        [Tooltip("Высота reference-разрешения Canvas/GamePlay в pixels.")]
        [Min(1f)]
        [SerializeField] private float _referenceResolutionHeight = 1920f;

        [Tooltip("Ширина эталонного gameplay-контейнера. Для 1:2 оставьте 1.")]
        [Min(0.01f)]
        [SerializeField] private float _gameplayAspectWidth = 1f;

        [Tooltip("Высота эталонного gameplay-контейнера. Для 1:2 оставьте 2.")]
        [Min(0.01f)]
        [SerializeField] private float _gameplayAspectHeight = 2f;

        [Header("Camera")]
        [Tooltip("Ортографический размер камеры при эталонном соотношении gameplay-контейнера.")]
        [Min(0.01f)]
        [SerializeField] private float _referenceOrthographicSize = 5f;

        [Header("Safe Area")]
        [Tooltip("Учитывать Screen.safeArea при расчете gameplay-контейнера.")]
        [SerializeField] private bool _useSafeArea = true;

        [Tooltip("Разрешить world-content зоне использовать нижнюю unsafe area. TopBar при этом остается внутри safe area.")]
        [SerializeField] private bool _worldExtendsIntoUnsafeBottomArea = true;

        [Tooltip("Дополнительный внутренний отступ от safe area в reference pixels.")]
        [Min(0f)]
        [SerializeField] private float _safeAreaPadding = 0f;

        [Header("Top Bar")]
        [Tooltip("Высота зоны TopBar внутри gameplay-контейнера в reference pixels.")]
        [Min(0f)]
        [SerializeField] private float _topBarHeight = 80f;

        [Tooltip("Боковой отступ TopBar внутри gameplay-контейнера в reference pixels.")]
        [Min(0f)]
        [SerializeField] private float _topBarSidePadding = 140f;

        [Tooltip("Нижний отступ TopBar от верха BattleField в reference pixels.")]
        [Min(0f)]
        [FormerlySerializedAs("_topBarToWorldGap")]
        [SerializeField] private float _topBarBottomPadding = 5f;

        [Header("World Content")]
        [Tooltip("Отступ world-content зоны от нижнего края gameplay-контейнера в reference pixels.")]
        [Min(0f)]
        [SerializeField] private float _worldBottomPadding = 8f;

        [Tooltip("Боковой отступ world-content зоны внутри gameplay-контейнера в reference pixels.")]
        [Min(0f)]
        [SerializeField] private float _worldSidePadding = 0f;

        [Tooltip("Минимальная доля vertical gaps между world блоками перед тем, как match-доска начнет уменьшаться.")]
        [Range(0f, 1f)]
        [SerializeField] private float _worldStackMinGapScale = 0.25f;

        
        public float ReferenceResolutionWidth => _referenceResolutionWidth;
        public float ReferenceResolutionHeight => _referenceResolutionHeight;
        public float GameplayAspect => _gameplayAspectWidth / _gameplayAspectHeight;
        public float ReferenceOrthographicSize => _referenceOrthographicSize;
        public bool UseSafeArea => _useSafeArea;
        public bool WorldExtendsIntoUnsafeBottomArea => _worldExtendsIntoUnsafeBottomArea;
        public float SafeAreaPadding => _safeAreaPadding;
        public float TopBarHeight => _topBarHeight;
        public float TopBarSidePadding => _topBarSidePadding;
        public float TopBarBottomPadding => _topBarBottomPadding;
        public float WorldBottomPadding => _worldBottomPadding;
        public float WorldSidePadding => _worldSidePadding;
        public float WorldStackMinGapScale => _worldStackMinGapScale;
        
#if UNITY_EDITOR
        public static event Action LayoutChanged;
        public static event Action TopBarLayoutChanged;

        private bool _hasValidated;
        private bool _lastUseSafeArea;
        private bool _lastWorldExtendsIntoUnsafeBottomArea;
        private float _lastReferenceResolutionWidth;
        private float _lastReferenceResolutionHeight;
        private float _lastGameplayAspectWidth;
        private float _lastGameplayAspectHeight;
        private float _lastSafeAreaPadding;
        private float _lastTopBarHeight;
        private float _lastTopBarSidePadding;
        private float _lastTopBarBottomPadding;
        private float _lastWorldBottomPadding;
        private float _lastWorldSidePadding;


        private void OnEnable()
        {
            CaptureValidatedValues();
        }


        private void OnValidate()
        {
            if (false == _hasValidated)
            {
                CaptureValidatedValues();
                return;
            }

            var topBarChanged = !Mathf.Approximately(_lastTopBarHeight, _topBarHeight)
                                || !Mathf.Approximately(_lastTopBarSidePadding, _topBarSidePadding)
                                || !Mathf.Approximately(_lastTopBarBottomPadding, _topBarBottomPadding);
            var layoutChanged = _lastUseSafeArea != _useSafeArea
                                || _lastWorldExtendsIntoUnsafeBottomArea != _worldExtendsIntoUnsafeBottomArea
                                || !Mathf.Approximately(_lastReferenceResolutionWidth, _referenceResolutionWidth)
                                || !Mathf.Approximately(_lastReferenceResolutionHeight, _referenceResolutionHeight)
                                || !Mathf.Approximately(_lastGameplayAspectWidth, _gameplayAspectWidth)
                                || !Mathf.Approximately(_lastGameplayAspectHeight, _gameplayAspectHeight)
                                || !Mathf.Approximately(_lastSafeAreaPadding, _safeAreaPadding)
                                || !Mathf.Approximately(_lastWorldBottomPadding, _worldBottomPadding)
                                || !Mathf.Approximately(_lastWorldSidePadding, _worldSidePadding);

            CaptureValidatedValues();

            if (layoutChanged)
                LayoutChanged?.Invoke();
            else if (topBarChanged)
                TopBarLayoutChanged?.Invoke();
        }


        private void CaptureValidatedValues()
        {
            _hasValidated = true;
            _lastUseSafeArea = _useSafeArea;
            _lastWorldExtendsIntoUnsafeBottomArea = _worldExtendsIntoUnsafeBottomArea;
            _lastReferenceResolutionWidth = _referenceResolutionWidth;
            _lastReferenceResolutionHeight = _referenceResolutionHeight;
            _lastGameplayAspectWidth = _gameplayAspectWidth;
            _lastGameplayAspectHeight = _gameplayAspectHeight;
            _lastSafeAreaPadding = _safeAreaPadding;
            _lastTopBarHeight = _topBarHeight;
            _lastTopBarSidePadding = _topBarSidePadding;
            _lastTopBarBottomPadding = _topBarBottomPadding;
            _lastWorldBottomPadding = _worldBottomPadding;
            _lastWorldSidePadding = _worldSidePadding;
        }
#endif
    }
}