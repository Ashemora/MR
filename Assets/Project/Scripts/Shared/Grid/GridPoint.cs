using System;

namespace Project.Scripts.Shared.Grid
{
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        public readonly int X;
        public readonly int Y;

        public static readonly GridPoint Zero = new(0,  0);
        public static readonly GridPoint Up = new(0,  1);
        public static readonly GridPoint Down = new(0, -1);
        public static readonly GridPoint Left = new(-1, 0);
        public static readonly GridPoint Right = new(1,  0);
        

        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static GridPoint operator +(GridPoint a, GridPoint b)
        {
            return new GridPoint(a.X + b.X, a.Y + b.Y);
        }

        public static GridPoint operator -(GridPoint a, GridPoint b)
        {
            return new GridPoint(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(GridPoint a, GridPoint b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(GridPoint a, GridPoint b)
        {
            return !(a == b);
        }

        public bool Equals(GridPoint other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPoint p && this == p;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}