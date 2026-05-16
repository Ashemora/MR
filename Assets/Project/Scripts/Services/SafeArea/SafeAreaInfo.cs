using UnityEngine;

namespace Project.Scripts.Services.SafeArea
{
    public readonly struct SafeAreaInfo
    {
        public readonly Rect Raw;
        public readonly Vector2 ScreenSize;
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;
        public readonly ScreenOrientation Orientation;


        public SafeAreaInfo(Rect raw, Vector2 screenSize, ScreenOrientation orientation)
        {
            Raw = raw;
            ScreenSize = screenSize;
            Orientation = orientation;

            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                AnchorMin = Vector2.zero;
                AnchorMax = Vector2.one;
                return;
            }

            AnchorMin = new Vector2(raw.xMin / screenSize.x, raw.yMin / screenSize.y);
            AnchorMax = new Vector2(raw.xMax / screenSize.x, raw.yMax / screenSize.y);
        }

        public bool Equivalent(SafeAreaInfo other)
        {
            return Raw == other.Raw
                   && ScreenSize == other.ScreenSize
                   && Orientation == other.Orientation;
        }
    }
}
