#if DEV
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Battle.Units;

namespace Project.Scripts.Dev
{
    public readonly struct DevOpponentSelection
    {
        public DevOpponentSelection(AvatarConfig avatar, HeroConfig[] heroes, BotConfig botConfig)
        {
            Avatar = avatar;
            Heroes = heroes;
            BotConfig = botConfig;
        }


        public AvatarConfig Avatar { get; }
        public HeroConfig[] Heroes { get; }
        public BotConfig BotConfig { get; }
    }
}
#endif