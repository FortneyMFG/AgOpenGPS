using SystemMath = System.Math;

namespace AgOpenGPS.Core.Kinematics.Math
{
    public readonly struct Vector3
    {
        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vector3 Zero => new Vector3(0, 0, 0);

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3 operator *(double scalar, Vector3 vector)
        {
            return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        }

        public static Vector3 operator *(Vector3 vector, double scalar)
        {
            return scalar * vector;
        }

        public double LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z);
        }

        public double Length()
        {
            return SystemMath.Sqrt(LengthSquared());
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
