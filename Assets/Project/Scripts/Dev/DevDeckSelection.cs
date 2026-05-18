#if DEV
using Project.Scripts.Configs.Battle.Units;

namespace Project.Scripts.Dev
{
    public readonly struct DevDeckSelection
    {
        public DevDeckSelection(AvatarConfig avatar, HeroConfig[] heroes)
        {
            Avatar = avatar;
            Heroes = heroes;
        }


        public AvatarConfig Avatar { get; }
        public HeroConfig[] Heroes { get; }
    }
}
#endif