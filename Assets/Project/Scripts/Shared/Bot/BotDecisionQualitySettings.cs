namespace Project.Scripts.Shared.Bot
{
    public readonly struct BotDecisionQualitySettings
    {
        public readonly int TopCandidateCount;
        public readonly float RandomNoise;
        public readonly float MistakeChance;
        public readonly float Temperature;
        public readonly float MinScoreToAct;


        public BotDecisionQualitySettings(int topCandidateCount, float randomNoise, float mistakeChance,
            float temperature, float minScoreToAct)
        {
            TopCandidateCount = topCandidateCount < 1 ? 1 : topCandidateCount;
            RandomNoise = randomNoise < 0f ? 0f : randomNoise;
            MistakeChance = Clamp01(mistakeChance);
            Temperature = temperature <= 0f ? 1f : temperature;
            MinScoreToAct = minScoreToAct < 0f ? 0f : minScoreToAct;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }
}