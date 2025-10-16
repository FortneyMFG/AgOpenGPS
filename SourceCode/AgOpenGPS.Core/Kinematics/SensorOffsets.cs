using AgOpenGPS.Core.Kinematics.Math;
using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.Kinematics
{
    public static class SensorOffsets
    {
        public static Vector3 ReadAntennaOffsetFrontFrame(VehicleConfig config)
        {
            return new Vector3(
                config.AntennaPivot,
                -config.AntennaOffset,
                config.AntennaHeight);
        }

        public static Vector3 ReadImuOffsetFrontFrame(VehicleConfig config)
        {
            return Vector3.Zero;
        }

        public static Vector3 ReadDrawbarOffsetRearFrame(double hitchLength)
        {
            return new Vector3(-hitchLength, 0, 0);
        }

        public static double ReadWheelbaseFront(VehicleConfig config)
        {
            return config.Wheelbase > 0 ? config.Wheelbase * 0.5 : 0.0;
        }

        public static double ReadWheelbaseRear(VehicleConfig config)
        {
            return config.Wheelbase > 0 ? config.Wheelbase * 0.5 : 0.0;
        }
    }
}
