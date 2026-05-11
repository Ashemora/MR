namespace Project.Scripts.Shared.Heroes
{
    public readonly struct HealthChangeResult
    {
        public int PreviousHP { get; }
        public int CurrentHP { get; }
        public bool WasChanged => CurrentHP != PreviousHP;
        public bool BecameDefeated => PreviousHP > 0 && CurrentHP <= 0;


        public HealthChangeResult(int previousHP, int currentHP)
        {
            PreviousHP = previousHP;
            CurrentHP = currentHP;
        }
    }
}