using System;
using DG.Tweening;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Utils.Buttons;
using UnityEngine;

namespace Project.Scripts.Configs.Battle
{
    [CreateAssetMenu(fileName = "BattleFieldLayoutConfig", menuName = "Configs/Battle/Battle Field Layout Config")]
    public class BattleFieldLayoutConfig : ScriptableObject
    {
#if UNITY_EDITOR
        private const string DefaultCompressedPrefabPath = "Assets/Project/Prefabs/Battle/BattleFieldView_Compressed.prefab";
        private const string DefaultFullPrefabPath = "Assets/Project/Prefabs/Battle/BattleFieldView_Full.prefab";
#endif

        [Header("Battle Field Prefabs")]
        [Tooltip("Prefab сжатого состояния боевого поля. Используется как источник snapshot-данных для Match-фазы.")]
        [SerializeField] private GameObject _compressedBattleFieldPrefab;

        [Tooltip("Prefab полного состояния боевого поля. Используется как источник snapshot-данных для Hero-фазы.")]
        [SerializeField] private GameObject _fullBattleFieldPrefab;

        [Header("Captured Profiles")]
        [SerializeField] private BattleFieldLayoutSnapshot _compressedProfile = BattleFieldLayoutSnapshot.CreateDefault();
        [SerializeField] private BattleFieldLayoutSnapshot _fullProfile = BattleFieldLayoutSnapshot.CreateDefault();

        [Header("Transition")]
        [Tooltip("Длительность перехода между сжатым и полным состояниями боевого поля.")]
        [Min(0f)]
        [SerializeField] private float _transitionDuration = 0.35f;

        [Tooltip("Кривая перехода между сжатым и полным состояниями боевого поля.")]
        [SerializeField] private Ease _transitionEase = Ease.InOutSine;

        [Header("Hero Phase World Stack")]
        [Tooltip("Дополнительное смещение доски и energy bars вниз в Hero-фазе сверх прироста высоты BattleField, в долях высоты доски.")]
        [Min(0f)]
        [SerializeField] private float _heroPhaseBoardOffsetFrameHeight = 0f;

        public GameObject CompressedBattleFieldPrefab => _compressedBattleFieldPrefab;
        public GameObject FullBattleFieldPrefab => _fullBattleFieldPrefab;
        public BattleFieldLayoutSnapshot CompressedProfile => _compressedProfile;
        public BattleFieldLayoutSnapshot FullProfile => _fullProfile;
        public float TransitionDuration => _transitionDuration;
        public Ease TransitionEase => _transitionEase;
        public float HeroPhaseBoardOffsetFrameHeight => _heroPhaseBoardOffsetFrameHeight;

        
        [Button]
        public void CaptureFromPrefabs()
        {
#if UNITY_EDITOR
            EnsureDefaultPrefabReferences();

            if (false == ValidateBattleFieldPrefabs(out var error))
            {
                Debug.LogError($"BattleFieldLayoutConfig capture failed: {error}", this);
                return;
            }

            var compressedView = GetBattleFieldView(_compressedBattleFieldPrefab);
            var fullView = GetBattleFieldView(_fullBattleFieldPrefab);
            _compressedProfile = compressedView.CaptureLayoutSnapshot();
            _fullProfile = fullView.CaptureLayoutSnapshot();

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            Debug.Log("BattleFieldLayoutConfig captured compressed/full snapshots from authoring prefabs.", this);
#else
            Debug.LogWarning("BattleFieldLayoutConfig capture is only available in the Unity Editor.", this);
#endif
        }

        [Button]
        private void PreviewCompressed()
        {
#if UNITY_EDITOR
            ApplyScenePreview(_compressedProfile, 0f, "Preview Compressed BattleField");
#else
            Debug.LogWarning("BattleFieldLayoutConfig preview is only available in the Unity Editor.", this);
#endif
        }

