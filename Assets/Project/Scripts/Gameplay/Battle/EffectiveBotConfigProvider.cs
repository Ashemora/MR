using Project.Scripts.Configs.Battle.Bot;

namespace Project.Scripts.Gameplay.Battle
{
    public sealed class EffectiveBotConfigProvider
    {
        public EffectiveBotConfigProvider(BotStrengthConfig botStrengthConfig, BotStrategyConfig botStrategyConfig)
        {
            BotStrengthConfig = botStrengthConfig;
            BotStrategyConfig = botStrategyConfig;
        }


        public BotStrengthConfig BotStrengthConfig { get; }
        public BotStrategyConfig BotStrategyConfig { get; }
    }
}