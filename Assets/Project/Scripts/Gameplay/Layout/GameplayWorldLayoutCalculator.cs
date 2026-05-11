namespace Project.Scripts.Gameplay.Layout
{
    public static class GameplayWorldLayoutCalculator
    {
        public static GameplayWorldLayout Calculate(ScreenLayoutRect worldRect, float maxAspectRatio,
            float framePaddingPercent, float tilePaddingPercent, int gridWidth, int gridHeight,
            float frameExtraHeight, float fixedContentHeight, float gapCellUnits, float minGapScale,
            float minCellSize)
        {
            if (gridWidth <= 0 || gridHeight <= 0)
                return new GameplayWorldLayout(worldRect, 0f, 0f, minCellSize, minCellSize, 1f);

            var safeMinCellSize = Max(0f, minCellSize);
            _ = minGapScale;
            var safeFramePadding = Clamp01(framePaddingPercent);
            var safeTilePadding = Clamp01(tilePaddingPercent);

            var designFrameCellSize = Max(safeMinCellSize, worldRect.Width * (1f - safeFramePadding) / gridWidth);
            var designTileCellSize = Max(safeMinCellSize, worldRect.Width * (1f - safeTilePadding) / gridWidth);
            var tileToFrameRatio = designFrameCellSize > 0f
                ? designTileCellSize / designFrameCellSize
                : 1f;

            var designFrameHeight = gridHeight * designFrameCellSize + frameExtraHeight;
            var desiredStackHeight = StackHeight(designFrameHeight, fixedContentHeight, gapCellUnits, designFrameCellSize, 1f);
            var fitScale = desiredStackHeight > 0f ? Min(1f, worldRect.Height / desiredStackHeight) : 1f;

            var frameCellSize = Max(safeMinCellSize, designFrameCellSize * fitScale);
            var tileCellSize = Max(safeMinCellSize, frameCellSize * tileToFrameRatio);
            var frameHeight = (gridHeight * designFrameCellSize + frameExtraHeight) * fitScale;
            var frameWidth = gridWidth * frameCellSize;

            return new GameplayWorldLayout(worldRect, frameWidth, frameHeight, frameCellSize,
                tileCellSize, 1f, desiredStackHeight, worldRect.Height, fitScale);
        }


        private static float StackHeight(float frameHeight, float fixedContentHeight, float gapCellUnits,
            float tileCellSize, float gapScale)
        {
            return frameHeight + fixedContentHeight + gapCellUnits * tileCellSize * gapScale;
        }

        private static float Clamp01(float value)
        {
            return Max(0f, Min(1f, value));
        }

        private static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        private static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}