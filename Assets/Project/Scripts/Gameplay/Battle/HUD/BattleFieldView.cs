using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.Results;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.Battle.Units;
using Project.Scripts.Services.Input;
using Project.Scripts.Services.UISystem;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Utils.Buttons;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class BattleFieldView : BaseView<BattleFieldViewModel>, IGameResultVisuals
    {
        [Tooltip("Четыре вида слота героя для стороны игрока, упорядочены слева направо (индексы 0-3)")]
        [SerializeField] private HeroSlotView[] _playerHeroSlots;

        [Tooltip("Четыре вида слота героя для стороны врага, упорядочены слева направо (индексы 0-3)")]
        [SerializeField] private HeroSlotView[] _enemyHeroSlots;

        [Tooltip("Вид слота аватара игрока")]
        [SerializeField] private AvatarSlotView _playerAvatarSlot;

        [Tooltip("Вид слота аватара врага")]
        [SerializeField] private AvatarSlotView _enemyAvatarSlot;

        [Tooltip("Обрабатывает жесты перетаскивания к цели в мировом пространстве")]
        [SerializeField] private TargetingInputHandler _targetingInputHandler;

        [Tooltip("Optional HUD-side energy orb FX view")]
        [SerializeField] private BattleEnergyFXView _energyFXView;

        [Tooltip("Управляет геометрией, snapshot-позами и фазовым blend боевого поля.")]
        [SerializeField] private BattleFieldLayoutController _layoutController;

        [Tooltip("Управляет пулом и подписками всплывающих чисел урона/лечения.")]
        [SerializeField] private BattleFieldFloatingNumbers _floatingNumbers;

        [Space(10)]
        [Tooltip("Щит первой группы героев врага - скрывается, когда первая группа уничтожена")]
        [SerializeField] private GroupShieldView _enemyGroup1Shield;

        [Tooltip("Щит второй группы героев врага - скрывается, когда вторая группа уничтожена")]
        [SerializeField] private GroupShieldView _enemyGroup2Shield;
        
        [Tooltip("Щит первой группы героев игрока - скрывается, когда первая группа уничтожена")]
        [SerializeField] private GroupShieldView _playerGroup1Shield;

        [Tooltip("Щит второй группы героев игрока - скрывается, когда вторая группа уничтожена")]
        [SerializeField] private GroupShieldView _playerGroup2Shield;


        private IInputService _inputService;
        private TileKindPaletteConfig _tileKindPalette;
        private Transform _playerEnergyAbsorbTarget;


        public float BaseLayoutHeight => _layoutController ? _layoutController.BaseLayoutHeight : 0f;
        public float LayoutScale => _layoutController ? _layoutController.LayoutScale : 1f;
        public float LayoutTopWorldY => _layoutController ? _layoutController.LayoutTopWorldY : transform.position.y;


        public BattleFieldLayoutSnapshot CaptureLayoutSnapshot()
        {
            return _layoutController
                ? _layoutController.CaptureLayoutSnapshot(CreateLayoutTargets())
                : BattleFieldLayoutSnapshot.CreateDefault();
        }

        public bool HasCompatibleLayoutStructure(BattleFieldView other, out string error)
        {
            if (false == other)
            {
                error = "Other BattleFieldView is not assigned.";
                return false;
            }

            if (false == HasRequiredLayoutReferences(out error))
                return false;

            if (false == other.HasRequiredLayoutReferences(out error))
            {
                error = $"Other BattleFieldView is invalid: {error}";
                return false;
            }

            if (GetLength(_playerHeroSlots) != GetLength(other._playerHeroSlots))
            {
                error = "Player hero slot counts do not match.";
                return false;
            }

            if (GetLength(_enemyHeroSlots) != GetLength(other._enemyHeroSlots))
            {
                error = "Enemy hero slot counts do not match.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public void ApplyLayoutSnapshotPreservingTop(BattleFieldLayoutSnapshot snapshot)
        {
            _layoutController?.ApplyLayoutSnapshotPreservingTop(snapshot, CreateLayoutTargets());
        }

        public void ApplyLayoutBlendPreservingTop(BattleFieldLayoutSnapshot compressed, BattleFieldLayoutSnapshot full, float t)
        {
            _layoutController?.ApplyLayoutBlendPreservingTop(compressed, full, t, CreateLayoutTargets());
        }

        protected override UniTask OnBindViewModel()
        {
            _layoutController?.RefreshPosition();
            BindSlots();
            BindGroupShields();
            SetupTargeting();
            BindShieldPulse();
            SetupEnergyFX();
            SetupFloatingNumbers();
            BindInteractionOverlay();

            return UniTask.CompletedTask;
        }

        protected override void OnClose()
        {
            CleanupRuntimeResources();
        }

        public void ReleaseSceneInstance()
        {
            CleanupRuntimeResources();
        }


        public void SetDependencies(IInputService inputService, TileKindPaletteConfig tileKindPalette, Transform playerEnergyAbsorbTarget)
        {
            _inputService = inputService;
            _tileKindPalette = tileKindPalette;
            _playerEnergyAbsorbTarget = playerEnergyAbsorbTarget;
        }

        public async UniTask PlayAvatarPulse(BattleSide side, AvatarPulseStepConfig config)
        {
            var targetView = side == BattleSide.Player
                ? _playerAvatarSlot
                : _enemyAvatarSlot;

            if (false == targetView)
                return;

            await targetView.PlayResultPulse(config);
        }

        public void RefreshPosition()
        {
            _layoutController?.RefreshPosition();
        }

        public void SetLayoutBottomWorldY(float worldY)
        {
            _layoutController?.SetLayoutBottomWorldY(worldY);
        }

        public void SetLayoutScale(float scale)
        {
            _layoutController?.SetLayoutScale(scale);
        }

        [Button]
        private void SaveToConfig()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:BattleFieldLayoutConfig");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError("BattleFieldView SaveToConfig failed: BattleFieldLayoutConfig asset was not found.", this);
                return;
            }

            if (guids.Length > 1)
                Debug.LogWarning("BattleFieldView SaveToConfig found multiple BattleFieldLayoutConfig assets. Using the first one.", this);

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<BattleFieldLayoutConfig>(path);
            if (false == config)
            {
                Debug.LogError($"BattleFieldView SaveToConfig failed: could not load BattleFieldLayoutConfig at '{path}'.", this);
                return;
            }

            config.CaptureFromPrefabs();
#else
            Debug.LogWarning("BattleFieldView SaveToConfig is only available in the Unity Editor.", this);
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_layoutController)
                _layoutController = GetComponent<BattleFieldLayoutController>();

            if (!_floatingNumbers)
                _floatingNumbers = GetComponent<BattleFieldFloatingNumbers>();
        }
