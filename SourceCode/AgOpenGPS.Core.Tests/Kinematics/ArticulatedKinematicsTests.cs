using AgOpenGPS.Core.Kinematics;
using AgOpenGPS.Core.Kinematics.Math;
using AgOpenGPS.Core.Models;
using NUnit.Framework;

namespace AgOpenGPS.Core.Tests.Kinematics
{
    public class ArticulatedKinematicsTests
    {
        [Test]
        public void Compute_ShouldMatchStraightLineExpectations()
        {
            double psi = 0;
            double alpha = 0;
            Vector3 articulation = new Vector3(0, 0, 0);
            double wheelbaseFront = 2;
            double wheelbaseRear = 2;
            Vector3 antennaLocal = new Vector3(1, 0, 0);
            Vector3 imuLocal = new Vector3(0, 0, 0);
            Vector3 drawbarLocal = new Vector3(-1, 0, 0);

            ArticulatedKinematics.Result result = ArticulatedKinematics.Compute(
                psi,
                alpha,
                articulation,
                wheelbaseFront,
                wheelbaseRear,
                antennaLocal,
                imuLocal,
                drawbarLocal);

            Assert.That(result.FrontFrameWorldPosition.X, Is.EqualTo(2).Within(1e-9));
            Assert.That(result.FrontFrameWorldPosition.Y, Is.EqualTo(0).Within(1e-9));
            Assert.That(result.RearFrameWorldPosition.X, Is.EqualTo(-2).Within(1e-9));
            Assert.That(result.RearFrameWorldPosition.Y, Is.EqualTo(0).Within(1e-9));
            Assert.That(result.AntennaWorldPosition.X, Is.EqualTo(3).Within(1e-9));
            Assert.That(result.DrawbarWorldPosition.X, Is.EqualTo(-3).Within(1e-9));
        }

        [Test]
        public void Compute_ShouldRespectLeftSteerSymmetry()
        {
            double alpha = Units.DegreesToRadians(20);
            double psi = 0;
            Vector3 articulation = new Vector3(0, 0, 0);
            double wheelbaseFront = 2;
            double wheelbaseRear = 2;
            Vector3 antennaLocal = new Vector3(1, 0, 0);
            Vector3 imuLocal = Vector3.Zero;
            Vector3 drawbarLocal = new Vector3(-1, 0, 0);

            ArticulatedKinematics.Result result = ArticulatedKinematics.Compute(
                psi,
                alpha,
                articulation,
                wheelbaseFront,
                wheelbaseRear,
                antennaLocal,
                imuLocal,
                drawbarLocal);

            Assert.That(result.FrontFrameOrientation.Yaw, Is.EqualTo(Units.DegreesToRadians(10)).Within(1e-6));
            Assert.That(result.RearFrameOrientation.Yaw, Is.EqualTo(Units.DegreesToRadians(-10)).Within(1e-6));
            Assert.That(result.AntennaWorldPosition.Y, Is.GreaterThan(0));
            Assert.That(result.DrawbarWorldPosition.Y, Is.LessThan(0));
        }

        [Test]
        public void Compute_ShouldHandleUnequalWheelbases()
        {
            double psi = Units.DegreesToRadians(15);
            double alpha = Units.DegreesToRadians(30);
            Vector3 articulation = new Vector3(1, -2, 0.5);
            double wheelbaseFront = 2.5;
            double wheelbaseRear = 1.5;
            Vector3 antennaLocal = new Vector3(0.75, 0.25, 0.0);
            Vector3 imuLocal = Vector3.Zero;
            Vector3 drawbarLocal = new Vector3(-0.5, 0.1, 0);

            ArticulatedKinematics.Result result = ArticulatedKinematics.Compute(
                psi,
                alpha,
                articulation,
                wheelbaseFront,
                wheelbaseRear,
                antennaLocal,
                imuLocal,
                drawbarLocal);

            Vector3 expectedFront = articulation + Rotation.Rz(psi) * new Vector3(wheelbaseFront, 0, 0);
            Vector3 expectedRear = articulation + Rotation.Rz(psi) * new Vector3(-wheelbaseRear, 0, 0);
            Vector3 expectedAntenna = expectedFront + Rotation.Rz(psi + alpha * 0.5) * antennaLocal;
            Vector3 expectedDrawbar = expectedRear + Rotation.Rz(psi - alpha * 0.5) * drawbarLocal;

            Assert.That(result.FrontFrameWorldPosition.X, Is.EqualTo(expectedFront.X).Within(1e-9));
            Assert.That(result.FrontFrameWorldPosition.Y, Is.EqualTo(expectedFront.Y).Within(1e-9));
            Assert.That(result.RearFrameWorldPosition.X, Is.EqualTo(expectedRear.X).Within(1e-9));
            Assert.That(result.RearFrameWorldPosition.Y, Is.EqualTo(expectedRear.Y).Within(1e-9));
            Assert.That(result.AntennaWorldPosition.X, Is.EqualTo(expectedAntenna.X).Within(1e-9));
            Assert.That(result.AntennaWorldPosition.Y, Is.EqualTo(expectedAntenna.Y).Within(1e-9));
            Assert.That(result.DrawbarWorldPosition.X, Is.EqualTo(expectedDrawbar.X).Within(1e-9));
            Assert.That(result.DrawbarWorldPosition.Y, Is.EqualTo(expectedDrawbar.Y).Within(1e-9));
        }

        [Test]
        public void Compute_ShouldProvideOrientationWithoutImu()
        {
            double psi = Units.DegreesToRadians(80);
            double alpha = Units.DegreesToRadians(0);
            Vector3 articulation = new Vector3(2, 4, 0);
            double wheelbaseFront = 1.5;
            double wheelbaseRear = 1.5;
            Vector3 antennaLocal = new Vector3(0, 0, 0);
            Vector3 imuLocal = Vector3.Zero;
            Vector3 drawbarLocal = Vector3.Zero;

            ArticulatedKinematics.Result result = ArticulatedKinematics.Compute(
                psi,
                alpha,
                articulation,
                wheelbaseFront,
                wheelbaseRear,
                antennaLocal,
                imuLocal,
                drawbarLocal);

            Assert.That(result.FrontFrameOrientation.Yaw, Is.EqualTo(psi).Within(1e-9));
            Assert.That(result.RearFrameOrientation.Yaw, Is.EqualTo(psi).Within(1e-9));
        }
    }
}
