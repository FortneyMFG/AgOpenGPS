using System;

namespace AgOpenGPS.Core.Kinematics.Math
{
    public static class Rotation
    {
        public static Matrix3 Rz(double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            return new Matrix3(
                cos, -sin, 0,
                sin,  cos, 0,
                0,    0,   1);
        }
    }
}
