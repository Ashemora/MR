using Project.Scripts.Gameplay.Battle.Units;
using Project.Scripts.Gameplay.UI;
using R3;
using UnityEngine;
using UnityEngine.Pool;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class BattleFieldFloatingNumbers : MonoBehaviour
    {
        private const int DefaultPoolCapacity = 4;
        private const int MaxPoolSize = 16;

        
        [Tooltip("Префаб с компонентом FloatingDamageNumber")]
        [SerializeField] private FloatingDamageNumber _floatingDamagePrefab;

        
        private ObjectPool<FloatingDamageNumber> _floatingPool;

        
        public void Setup(BattleFieldViewModel viewModel, AvatarSlotView playerAvatarSlot, AvatarSlotView enemyAvatarSlot,
            HeroSlotView[] playerHeroSlots, HeroSlotView[] enemyHeroSlots, CompositeDisposable disposables)
        {
            if (false == _floatingDamagePrefab || viewModel == null || disposables == null)
                return;

            Cleanup();
            _floatingPool = new ObjectPool<FloatingDamageNumber>(
                createFunc: () => Instantiate(_floatingDamagePrefab, transform),
                actionOnGet: c => c.gameObject.SetActive(true),
                actionOnRelease: c => { c.Kill(); c.gameObject.SetActive(false); },
                actionOnDestroy: c => { if (c) Destroy(c.gameObject); },
                defaultCapacity: DefaultPoolCapacity,
                maxSize: MaxPoolSize);

            viewModel.EnemyAvatar.Hit
                .Subscribe(dmg => SpawnFloatingNumber(dmg, FloatingNumberType.Damage, enemyAvatarSlot.HitAnchor, viewModel))
                .AddTo(disposables);

            viewModel.PlayerAvatar.Hit
                .Subscribe(dmg => SpawnFloatingNumber(dmg, FloatingNumberType.Damage, playerAvatarSlot.HitAnchor, viewModel))
                .AddTo(disposables);

            viewModel.EnemyAvatar.Heal
                .Subscribe(amt => SpawnFloatingNumber(amt, FloatingNumberType.Heal, enemyAvatarSlot.HitAnchor, viewModel))
                .AddTo(disposables);

            viewModel.PlayerAvatar.Heal
                .Subscribe(amt => SpawnFloatingNumber(amt, FloatingNumberType.Heal, playerAvatarSlot.HitAnchor, viewModel))
                .AddTo(disposables);

            BindHeroFloatingNumbers(playerHeroSlots, viewModel.PlayerHeroSlots, viewModel, disposables);
            BindHeroFloatingNumbers(enemyHeroSlots, viewModel.EnemyHeroSlots, viewModel, disposables);
        }

        public void Cleanup()
        {
            _floatingPool?.Dispose();
            _floatingPool = null;
        }
        

        private void BindHeroFloatingNumbers(HeroSlotView[] views, HeroSlotViewModel[] viewModels,
            BattleFieldViewModel battleFieldViewModel, CompositeDisposable disposables)
        {
            if (null == views || null == viewModels)
                return;

            var count = Mathf.Min(views.Length, viewModels.Length);
            for (var i = 0; i < count; i++)
            {
                if (false == views[i] || false == viewModels[i].IsAssigned)
                    continue;

                var anchor = views[i].HitAnchor;
                var vm = viewModels[i];

                vm.Hit
                    .Subscribe(dmg => SpawnFloatingNumber(dmg, FloatingNumberType.Damage, anchor, battleFieldViewModel))
                    .AddTo(disposables);

                vm.Heal
                    .Subscribe(amt => SpawnFloatingNumber(amt, FloatingNumberType.Heal, anchor, battleFieldViewModel))
                    .AddTo(disposables);
            }
        }

        private void SpawnFloatingNumber(int value, FloatingNumberType type, Transform anchor, BattleFieldViewModel viewModel)
        {
            if (null == _floatingPool || false == anchor)
                return;

            var item = _floatingPool.Get();
            item.Play(value, type, anchor, viewModel.BattleAnimConfig,
                () => _floatingPool.Release(item));
        }
    }
}