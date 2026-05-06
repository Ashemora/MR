using System;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Energy;
using VContainer.Unity;

namespace Project.Scripts.Services.Combat
{
    public class BattleSideEnergyService : IBattleSideEnergyService, IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly DebugConfig _debugConfig;
        private readonly BattleFlowConfig _battleFlowConfig;
        private readonly IBattleEconomyModifierService _battleEconomyModifier;
        private readonly IEnergyGainModifierService _energyGainModifier;
        private readonly SideEnergyPoolEngine _playerPool;
        private readonly SideEnergyPoolEngine _enemyPool;
        private IDisposable _energyGeneratedSubscription;
        private IDisposable _roundChangedSubscription;


        public int EnergyCap => _battleFlowConfig.EnergyCap;


        public BattleSideEnergyService(EventBus eventBus, DebugConfig debugConfig, BattleFlowConfig battleFlowConfig,
            IBattleEconomyModifierService battleEconomyModifier, IEnergyGainModifierService energyGainModifier)
        {
            _eventBus = eventBus;
            _debugConfig = debugConfig;
            _battleFlowConfig = battleFlowConfig;
            _battleEconomyModifier = battleEconomyModifier;
            _energyGainModifier = energyGainModifier;
            _playerPool = new SideEnergyPoolEngine(battleFlowConfig.EnergyCap);
            _enemyPool = new SideEnergyPoolEngine(battleFlowConfig.EnergyCap);
        }


        public void Start()
        {
            _energyGeneratedSubscription = _eventBus.Subscribe<EnergyGeneratedEvent>(OnEnergyGenerated);
            _roundChangedSubscription = _eventBus.Subscribe<BattleFlowRoundChangedEvent>(OnRoundChanged);

            PublishEnergyChanged(BattleSide.Player);
            PublishEnergyChanged(BattleSide.Enemy);
        }

        public void Dispose()
        {
            _energyGeneratedSubscription?.Dispose();
            _energyGeneratedSubscription = null;
            _roundChangedSubscription?.Dispose();
            _roundChangedSubscription = null;
        }


        public int GetDisplayEnergy(BattleSide side)
        {
            return (int)GetPool(side).Snapshot.CurrentEnergy;
        }

        public bool CanSpend(BattleSide side, int amount)
        {
            return GetPool(side).CanSpend(amount);
        }

        public bool TrySpend(BattleSide side, int amount)
        {
            if (false == GetPool(side).TrySpend(amount))
                return false;

            PublishEnergyChanged(side);
            
            return true;
        }

        public void AddEnergy(BattleSide side, float amount)
        {
            var added = GetPool(side).AddEnergy(amount);
            if (added <= 0f)
                return;

            _eventBus.Publish(new BattleSideEnergyAddedEvent(side, added));
            PublishEnergyChanged(side);
        }

        public void Reset(BattleSide side)
        {
            GetPool(side).Reset();
            PublishEnergyChanged(side);
        }


        private void OnEnergyGenerated(EnergyGeneratedEvent e)
        {
            var gain = _energyGainModifier.CalculateEnergy(e.Side, e.Breakdown)
                       * _battleEconomyModifier.CascadeEnergyMultiplier;
            if (gain <= 0f)
                return;

            if (_debugConfig.LogEnergyAccumulation)
                UnityEngine.Debug.Log($"[SharedEnergy] {e.Side} +{gain:F2}");

            AddEnergy(e.Side, gain);
        }

        private void OnRoundChanged(BattleFlowRoundChangedEvent e)
        {
            if (_battleFlowConfig.EnergyCarryoverMode != EnergyCarryoverMode.ResetEachRound)
                return;

            if (e.CurrentRound <= 1)
                return;

            Reset(BattleSide.Player);
            Reset(BattleSide.Enemy);
        }

        private void PublishEnergyChanged(BattleSide side)
        {
            _eventBus.Publish(new BattleSideEnergyChangedEvent(side, GetDisplayEnergy(side), EnergyCap));
        }

        private SideEnergyPoolEngine GetPool(BattleSide side)
        {
            return side == BattleSide.Player ? _playerPool : _enemyPool;
        }
    }
}