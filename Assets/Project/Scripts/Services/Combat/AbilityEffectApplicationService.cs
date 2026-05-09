using System.Collections.Generic;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Combat
{
    public interface IAbilityEffectApplicationService
    {
        AbilityEffectApplicationResult Apply(UnitDescriptor source, UnitDescriptor selectedTarget,
            IReadOnlyList<AbilityEffectEntryDefinition> entries, TileKind sourceSlotKind, int currentRound,
            BattlePhaseKind currentPhase, long occurredAtTick, int applicationIndexOffset = 0, bool isRepeat = false,
            float presentationDelaySeconds = 0f);
    }

    public readonly struct AbilityEffectApplicationResult
    {
        public int DirectActionCount { get; }
        public int BuffApplicationCount { get; }
        public bool WasChanged => DirectActionCount > 0 || BuffApplicationCount > 0;


        public AbilityEffectApplicationResult(int directActionCount, int buffApplicationCount)
        {
            DirectActionCount = directActionCount < 0 ? 0 : directActionCount;
            BuffApplicationCount = buffApplicationCount < 0 ? 0 : buffApplicationCount;
        }
    }

    public class AbilityEffectApplicationService : IAbilityEffectApplicationService
    {
        private const int SlotCount = 4;


        private readonly LevelConfig _levelConfig;
        private readonly IHeroService _heroService;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly IBuffService _buffService;
        private readonly EventBus _eventBus;


        public AbilityEffectApplicationService(LevelConfig levelConfig, IHeroService heroService,
            IPlayerStateService playerState, IEnemyStateService enemyState, IAvatarGroupDefenseService groupDefense,
            IBuffService buffService, EventBus eventBus)
        {
            _levelConfig = levelConfig;
            _heroService = heroService;
            _playerState = playerState;
            _enemyState = enemyState;
            _groupDefense = groupDefense;
            _buffService = buffService;
            _eventBus = eventBus;
        }


        public AbilityEffectApplicationResult Apply(UnitDescriptor source, UnitDescriptor selectedTarget,
            IReadOnlyList<AbilityEffectEntryDefinition> entries, TileKind sourceSlotKind, int currentRound,
            BattlePhaseKind currentPhase, long occurredAtTick, int applicationIndexOffset = 0, bool isRepeat = false,
            float presentationDelaySeconds = 0f)
        {
            if (entries == null || entries.Count == 0)
                return default;

            var directActionCount = 0;
            var buffApplicationCount = 0;
            var candidates = CollectCandidates();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (false == entry.IsConfigured)
                    continue;

                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, selectedTarget, candidates);
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var target = targets[targetIndex];
                    directActionCount += ApplyDirectActions(source, target, entry.DirectActions, occurredAtTick,
                        applicationIndexOffset, isRepeat, presentationDelaySeconds);
                    buffApplicationCount += ApplyBuffApplications(source, target, sourceSlotKind, entry.BuffApplications,
                        currentRound, currentPhase);
                }
            }

            if (buffApplicationCount > 0)
                _eventBus.Publish(new BuffsChangedEvent());

            return new AbilityEffectApplicationResult(directActionCount, buffApplicationCount);
        }

        private int ApplyDirectActions(UnitDescriptor source, UnitDescriptor target,
            IReadOnlyList<DirectActionDefinition> actions, long occurredAtTick, int applicationIndexOffset,
            bool isRepeat, float presentationDelaySeconds)
        {
            if (null == actions || actions.Count == 0)
                return 0;

            var appliedCount = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (false == action.IsConfigured || false == CanApplyDirectAction(action, target))
                    continue;

                if (ApplyDirectAction(target, action))
                {
                    _eventBus.Publish(new AbilityApplicationEvent(source, target, ToHeroActionType(action.Kind),
                        action.Value, applicationIndexOffset + i, isRepeat, presentationDelaySeconds, occurredAtTick));
                    appliedCount++;
                }
            }

            return appliedCount;
        }

        private int ApplyBuffApplications(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind,
            IReadOnlyList<BuffApplicationDefinition> applications, int currentRound, BattlePhaseKind currentPhase)
        {
            if (null == applications || applications.Count == 0)
                return 0;

            var appliedCount = 0;
            for (var i = 0; i < applications.Count; i++)
            {
                var application = applications[i];
                if (false == application.IsConfigured)
                    continue;

                if (_buffService.AddBuff(source, target, sourceSlotKind, application.Buff, currentRound, currentPhase,
                        application.DurationSeconds))
                {
                    PublishAbilityStatsChanged(target);
                    appliedCount++;
                }
            }

            return appliedCount;
        }

        private bool ApplyDirectAction(UnitDescriptor target, DirectActionDefinition action)
        {
            if (action.Kind == DirectActionKind.Damage)
            {
                ApplyDamage(target, action.Value);
                return true;
            }

            if (action.Kind == DirectActionKind.Heal)
            {
                ApplyHeal(target, action.Value);
                return true;
            }

            return false;
        }

        private void ApplyDamage(UnitDescriptor target, int value)
        {
            if (target.Kind == UnitKind.Avatar)
            {
                if (target.Side == BattleSide.Player)
                    _playerState.TakeDamage(value);
                else
                    _enemyState.ApplyDamage(value);

                return;
            }

            _heroService.ApplyDamageToHero(target.Side, target.SlotIndex, value);
        }

        private void ApplyHeal(UnitDescriptor target, int value)
        {
            if (target.Kind == UnitKind.Avatar)
            {
                if (target.Side == BattleSide.Player)
                    _playerState.Heal(value);
                else
                    _enemyState.ApplyHeal(value);

                return;
            }

            _heroService.ApplyHealToHero(target.Side, target.SlotIndex, value);
        }

        private bool CanApplyDirectAction(DirectActionDefinition action, UnitDescriptor target)
        {
            if (false == TryGetTargetState(target, out var isAlive, out var isHpFull, out var isExposed))
                return false;

            return AbilityTargetRules.IsTargetValid(ToHeroActionType(action.Kind), true, isAlive, isHpFull, isExposed);
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

        private List<UnitTargetCandidate> CollectCandidates()
        {
            var playerAvatarActionType = _levelConfig.PlayerAvatarConfig
                ? _levelConfig.PlayerAvatarConfig.AbilityType
                : HeroActionType.DealDamage;
            var enemyAvatarActionType = _levelConfig.EnemyAvatarConfig
                ? _levelConfig.EnemyAvatarConfig.AbilityType
                : HeroActionType.DealDamage;
            var result = new List<UnitTargetCandidate>(10)
            {
                new(UnitDescriptor.Avatar(BattleSide.Player, playerAvatarActionType),
                    _playerState.CurrentHP,
                    _playerState.MaxHP,
                    _playerState.CurrentHP > 0),
                new(UnitDescriptor.Avatar(BattleSide.Enemy, enemyAvatarActionType),
                    _enemyState.CurrentHP,
                    _enemyState.MaxHP,
                    _enemyState.CurrentHP > 0)
            };

            AddHeroCandidates(result, BattleSide.Player);
            AddHeroCandidates(result, BattleSide.Enemy);

            return result;
        }

        private void AddHeroCandidates(List<UnitTargetCandidate> result, BattleSide side)
        {
            var slots = _heroService.GetSlots(side);
            for (var i = 0; i < SlotCount && i < slots.Count; i++)
            {
                var slot = slots[i];
                result.Add(new UnitTargetCandidate(UnitDescriptor.Hero(side, i, slot.ActionType),
                    slot.CurrentHP, slot.MaxHP, slot is { IsAssigned: true, IsAlive: true }));
            }
        }

        private void PublishAbilityStatsChanged(UnitDescriptor target)
        {
            if (target.Kind == UnitKind.Hero)
            {
                PublishHeroAbilityStatsChanged(target.Side, target.SlotIndex);
                return;
            }

            PublishAvatarAbilityPowerChanged(target.Side);
        }

        private void PublishHeroAbilityStatsChanged(BattleSide side, int slotIndex)
        {
            var heroConfig = GetHeroConfig(side, slotIndex);
            if (!heroConfig)
                return;

            _eventBus.Publish(new HeroAbilityStatsChangedEvent(side, slotIndex,
                GetActivationEnergyCost(side, slotIndex, heroConfig.ActivationEnergyCost),
                GetAbilityPower(UnitDescriptor.Hero(side, slotIndex, heroConfig.AbilityType), heroConfig.AbilityPower)));
        }

        private void PublishAvatarAbilityPowerChanged(BattleSide side)
        {
            var config = side == BattleSide.Player
                ? _levelConfig.PlayerAvatarConfig
                : _levelConfig.EnemyAvatarConfig;
            if (!config)
                return;

            var target = UnitDescriptor.Avatar(side, config.AbilityType);
            _eventBus.Publish(new AvatarAbilityPowerChangedEvent(side, GetAbilityPower(target, config.AbilityPower)));
        }

        private int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost)
        {
            return (_buffService as IHeroAbilityModifierService)?.GetActivationEnergyCost(side, slotIndex, baseCost)
                   ?? baseCost;
        }

        private int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return (_buffService as IAbilityPowerModifierService)?.GetAbilityPower(target, basePower)
                   ?? basePower;
        }

        private HeroConfig GetHeroConfig(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return null;

            var heroes = side == BattleSide.Player
                ? _levelConfig.PlayerHeroes
                : _levelConfig.EnemyHeroes;

            return heroes != null && slotIndex < heroes.Length ? heroes[slotIndex] : null;
        }

        private static HeroActionType ToHeroActionType(DirectActionKind actionKind)
        {
            return actionKind == DirectActionKind.Heal ? HeroActionType.HealAlly : HeroActionType.DealDamage;
        }
    }
}