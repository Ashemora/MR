namespace Project.Scripts.Gameplay.Layout
{
    public readonly struct ScreenLayoutRect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float XMin => X;
        public float XMax => X + Width;
        public float YMin => Y;
        public float YMax => Y + Height;


        public ScreenLayoutRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width < 0f ? 0f : width;
            Height = height < 0f ? 0f : height;
        }

        public static ScreenLayoutRect FromMinMax(float xMin, float yMin, float xMax, float yMax)
        {
            return new ScreenLayoutRect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public ScreenLayoutRect Inset(float left, float right, float bottom, float top)
        {
            return FromMinMax(XMin + left, YMin + bottom, XMax - right, YMax - top);
        }
    }
}