        [Button]
        private void PreviewFull()
        {
#if UNITY_EDITOR
            ApplyScenePreview(_fullProfile, CalculateHeroPhaseBoardOffset(), "Preview Full BattleField");
#else
            Debug.LogWarning("BattleFieldLayoutConfig preview is only available in the Unity Editor.", this);
#endif
        }

        [Button]
        private void ResetSceneToCompressed()
        {
#if UNITY_EDITOR
            ApplyScenePreview(_compressedProfile, 0f, "Reset BattleField Preview To Compressed");
#else
            Debug.LogWarning("BattleFieldLayoutConfig preview reset is only available in the Unity Editor.", this);
#endif
        }

        private bool ValidateBattleFieldPrefabs(out string error)
        {
#if UNITY_EDITOR
            EnsureDefaultPrefabReferences();
#endif

            if (false == _compressedBattleFieldPrefab)
            {
                error = "Compressed BattleField prefab is not assigned.";
                return false;
            }

            if (false == _fullBattleFieldPrefab)
            {
                error = "Full BattleField prefab is not assigned.";
                return false;
            }

            var compressedView = GetBattleFieldView(_compressedBattleFieldPrefab);
            var fullView = GetBattleFieldView(_fullBattleFieldPrefab);
            if (false == compressedView)
            {
                error = "Compressed BattleField prefab does not contain BattleFieldView.";
                return false;
            }

            if (false == fullView)
            {
                error = "Full BattleField prefab does not contain BattleFieldView.";
                return false;
            }

            if (false == compressedView.HasCompatibleLayoutStructure(fullView, out error))
                return false;

            error = string.Empty;
            
            return true;
        }

        private static BattleFieldView GetBattleFieldView(GameObject prefab)
        {
            return prefab ? prefab.GetComponent<BattleFieldView>() : null;
        }

#if UNITY_EDITOR
        private void EnsureDefaultPrefabReferences()
        {
            if (false == _compressedBattleFieldPrefab)
                _compressedBattleFieldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultCompressedPrefabPath);

            if (false == _fullBattleFieldPrefab)
                _fullBattleFieldPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultFullPrefabPath);

            if (_compressedBattleFieldPrefab || _fullBattleFieldPrefab)
                UnityEditor.EditorUtility.SetDirty(this);
        }

        private void ApplyScenePreview(BattleFieldLayoutSnapshot snapshot, float boardAndEnergyYOffset, string undoName)
        {
            var layout = UnityEngine.Object.FindFirstObjectByType<BattleWorldLayout>();
            if (false == layout || false == layout.BattleFieldView)
            {
                Debug.LogError("BattleFieldLayoutConfig preview failed: BattleWorldLayout with BattleFieldView was not found in the open scene.", this);
                return;
            }

            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(layout.gameObject, undoName);
            layout.BattleFieldView.ApplyLayoutSnapshotPreservingTop(snapshot);
            layout.SetBoardAndEnergyPreviewYOffset(boardAndEnergyYOffset);
            layout.RefreshBindings();

            UnityEditor.EditorUtility.SetDirty(layout);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
            Debug.Log($"{undoName} applied to open Gameplay scene.", this);
        }

        private float CalculateHeroPhaseBoardOffset()
        {
            var layout = UnityEngine.Object.FindFirstObjectByType<BattleWorldLayout>();
            if (false == layout || false == layout.BattleFieldView)
                return 0f;

            var heightDelta = Mathf.Max(0f, _fullProfile.LayoutHeight - _compressedProfile.LayoutHeight);
            var extraOffset = layout.GetBoardWorldHeight() * _heroPhaseBoardOffsetFrameHeight;
            
            return -(heightDelta * layout.BattleFieldView.LayoutScale + extraOffset);
        }
