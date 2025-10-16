using AgOpenGPS.Core.Models;
using NUnit.Framework;
using System;

namespace AgOpenGPS.Core.Tests.Models.Vehicle
{
    public class VehiclePoseCalculatorTests
    {
        [Test]
        public void Compute_RigidVehicle_MatchesLegacyPivotMath()
        {
            XyCoord antenna = new XyCoord(10.0, 20.0);
            double heading = Math.PI / 4; // 45 degrees
            double wheelbase = 4.0;
            double antennaPivot = 1.0;
            double antennaOffset = 0.5;
            double hitchLength = -2.0;

            VehiclePoseSnapshot pose = VehiclePoseCalculator.Compute(
                articulationModelEnabled: false,
                pivotHeading: heading,
                articulationAngle: 0.0,
                antennaWorld: antenna,
                wheelbase: wheelbase,
                antennaPivot: antennaPivot,
                antennaOffset: antennaOffset,
                hitchLength: hitchLength);

            XyCoord pivotToAntenna = Pose2.Rotate(heading, -antennaOffset, antennaPivot);
            double expectedPivotX = antenna.X - pivotToAntenna.X;
            double expectedPivotY = antenna.Y - pivotToAntenna.Y;

            Assert.That(pose.PivotPose.X, Is.EqualTo(expectedPivotX).Within(1e-9));
            Assert.That(pose.PivotPose.Y, Is.EqualTo(expectedPivotY).Within(1e-9));
            Assert.That(pose.FrontPose.X, Is.EqualTo(pose.PivotPose.X + Math.Sin(heading) * wheelbase).Within(1e-9));
            Assert.That(pose.FrontPose.Y, Is.EqualTo(pose.PivotPose.Y + Math.Cos(heading) * wheelbase).Within(1e-9));
            Assert.That(pose.FrontPose.Yaw, Is.EqualTo(heading).Within(1e-9));

            double hitchDelta = hitchLength - antennaPivot;
            Assert.That(pose.HitchWorld.X, Is.EqualTo(antenna.X + Math.Sin(heading) * hitchDelta).Within(1e-9));
            Assert.That(pose.HitchWorld.Y, Is.EqualTo(antenna.Y + Math.Cos(heading) * hitchDelta).Within(1e-9));

            Assert.That(pose.AntennaWorld.X, Is.EqualTo(antenna.X).Within(1e-9));
            Assert.That(pose.AntennaWorld.Y, Is.EqualTo(antenna.Y).Within(1e-9));
            Assert.That(pose.IsArticulationModelEnabled, Is.False);
        }

        [Test]
        public void Compute_ArticulatedVehicle_SplitsHeadingAcrossFrames()
        {
            XyCoord antenna = new XyCoord(0.0, 0.0);
            double pivotHeading = 0.0;
            double articulationAngle = Math.PI / 6; // 30 degrees
            double wheelbase = 4.0;
            double antennaPivot = 2.0;
            double antennaOffset = 0.0;
            double hitchLength = -2.0;

            VehiclePoseSnapshot pose = VehiclePoseCalculator.Compute(
                articulationModelEnabled: true,
                pivotHeading: pivotHeading,
                articulationAngle: articulationAngle,
                antennaWorld: antenna,
                wheelbase: wheelbase,
                antennaPivot: antennaPivot,
                antennaOffset: antennaOffset,
                hitchLength: hitchLength);

            Assert.That(pose.IsArticulationModelEnabled, Is.True);

            double halfWheelbase = wheelbase / 2.0;
            double frontYaw = pivotHeading + articulationAngle / 2.0;
            double rearYaw = pivotHeading - articulationAngle / 2.0;

            XyCoord pivotToAntenna = Pose2.Rotate(frontYaw, -antennaOffset, antennaPivot);
            double expectedPivotX = antenna.X - pivotToAntenna.X;
            double expectedPivotY = antenna.Y - pivotToAntenna.Y;

            Assert.That(pose.PivotPose.X, Is.EqualTo(expectedPivotX).Within(1e-9));
            Assert.That(pose.PivotPose.Y, Is.EqualTo(expectedPivotY).Within(1e-9));
            Assert.That(pose.FrontPose.Yaw, Is.EqualTo(frontYaw).Within(1e-9));
            Assert.That(pose.RearPose.Yaw, Is.EqualTo(rearYaw).Within(1e-9));

            XyCoord expectedFrontLocal = Pose2.Rotate(frontYaw - pivotHeading, 0.0, halfWheelbase);
            XyCoord expectedAntennaLocal = Pose2.Rotate(frontYaw - pivotHeading, -antennaOffset, antennaPivot);
            XyCoord expectedRearLocal = Pose2.Rotate(rearYaw - pivotHeading, 0.0, -halfWheelbase);
            double hitchYaw = hitchLength >= 0 ? frontYaw : rearYaw;
            XyCoord expectedHitchLocal = Pose2.Rotate(hitchYaw - pivotHeading, 0.0, hitchLength);

            Assert.That(pose.PivotLocalFrontAxle.X, Is.EqualTo(expectedFrontLocal.X).Within(1e-9));
            Assert.That(pose.PivotLocalFrontAxle.Y, Is.EqualTo(expectedFrontLocal.Y).Within(1e-9));
            Assert.That(pose.PivotLocalAntenna.X, Is.EqualTo(expectedAntennaLocal.X).Within(1e-9));
            Assert.That(pose.PivotLocalAntenna.Y, Is.EqualTo(expectedAntennaLocal.Y).Within(1e-9));
            Assert.That(pose.PivotLocalRearAxle.X, Is.EqualTo(expectedRearLocal.X).Within(1e-9));
            Assert.That(pose.PivotLocalRearAxle.Y, Is.EqualTo(expectedRearLocal.Y).Within(1e-9));
            Assert.That(pose.PivotLocalHitch.X, Is.EqualTo(expectedHitchLocal.X).Within(1e-9));
            Assert.That(pose.PivotLocalHitch.Y, Is.EqualTo(expectedHitchLocal.Y).Within(1e-9));

            XyCoord expectedFrontWorld = new XyCoord(
                expectedPivotX + Pose2.Rotate(frontYaw, 0.0, halfWheelbase).X,
                expectedPivotY + Pose2.Rotate(frontYaw, 0.0, halfWheelbase).Y);
            Assert.That(pose.FrontPose.X, Is.EqualTo(expectedFrontWorld.X).Within(1e-9));
            Assert.That(pose.FrontPose.Y, Is.EqualTo(expectedFrontWorld.Y).Within(1e-9));

            XyCoord expectedHitchWorld = new XyCoord(
                expectedPivotX + Pose2.Rotate(hitchYaw, 0.0, hitchLength).X,
                expectedPivotY + Pose2.Rotate(hitchYaw, 0.0, hitchLength).Y);
            Assert.That(pose.HitchWorld.X, Is.EqualTo(expectedHitchWorld.X).Within(1e-9));
            Assert.That(pose.HitchWorld.Y, Is.EqualTo(expectedHitchWorld.Y).Within(1e-9));

            Assert.That(pose.AntennaWorld.X, Is.EqualTo(antenna.X).Within(1e-9));
            Assert.That(pose.AntennaWorld.Y, Is.EqualTo(antenna.Y).Within(1e-9));
        }
    }
}
