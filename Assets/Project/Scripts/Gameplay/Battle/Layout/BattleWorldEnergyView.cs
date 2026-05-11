using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Services.Announcements;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Layout
{
    public class BattleWorldEnergyView : MonoBehaviour
    {
        [Tooltip("Бар энергии игрока")]
        [SerializeField] private EnergyBarView _playerBar;

        [Tooltip("Бар энергии врага")]
        [SerializeField] private EnergyBarView _enemyBar;

        [Tooltip("Маркер высоты, на которой сферы энергии игрока поглощаются общим запасом")]
        [SerializeField] private Transform _playerEnergyAbsorbTarget;


        public Transform PlayerEnergyAbsorbTarget => _playerEnergyAbsorbTarget ? _playerEnergyAbsorbTarget : _playerBar ? _playerBar.transform : null;
        public float PlayerEnergyHeight => _playerBar ? _playerBar.Height : 0f;
        public float EnemyEnergyHeight => _enemyBar ? _enemyBar.Height : 0f;
        public float PlayerEnergyBaseHeight => _playerBar ? _playerBar.BaseHeight : 0f;
        public float EnemyEnergyBaseHeight => _enemyBar ? _enemyBar.BaseHeight : 0f;


        private CompositeDisposable _disposables;
        private bool _previewOffsetBaselineCaptured;
        private Vector3 _playerBarBaseLocalPosition;
        private Vector3 _enemyBarBaseLocalPosition;
        private Vector3 _playerEnergyAbsorbTargetBaseLocalPosition;


        private void OnDestroy()
        {
            Cleanup();
        }


        public void SetPlayerEnergyWorldY(float worldCenterY)
        {
            _playerBar?.SetWorldCenterY(worldCenterY);

            if (_playerEnergyAbsorbTarget && _playerBar && _playerEnergyAbsorbTarget.parent != _playerBar.transform)
            {
                var absorbPos = _playerEnergyAbsorbTarget.position;
                _playerEnergyAbsorbTarget.position = new Vector3(absorbPos.x, worldCenterY, absorbPos.z);
            }
        }

        public void SetEnemyEnergyWorldY(float worldCenterY)
        {
            _enemyBar?.SetWorldCenterY(worldCenterY);
        }

        public void SetLayoutScale(float scale)
        {
            _playerBar?.SetLayoutScale(scale);
            _enemyBar?.SetLayoutScale(scale);
        }

        public void SetPreviewLocalYOffset(float offset)
        {
            if (false == _previewOffsetBaselineCaptured && Mathf.Approximately(offset, 0f))
                return;

            CapturePreviewOffsetBaseline();

            ApplyLocalYOffset(_playerBar ? _playerBar.transform : null, _playerBarBaseLocalPosition, offset);
            ApplyLocalYOffset(_enemyBar ? _enemyBar.transform : null, _enemyBarBaseLocalPosition, offset);

            if (_playerEnergyAbsorbTarget && (false == _playerBar || _playerEnergyAbsorbTarget.parent != _playerBar.transform))
                ApplyLocalYOffset(_playerEnergyAbsorbTarget, _playerEnergyAbsorbTargetBaseLocalPosition, offset);
        }

        public void Bind(BattleFieldViewModel viewModel, IBoardAnnouncementService announcementService = null)
        {
            Cleanup();
            _disposables = new CompositeDisposable();

            if (null == viewModel)
                return;

            _playerBar?.SetMaxValue(viewModel.EnergyCap.CurrentValue);
            _enemyBar?.SetMaxValue(viewModel.EnergyCap.CurrentValue);
            _playerBar?.SetValue(viewModel.PlayerEnergy.CurrentValue, false);
            _enemyBar?.SetValue(viewModel.EnemyEnergy.CurrentValue, false);

            _disposables.Add(viewModel.PlayerEnergy.Subscribe(v => _playerBar?.SetValue(v)));
            _disposables.Add(viewModel.EnemyEnergy.Subscribe(v => _enemyBar?.SetValue(v)));
            _disposables.Add(viewModel.EnergyCap.Subscribe(cap =>
            {
                _playerBar?.SetMaxValue(cap);
                _enemyBar?.SetMaxValue(cap);
            }));

            if (announcementService != null)
            {
                _disposables.Add(announcementService.IsEnergyTextHidden.Subscribe(hidden =>
                {
                    _playerBar?.SetTextVisible(!hidden);
                    _enemyBar?.SetTextVisible(!hidden);
                }));
            }
        }

        public void Cleanup()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void CapturePreviewOffsetBaseline()
        {
            if (_previewOffsetBaselineCaptured)
                return;

            _previewOffsetBaselineCaptured = true;
            _playerBarBaseLocalPosition = _playerBar ? _playerBar.transform.localPosition : Vector3.zero;
            _enemyBarBaseLocalPosition = _enemyBar ? _enemyBar.transform.localPosition : Vector3.zero;
            _playerEnergyAbsorbTargetBaseLocalPosition = _playerEnergyAbsorbTarget
                ? _playerEnergyAbsorbTarget.localPosition
                : Vector3.zero;
        }

        private static void ApplyLocalYOffset(Transform target, Vector3 baseLocalPosition, float offset)
        {
            if (false == target)
                return;

            target.localPosition = new Vector3(
                baseLocalPosition.x,
                baseLocalPosition.y + offset,
                baseLocalPosition.z);
        }
    }
}