#endif
    }

    [Serializable]
    public struct BattleFieldLayoutSnapshot
    {
        [Min(0.01f)]
        [SerializeField] private float _layoutHeight;
        [SerializeField] private BattleFieldUnitPose _playerAvatar;
        [SerializeField] private BattleFieldUnitPose _enemyAvatar;
        [SerializeField] private BattleFieldUnitPose[] _playerHeroSlots;
        [SerializeField] private BattleFieldUnitPose[] _enemyHeroSlots;
        [SerializeField] private BattleFieldUnitPose[] _groupShields;
        [SerializeField] private BattleFieldUnitPose _playerPanel;
        [SerializeField] private BattleFieldUnitPose _enemyPanel;

        
        public float LayoutHeight => _layoutHeight;
        public BattleFieldUnitPose PlayerAvatar => _playerAvatar;
        public BattleFieldUnitPose EnemyAvatar => _enemyAvatar;
        public BattleFieldUnitPose[] PlayerHeroSlots => _playerHeroSlots;
        public BattleFieldUnitPose[] EnemyHeroSlots => _enemyHeroSlots;
        public BattleFieldUnitPose[] GroupShields => _groupShields;
        public BattleFieldUnitPose PlayerPanel => _playerPanel;
        public BattleFieldUnitPose EnemyPanel => _enemyPanel;

        
        public BattleFieldLayoutSnapshot(
            float layoutHeight,
            BattleFieldUnitPose playerAvatar,
            BattleFieldUnitPose enemyAvatar,
            BattleFieldUnitPose[] playerHeroSlots,
            BattleFieldUnitPose[] enemyHeroSlots,
            BattleFieldUnitPose[] groupShields,
            BattleFieldUnitPose playerPanel,
            BattleFieldUnitPose enemyPanel)
        {
            _layoutHeight = Mathf.Max(0.01f, layoutHeight);
            _playerAvatar = playerAvatar;
            _enemyAvatar = enemyAvatar;
            _playerHeroSlots = playerHeroSlots ?? Array.Empty<BattleFieldUnitPose>();
            _enemyHeroSlots = enemyHeroSlots ?? Array.Empty<BattleFieldUnitPose>();
            _groupShields = groupShields ?? Array.Empty<BattleFieldUnitPose>();
            _playerPanel = playerPanel;
            _enemyPanel = enemyPanel;
        }

        public static BattleFieldLayoutSnapshot CreateDefault()
        {
            return new BattleFieldLayoutSnapshot
            {
                _layoutHeight = 4.2f,
                _playerAvatar = BattleFieldUnitPose.Identity,
                _enemyAvatar = BattleFieldUnitPose.Identity,
                _playerHeroSlots = CreatePoseArray(4),
                _enemyHeroSlots = CreatePoseArray(4),
                _groupShields = CreatePoseArray(4),
                _playerPanel = BattleFieldUnitPose.Identity,
                _enemyPanel = BattleFieldUnitPose.Identity
            };
        }

        private static BattleFieldUnitPose[] CreatePoseArray(int count)
        {
            var result = new BattleFieldUnitPose[count];
            for (var i = 0; i < result.Length; i++)
                result[i] = BattleFieldUnitPose.Identity;
            
            return result;
        }
    }

    [Serializable]
    public struct BattleFieldUnitPose
    {
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localScale;

        
        public Vector3 LocalPosition => _localPosition;
        public Vector3 LocalScale => _localScale;
        public static BattleFieldUnitPose Identity => new(Vector3.zero, Vector3.one);

        
        public BattleFieldUnitPose(Vector3 localPosition, Vector3 localScale)
        {
            _localPosition = localPosition;
            _localScale = localScale;
        }

        public static BattleFieldUnitPose Lerp(BattleFieldUnitPose from, BattleFieldUnitPose to, float t)
        {
            return new BattleFieldUnitPose(
                Vector3.LerpUnclamped(from.LocalPosition, to.LocalPosition, t),
                Vector3.LerpUnclamped(from.LocalScale, to.LocalScale, t));
        }
    }
}