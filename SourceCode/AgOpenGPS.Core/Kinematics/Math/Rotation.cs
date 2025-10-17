using SystemMath = System.Math;

namespace AgOpenGPS.Core.Kinematics.Math
{
    public static class Rotation
    {
        public static Matrix3 Rz(double angle)
        {
            double cos = SystemMath.Cos(angle);
            double sin = SystemMath.Sin(angle);

            return new Matrix3(
                cos, -sin, 0,
                sin,  cos, 0,
                0,    0,   1);
        }
    }
}
