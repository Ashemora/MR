using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Energy
{
    public readonly struct CascadeEnergySettings
    {
        public float CascadeMultiplierStep { get; }
        public float MultiMatchMultiplier { get; }
        public float LShapeMultiplier { get; }
        public float TShapeMultiplier { get; }
        public float BombEnergyMultiplier { get; }
        public float LineRuneEnergyMultiplier { get; }
        public float StormEnergyMultiplier { get; }


        public CascadeEnergySettings(float cascadeMultiplierStep, float multiMatchMultiplier,
            float lShapeMultiplier, float tShapeMultiplier, float bombEnergyMultiplier,
            float lineRuneEnergyMultiplier, float stormEnergyMultiplier)
        {
            CascadeMultiplierStep = cascadeMultiplierStep;
            MultiMatchMultiplier = multiMatchMultiplier;
            LShapeMultiplier = lShapeMultiplier;
            TShapeMultiplier = tShapeMultiplier;
            BombEnergyMultiplier = bombEnergyMultiplier;
            LineRuneEnergyMultiplier = lineRuneEnergyMultiplier;
            StormEnergyMultiplier = stormEnergyMultiplier;
        }

        public float GetSpecialTileMultiplier(TileKind kind) => kind switch
        {
            TileKind.Bomb => BombEnergyMultiplier,
            TileKind.LineRuneH or TileKind.LineRuneV => LineRuneEnergyMultiplier,
            TileKind.Storm => StormEnergyMultiplier,
            _ => 1f
        };
    }
}