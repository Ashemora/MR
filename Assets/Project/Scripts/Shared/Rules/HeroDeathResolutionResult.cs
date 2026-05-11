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
}