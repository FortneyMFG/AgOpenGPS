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

            Assert.That(pose.PivotPose.X, Is.EqualTo(antenna.X - Math.Sin(heading) * antennaPivot).Within(1e-9));
            Assert.That(pose.PivotPose.Y, Is.EqualTo(antenna.Y - Math.Cos(heading) * antennaPivot).Within(1e-9));
            Assert.That(pose.FrontPose.X, Is.EqualTo(pose.PivotPose.X + Math.Sin(heading) * wheelbase).Within(1e-9));
            Assert.That(pose.FrontPose.Y, Is.EqualTo(pose.PivotPose.Y + Math.Cos(heading) * wheelbase).Within(1e-9));
            Assert.That(pose.FrontPose.Yaw, Is.EqualTo(heading).Within(1e-9));

            double hitchDelta = hitchLength - antennaPivot;
            Assert.That(pose.HitchWorld.X, Is.EqualTo(antenna.X + Math.Sin(heading) * hitchDelta).Within(1e-9));
            Assert.That(pose.HitchWorld.Y, Is.EqualTo(antenna.Y + Math.Cos(heading) * hitchDelta).Within(1e-9));

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
            Assert.That(pose.FrontPose.Yaw, Is.EqualTo(pivotHeading + articulationAngle / 2.0).Within(1e-9));
            Assert.That(pose.RearPose.Yaw, Is.EqualTo(pivotHeading - articulationAngle / 2.0).Within(1e-9));
            Assert.That(pose.PivotLocalFrontAxle.X, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(pose.PivotLocalFrontAxle.Y, Is.EqualTo(halfWheelbase).Within(1e-9));
            Assert.That(pose.PivotLocalAntenna.X, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(pose.PivotLocalAntenna.Y, Is.EqualTo(antennaPivot).Within(1e-9));
            double expectedHitchLocalY = hitchLength + halfWheelbase;
            Assert.That(pose.PivotLocalHitch.X, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(pose.PivotLocalHitch.Y, Is.EqualTo(expectedHitchLocalY).Within(1e-9));
        }
    }
}
