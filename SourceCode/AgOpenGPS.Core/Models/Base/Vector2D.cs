using System;

namespace AgOpenGPS.Core.Models
{
    /// <summary>
    /// Simple two dimensional vector that stores double precision values.
    /// Explicitly kept in the core model layer so geometry code does not depend on
    /// presentation specific vector types (System.Numerics uses single precision).
    /// </summary>
    public struct Vector2D : IEquatable<Vector2D>
    {
        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public static Vector2D Zero => new Vector2D(0.0, 0.0);

        public bool Equals(Vector2D other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2D other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static Vector2D operator +(Vector2D left, Vector2D right)
        {
            return new Vector2D(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2D operator -(Vector2D left, Vector2D right)
        {
            return new Vector2D(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2D operator *(Vector2D vector, double scalar)
        {
            return new Vector2D(vector.X * scalar, vector.Y * scalar);
        }

        public static Vector2D operator *(double scalar, Vector2D vector)
        {
            return vector * scalar;
        }

        public static bool operator ==(Vector2D left, Vector2D right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2D left, Vector2D right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}

