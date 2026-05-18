using Project.Scripts.Configs.Battle.Units;

namespace Project.Scripts.Gameplay.Battle
{
    public sealed class MatchVisualsProvider
    {
        public MatchVisualsProvider(AvatarConfig playerAvatar, HeroConfig[] playerHeroes,
            AvatarConfig opponentAvatar, HeroConfig[] opponentHeroes)
        {
            PlayerAvatar = playerAvatar;
            PlayerHeroes = playerHeroes;
            OpponentAvatar = opponentAvatar;
            OpponentHeroes = opponentHeroes;
        }


        public AvatarConfig PlayerAvatar { get; }
        public HeroConfig[] PlayerHeroes { get; }
        public AvatarConfig OpponentAvatar { get; }
        public HeroConfig[] OpponentHeroes { get; }
    }
}