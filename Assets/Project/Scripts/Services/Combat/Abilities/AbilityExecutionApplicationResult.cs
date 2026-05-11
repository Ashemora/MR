namespace Project.Scripts.Services.Combat.Abilities
{
    public readonly struct AbilityExecutionApplicationResult
    {
        public AbilityDirectApplicationResult Application { get; }
        public float PresentationDelaySeconds { get; }


        public AbilityExecutionApplicationResult(AbilityDirectApplicationResult application,
            float presentationDelaySeconds)
        {
            Application = application;
            PresentationDelaySeconds = presentationDelaySeconds < 0f ? 0f : presentationDelaySeconds;
        }
    }
}