#endif

        private bool HasRequiredLayoutReferences(out string error)
        {
            if (false == _layoutController)
            {
                error = "BattleFieldLayoutController is not assigned.";
                return false;
            }

            if (false == _layoutController.HasRequiredLayoutReferences(out error))
                return false;

            if (false == _playerAvatarSlot)
            {
                error = "Player avatar slot is not assigned.";
                return false;
            }

            if (false == _enemyAvatarSlot)
            {
                error = "Enemy avatar slot is not assigned.";
                return false;
            }

            if (false == HasAllHeroSlots(_playerHeroSlots))
            {
                error = "Player hero slots are missing or contain null references.";
                return false;
            }

            if (false == HasAllHeroSlots(_enemyHeroSlots))
            {
                error = "Enemy hero slots are missing or contain null references.";
                return false;
            }

            if (false == _enemyGroup1Shield || false == _enemyGroup2Shield
                || false == _playerGroup1Shield || false == _playerGroup2Shield)
            {
                error = "One or more group shield references are missing.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool HasAllHeroSlots(HeroSlotView[] slots)
        {
            if (slots == null || slots.Length == 0)
                return false;

            for (var i = 0; i < slots.Length; i++)
                if (false == slots[i])
                    return false;

            return true;
        }

        private static int GetLength(HeroSlotView[] slots) => slots?.Length ?? 0;

        private BattleFieldLayoutTargets CreateLayoutTargets()
        {
            return new BattleFieldLayoutTargets(
                _playerAvatarSlot,
                _enemyAvatarSlot,
                _playerHeroSlots,
                _enemyHeroSlots,
                _enemyGroup1Shield,
                _enemyGroup2Shield,
                _playerGroup1Shield,
                _playerGroup2Shield);
        }

        private void BindSlots()
        {
            _playerAvatarSlot?.Bind(ViewModel.PlayerAvatar, ViewModel.GroupDefense, ViewModel.DeathConfig);
            _enemyAvatarSlot?.Bind(ViewModel.EnemyAvatar, ViewModel.GroupDefense, ViewModel.DeathConfig);

            BindHeroRow(_playerHeroSlots, ViewModel.PlayerHeroSlots);
            BindHeroRow(_enemyHeroSlots, ViewModel.EnemyHeroSlots);
        }

        private void BindGroupShields()
        {
            var playerDef = ViewModel.GroupDefense.PlayerDefense;
            var enemyDef = ViewModel.GroupDefense.EnemyDefense;

            _playerGroup1Shield?.Bind(playerDef.Select(s => false == s.IsGroup1Destroyed));
            _playerGroup2Shield?.Bind(playerDef.Select(s => false == s.IsGroup2Destroyed));
            _enemyGroup1Shield?.Bind(enemyDef.Select(s => false == s.IsGroup1Destroyed));
            _enemyGroup2Shield?.Bind(enemyDef.Select(s => false == s.IsGroup2Destroyed));
        }

        private void BindShieldPulse()
        {
            if (false == _targetingInputHandler || false == ViewModel.BattleAnimConfig)
                return;

            var config = ViewModel.BattleAnimConfig.ShieldPulse;

            _targetingInputHandler.IsHoveringBlockedAvatar
                .Subscribe(hovering =>
                {
                    if (hovering)
                    {
                        _enemyGroup1Shield?.StartPulse(config);
                        _enemyGroup2Shield?.StartPulse(config);
                    }
                    else
                    {
                        _enemyGroup1Shield?.StopPulse();
                        _enemyGroup2Shield?.StopPulse();
                    }
                })
                .AddTo(Disposables);
        }

        private void BindHeroRow(HeroSlotView[] views, HeroSlotViewModel[] viewModels)
        {
            if (null == views || null == viewModels)
                return;

            var count = Mathf.Min(views.Length, viewModels.Length);
            for (var i = 0; i < count; i++)
            {
                if (views[i])
                    views[i].Bind(viewModels[i], ViewModel.BattleAnimConfig, ViewModel.DeathConfig);
            }
        }

        private void SetupTargeting()
        {
            if (false == _targetingInputHandler || null == _inputService)
                return;

            var registry = new TargetingRegistry();

            if (_playerAvatarSlot)
                registry.Register(_playerAvatarSlot);

            if (_enemyAvatarSlot)
                registry.Register(_enemyAvatarSlot);

            if (_playerHeroSlots != null)
                for (var i = 0; i < _playerHeroSlots.Length; i++)
                    if (_playerHeroSlots[i])
                        registry.Register(_playerHeroSlots[i]);

            if (_enemyHeroSlots != null)
                for (var i = 0; i < _enemyHeroSlots.Length; i++)
                    if (_enemyHeroSlots[i])
                        registry.Register(_enemyHeroSlots[i]);

            _targetingInputHandler.Init(
                _inputService,
                registry,
                ViewModel.AbilityExecution,
                ViewModel.GameStateService,
                ViewModel.BattleActionRuntime,
                Camera.main);
        }

        private void SetupEnergyFX()
        {
            if (false == _energyFXView)
                return;

            _energyFXView.Initialize(
                ViewModel.EventBus,
                _tileKindPalette,
                _playerEnergyAbsorbTarget,
                ViewModel.BattleAnimConfig);
        }

        private void BindInteractionOverlay()
        {
            if (false == _layoutController)
                return;

            _layoutController.SetInteractionOverlayActive(ViewModel.IsInteractionOverlayVisible.CurrentValue);
            ViewModel.IsInteractionOverlayVisible
                .Subscribe(_layoutController.SetInteractionOverlayActive)
                .AddTo(Disposables);
        }

        private void SetupFloatingNumbers()
        {
            _floatingNumbers?.Setup(
                ViewModel,
                _playerAvatarSlot,
                _enemyAvatarSlot,
                _playerHeroSlots,
                _enemyHeroSlots,
                Disposables);
        }

        private void CleanupRuntimeResources()
        {
            _energyFXView?.Cleanup();
            _floatingNumbers?.Cleanup();
        }
    }
}