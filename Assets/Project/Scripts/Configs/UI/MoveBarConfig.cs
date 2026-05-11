using Project.Scripts.Shared.MoveBar;
using UnityEngine;

namespace Project.Scripts.Configs.UI
{
    [CreateAssetMenu(fileName = "MoveBarConfig", menuName = "Configs/Move Bar Config")]
    public class MoveBarConfig : ScriptableObject
    {
        [Tooltip("Включить механику полосы ходов. Если снято — ходы не ограничиваются и UI не отображается")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Максимальное количество зарядов ходов")]
        [SerializeField] private int _maxMoves = 10;

        [Tooltip("Время в секундах для восстановления одного заряда хода")]
        [SerializeField] private float _secondsPerMove = 2.5f;

        [Tooltip("Количество зарядов ходов в начале каждого боя")]
        [SerializeField] private int _startMoves = 4;

        [Header("Visual")]
        [Tooltip("Зазор между сегментами как доля от общей ширины полосы (например, 0.01 = 1%)")]
        [SerializeField] private float _gapFraction = 0.01f;

        [Tooltip("Сила масштабирующего удара по сегменту при активации нового заряда хода")]
        [SerializeField] private float _punchStrength = 0.15f;

        [Tooltip("Длительность анимации масштабирующего удара активации в секундах")]
        [SerializeField] private float _punchDuration = 0.2f;

        [Tooltip("Минимальная прозрачность в нижней точке цикла мигания заполненной полосы (0..1)")]
        [SerializeField] private float _fullBlinkMinAlpha = 0.35f;

        [Tooltip("Длительность одного полуцикла мигания заполненной полосы в секундах")]
        [SerializeField] private float _fullBlinkHalfDuration = 0.45f;

        [Tooltip("Длительность анимации тряски при попытке обмена без зарядов ходов в секундах")]
        [SerializeField] private float _emptyShakeDuration = 0.3f;

        [Tooltip("Амплитуда горизонтальной тряски в пикселях при попытке обмена без зарядов ходов")]
        [SerializeField] private float _emptyShakeStrength = 8f;


        public bool IsEnabled => _enabled;
        public int MaxMoves => _maxMoves;
        public float SecondsPerMove => _secondsPerMove;
        public int StartMoves => _startMoves;
        public float GapFraction => _gapFraction;
        public float PunchStrength => _punchStrength;
        public float PunchDuration => _punchDuration;
        public float FullBlinkMinAlpha => _fullBlinkMinAlpha;
        public float FullBlinkHalfDuration => _fullBlinkHalfDuration;
        public float EmptyShakeDuration => _emptyShakeDuration;
        public float EmptyShakeStrength => _emptyShakeStrength;


        public MoveBarSettings ToSettings()
        {
            return new MoveBarSettings(_maxMoves, _secondsPerMove, _startMoves);
        }
    }
}