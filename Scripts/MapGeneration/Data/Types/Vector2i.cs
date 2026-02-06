using System;
using System.Runtime.CompilerServices;

namespace TianYanShop.MapGeneration.Data.Types
{
    /// <summary>
    /// 整型二维向量，用于网格坐标
    /// </summary>
    [Serializable]
    public struct Vector2i : IEquatable<Vector2i>, IComparable<Vector2i>
    {
        public int X;
        public int Y;

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2i(Vector2i other)
        {
            X = other.X;
            Y = other.Y;
        }

        public static readonly Vector2i Zero = new Vector2i(0, 0);
        public static readonly Vector2i One = new Vector2i(1, 1);
        public static readonly Vector2i Up = new Vector2i(0, -1);
        public static readonly Vector2i Down = new Vector2i(0, 1);
        public static readonly Vector2i Left = new Vector2i(-1, 0);
        public static readonly Vector2i Right = new Vector2i(1, 0);

        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    _ => throw new IndexOutOfRangeException()
                };
            }
            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public int LengthSquared => X * X + Y * Y;
        public float Length => MathF.Sqrt(LengthSquared);

        public Vector2i Abs() => new Vector2i(global::System.Math.Abs(X), global::System.Math.Abs(Y));
        public Vector2i Sign() => new Vector2i(global::System.Math.Sign(X), global::System.Math.Sign(Y));

        public Vector2i Clamp(int min, int max) => new Vector2i(
            global::System.Math.Clamp(X, min, max),
            global::System.Math.Clamp(Y, min, max)
        );

        public Vector2i Min(Vector2i other) => new Vector2i(
            global::System.Math.Min(X, other.X),
            global::System.Math.Min(Y, other.Y)
        );

        public Vector2i Max(Vector2i other) => new Vector2i(
            global::System.Math.Max(X, other.X),
            global::System.Math.Max(Y, other.Y)
        );

        public static Vector2i operator +(Vector2i a, Vector2i b) => new Vector2i(a.X + b.X, a.Y + b.Y);
        public static Vector2i operator -(Vector2i a, Vector2i b) => new Vector2i(a.X - b.X, a.Y - b.Y);
        public static Vector2i operator *(Vector2i a, int s) => new Vector2i(a.X * s, a.Y * s);
        public static Vector2i operator /(Vector2i a, int s) => new Vector2i(a.X / s, a.Y / s);

        public static bool operator ==(Vector2i a, Vector2i b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector2i a, Vector2i b) => a.X != b.X || a.Y != b.Y;

        public override bool Equals(object obj) => obj is Vector2i other && Equals(other);
        public bool Equals(Vector2i other) => X == other.X && Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public int CompareTo(Vector2i other)
        {
            int xCompare = X.CompareTo(other.X);
            return xCompare != 0 ? xCompare : Y.CompareTo(other.Y);
        }

        public override string ToString() => $"({X}, {Y})";

        public static explicit operator Godot.Vector2(Vector2i v) => new Godot.Vector2(v.X, v.Y);
        public static explicit operator Vector2i(Godot.Vector2 v) => new Vector2i((int)v.X, (int)v.Y);

        public static float DistanceTo(Vector2i a, Vector2i b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static int DistanceSquaredTo(Vector2i a, Vector2i b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static int Dot(Vector2i a, Vector2i b) => a.X * b.X + a.Y * b.Y;
    }
}
