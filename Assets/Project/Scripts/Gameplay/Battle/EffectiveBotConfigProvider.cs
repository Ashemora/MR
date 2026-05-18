using Project.Scripts.Configs.Battle.Bot;

namespace Project.Scripts.Gameplay.Battle
{
    public sealed class EffectiveBotConfigProvider
    {
        public EffectiveBotConfigProvider(BotConfig botConfig)
        {
            BotConfig = botConfig;
        }


        public BotConfig BotConfig { get; }
    }
}