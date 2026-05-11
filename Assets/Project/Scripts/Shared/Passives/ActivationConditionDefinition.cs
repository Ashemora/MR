namespace Project.Scripts.Shared.Passives
{
    public readonly struct ActivationConditionDefinition
    {
        public ActivationConditionKind Kind { get; }
        public ActivationConditionSubject Subject { get; }
        public int RequiredCount { get; }
        public float WindowSeconds { get; }
        public bool IsConfigured => Kind != ActivationConditionKind.None
                                    && (RequiresWindow(Kind) == false || WindowSeconds > 0f);


        public ActivationConditionDefinition(ActivationConditionKind kind, ActivationConditionSubject subject,
            int requiredCount, float windowSeconds = 0f)
        {
            Kind = kind;
            Subject = subject;
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
            WindowSeconds = windowSeconds < 0f ? 0f : windowSeconds;
        }

        private static bool RequiresWindow(ActivationConditionKind kind)
        {
            return kind is ActivationConditionKind.UnitActivationsInTimeWindow
                or ActivationConditionKind.SlotKindMatchesInTimeWindow
                or ActivationConditionKind.EnemyHeroDefeatsInTimeWindow;
        }
    }
}