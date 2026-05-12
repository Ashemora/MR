using System;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Gameplay.Battle.Units;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class BattleFieldLayoutController : MonoBehaviour
    {
        [Tooltip("Высота визуальной области боевого поля в world units. Управляет Background, Floor, layout anchors и позициями panel rows.")]
        [Min(0.01f)]
        [SerializeField] private float _layoutHeight = 4.2f;

        [Tooltip("Sliced SpriteRenderer фоновой рамки боевого поля.")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        [Tooltip("Внутренняя подложка поля, масштабируется по X/Y под размеры Background.")]
        [SerializeField] private Transform _floorTransform;

        [Tooltip("SpriteRenderer подложки поля; используется для расчёта localScale из sprite.bounds.")]
        [SerializeField] private SpriteRenderer _floorRenderer;

        [Tooltip("Отступ Floor с каждой стороны от Background в world units (учитывает прозрачные пиксели рамки). X — по горизонтали, Y — по вертикали.")]
        [SerializeField] private Vector2 _floorInset = Vector2.zero;

        [Tooltip("Панель героев игрока, позиционируется от нижней границы Layout Height.")]
        [SerializeField] private Transform _playerPanel;

        [Tooltip("Панель героев врага, позиционируется от верхней границы Layout Height.")]
        [SerializeField] private Transform _enemyPanel;

        [Tooltip("Отступ PlayerPanel от нижней границы Layout Height.")]
        [SerializeField] private float _playerPanelBottomPadding = 1.11f;

        [Tooltip("Отступ EnemyPanel от верхней границы Layout Height.")]
        [SerializeField] private float _enemyPanelTopPadding = 1.09f;

        [Tooltip("Маркер нижнего края визуальной области боевого поля; используется для вертикального автостекинга блоков над доской матчинга")]
        [SerializeField] private Transform _layoutBottomAnchor;

        [SerializeField] private Transform _layoutTopAnchor;

        [Tooltip("Опциональный SpriteRenderer затемнения боевого поля; включается, когда боевые действия недоступны")]
        [SerializeField] private SpriteRenderer _phaseOverlay;

        
        public float BaseLayoutHeight => Mathf.Max(0.01f, _layoutHeight);
        public float LayoutScale => _layoutScale;
        
        
        private BattleFieldUnitPose _activePlayerPanelPose;
        private BattleFieldUnitPose _activeEnemyPanelPose;
        private bool _layoutSnapshotPoseActive;
        private float _layoutScale = 1f;
        

        public float LayoutTopWorldY
        {
            get
            {
                ApplyBattleFieldGeometry();
                return _layoutTopAnchor ? _layoutTopAnchor.position.y : transform.position.y + GetLayoutHeight() * 0.5f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyBattleFieldGeometry();
        }
#endif

        public BattleFieldLayoutSnapshot CaptureLayoutSnapshot(BattleFieldLayoutTargets targets)
        {
            return new BattleFieldLayoutSnapshot(Mathf.Max(0.01f, _layoutHeight),
                CapturePose(targets.PlayerAvatarSlot ? targets.PlayerAvatarSlot.transform : null),
                CapturePose(targets.EnemyAvatarSlot ? targets.EnemyAvatarSlot.transform : null),
                CaptureHeroSlotPoses(targets.PlayerHeroSlots),
                CaptureHeroSlotPoses(targets.EnemyHeroSlots),
                new[]
                {
                    CapturePose(targets.EnemyGroup1Shield ? targets.EnemyGroup1Shield.transform : null),
                    CapturePose(targets.EnemyGroup2Shield ? targets.EnemyGroup2Shield.transform : null),
                    CapturePose(targets.PlayerGroup1Shield ? targets.PlayerGroup1Shield.transform : null),
                    CapturePose(targets.PlayerGroup2Shield ? targets.PlayerGroup2Shield.transform : null)
                },
                CapturePose(_playerPanel),
                CapturePose(_enemyPanel));
        }

        public void ApplyLayoutSnapshot(BattleFieldLayoutSnapshot snapshot, BattleFieldLayoutTargets targets)
        {
            ApplyLayoutBlend(snapshot, snapshot, 0f, targets);
        }

        public void ApplyLayoutSnapshotPreservingTop(BattleFieldLayoutSnapshot snapshot, BattleFieldLayoutTargets targets)
        {
            ApplyLayoutBlendPreservingTop(snapshot, snapshot, 0f, targets);
        }

        public void ApplyLayoutBlend(BattleFieldLayoutSnapshot compressed, BattleFieldLayoutSnapshot full,
            float t, BattleFieldLayoutTargets targets)
        {
            t = Mathf.Clamp01(t);
            _layoutSnapshotPoseActive = true;
            _layoutHeight = Mathf.Max(0.01f, Mathf.LerpUnclamped(compressed.LayoutHeight, full.LayoutHeight, t));
            _activePlayerPanelPose = BattleFieldUnitPose.Lerp(compressed.PlayerPanel, full.PlayerPanel, t);
            _activeEnemyPanelPose = BattleFieldUnitPose.Lerp(compressed.EnemyPanel, full.EnemyPanel, t);

            ApplyBattleFieldGeometry();
            ApplyPose(targets.PlayerAvatarSlot ? targets.PlayerAvatarSlot.transform : null,
                BattleFieldUnitPose.Lerp(compressed.PlayerAvatar, full.PlayerAvatar, t));
            ApplyPose(targets.EnemyAvatarSlot ? targets.EnemyAvatarSlot.transform : null,
                BattleFieldUnitPose.Lerp(compressed.EnemyAvatar, full.EnemyAvatar, t));
            ApplyHeroSlotPoses(targets.PlayerHeroSlots, compressed.PlayerHeroSlots, full.PlayerHeroSlots, t);
            ApplyHeroSlotPoses(targets.EnemyHeroSlots, compressed.EnemyHeroSlots, full.EnemyHeroSlots, t);
            ApplyGroupShieldPoses(targets, compressed.GroupShields, full.GroupShields, t);
        }

        public void ApplyLayoutBlendPreservingTop(BattleFieldLayoutSnapshot compressed, BattleFieldLayoutSnapshot full,
            float t, BattleFieldLayoutTargets targets)
        {
            var topWorldY = LayoutTopWorldY;
            ApplyLayoutBlend(compressed, full, t, targets);
            PreserveLayoutTopWorldY(topWorldY);
        }

        public void RefreshPosition()
        {
            ApplyBattleFieldGeometry();
        }

        public void SetLayoutBottomWorldY(float worldY)
        {
            ApplyBattleFieldGeometry();
            float pivotToBottom;

            if (_layoutBottomAnchor)
                pivotToBottom = transform.position.y - _layoutBottomAnchor.position.y;
            else
            {
                var renderers = GetComponentsInChildren<SpriteRenderer>(false);
                var minY = transform.position.y;
                for (var i = 0; i < renderers.Length; i++)
                    if (renderers[i].sprite)
                        minY = Mathf.Min(minY, renderers[i].bounds.min.y);
                pivotToBottom = transform.position.y - minY;
            }

            var pos = transform.position;
            transform.position = new Vector3(pos.x, worldY + pivotToBottom, pos.z);
        }

        public float GetLayoutHeight()
        {
            ApplyBattleFieldGeometry();

            if (_layoutHeight > 0f)
                return _layoutHeight * _layoutScale;

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

        public void SetLayoutScale(float scale)
        {
            _layoutScale = Mathf.Max(0.01f, scale);
            ApplyBattleFieldGeometry();
        }

        public void SetInteractionOverlayActive(bool active)
        {
            if (_phaseOverlay)
                _phaseOverlay.enabled = active;
        }

        public bool HasRequiredLayoutReferences(out string error)
        {
            if (false == _playerPanel || false == _enemyPanel)
            {
                error = "Player or enemy panel transform is missing.";
                return false;
            }

            error = string.Empty;
            
            return true;
        }

        private void ApplyBattleFieldGeometry()
        {
            var baseHeight = Mathf.Max(0.01f, _layoutHeight);
            if (false == Mathf.Approximately(_layoutHeight, baseHeight))
                _layoutHeight = baseHeight;

            var safeHeight = baseHeight * _layoutScale;

            if (_backgroundRenderer)
            {
                var size = _backgroundRenderer.size;
                if (false == Mathf.Approximately(size.y, safeHeight))
                    _backgroundRenderer.size = new Vector2(size.x, safeHeight);
            }

            if (_floorTransform)
            {
                var scale = _floorTransform.localScale;
                var backgroundWidth = _backgroundRenderer ? _backgroundRenderer.size.x : 0f;
                var floorTargetWidth = Mathf.Max(0f, backgroundWidth - _floorInset.x * 2f);
                var floorTargetHeight = Mathf.Max(0f, safeHeight - _floorInset.y * 2f);
                var floorScaleX = CalculateScaleFromSpriteSize(_floorRenderer, floorTargetWidth, true);
                var floorScaleY = CalculateScaleFromSpriteSize(_floorRenderer, floorTargetHeight, false);
                if (false == Mathf.Approximately(scale.x, floorScaleX)
                    || false == Mathf.Approximately(scale.y, floorScaleY))
                    _floorTransform.localScale = new Vector3(floorScaleX, floorScaleY, scale.z);
            }

            if (_phaseOverlay)
            {
                var overlayTransform = _phaseOverlay.transform;
                var overlayScale = overlayTransform.localScale;
                var overlayScaleX = CalculateRendererScaleX(_phaseOverlay, _backgroundRenderer);
                var overlayScaleY = CalculateRendererScaleY(_phaseOverlay, safeHeight);

                if (false == Mathf.Approximately(overlayScale.x, overlayScaleX)
                    || false == Mathf.Approximately(overlayScale.y, overlayScaleY)) 
                    overlayTransform.localScale = new Vector3(overlayScaleX, overlayScaleY, overlayScale.z);
            }

            var halfHeight = safeHeight * 0.5f;

            if (_layoutBottomAnchor)
                SetLocalY(_layoutBottomAnchor, -halfHeight);

            if (_layoutTopAnchor)
                SetLocalY(_layoutTopAnchor, halfHeight);

            if (_layoutSnapshotPoseActive)
            {
                ApplyPose(_playerPanel, _activePlayerPanelPose);
                ApplyPose(_enemyPanel, _activeEnemyPanelPose);
            }
            else
            {
                if (_playerPanel)
                    SetLocalY(_playerPanel, -halfHeight + _playerPanelBottomPadding * _layoutScale);

                if (_enemyPanel)
                    SetLocalY(_enemyPanel, halfHeight - _enemyPanelTopPadding * _layoutScale);
            }
        }

        private void PreserveLayoutTopWorldY(float topWorldY)
        {
            ApplyBattleFieldGeometry();
            var currentTop = _layoutTopAnchor ? _layoutTopAnchor.position.y : transform.position.y + GetLayoutHeight() * 0.5f;
            var deltaY = topWorldY - currentTop;
            if (Mathf.Approximately(deltaY, 0f))
                return;

            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y + deltaY, pos.z);
        }

        private void ApplyGroupShieldPoses(BattleFieldLayoutTargets targets, BattleFieldUnitPose[] compressed,
            BattleFieldUnitPose[] full, float t)
        {
            ApplyPose(targets.EnemyGroup1Shield ? targets.EnemyGroup1Shield.transform : null, compressed, full, 0, t);
            ApplyPose(targets.EnemyGroup2Shield ? targets.EnemyGroup2Shield.transform : null, compressed, full, 1, t);
            ApplyPose(targets.PlayerGroup1Shield ? targets.PlayerGroup1Shield.transform : null, compressed, full, 2, t);
            ApplyPose(targets.PlayerGroup2Shield ? targets.PlayerGroup2Shield.transform : null, compressed, full, 3, t);
        }

        private static void ApplyHeroSlotPoses(HeroSlotView[] views, BattleFieldUnitPose[] compressed,
            BattleFieldUnitPose[] full, float t)
        {
            if (views == null)
                return;

            for (var i = 0; i < views.Length; i++)
                ApplyPose(views[i] ? views[i].transform : null, compressed, full, i, t);
        }

        private static void ApplyPose(Transform target, BattleFieldUnitPose[] compressed, BattleFieldUnitPose[] full,
            int index, float t)
        {
            if (false == target || compressed == null || full == null
                || index < 0 || index >= compressed.Length || index >= full.Length)
                return;

            ApplyPose(target, BattleFieldUnitPose.Lerp(compressed[index], full[index], t));
        }

        private static void ApplyPose(Transform target, BattleFieldUnitPose pose)
        {
            if (false == target)
                return;

            target.localPosition = pose.LocalPosition;
            target.localScale = pose.LocalScale;
            target.GetComponent<HeroSlotView>()?.CaptureCurrentLayoutPose();
            target.GetComponent<AvatarSlotView>()?.CaptureCurrentLayoutPose();
        }

        private static BattleFieldUnitPose[] CaptureHeroSlotPoses(HeroSlotView[] slots)
        {
            if (slots == null)
                return Array.Empty<BattleFieldUnitPose>();

            var result = new BattleFieldUnitPose[slots.Length];
            for (var i = 0; i < slots.Length; i++)
                result[i] = CapturePose(slots[i] ? slots[i].transform : null);
            
            return result;
        }

        private static BattleFieldUnitPose CapturePose(Transform target)
        {
            return target
                ? new BattleFieldUnitPose(target.localPosition, target.localScale)
                : BattleFieldUnitPose.Identity;
        }

        private static void SetLocalY(Transform target, float y)
        {
            var localPosition = target.localPosition;
            if (Mathf.Approximately(localPosition.y, y))
                return;

            target.localPosition = new Vector3(localPosition.x, y, localPosition.z);
        }

        private static float CalculateRendererScaleX(SpriteRenderer targetRenderer, SpriteRenderer sourceRenderer)
        {
            if (false == targetRenderer || false == targetRenderer.sprite)
                return 1f;

            var width = sourceRenderer ? sourceRenderer.size.x : targetRenderer.size.x;
            var spriteWidth = targetRenderer.sprite.bounds.size.x;
            
            return spriteWidth > 0f ? width / spriteWidth : 1f;
        }

        private static float CalculateScaleFromSpriteSize(SpriteRenderer targetRenderer, float worldSize, bool axisX)
        {
            if (false == targetRenderer || false == targetRenderer.sprite)
                return 1f;

            var spriteSize = axisX
                ? targetRenderer.sprite.bounds.size.x
                : targetRenderer.sprite.bounds.size.y;

            return spriteSize > 0f ? worldSize / spriteSize : 1f;
        }

        private static float CalculateRendererScaleY(SpriteRenderer targetRenderer, float height)
        {
            if (false == targetRenderer || false == targetRenderer.sprite)
                return 1f;

            var spriteHeight = targetRenderer.sprite.bounds.size.y;
            
            return spriteHeight > 0f ? height / spriteHeight : 1f;
        }
    }

    public readonly struct BattleFieldLayoutTargets
    {
        public readonly AvatarSlotView PlayerAvatarSlot;
        public readonly AvatarSlotView EnemyAvatarSlot;
        public readonly HeroSlotView[] PlayerHeroSlots;
        public readonly HeroSlotView[] EnemyHeroSlots;
        public readonly GroupShieldView EnemyGroup1Shield;
        public readonly GroupShieldView EnemyGroup2Shield;
        public readonly GroupShieldView PlayerGroup1Shield;
        public readonly GroupShieldView PlayerGroup2Shield;

        
        public BattleFieldLayoutTargets(
            AvatarSlotView playerAvatarSlot,
            AvatarSlotView enemyAvatarSlot,
            HeroSlotView[] playerHeroSlots,
            HeroSlotView[] enemyHeroSlots,
            GroupShieldView enemyGroup1Shield,
            GroupShieldView enemyGroup2Shield,
            GroupShieldView playerGroup1Shield,
            GroupShieldView playerGroup2Shield)
        {
            PlayerAvatarSlot = playerAvatarSlot;
            EnemyAvatarSlot = enemyAvatarSlot;
            PlayerHeroSlots = playerHeroSlots;
            EnemyHeroSlots = enemyHeroSlots;
            EnemyGroup1Shield = enemyGroup1Shield;
            EnemyGroup2Shield = enemyGroup2Shield;
            PlayerGroup1Shield = playerGroup1Shield;
            PlayerGroup2Shield = playerGroup2Shield;
        }
    }
}