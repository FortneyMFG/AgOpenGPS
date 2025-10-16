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

            double frontHeading = pivotHeading + (articulationAngle * 0.5);
            double rearHeading = pivotHeading - (articulationAngle * 0.5);

            XyCoord frontOriginLocalPivot = new XyCoord(0, halfWheelbase);
            XyCoord antennaLocalFront = new XyCoord(-antennaOffset, antennaPivot - halfWheelbase);

            XyCoord rotatedFrontOrigin = Pose2.Rotate(frontHeading, frontOriginLocalPivot.X, frontOriginLocalPivot.Y);
            XyCoord rotatedAntennaLocal = Pose2.Rotate(frontHeading, antennaLocalFront.X, antennaLocalFront.Y);

            double pivotX = antennaWorld.X - rotatedFrontOrigin.X - rotatedAntennaLocal.X;
            double pivotY = antennaWorld.Y - rotatedFrontOrigin.Y - rotatedAntennaLocal.Y;

            Pose2 pivotPose = new Pose2(pivotX, pivotY, pivotHeading);

            XyCoord frontOriginWorld = new XyCoord(pivotX + rotatedFrontOrigin.X, pivotY + rotatedFrontOrigin.Y);
            Pose2 frontPose = new Pose2(frontOriginWorld.X, frontOriginWorld.Y, frontHeading);

            XyCoord rearOriginLocalPivot = new XyCoord(0, -halfWheelbase);
            XyCoord rotatedRearOrigin = Pose2.Rotate(rearHeading, rearOriginLocalPivot.X, rearOriginLocalPivot.Y);
            XyCoord rearOriginWorld = new XyCoord(pivotX + rotatedRearOrigin.X, pivotY + rotatedRearOrigin.Y);
            Pose2 rearPose = new Pose2(rearOriginWorld.X, rearOriginWorld.Y, rearHeading);

            XyCoord antennaWorldComputed = frontPose.ApplyLocal(antennaLocalFront.X, antennaLocalFront.Y);

            XyCoord hitchLocalRear = new XyCoord(-antennaOffset, hitchLength + halfWheelbase);
            XyCoord hitchWorld = rearPose.ApplyLocal(hitchLocalRear.X, hitchLocalRear.Y);

            Pose2 imuPose = new Pose2(antennaWorldComputed.X, antennaWorldComputed.Y, frontPose.Yaw);

            XyCoord pivotLocalAntenna = pivotPose.ToLocal(antennaWorldComputed.X, antennaWorldComputed.Y);
            XyCoord pivotLocalHitch = pivotPose.ToLocal(hitchWorld.X, hitchWorld.Y);
            XyCoord pivotLocalFrontAxle = pivotPose.ToLocal(frontPose.X, frontPose.Y);
            XyCoord pivotLocalRearAxle = pivotPose.ToLocal(rearPose.X, rearPose.Y);

            return new VehiclePoseSnapshot(
                pivotPose,
                frontPose,
                rearPose,
                imuPose,
                antennaWorldComputed,
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
            double pivotX = antennaWorld.X - (Math.Sin(pivotHeading) * antennaPivot);
            double pivotY = antennaWorld.Y - (Math.Cos(pivotHeading) * antennaPivot);

            Pose2 pivotPose = new Pose2(pivotX, pivotY, pivotHeading);

            XyCoord frontWorld = pivotPose.ApplyLocal(0, wheelbase);
            Pose2 frontPose = new Pose2(frontWorld.X, frontWorld.Y, pivotHeading);

            Pose2 rearPose = pivotPose;

            XyCoord antennaWorldComputed = pivotPose.ApplyLocal(-antennaOffset, antennaPivot);

            XyCoord hitchWorld = new XyCoord(
                antennaWorld.X + (Math.Sin(pivotHeading) * (hitchLength - antennaPivot)),
                antennaWorld.Y + (Math.Cos(pivotHeading) * (hitchLength - antennaPivot)));

            Pose2 imuPose = new Pose2(antennaWorldComputed.X, antennaWorldComputed.Y, pivotHeading);

            XyCoord pivotLocalAntenna = pivotPose.ToLocal(antennaWorldComputed.X, antennaWorldComputed.Y);
            XyCoord pivotLocalHitch = pivotPose.ToLocal(hitchWorld.X, hitchWorld.Y);
            XyCoord pivotLocalFrontAxle = pivotPose.ToLocal(frontPose.X, frontPose.Y);
            XyCoord pivotLocalRearAxle = pivotPose.ToLocal(rearPose.X, rearPose.Y);

            return new VehiclePoseSnapshot(
                pivotPose,
                frontPose,
                rearPose,
                imuPose,
                antennaWorldComputed,
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
