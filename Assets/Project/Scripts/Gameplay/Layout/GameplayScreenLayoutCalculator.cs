namespace Project.Scripts.Gameplay.Layout
{
    public static class GameplayScreenLayoutCalculator
    {
        public static GameplayScreenLayout Calculate(ScreenLayoutRect screenRect, ScreenLayoutRect safeAreaRect,
            bool useSafeArea, bool worldExtendsIntoUnsafeBottomArea, float safeAreaPadding, 
            float gameplayAspect, float referenceResolutionWidth, float referenceResolutionHeight,
            float topBarHeight, float topBarSidePadding, float topBarBottomPadding, float worldBottomPadding,
            float worldSidePadding)
        {
            var availableRect = useSafeArea ? safeAreaRect : screenRect;
            var pixelScale = CalculatePixelScale(screenRect.Width, screenRect.Height, referenceResolutionWidth, referenceResolutionHeight);
            var paddedAvailableRect = availableRect.Inset(
                safeAreaPadding * pixelScale,
                safeAreaPadding * pixelScale,
                safeAreaPadding * pixelScale,
                safeAreaPadding * pixelScale);
            var gameplayRect = FitAspect(paddedAvailableRect, gameplayAspect);

            var scaledTopBarHeight = topBarHeight * pixelScale;
            var scaledTopBarSidePadding = topBarSidePadding * pixelScale;
            var scaledTopBarBottomPadding = topBarBottomPadding * pixelScale;
            var scaledWorldBottomPadding = worldBottomPadding * pixelScale;
            var scaledWorldSidePadding = worldSidePadding * pixelScale;

            var topBarAreaRect = useSafeArea ? paddedAvailableRect : gameplayRect;
            var topBarRect = ScreenLayoutRect.FromMinMax(
                gameplayRect.XMin + scaledTopBarSidePadding,
                topBarAreaRect.YMax - scaledTopBarHeight,
                gameplayRect.XMax - scaledTopBarSidePadding,
                topBarAreaRect.YMax);

            var worldBottomBase = worldExtendsIntoUnsafeBottomArea
                ? Min(gameplayRect.YMin, screenRect.YMin)
                : gameplayRect.YMin;
            var worldRect = ScreenLayoutRect.FromMinMax(
                gameplayRect.XMin + scaledWorldSidePadding,
                worldBottomBase + scaledWorldBottomPadding,
                gameplayRect.XMax - scaledWorldSidePadding,
                topBarAreaRect.YMax - scaledTopBarHeight - scaledTopBarBottomPadding);

            return new GameplayScreenLayout(safeAreaRect, gameplayRect, topBarRect, worldRect, pixelScale);
        }


        private static float CalculatePixelScale(float screenWidth, float screenHeight, float referenceWidth, float referenceHeight)
        {
            if (referenceWidth <= 0f || referenceHeight <= 0f)
                return 1f;

            return screenWidth / referenceWidth;
        }


        private static ScreenLayoutRect FitAspect(ScreenLayoutRect rect, float aspect)
        {
            if (aspect <= 0f || rect.Width <= 0f || rect.Height <= 0f)
                return rect;

            var rectAspect = rect.Width / rect.Height;
            if (rectAspect > aspect)
            {
                var width = rect.Height * aspect;
                var x = rect.X + (rect.Width - width) * 0.5f;

                return new ScreenLayoutRect(x, rect.Y, width, rect.Height);
            }

            var height = rect.Width / aspect;
            var y = rect.Y + (rect.Height - height) * 0.5f;

            return new ScreenLayoutRect(rect.X, y, rect.Width, height);
        }

        private static float Min(float a, float b)
        {
            return a < b ? a : b;
        }
    }
}