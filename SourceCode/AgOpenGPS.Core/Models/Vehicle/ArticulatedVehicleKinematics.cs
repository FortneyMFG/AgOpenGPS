using System;
using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.Models.Vehicle
{
    /// <summary>
    /// Articulated vehicle geometry model that keeps front mounted sensors (GNSS antenna, IMU, camera)
    /// expressed in the front frame and rear mounted implements expressed in the rear frame. The guidance
    /// system continues to reference the pivot as the ground truth point.
    /// </summary>
    public static class ArticulatedVehicleKinematics
    {
        public const double DegreesToRadians = Math.PI / 180.0;

        public static Matrix2 R(double heading)
        {
            return Matrix2.Rotation(heading);
        }

        public static Vector2D ToWorld(Vector2D origin, double heading, Vector2D local)
        {
            return origin + (R(heading) * local);
        }

        public static ArticulatedVehicleKinematicsSnapshot Forward(
            ArticulatedVehicleGeometry geometry,
            ArticulatedVehiclePose pose)
        {
            double psiPivot = pose.PivotHeading;
            double psiFront = pose.FrontHeading;
            double psiRear = pose.RearHeading;

            Vector2D pivot = pose.PivotPosition;

            Vector2D frontAxle = ToWorld(pivot, psiFront, new Vector2D(geometry.PivotToFront, 0.0));
            Vector2D rearAxle = ToWorld(pivot, psiRear, new Vector2D(-geometry.PivotToRear, 0.0));

            Vector2D antenna = ToWorld(frontAxle, psiFront, geometry.AntennaOffsetFront);

            Vector2D? imu = geometry.ImuOffsetFront.HasValue
                ? (Vector2D?)ToWorld(frontAxle, psiFront, geometry.ImuOffsetFront.Value)
                : null;

            Vector2D? camera = geometry.CameraOffsetFront.HasValue
                ? (Vector2D?)ToWorld(frontAxle, psiFront, geometry.CameraOffsetFront.Value)
                : null;

            Vector2D hitch = ToWorld(rearAxle, psiRear, geometry.HitchOffsetRear);

            return new ArticulatedVehicleKinematicsSnapshot(
                pivot,
                psiPivot,
                psiFront,
                psiRear,
                pose.ArticulationAngle,
                frontAxle,
                rearAxle,
                antenna,
                imu,
                camera,
                hitch);
        }

        public static Vector2D BackSolvePivotFromAntenna(
            ArticulatedVehicleGeometry geometry,
            Vector2D antennaWorld,
            double frontHeading)
        {
            Matrix2 rotation = R(frontHeading);
            Vector2D frontAxle = antennaWorld - (rotation * geometry.AntennaOffsetFront);
            Vector2D pivotOffset = rotation * new Vector2D(geometry.PivotToFront, 0.0);
            return frontAxle - pivotOffset;
        }
    }

    public readonly struct ArticulatedVehicleGeometry
    {
        public ArticulatedVehicleGeometry(
            double pivotToFront,
            double pivotToRear,
            Vector2D antennaOffsetFront,
            Vector2D? imuOffsetFront,
            Vector2D? cameraOffsetFront,
            Vector2D hitchOffsetRear,
            double articulationLimitDegrees)
        {
            if (pivotToFront <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(pivotToFront));
            }

            if (pivotToRear <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(pivotToRear));
            }

            PivotToFront = pivotToFront;
            PivotToRear = pivotToRear;
            AntennaOffsetFront = antennaOffsetFront;
            ImuOffsetFront = imuOffsetFront;
            CameraOffsetFront = cameraOffsetFront;
            HitchOffsetRear = hitchOffsetRear;
            ArticulationLimitDegrees = articulationLimitDegrees;
        }

        public double PivotToFront { get; }

        public double PivotToRear { get; }

        public double Wheelbase => PivotToFront + PivotToRear;

        public double ArticulationLimitDegrees { get; }

        public Vector2D AntennaOffsetFront { get; }

        public Vector2D? ImuOffsetFront { get; }

        public Vector2D? CameraOffsetFront { get; }

        public Vector2D HitchOffsetRear { get; }
    }

    public readonly struct ArticulatedVehiclePose
    {
        public ArticulatedVehiclePose(Vector2D pivotPosition, double pivotHeading, double articulationAngle)
        {
            PivotPosition = pivotPosition;
            PivotHeading = pivotHeading;
            ArticulationAngle = articulationAngle;
        }

        public Vector2D PivotPosition { get; }

        public double PivotHeading { get; }

        public double ArticulationAngle { get; }

        public double FrontHeading => PivotHeading + 0.5 * ArticulationAngle;

        public double RearHeading => PivotHeading - 0.5 * ArticulationAngle;

        public double ArticulationAngleDegrees => ArticulationAngle * 180.0 / Math.PI;
    }

    public readonly struct ArticulatedVehicleKinematicsSnapshot
    {
        public ArticulatedVehicleKinematicsSnapshot(
            Vector2D pivot,
            double pivotHeading,
            double frontHeading,
            double rearHeading,
            double articulationAngle,
            Vector2D frontAxle,
            Vector2D rearAxle,
            Vector2D antenna,
            Vector2D? imu,
            Vector2D? camera,
            Vector2D hitch)
        {
            Pivot = pivot;
            PivotHeading = pivotHeading;
            FrontHeading = frontHeading;
            RearHeading = rearHeading;
            ArticulationAngle = articulationAngle;
            FrontAxle = frontAxle;
            RearAxle = rearAxle;
            Antenna = antenna;
            Imu = imu;
            Camera = camera;
            Hitch = hitch;
        }

        public Vector2D Pivot { get; }

        public double PivotHeading { get; }

        public double FrontHeading { get; }

        public double RearHeading { get; }

        public double ArticulationAngle { get; }

        public Vector2D FrontAxle { get; }

        public Vector2D RearAxle { get; }

        public Vector2D Antenna { get; }

        public Vector2D? Imu { get; }

        public Vector2D? Camera { get; }

        public Vector2D Hitch { get; }
    }
}

