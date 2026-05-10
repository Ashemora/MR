using System;
using Project.Scripts.Configs.Battle.Visuals;
using Project.Scripts.Configs.Board;
using Project.Scripts.Gameplay.Battle.FX;
using Project.Scripts.Services.Events;
using Project.Scripts.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class BattleEnergyFXView : MonoBehaviour
    {
        private const int DefaultPoolCapacity = 4;
        private const int MaxPoolSize = 16;


        [Tooltip("Prefab with EnergyTransferView component")]
        [SerializeField] private EnergyTransferView _energyTransferPrefab;

        [Tooltip("Optional container for spawned energy transfer instances")]
        [SerializeField] private Transform _fxContainer;


        private BattleAnimationConfig _animationConfig;
        private TileKindPaletteConfig _palette;
        private IDisposable _subscription;
        private ObjectPool<EnergyTransferView> _transferPool;
        private Transform _playerEnergyAbsorbTarget;


        private void OnDestroy()
        {
            Cleanup();
        }


        public void Initialize(
            EventBus eventBus,
            TileKindPaletteConfig palette,
            Transform playerEnergyAbsorbTarget,
            BattleAnimationConfig animationConfig)
        {
            Cleanup();

            if (false == _energyTransferPrefab || null == eventBus || null == animationConfig)
                return;

            _animationConfig = animationConfig;
            _palette = palette;
            _playerEnergyAbsorbTarget = playerEnergyAbsorbTarget;

            var container = _fxContainer ? _fxContainer : transform;
            _transferPool = new ObjectPool<EnergyTransferView>(
                createFunc: () => Instantiate(_energyTransferPrefab, container),
                actionOnGet: transfer => transfer.gameObject.SetActive(true),
                actionOnRelease: transfer =>
                {
                    transfer.Kill();
                    transfer.gameObject.SetActive(false);
                },
                actionOnDestroy: transfer =>
                {
                    if (transfer)
                        Destroy(transfer.gameObject);
                },
                defaultCapacity: DefaultPoolCapacity,
                maxSize: MaxPoolSize);

            _subscription = eventBus.Subscribe<EnergyGeneratedVisualEvent>(OnEnergyGenerated);
        }

        public void Cleanup()
        {
            _subscription?.Dispose();
            _subscription = null;
            _transferPool?.Dispose();
            _transferPool = null;
            _animationConfig = null;
            _palette = null;
            _playerEnergyAbsorbTarget = null;
        }


        private void OnEnergyGenerated(EnergyGeneratedVisualEvent e)
        {
            if (null == _transferPool || false == _playerEnergyAbsorbTarget)
                return;

            foreach (var pair in e.SourceByKind)
            {
                var from = pair.Value.ToUnityVector3();
                var to = BuildAbsorbTargetPosition(from);
                var color = _palette ? _palette.GetColor(pair.Key, Color.white) : Color.white;

                var transfer = _transferPool.Get();
                transfer.Play(
                    from,
                    to,
                    color,
                    _animationConfig,
                    () => _transferPool.Release(transfer));
            }
        }

        private Vector3 BuildAbsorbTargetPosition(Vector3 from)
        {
            return new Vector3(from.x, _playerEnergyAbsorbTarget.position.y, from.z);
        }
    }
}