namespace Project.Scripts.Gameplay.Battle.Units
{
    public readonly struct ShieldBarUpdate
    {
        public float Fill { get; }
        public HealthBarUpdateMode Mode { get; }
        public int CurrentShield { get; }
        public int MaxShield { get; }


        public ShieldBarUpdate(float fill, HealthBarUpdateMode mode, int currentShield, int maxShield)
        {
            Fill = fill;
            Mode = mode;
            CurrentShield = currentShield;
            MaxShield = maxShield;
        }
    }
}
