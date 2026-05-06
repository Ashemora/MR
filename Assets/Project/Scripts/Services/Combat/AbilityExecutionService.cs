using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Rules;

namespace Project.Scripts.Services.Combat
{
    public class AbilityExecutionService : IAbilityExecutionService
    {
        private readonly IPlayerAvatarChargeService _playerAvatarCharge;
        private readonly IHeroService _heroService;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IHeroAbilityModifierService _heroAbilityModifierService;
        private readonly INextAttackBuffService _nextAttackBuffService;
        private readonly EventBus _eventBus;
        private readonly IBattleClock _battleClock;


        public AbilityExecutionService(
            IPlayerAvatarChargeService playerAvatarCharge,
            IHeroService heroService,
            IPlayerStateService playerState,
            IEnemyStateService enemyState,
            IAvatarGroupDefenseService groupDefense,
            IGameStateService gameStateService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IHeroAbilityModifierService heroAbilityModifierService,
            INextAttackBuffService nextAttackBuffService,
            EventBus eventBus,
            IBattleClock battleClock)
        {
            _playerAvatarCharge = playerAvatarCharge;
            _heroService = heroService;
            _playerState = playerState;
            _enemyState = enemyState;
            _groupDefense = groupDefense;
            _gameStateService = gameStateService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _heroAbilityModifierService = heroAbilityModifierService;
            _nextAttackBuffService = nextAttackBuffService;
            _eventBus = eventBus;
            _battleClock = battleClock;
        }

        public void Execute(UnitDescriptor source, UnitDescriptor target)
        {
            if (false == _gameStateService.IsPlaying)
                return;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return;

            if (source.Side != BattleSide.Player)
                return;

            if (false == TryGetSourceState(source, out var sourceActionType, out var sourceActionValue, out var isSourceAlive))
                return;

            if (sourceActionValue <= 0)
                return;

            if (false == TryGetTargetState(target, out var isTargetAlive, out var isTargetHpFull, out var isTargetExposed))
                return;

            if (false == AbilityTargetRules.IsTargetValid(source, target, sourceActionType, isSourceAlive,
                    isTargetAlive, isTargetHpFull, isTargetExposed))
                return;

            if (false == TryCommitSource(source, out var actionType, out var actionValue))
                return;

            if (actionType != sourceActionType || actionValue != sourceActionValue)
                return;

            ApplyToTarget(target, actionType, actionValue);
            _eventBus.Publish(new AbilityExecutedEvent(source, target, actionType, actionValue,
                _battleClock.CurrentTick));
        }


        private void ApplyToTarget(UnitDescriptor target, HeroActionType actionType, int actionValue)
        {
            if (actionType == HeroActionType.DealDamage)
            {
                if (target.Kind == UnitKind.Avatar)
                {
                    if (target.Side == BattleSide.Player)
                        _playerState.TakeDamage(actionValue);
                    else
                        _enemyState.ApplyDamage(actionValue);
                }
                else
                    _heroService.ApplyDamageToHero(target.Side, target.SlotIndex, actionValue);
            }
            else
            {
                if (target.Kind == UnitKind.Avatar)
                {
                    if (target.Side == BattleSide.Player)
                        _playerState.Heal(actionValue);
                    else
                        _enemyState.ApplyHeal(actionValue);
                }
                else
                    _heroService.ApplyHealToHero(target.Side, target.SlotIndex, actionValue);
            }
        }

        private bool TryGetSourceState(UnitDescriptor source, out HeroActionType actionType, out int actionValue, out bool isAlive)
        {
            actionType = default;
            actionValue = 0;
            isAlive = false;

            if (source.Kind == UnitKind.Avatar)
            {
                if (source.Side != BattleSide.Player)
                    return false;

                isAlive = _playerState.CurrentHP > 0;
                if (false == isAlive || false == _playerAvatarCharge.IsReady)
                    return false;

                actionType = _playerAvatarCharge.AbilityType;
                actionValue = GetActionValueWithNextAttackBuffPreview(source, actionType, _playerAvatarCharge.AbilityPower);
                return true;
            }

            var slots = _heroService.GetSlots(source.Side);
            if (source.SlotIndex < 0 || source.SlotIndex >= slots.Count)
                return false;

            var slot = slots[source.SlotIndex];
            isAlive = slot.IsAlive;
            if (false == _heroService.CanActivate(source.Side, source.SlotIndex) || false == isAlive)
                return false;

            actionType = slot.ActionType;
            actionValue = GetActionValueWithNextAttackBuffPreview(source, actionType,
                _heroAbilityModifierService.GetAbilityPower(source.Side, source.SlotIndex, slot.ActionValue));
            
            return true;
        }

        private bool TryGetTargetState(UnitDescriptor target, out bool isAlive, out bool isHpFull, out bool isExposed)
        {
            isAlive = false;
            isHpFull = false;
            isExposed = true;

            if (target.Kind == UnitKind.Avatar)
            {
                if (target.Side == BattleSide.Player)
                {
                    isAlive = _playerState.CurrentHP > 0;
                    isHpFull = _playerState.CurrentHP >= _playerState.MaxHP;
                }
                else
                {
                    isAlive = _enemyState.CurrentHP > 0;
                    isHpFull = _enemyState.CurrentHP >= _enemyState.MaxHP;
                }

                isExposed = _groupDefense.IsExposed(target.Side);
                return true;
            }

            var slots = _heroService.GetSlots(target.Side);
            if (target.SlotIndex < 0 || target.SlotIndex >= slots.Count)
                return false;

            var slot = slots[target.SlotIndex];
            if (false == slot.IsAssigned)
                return false;

            isAlive = slot.IsAlive;
            isHpFull = slot.CurrentHP >= slot.MaxHP;
            
            return true;
        }

        private bool TryCommitSource(UnitDescriptor source, out HeroActionType actionType, out int actionValue)
        {
            actionType = default;
            actionValue = 0;

            if (source.Kind == UnitKind.Avatar)
            {
                if (false == _playerAvatarCharge.TryRelease())
                    return false;

                actionType = _playerAvatarCharge.AbilityType;
                actionValue = GetActionValueWithNextAttackBuff(source, actionType, _playerAvatarCharge.AbilityPower);
                
                return true;
            }

            return _heroService.TryDischargeHero(source.Side, source.SlotIndex, out actionType, out actionValue);
        }

        private int GetActionValueWithNextAttackBuffPreview(UnitDescriptor source, HeroActionType actionType, int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            return baseActionValue + _nextAttackBuffService.Get(source);
        }

        private int GetActionValueWithNextAttackBuff(UnitDescriptor source, HeroActionType actionType, int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            return baseActionValue + _nextAttackBuffService.Consume(source);
        }
    }
}