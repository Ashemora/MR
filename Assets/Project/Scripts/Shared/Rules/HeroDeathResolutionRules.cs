namespace Project.Scripts.Shared.Rules
{
    public readonly struct HeroDeathResolutionResult
    {
        public HealthChangeResult HealthChange { get; }
        public bool WasResurrected { get; }
        public int ResurrectedHP { get; }


        public HeroDeathResolutionResult(HealthChangeResult healthChange, bool wasResurrected, int resurrectedHP)
        {
            HealthChange = healthChange;
            WasResurrected = wasResurrected;
            ResurrectedHP = resurrectedHP < 0 ? 0 : resurrectedHP;
        }
    }

    public static class HeroDeathResolutionRules
    {
        public static HeroDeathResolutionResult Resolve(int currentHP, int maxHP, int delta, int resurrectChargeHP,
            bool isBurndownActive)
        {
            var healthChange = HealthChangeRules.Apply(currentHP, maxHP, delta);

            if (false == healthChange.BecameDefeated)
                return new HeroDeathResolutionResult(healthChange, false, 0);

            if (resurrectChargeHP <= 0 || isBurndownActive)
                return new HeroDeathResolutionResult(healthChange, false, 0);

            var clamped = resurrectChargeHP > maxHP ? maxHP : resurrectChargeHP;
            if (clamped < 1)
                clamped = 1;

            var resurrected = new HealthChangeResult(healthChange.PreviousHP, clamped);

            return new HeroDeathResolutionResult(resurrected, true, clamped);
        }
    }
}