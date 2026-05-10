using System;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Heroes;
using R3;

namespace Project.Scripts.Services.Game
{
    public class GameStateService : IGameStateService, IDisposable
    {
        public ReadOnlyReactiveProperty<GameState> State => _state;
        public bool IsPlaying => _state.Value == GameState.Playing;


        private readonly EventBus _eventBus;
        private readonly IAvatarService _avatarService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly ReactiveProperty<GameState> _state = new(GameState.Playing);
        private IDisposable _winSub;
        private IDisposable _loseSub;


        public GameStateService(
            EventBus eventBus,
            IAvatarService avatarService,
            IBattleActionRuntimeService battleActionRuntimeService)
        {
            _eventBus = eventBus;
            _avatarService = avatarService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _winSub = _eventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            _loseSub = _eventBus.Subscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
        }


        public void SetState(GameState state)
        {
            _state.Value = state;
        }

        public void Dispose()
        {
            _winSub?.Dispose();
            _loseSub?.Dispose();
            _state.Dispose();
        }


        private void OnEnemyDefeated(EnemyDefeatedEvent _)
        {
            if (false == IsPlaying && _state.Value != GameState.Burndown)
                return;

            _battleActionRuntimeService.MarkBlocked();
            var isFlawless = _avatarService.IsHpFull(BattleSide.Player);
            _eventBus.Publish(new GameResultEvent(BattleSide.Player, isFlawless));
            SetState(GameState.Win);
        }

        private void OnPlayerDefeated(PlayerDefeatedEvent _)
        {
            if (false == IsPlaying && _state.Value != GameState.Burndown)
                return;

            _battleActionRuntimeService.MarkBlocked();
            _eventBus.Publish(new GameResultEvent(BattleSide.Enemy, isFlawless: false));
            SetState(GameState.Lose);
        }
    }
}