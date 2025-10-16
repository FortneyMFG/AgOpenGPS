using System;

namespace AgOpenGPS.Core.Models
{
    public readonly struct VehiclePoseSnapshot
    {
        public static VehiclePoseSnapshot Identity { get; } = new VehiclePoseSnapshot(
            pivotPose: new Pose2(0, 0, 0),
            frontPose: new Pose2(0, 0, 0),
            rearPose: new Pose2(0, 0, 0),
            imuPose: new Pose2(0, 0, 0),
            antennaWorld: new XyCoord(0, 0),
            hitchWorld: new XyCoord(0, 0),
            pivotLocalAntenna: new XyCoord(0, 0),
            pivotLocalHitch: new XyCoord(0, 0),
            pivotLocalFrontAxle: new XyCoord(0, 0),
            pivotLocalRearAxle: new XyCoord(0, 0),
            articulationAngle: 0,
            isModelEnabled: false);

        public VehiclePoseSnapshot(
            Pose2 pivotPose,
            Pose2 frontPose,
            Pose2 rearPose,
            Pose2 imuPose,
            XyCoord antennaWorld,
            XyCoord hitchWorld,
            XyCoord pivotLocalAntenna,
            XyCoord pivotLocalHitch,
            XyCoord pivotLocalFrontAxle,
            XyCoord pivotLocalRearAxle,
            double articulationAngle,
            bool isModelEnabled)
        {
            PivotPose = pivotPose;
            FrontPose = frontPose;
            RearPose = rearPose;
            ImuPose = imuPose;
            AntennaWorld = antennaWorld;
            HitchWorld = hitchWorld;
            PivotLocalAntenna = pivotLocalAntenna;
            PivotLocalHitch = pivotLocalHitch;
            PivotLocalFrontAxle = pivotLocalFrontAxle;
            PivotLocalRearAxle = pivotLocalRearAxle;
            ArticulationAngle = articulationAngle;
            IsArticulationModelEnabled = isModelEnabled;
        }

        public Pose2 PivotPose { get; }
        public Pose2 FrontPose { get; }
        public Pose2 RearPose { get; }
        public Pose2 ImuPose { get; }
        public XyCoord AntennaWorld { get; }
        public XyCoord HitchWorld { get; }
        public XyCoord PivotLocalAntenna { get; }
        public XyCoord PivotLocalHitch { get; }
        public XyCoord PivotLocalFrontAxle { get; }
        public XyCoord PivotLocalRearAxle { get; }
        public double ArticulationAngle { get; }
        public bool IsArticulationModelEnabled { get; }
    }

