using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Board
{
    public class BoardView : MonoBehaviour
    {
        [Tooltip("SpriteRenderer рамки, окружающей доску (Draw Mode должен быть Sliced)")]
        [SerializeField] private SpriteRenderer _frame;

        [Tooltip("SpriteMask для скрытия тайлов, появляющихся выше доски во время гравитации")]
        [SerializeField] private SpriteMask _spriteMask;

        [Tooltip("Опциональный SpriteRenderer затемнения доски; включается, когда доска недоступна")]
        [SerializeField] private SpriteRenderer _phaseOverlay;


        public void Setup(float frameWidth, float frameHeight, float tileCellSize, float maskTopPadding)
        {
            if (_frame)
            {
                var worldScale = _frame.transform.lossyScale;
                _frame.size = new Vector2(
                    frameWidth  / worldScale.x,
                    frameHeight / worldScale.y
                );
            }

            if (_spriteMask && _spriteMask.sprite)
            {
                var maskExtraHeight = maskTopPadding * tileCellSize;
                var maskHeight = Mathf.Max(0.01f, frameHeight + maskExtraHeight);
                var maskOffsetY = maskExtraHeight * 0.5f;

                var spriteSize = _spriteMask.sprite.bounds.size;
                var parentScale = _spriteMask.transform.parent
                    ? _spriteMask.transform.parent.lossyScale
                    : Vector3.one;

                _spriteMask.transform.localScale = new Vector3(
                    frameWidth / (spriteSize.x * parentScale.x),
                    maskHeight / (spriteSize.y * parentScale.y),
                    1f
                );
                _spriteMask.transform.localPosition = new Vector3(0f, maskOffsetY, 0f);
            }

            if (_phaseOverlay && _phaseOverlay.sprite)
            {
                var spriteSize = _phaseOverlay.sprite.bounds.size;
                var parentScale = _phaseOverlay.transform.parent
                    ? _phaseOverlay.transform.parent.lossyScale
                    : Vector3.one;

                _phaseOverlay.transform.localScale = new Vector3(
                    frameWidth / (spriteSize.x * parentScale.x),
                    frameHeight / (spriteSize.y * parentScale.y),
                    1f
                );
                _phaseOverlay.transform.localPosition = Vector3.zero;
            }
        }

        public void SetInteractionOverlayActive(bool active)
        {
            if (_phaseOverlay)
                _phaseOverlay.enabled = active;
        }

        public bool TryGetWorldFrameBounds(out float centerX, out float topWorldY, out float halfWidth)
        {
            centerX = 0f;
            topWorldY = 0f;
            halfWidth = 0f;

            if (!_frame)
                return false;

            var bounds = _frame.bounds;
            centerX = bounds.center.x;
            topWorldY = bounds.max.y;
            halfWidth = bounds.extents.x;

            return halfWidth > 0f;
        }

        public float GetWorldHeight()
        {
            if (_frame)
                return _frame.bounds.size.y;

            var renderers = GetComponentsInChildren<SpriteRenderer>(false);
            if (renderers.Length == 0)
                return 0f;

            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (false == renderers[i].sprite)
                    continue;

                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
                maxY = Mathf.Max(maxY, renderers[i].bounds.max.y);
            }

            return float.IsInfinity(minY) || float.IsInfinity(maxY) ? 0f : maxY - minY;
        }
    }
}