using Project.Scripts.Gameplay.Battle.Board;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Services.Board;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Layout
{
    public class BattleWorldLayout : MonoBehaviour
    {
        [Tooltip("View матч доски")]
        [SerializeField] private BoardView _boardView;

        [Tooltip("Контейнер для тайлов доски")]
        [SerializeField] private Transform _tileContainer;

        [Tooltip("View боевого поля")]
        [SerializeField] private BattleFieldView _battleFieldView;

        [Tooltip("Вью для отображения энергии игрока и врага в мировом пространстве")]
        [SerializeField] private BattleWorldEnergyView _energyView;

        [Header("Announcement Anchors")]
        [Tooltip("Якорь для объявлений в зоне боевого поля (герои, аватары)")]
        [SerializeField] private Transform _battleFieldAnnouncementAnchor;

        [Tooltip("Якорь для объявлений в зоне баров энергии")]
        [SerializeField] private Transform _energyBarsAnnouncementAnchor;

        [Tooltip("Якорь для объявлений в зоне доски матчинга")]
        [SerializeField] private Transform _boardAnnouncementAnchor;


        public BoardView BoardView => _boardView;
        public Transform TileContainer => _tileContainer;
        public BattleFieldView BattleFieldView => _battleFieldView;
        public BattleWorldEnergyView EnergyView => _energyView;

        
        private bool _previewOffsetBaselineCaptured;
        private Vector3 _boardBaseLocalPosition;
        private Vector3 _tileContainerBaseLocalPosition;
        private float _boardAndEnergyPreviewYOffset;


        public void SetBoardWorldCenter(Vector3 boardWorldCenter)
        {
            if (!_boardView)
                return;

            transform.position = boardWorldCenter - _boardView.transform.localPosition;
        }

        public void SetVerticalLayout(float boardTopWorldY, float cellSize, float gapBoardToPlayerEnergy,
            float gapPlayerEnergyToEnemyEnergy, float gapEnemyEnergyToBattleField)
        {
            var cursor = boardTopWorldY + gapBoardToPlayerEnergy * cellSize;

            if (_energyView)
            {
                var playerH = _energyView.PlayerEnergyHeight;
                _energyView.SetPlayerEnergyWorldY(cursor + playerH * 0.5f);
                cursor += playerH + gapPlayerEnergyToEnemyEnergy * cellSize;

                var enemyH = _energyView.EnemyEnergyHeight;
                _energyView.SetEnemyEnergyWorldY(cursor + enemyH * 0.5f);
                cursor += enemyH + gapEnemyEnergyToBattleField * cellSize;
            }

            if (_battleFieldView)
                _battleFieldView.SetLayoutBottomWorldY(cursor);
        }

        public void RefreshBindings()
        {
            _battleFieldView?.RefreshPosition();
        }

        public float GetBoardWorldHeight()
        {
            return _boardView ? _boardView.GetWorldHeight() : 0f;
        }

        public void SetBoardAndEnergyPreviewYOffset(float offset)
        {
            _boardAndEnergyPreviewYOffset = offset;
            if (false == _previewOffsetBaselineCaptured && Mathf.Approximately(offset, 0f))
                return;

            CapturePreviewOffsetBaseline();

            if (_boardView)
                _boardView.transform.localPosition = WithYOffset(_boardBaseLocalPosition, offset);

            if (_tileContainer && _boardView && false == _tileContainer.IsChildOf(_boardView.transform))
                _tileContainer.localPosition = WithYOffset(_tileContainerBaseLocalPosition, offset);

            _energyView?.SetPreviewLocalYOffset(offset);
        }

        public float GetBoardAndEnergyPreviewYOffset()
        {
            return _boardAndEnergyPreviewYOffset;
        }

        public void PublishAnnouncementAnchors(IBoardBoundsProvider boardBounds)
        {
            if (boardBounds == null)
                return;

            if (_battleFieldAnnouncementAnchor)
                boardBounds.SetBattleFieldAnchorY(_battleFieldAnnouncementAnchor.position.y);

            if (_energyBarsAnnouncementAnchor)
                boardBounds.SetEnergyBarsAnchorY(_energyBarsAnnouncementAnchor.position.y);

            if (_boardAnnouncementAnchor)
                boardBounds.SetBoardAnchorY(_boardAnnouncementAnchor.position.y);
        }

        private void CapturePreviewOffsetBaseline()
        {
            if (_previewOffsetBaselineCaptured)
                return;

            _previewOffsetBaselineCaptured = true;
            _boardBaseLocalPosition = _boardView ? _boardView.transform.localPosition : Vector3.zero;
            _tileContainerBaseLocalPosition = _tileContainer ? _tileContainer.localPosition : Vector3.zero;
        }

        private static Vector3 WithYOffset(Vector3 baseLocalPosition, float offset)
        {
            return new Vector3(baseLocalPosition.x, baseLocalPosition.y + offset, baseLocalPosition.z);
        }
    }
}