namespace Project.Scripts.Shared.Layout
{
    public readonly struct GameplayWorldLayout
    {
        public ScreenLayoutRect WorldRect { get; }
        public float FrameWidth { get; }
        public float FrameHeight { get; }
        public float FrameCellSize { get; }
        public float TileCellSize { get; }
        public float GapScale { get; }
        public float DesiredStackHeight { get; }
        public float AvailableStackHeight { get; }
        public float FitScale { get; }


        public GameplayWorldLayout(ScreenLayoutRect worldRect, float frameWidth, float frameHeight,
            float frameCellSize, float tileCellSize, float gapScale, float desiredStackHeight = 0f,
            float availableStackHeight = 0f, float fitScale = 1f)
        {
            WorldRect = worldRect;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameCellSize = frameCellSize;
            TileCellSize = tileCellSize;
            GapScale = gapScale;
            DesiredStackHeight = desiredStackHeight;
            AvailableStackHeight = availableStackHeight;
            FitScale = fitScale;
        }
    }
}