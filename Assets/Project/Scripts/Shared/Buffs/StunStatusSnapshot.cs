namespace Project.Scripts.Shared.Buffs
{
    public readonly struct StunStatusSnapshot
    {
        public float RemainingSeconds { get; }
        public float DurationSeconds { get; }
        public bool IsActive => RemainingSeconds > 0f && DurationSeconds > 0f;


        public StunStatusSnapshot(float remainingSeconds, float durationSeconds)
        {
            RemainingSeconds = remainingSeconds < 0f ? 0f : remainingSeconds;
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
        }
    }
}
