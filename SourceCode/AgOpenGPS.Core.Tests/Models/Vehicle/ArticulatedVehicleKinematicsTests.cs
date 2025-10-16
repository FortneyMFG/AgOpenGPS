using System;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Models.Vehicle;
using NUnit.Framework;

namespace AgOpenGPS.Core.Tests.Models.Vehicle
{
    [TestFixture]
    public class ArticulatedVehicleKinematicsTests
    {
        private const double Tolerance = 1e-9;

        private static ArticulatedVehicleGeometry CreateGeometry(
            double pivotToFront,
            double pivotToRear,
            Vector2D antennaOffset,
            Vector2D hitchOffset)
        {
            return new ArticulatedVehicleGeometry(
                pivotToFront,
                pivotToRear,
                antennaOffset,
                null,
                null,
                hitchOffset,
                articulationLimitDegrees: 35.0);
        }

        [Test]
        public void ForwardKinematics_CaseA_MatchesGoldenValues()
        {
            ArticulatedVehicleGeometry geometry = CreateGeometry(
                pivotToFront: 2.0,
                pivotToRear: 2.0,
                antennaOffset: new Vector2D(1.0, 0.0),
                hitchOffset: new Vector2D(1.0, 0.0));

            ArticulatedVehiclePose pose = new ArticulatedVehiclePose(
                Vector2D.Zero,
                pivotHeading: 0.0,
                articulationAngle: 0.0);

            ArticulatedVehicleKinematicsSnapshot snapshot = ArticulatedVehicleKinematics.Forward(geometry, pose);

            AssertVector(new Vector2D(2.0, 0.0), snapshot.FrontAxle);
            AssertVector(new Vector2D(-2.0, 0.0), snapshot.RearAxle);
            AssertVector(new Vector2D(3.0, 0.0), snapshot.Antenna);
            AssertVector(new Vector2D(-1.0, 0.0), snapshot.Hitch);
            Assert.That(snapshot.FrontHeading, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(snapshot.RearHeading, Is.EqualTo(0.0).Within(Tolerance));
        }

        [Test]
        public void ForwardKinematics_CaseB_PreservesLocalDistances()
        {
            ArticulatedVehicleGeometry geometry = CreateGeometry(
                pivotToFront: 2.0,
                pivotToRear: 2.0,
                antennaOffset: new Vector2D(1.0, 0.0),
                hitchOffset: new Vector2D(1.0, 0.0));

            double articulationRadians = 10.0 * Math.PI / 180.0;
            ArticulatedVehiclePose pose = new ArticulatedVehiclePose(
                Vector2D.Zero,
                pivotHeading: 0.0,
                articulationAngle: articulationRadians);

            ArticulatedVehicleKinematicsSnapshot snapshot = ArticulatedVehicleKinematics.Forward(geometry, pose);

            Assert.That(snapshot.FrontHeading, Is.EqualTo(5.0 * Math.PI / 180.0).Within(1e-12));
            Assert.That(snapshot.RearHeading, Is.EqualTo(-5.0 * Math.PI / 180.0).Within(1e-12));

            Vector2D frontLocal = Matrix2.Rotation(-snapshot.FrontHeading) * (snapshot.FrontAxle - pose.PivotPosition);
            Vector2D rearLocal = Matrix2.Rotation(-snapshot.RearHeading) * (snapshot.Hitch - snapshot.RearAxle);

            AssertVector(new Vector2D(geometry.PivotToFront, 0.0), frontLocal, 1e-12);
            AssertVector(new Vector2D(geometry.HitchOffsetRear.X, geometry.HitchOffsetRear.Y), rearLocal, 1e-12);
        }

        [Test]
        public void BackSolvePivot_PositivePivotDistance_RestoresPivot()
        {
            ArticulatedVehicleGeometry geometry = CreateGeometry(
                pivotToFront: 3.0,
                pivotToRear: 2.0,
                antennaOffset: new Vector2D(1.5, 0.0),
                hitchOffset: new Vector2D(0.5, 0.0));

            Vector2D pivot = new Vector2D(-4.0, 8.0);
            double pivotHeading = 25.0 * Math.PI / 180.0;
            ArticulatedVehiclePose pose = new ArticulatedVehiclePose(pivot, pivotHeading, articulationAngle: 0.0);

            ArticulatedVehicleKinematicsSnapshot snapshot = ArticulatedVehicleKinematics.Forward(geometry, pose);

            Vector2D solvedPivot = ArticulatedVehicleKinematics.BackSolvePivotFromAntenna(
                geometry,
                snapshot.Antenna,
                snapshot.FrontHeading);

            AssertVector(pivot, solvedPivot);
        }

        [Test]
        public void BackSolvePivot_NegativePivotDistance_RestoresPivot()
        {
            ArticulatedVehicleGeometry geometry = CreateGeometry(
                pivotToFront: 3.0,
                pivotToRear: 2.0,
                antennaOffset: new Vector2D(-1.0, 0.0),
                hitchOffset: new Vector2D(0.5, 0.0));

            Vector2D pivot = new Vector2D(1.25, -2.5);
            double pivotHeading = -15.0 * Math.PI / 180.0;
            ArticulatedVehiclePose pose = new ArticulatedVehiclePose(pivot, pivotHeading, articulationAngle: 0.0);

            ArticulatedVehicleKinematicsSnapshot snapshot = ArticulatedVehicleKinematics.Forward(geometry, pose);

            Vector2D solvedPivot = ArticulatedVehicleKinematics.BackSolvePivotFromAntenna(
                geometry,
                snapshot.Antenna,
                snapshot.FrontHeading);

            AssertVector(pivot, solvedPivot);
        }

        [Test]
        public void HitchUsesRearFrameHeading()
        {
            ArticulatedVehicleGeometry geometry = CreateGeometry(
                pivotToFront: 2.0,
                pivotToRear: 2.5,
                antennaOffset: new Vector2D(0.5, 0.0),
                hitchOffset: new Vector2D(1.0, 0.75));

            double articulationRadians = 20.0 * Math.PI / 180.0;
            ArticulatedVehiclePose pose = new ArticulatedVehiclePose(
                new Vector2D(5.0, -3.0),
                pivotHeading: 40.0 * Math.PI / 180.0,
                articulationAngle: articulationRadians);

            ArticulatedVehicleKinematicsSnapshot snapshot = ArticulatedVehicleKinematics.Forward(geometry, pose);

            Vector2D hitchLocal = Matrix2.Rotation(-snapshot.RearHeading) * (snapshot.Hitch - snapshot.RearAxle);

            AssertVector(geometry.HitchOffsetRear, hitchLocal, 1e-12);
        }

        private static void AssertVector(Vector2D expected, Vector2D actual, double tolerance = Tolerance)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance));
        }
    }
}

