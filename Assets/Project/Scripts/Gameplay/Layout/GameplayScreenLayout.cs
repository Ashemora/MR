namespace Project.Scripts.Gameplay.Layout
{
    public readonly struct GameplayScreenLayout
    {
        public ScreenLayoutRect SafeAreaRect { get; }
        public ScreenLayoutRect GameplayRect { get; }
        public ScreenLayoutRect TopBarRect { get; }
        public ScreenLayoutRect WorldRect { get; }
        public float PixelScale { get; }


        public GameplayScreenLayout(ScreenLayoutRect safeAreaRect, ScreenLayoutRect gameplayRect,
            ScreenLayoutRect topBarRect, ScreenLayoutRect worldRect, float pixelScale)
        {
            SafeAreaRect = safeAreaRect;
            GameplayRect = gameplayRect;
            TopBarRect = topBarRect;
            WorldRect = worldRect;
            PixelScale = pixelScale;
        }
    }
}