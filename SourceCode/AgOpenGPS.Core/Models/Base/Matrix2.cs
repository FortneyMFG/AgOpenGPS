using System;

namespace AgOpenGPS.Core.Models
{
    /// <summary>
    /// Minimal 2x2 matrix used for rotating local frame vectors into world space.
    /// Stored as double precision to match the geometry model.
    /// </summary>
    public struct Matrix2
    {
        public Matrix2(double m11, double m12, double m21, double m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }

        public double M11 { get; }
        public double M12 { get; }
        public double M21 { get; }
        public double M22 { get; }

        public static Matrix2 Rotation(double angleRadians)
        {
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            return new Matrix2(cos, -sin, sin, cos);
        }

        public static Vector2D operator *(Matrix2 matrix, Vector2D vector)
        {
            double x = matrix.M11 * vector.X + matrix.M12 * vector.Y;
            double y = matrix.M21 * vector.X + matrix.M22 * vector.Y;
            return new Vector2D(x, y);
        }
    }
}

