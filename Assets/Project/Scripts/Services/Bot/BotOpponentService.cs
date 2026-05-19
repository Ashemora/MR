using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Gameplay.Battle;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Bot;
using R3;
using UnityEngine;
using VContainer.Unity;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Bot
{
    public class BotOpponentService : IBotOpponentService, IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IUnitAbilityActivationService _unitAbilityActivationService;
        private readonly IAbilityExecutionService _abilityExecutionService;
        private readonly IBotActionCandidateBuilder _candidateBuilder;
        private readonly IBattleEconomyModifierService _battleEconomyModifier;
        private readonly BotStrengthConfig _botStrengthConfig;
        private readonly EffectiveBotConfigProvider _effectiveBotConfigProvider;

        private System.Random _delayRandom;
        private BotUtilityDecisionEngine _utilityEngine;
        private BotUtilityProfile _utilityProfile;
        private CancellationTokenSource _cts;
        private IDisposable _stateSub;
        private IDisposable _phaseSub;
        private readonly bool[] _heroActivationPending = new bool[4];
        private bool _dischargeScheduled;


        public BotOpponentService(
            EventBus eventBus,
            IGameStateService gameStateService,
            IBattleFlowService battleFlowService,
            IBattleSideEnergyService battleSideEnergyService,
            IUnitAbilityActivationService unitAbilityActivationService,
            IAbilityExecutionService abilityExecutionService,
            IBotActionCandidateBuilder candidateBuilder,
            IBattleEconomyModifierService battleEconomyModifier,
            BotStrengthConfig botStrengthConfig,
            EffectiveBotConfigProvider effectiveBotConfigProvider)
        {
            _eventBus = eventBus;
            _gameStateService = gameStateService;
            _battleFlowService = battleFlowService;
            _battleSideEnergyService = battleSideEnergyService;
            _unitAbilityActivationService = unitAbilityActivationService;
            _abilityExecutionService = abilityExecutionService;
            _candidateBuilder = candidateBuilder;
            _battleEconomyModifier = battleEconomyModifier;
            _botStrengthConfig = botStrengthConfig;
            _effectiveBotConfigProvider = effectiveBotConfigProvider;
        }


        public void Start()
        {
            if (false == _botStrengthConfig.Enabled)
                return;

            var seed = UnityEngine.Random.Range(0, int.MaxValue);
            _delayRandom = new System.Random(seed);
            _utilityEngine = new BotUtilityDecisionEngine(seed ^ 0x2D31);
            var strategyConfig = _effectiveBotConfigProvider.BotStrategyConfig;
            _utilityProfile = strategyConfig ? strategyConfig.ToProfile() : CreateFallbackProfile();

            _stateSub = _gameStateService.State
                .Where(s => s != GameState.Playing)
                .Take(1)
                .Subscribe(_ => StopLoops());
            _phaseSub = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);

            if (_battleFlowService.IsInitialized)
                SyncLoopState(_battleFlowService.Snapshot.Phase);
        }

        public void Dispose()
        {
            StopLoops();
            _stateSub?.Dispose();
            _stateSub = null;
            _phaseSub?.Dispose();
            _phaseSub = null;
        }


        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            SyncLoopState(e.Phase);
        }

        private async UniTaskVoid RunEnemyChargeLoop(CancellationToken ct)
        {
            while (false == ct.IsCancellationRequested)
            {
                var interval = _botStrengthConfig.MatchEnergyTickInterval /
                               _battleEconomyModifier.AutoEnergyIntervalMultiplier;
                var cancelled = await UniTask
                    .Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled || false == _gameStateService.IsPlaying)
                    return;

                var phase = _battleFlowService.Snapshot.Phase;
                if (phase == BattlePhaseKind.Match)
                {
                    var generatedEnergy = GenerateSimulatedMatchEnergy();
                    _battleSideEnergyService.AddEnergy(BattleSide.Enemy, generatedEnergy);
                }

                if (phase == BattlePhaseKind.Hero && false == _dischargeScheduled
                                                   && TryPreviewEnemyAvatar(out _))
                {
                    _dischargeScheduled = true;
                    ScheduleDischarge(ct).Forget();
                }
            }
        }

        private float RollCascadeMultiplier()
        {
            var roll = UnityEngine.Random.value;
            if (roll < _botStrengthConfig.GreatCascadeChance)
                return _botStrengthConfig.GreatCascadeMultiplier;
            if (roll < _botStrengthConfig.GoodCascadeChance)
                return _botStrengthConfig.GoodCascadeMultiplier;
            
            return 1f;
        }

        private int GenerateSimulatedMatchEnergy()
        {
            var variation = 1f + UnityEngine.Random.Range(-_botStrengthConfig.CascadeVariation,
                _botStrengthConfig.CascadeVariation);
            var baseEnergy = _botStrengthConfig.BaseMatchEnergyPerTick * variation * RollCascadeMultiplier();
            
            return Mathf.Max(1, Mathf.RoundToInt(baseEnergy));
        }

        private async UniTaskVoid ScheduleDischarge(CancellationToken ct)
        {
            var delay = GenerateDelay(_botStrengthConfig.MinAvatarActivationDelay,
                _botStrengthConfig.MaxAvatarActivationDelay);

            var cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct)
                .SuppressCancellationThrow();

            _dischargeScheduled = false;

            if (cancelled || false == _gameStateService.IsPlaying)
                return;

            if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                return;

            if (false == TryPreviewEnemyAvatar(out _))
                return;

            if (TryChooseAction(UnitDescriptor.Avatar(BattleSide.Enemy), out var decision))
                _abilityExecutionService.Execute(decision.Source, decision.Target);
        }

        private bool TryPreviewEnemyAvatar(out UnitAbilityActivationState preview)
        {
            return _unitAbilityActivationService.TryPreview(
                UnitDescriptor.Avatar(BattleSide.Enemy), out preview);
        }

        private async UniTaskVoid RunHeroEnergyLoop(CancellationToken ct)
        {
            while (false == ct.IsCancellationRequested)
            {
                var interval = _botStrengthConfig.HeroActivationCheckInterval /
                               _battleEconomyModifier.AutoEnergyIntervalMultiplier;
                var cancelled = await UniTask
                    .Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (cancelled || false == _gameStateService.IsPlaying)
                    return;

                if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                    continue;

                var pickedIndex = PickReadyHeroActivationIndex();

                if (pickedIndex < 0)
                    continue;

                _heroActivationPending[pickedIndex] = true;
                ActivateWithDelay(pickedIndex, ct).Forget();
            }
        }

        private int PickReadyHeroActivationIndex()
        {
            var candidates = new List<BotActionCandidate>();
            for (var i = 0; i < _heroActivationPending.Length; i++)
            {
                if (_heroActivationPending[i])
                    continue;

                var source = UnitDescriptor.Hero(BattleSide.Enemy, i);
                AddCandidatesForSource(source, candidates);
            }

            return TryChooseAction(candidates, out var decision)
                ? decision.Source.SlotIndex
                : -1;
        }

        private async UniTaskVoid ActivateWithDelay(int slotIndex, CancellationToken ct)
        {
            var delay = GenerateDelay(
                _botStrengthConfig.MinHeroActivationDelay,
                _botStrengthConfig.MaxHeroActivationDelay);

            var cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct)
                .SuppressCancellationThrow();

            _heroActivationPending[slotIndex] = false;

            if (cancelled || false == _gameStateService.IsPlaying)
                return;

            if (_battleFlowService.Snapshot.Phase != BattlePhaseKind.Hero)
                return;

            var source = UnitDescriptor.Hero(BattleSide.Enemy, slotIndex);
            if (false == _unitAbilityActivationService.TryPreview(source, out _))
                return;

            if (TryChooseAction(source, out var decision))
                _abilityExecutionService.Execute(decision.Source, decision.Target);
        }

        private bool TryChooseAction(UnitDescriptor source, out BotDecision decision)
        {
            var candidates = new List<BotActionCandidate>();
            AddCandidatesForSource(source, candidates);
            
            return TryChooseAction(candidates, out decision);
        }

        private bool TryChooseAction(IReadOnlyList<BotActionCandidate> candidates, out BotDecision decision)
        {
            var request = new BotDecisionRequest(candidates, _utilityProfile,
                _botStrengthConfig.ToDecisionQualitySettings());
            
            return _utilityEngine.TryChoose(request, out decision);
        }

        private void AddCandidatesForSource(UnitDescriptor source, List<BotActionCandidate> result)
        {
            _candidateBuilder.AddCandidatesForSource(source, result);
        }

        private float GenerateDelay(float min, float max)
        {
            return (float)(_delayRandom.NextDouble() * (max - min) + min);
        }

        private void StopLoops()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            ResetPendingActions();
        }

        private void SyncLoopState(BattlePhaseKind phase)
        {
            if (false == _gameStateService.IsPlaying)
            {
                StopLoops();
                return;
            }

            if (phase is BattlePhaseKind.Match or BattlePhaseKind.Hero)
            {
                EnsureLoopsRunning();
                return;
            }

            StopLoops();
        }

        private void EnsureLoopsRunning()
        {
            if (null != _cts)
                return;

            _cts = new CancellationTokenSource();
            RunEnemyChargeLoop(_cts.Token).Forget();
            RunHeroEnergyLoop(_cts.Token).Forget();
        }

        private void ResetPendingActions()
        {
            Array.Clear(_heroActivationPending, 0, _heroActivationPending.Length);
            _dischargeScheduled = false;
        }

        private static BotUtilityProfile CreateFallbackProfile()
        {
            return new BotUtilityProfile(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f);
        }
    }
}