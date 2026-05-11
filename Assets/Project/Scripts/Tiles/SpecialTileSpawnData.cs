using Project.Scripts.Configs.Board;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Tiles
{
    public readonly struct SpecialTileSpawnData
    {
        public readonly TileConfig Config;
        public readonly TileKind PayloadKind;


        public SpecialTileSpawnData(TileConfig config, TileKind payloadKind = TileKind.None)
        {
            Config = config;
            PayloadKind = payloadKind;
        }
    }
}