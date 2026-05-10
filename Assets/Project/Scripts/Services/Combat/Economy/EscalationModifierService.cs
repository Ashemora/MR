using Project.Scripts.Configs.Battle;
using Project.Scripts.Services.Events;

namespace Project.Scripts.Services.Combat.Economy
{
    public class EscalationModifierService : IEscalationModifierService, IBattleEconomyModifierService
    {
        public bool IsEscalationActive { get; private set; }
        public float CascadeEnergyMultiplier { get; private set; } = 1f;
        public float AutoEnergyIntervalMultiplier { get; private set; } = 1f;


        private readonly EscalationConfig _config;
        private readonly EventBus _eventBus;


        public EscalationModifierService(EscalationConfig config, EventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void ActivateEscalation()
        {
            if (IsEscalationActive)
                return;

            IsEscalationActive = true;
            CascadeEnergyMultiplier = _config.CascadeEnergyMultiplier;
            AutoEnergyIntervalMultiplier = _config.AutoEnergyIntervalMultiplier;
            _eventBus.Publish(new EscalationModifiersAppliedEvent(CascadeEnergyMultiplier));
        }
    }
}