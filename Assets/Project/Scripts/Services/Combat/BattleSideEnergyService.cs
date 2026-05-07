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
        private readonly RoundEnergyCapSchedule _capSchedule;
        private readonly SideEnergyPoolEngine _playerPool;
        private readonly SideEnergyPoolEngine _enemyPool;
        private int _currentRound = 1;
        private IDisposable _energyGeneratedSubscription;
        private IDisposable _roundChangedSubscription;


        public int EnergyCap => _capSchedule.GetCapForRound(_currentRound);


        public BattleSideEnergyService(EventBus eventBus, DebugConfig debugConfig, BattleFlowConfig battleFlowConfig,
            IBattleEconomyModifierService battleEconomyModifier, IEnergyGainModifierService energyGainModifier)
        {
            _eventBus = eventBus;
            _debugConfig = debugConfig;
            _battleFlowConfig = battleFlowConfig;
            _battleEconomyModifier = battleEconomyModifier;
            _energyGainModifier = energyGainModifier;
            _capSchedule = new RoundEnergyCapSchedule(battleFlowConfig.EnergyCaps);

            if (false == _capSchedule.HasExplicitCaps)
                UnityEngine.Debug.LogError(
                    $"[BattleSideEnergyService] BattleFlowConfig.EnergyCaps is empty. Falling back to RoundEnergyCapSchedule.DefaultCap={RoundEnergyCapSchedule.DefaultCap} for all rounds.");

            var initialCap = _capSchedule.GetCapForRound(_currentRound);
            _playerPool = new SideEnergyPoolEngine(initialCap);
            _enemyPool = new SideEnergyPoolEngine(initialCap);
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
            _currentRound = e.CurrentRound;

            var newCap = _capSchedule.GetCapForRound(_currentRound);
            _playerPool.SetCap(newCap);
            _enemyPool.SetCap(newCap);

            if (_battleFlowConfig.EnergyCarryoverMode == EnergyCarryoverMode.ResetEachRound && e.CurrentRound > 1)
            {
                Reset(BattleSide.Player);
                Reset(BattleSide.Enemy);
                
                return;
            }

            PublishEnergyChanged(BattleSide.Player);
            PublishEnergyChanged(BattleSide.Enemy);
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