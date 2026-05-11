namespace Project.Scripts.Shared.Abilities
{
    public static class CooldownRules
    {
        public static float ApplyMinimumUnitActivationCooldown(float duration, float minDuration)
        {
            var minimum = minDuration < 0f ? 0f : minDuration;
            if (minimum <= 0f)
                return duration < 0f ? 0f : duration;

            var normalizedDuration = duration < 0f ? 0f : duration;
            
            return normalizedDuration < minimum ? minimum : normalizedDuration;
        }
    }
}