    public static class VehiclePoseCalculator
    {
        public static VehiclePoseSnapshot Compute(
            bool articulationModelEnabled,
            double pivotHeading,
            double articulationAngle,
            XyCoord antennaWorld,
            double wheelbase,
            double antennaPivot,
            double antennaOffset,
            double hitchLength)
        {
            if (!articulationModelEnabled)
            {
                return ComputeRigid(
                    pivotHeading,
                    antennaWorld,
                    wheelbase,
                    antennaPivot,
                    antennaOffset,
                    hitchLength);
            }

            double halfWheelbase = wheelbase * 0.5;
            double frontYaw = pivotHeading + (articulationAngle * 0.5);
            double rearYaw = pivotHeading - (articulationAngle * 0.5);

            XyCoord pivotToAntennaLocal = new XyCoord(-antennaOffset, antennaPivot);
            XyCoord pivotToAntennaWorld = Pose2.Rotate(frontYaw, pivotToAntennaLocal.X, pivotToAntennaLocal.Y);

            double pivotX = antennaWorld.X - pivotToAntennaWorld.X;
            double pivotY = antennaWorld.Y - pivotToAntennaWorld.Y;

            Pose2 pivotPose = new Pose2(pivotX, pivotY, pivotHeading);

            XyCoord pivotToFrontWorld = Pose2.Rotate(frontYaw, 0, halfWheelbase);
            XyCoord frontWorld = new XyCoord(pivotX + pivotToFrontWorld.X, pivotY + pivotToFrontWorld.Y);
            Pose2 frontPose = new Pose2(frontWorld.X, frontWorld.Y, frontYaw);

            XyCoord pivotToRearWorld = Pose2.Rotate(rearYaw, 0, -halfWheelbase);
            XyCoord rearWorld = new XyCoord(pivotX + pivotToRearWorld.X, pivotY + pivotToRearWorld.Y);
            Pose2 rearPose = new Pose2(rearWorld.X, rearWorld.Y, rearYaw);

            double hitchYaw = hitchLength >= 0 ? frontYaw : rearYaw;
            XyCoord pivotToHitchWorld = Pose2.Rotate(hitchYaw, 0, hitchLength);
            XyCoord hitchWorld = new XyCoord(pivotX + pivotToHitchWorld.X, pivotY + pivotToHitchWorld.Y);

            Pose2 imuPose = new Pose2(antennaWorld.X, antennaWorld.Y, frontYaw);

            XyCoord pivotLocalAntenna = pivotPose.ToLocal(antennaWorld.X, antennaWorld.Y);
            XyCoord pivotLocalHitch = pivotPose.ToLocal(hitchWorld.X, hitchWorld.Y);
            XyCoord pivotLocalFrontAxle = pivotPose.ToLocal(frontPose.X, frontPose.Y);
            XyCoord pivotLocalRearAxle = pivotPose.ToLocal(rearPose.X, rearPose.Y);

            return new VehiclePoseSnapshot(
                pivotPose,
                frontPose,
                rearPose,
                imuPose,
                antennaWorld,
                hitchWorld,
                pivotLocalAntenna,
                pivotLocalHitch,
                pivotLocalFrontAxle,
                pivotLocalRearAxle,
                articulationAngle,
                true);
        }

        private static VehiclePoseSnapshot ComputeRigid(
            double pivotHeading,
            XyCoord antennaWorld,
            double wheelbase,
            double antennaPivot,
            double antennaOffset,
            double hitchLength)
        {
            XyCoord pivotToAntennaLocal = new XyCoord(-antennaOffset, antennaPivot);
            XyCoord pivotToAntennaWorld = Pose2.Rotate(pivotHeading, pivotToAntennaLocal.X, pivotToAntennaLocal.Y);

            double pivotX = antennaWorld.X - pivotToAntennaWorld.X;
            double pivotY = antennaWorld.Y - pivotToAntennaWorld.Y;

            Pose2 pivotPose = new Pose2(pivotX, pivotY, pivotHeading);

            XyCoord frontWorld = pivotPose.ApplyLocal(0, wheelbase);
            Pose2 frontPose = new Pose2(frontWorld.X, frontWorld.Y, pivotHeading);

            Pose2 rearPose = pivotPose;

            XyCoord hitchWorld = new XyCoord(
                antennaWorld.X + (Math.Sin(pivotHeading) * (hitchLength - antennaPivot)),
                antennaWorld.Y + (Math.Cos(pivotHeading) * (hitchLength - antennaPivot)));

            Pose2 imuPose = new Pose2(antennaWorld.X, antennaWorld.Y, pivotHeading);

            XyCoord pivotLocalAntenna = pivotPose.ToLocal(antennaWorld.X, antennaWorld.Y);
            XyCoord pivotLocalHitch = pivotPose.ToLocal(hitchWorld.X, hitchWorld.Y);
            XyCoord pivotLocalFrontAxle = pivotPose.ToLocal(frontPose.X, frontPose.Y);
            XyCoord pivotLocalRearAxle = pivotPose.ToLocal(rearPose.X, rearPose.Y);

            return new VehiclePoseSnapshot(
                pivotPose,
                frontPose,
                rearPose,
                imuPose,
                antennaWorld,
                hitchWorld,
                pivotLocalAntenna,
                pivotLocalHitch,
                pivotLocalFrontAxle,
                pivotLocalRearAxle,
                articulationAngle: 0,
                isModelEnabled: false);
        }
    }
}
