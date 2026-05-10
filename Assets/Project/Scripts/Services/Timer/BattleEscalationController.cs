using System;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleFlow;
using VContainer.Unity;

namespace Project.Scripts.Services.Timer
{
    public class BattleEscalationController : IStartable, IDisposable
    {
        private readonly EscalationConfig _config;
        private readonly IEscalationModifierService _escalationModifierService;
        private readonly EventBus _eventBus;
        private readonly IDisposable _timerSub;
        private bool _escalationTriggered;


        public BattleEscalationController(
            EscalationConfig config,
            IEscalationModifierService escalationModifierService,
            EventBus eventBus)
        {
            _config = config;
            _escalationModifierService = escalationModifierService;
            _eventBus = eventBus;

            _timerSub = _eventBus.Subscribe<BattleFlowTimerChangedEvent>(OnTimerChanged);
        }


        public void Start()
        {
        }

        public void Dispose()
        {
            _timerSub.Dispose();
        }


        private void OnTimerChanged(BattleFlowTimerChangedEvent e)
        {
            TryTriggerEscalation(e);
        }

        private void TryTriggerEscalation(BattleFlowTimerChangedEvent e)
        {
            if (_escalationTriggered || e.Phase != BattlePhaseKind.Hero || e.TimeRemaining > _config.ActivationThreshold)
                return;

            _escalationTriggered = true;
            _escalationModifierService.ActivateEscalation();
            _eventBus.Publish(new BattleEscalationReachedEvent(e.TimeRemaining));
        }
    }
}