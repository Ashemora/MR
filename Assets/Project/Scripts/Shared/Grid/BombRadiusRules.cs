namespace Project.Scripts.Shared.Grid
{
    public static class BombRadiusRules
    {
        public static int GetEffectiveRadius(int baseRadius, int radiusBonus)
        {
            var result = NormalizeRadius(baseRadius) + radiusBonus;
            return NormalizeRadius(result);
        }

        private static int NormalizeRadius(int radius)
        {
            return radius < 0 ? 0 : radius;
        }
    }
}