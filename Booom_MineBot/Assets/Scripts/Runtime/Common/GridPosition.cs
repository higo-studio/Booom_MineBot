using System;
using UnityEngine;

namespace Minebot.Common
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        [field: SerializeField]
        public int X { get; }

        [field: SerializeField]
        public int Y { get; }

        public static GridPosition Zero => new GridPosition(0, 0);
        public static GridPosition Up => new GridPosition(0, 1);
        public static GridPosition Down => new GridPosition(0, -1);
        public static GridPosition Left => new GridPosition(-1, 0);
        public static GridPosition Right => new GridPosition(1, 0);

        public static GridPosition operator +(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.X + b.X, a.Y + b.Y);
        }

        public static GridPosition operator -(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.X - b.X, a.Y - b.Y);
        }

        public int ManhattanDistance(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
