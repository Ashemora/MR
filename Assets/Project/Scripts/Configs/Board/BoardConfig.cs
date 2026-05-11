using Project.Scripts.Tiles;
using UnityEngine;
#if UNITY_EDITOR
using System;
#endif

namespace Project.Scripts.Configs.Board
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Configs/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        [Header("Tile Grid")]
        [Tooltip("Доля ширины экрана, зарезервированная как отступ вокруг сетки тайлов (0 = без отступа, 0.5 = половина экрана)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _tilePaddingPercent = 0.16f;

        [Tooltip("Доля ячейки, которую занимает визуальный тайл. Сохраняет одинаковые визуальные зазоры при авто-масштабировании доски.")]
        [Range(0.5f, 1.2f)]
        [SerializeField] private float _tileFillPercent = 1.2f;

        [Header("Frame / Background")]
        [Tooltip("Доля ширины экрана, зарезервированная как отступ вокруг рамки-фона доски. " +
                 "Должна быть меньше TilePaddingPercent, чтобы рамка выступала за тайлы. " +
                 "Равна TilePaddingPercent - рамка вплотную к тайлам. Больше - рамка меньше тайлов (нетипично).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _framePaddingPercent = 0f;

        [Tooltip("Дополнительная высота рамки-фона в Unity units поверх высоты тайловой сетки. " +
                 "0 = рамка по высоте точно под тайлы. >0 = рамка вытягивается вверх, тайловая сетка центрируется внутри.")]
        [Range(0f, 5f)]
        [SerializeField] private float _frameExtraHeight = 0.08f;

        [Header("Board Layout")]
        [Tooltip("Сколько рядов тайлов маска открывает выше рамки (>0 - скрывает спавн сверху, <0 - обрезает тайлы внутри рамки сверху)")]
        [Range(-2f, 3f)]
        [SerializeField] private float _maskTopPadding = -0.3f;

        [Tooltip("Максимальное соотношение ширины к высоте игровой области. На широких экранах контент обрамляется до этого соотношения (0.5 = 1:2)")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _maxAspectRatio = 0.5f;

        [Tooltip("Префаб для инстанцирования каждого тайла")]
        [Space(10)]
        [SerializeField] private Tile _tilePrefab;
        
        
        public float TilePaddingPercent => _tilePaddingPercent;
        public float TileFillPercent => _tileFillPercent;
        public float FramePaddingPercent => _framePaddingPercent;
        public float FrameExtraHeight => _frameExtraHeight;
        public float MaskTopPadding => _maskTopPadding;
        public float MaxAspectRatio => _maxAspectRatio;
        public Tile TilePrefab => _tilePrefab;
        

#if UNITY_EDITOR
        public static event Action LayoutChanged;
        public static event Action TileLayoutChanged;

        private bool _hasValidated;
        private float _lastTilePaddingPercent;
        private float _lastTileFillPercent;
        private float _lastFramePaddingPercent;
        private float _lastFrameExtraHeight;
        private float _lastMaskTopPadding;
        private float _lastMaxAspectRatio;


        private void OnEnable()
        {
            CaptureValidatedValues();
        }

        
        private void OnValidate()
        {
            if (false == _hasValidated)
            {
                CaptureValidatedValues();
                TileLayoutChanged?.Invoke();
                
                return;
            }

            var tileLayoutChanged = !Mathf.Approximately(_lastTilePaddingPercent, _tilePaddingPercent)
                                    || !Mathf.Approximately(_lastTileFillPercent, _tileFillPercent);
            var boardLayoutChanged = !Mathf.Approximately(_lastFramePaddingPercent, _framePaddingPercent)
                                     || !Mathf.Approximately(_lastFrameExtraHeight, _frameExtraHeight)
                                     || !Mathf.Approximately(_lastMaskTopPadding, _maskTopPadding)
                                     || !Mathf.Approximately(_lastMaxAspectRatio, _maxAspectRatio);

            CaptureValidatedValues();

            if (boardLayoutChanged)
                LayoutChanged?.Invoke();
            else if (tileLayoutChanged)
                TileLayoutChanged?.Invoke();
        }

        private void CaptureValidatedValues()
        {
            _hasValidated = true;
            _lastTilePaddingPercent = _tilePaddingPercent;
            _lastTileFillPercent = _tileFillPercent;
            _lastFramePaddingPercent = _framePaddingPercent;
            _lastFrameExtraHeight = _frameExtraHeight;
            _lastMaskTopPadding = _maskTopPadding;
            _lastMaxAspectRatio = _maxAspectRatio;
        }
#endif
    }
}