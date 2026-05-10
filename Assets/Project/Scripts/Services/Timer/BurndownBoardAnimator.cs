using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Grid;
using VContainer.Unity;

namespace Project.Scripts.Services.Timer
{
    public class BurndownBoardAnimator : IStartable, IDisposable
    {
        private readonly BurndownConfig _config;
        private readonly IGridOperations _gridOps;
        private readonly IDisposable _subscription;


        public BurndownBoardAnimator(EventBus eventBus, BurndownConfig config, IGridOperations gridOps)
        {
            _config = config;
            _gridOps = gridOps;
            _subscription = eventBus.Subscribe<BurndownStartedEvent>(OnBurndownStarted);
        }


        public void Start()
        {
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }


        private void OnBurndownStarted(BurndownStartedEvent _)
        {
            _gridOps.CollapseAll(_config.CollapseAllDuration, _config.CollapseAllEase).Forget();
        }
    }
}