using System.Collections.Generic;

namespace Project.Scripts.Shared.Grid
{
    public static class LineClearRules
    {
        public static List<int> GetLineIndices(int axisLength, int lineIndex, int thicknessBonus)
        {
            var result = new List<int>();
            if (axisLength <= 0 || lineIndex < 0 || lineIndex >= axisLength)
                return result;

            var targetCount = 1 + (thicknessBonus < 0 ? 0 : thicknessBonus);
            if (targetCount > axisLength)
                targetCount = axisLength;

            var minStart = lineIndex - targetCount + 1;
            if (minStart < 0)
                minStart = 0;

            var maxStart = lineIndex;
            var lastValidStart = axisLength - targetCount;
            if (maxStart > lastValidStart)
                maxStart = lastValidStart;

            var bestStart = minStart;
            var bestDistance = float.MaxValue;
            var boardCenter = (axisLength - 1) * 0.5f;
            for (var start = minStart; start <= maxStart; start++)
            {
                var blockCenter = start + (targetCount - 1) * 0.5f;
                var distance = Abs(blockCenter - boardCenter);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestStart = start;
            }

            for (var i = 0; i < targetCount; i++)
                result.Add(bestStart + i);

            return result;
        }

        public static List<GridPoint> GetAffectedPositions(IGridState state, GridPoint origin,
            LineClearOrientation orientation, int thicknessBonus)
        {
            var result = new List<GridPoint>();
            if (null == state || false == state.IsValidPosition(origin))
                return result;

            if (orientation == LineClearOrientation.Horizontal)
            {
                var rows = GetLineIndices(state.Height, origin.Y, thicknessBonus);
                for (var i = 0; i < rows.Count; i++)
                    AddRow(result, state.Width, rows[i]);

                return result;
            }

            var columns = GetLineIndices(state.Width, origin.X, thicknessBonus);
            for (var i = 0; i < columns.Count; i++)
                AddColumn(result, state.Height, columns[i]);

            return result;
        }

        private static void AddRow(List<GridPoint> target, int width, int y)
        {
            for (var x = 0; x < width; x++)
                target.Add(new GridPoint(x, y));
        }

        private static void AddColumn(List<GridPoint> target, int height, int x)
        {
            for (var y = 0; y < height; y++)
                target.Add(new GridPoint(x, y));
        }

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}