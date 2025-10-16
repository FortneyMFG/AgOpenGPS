using AgOpenGPS.Core.Kinematics.Math;

namespace AgOpenGPS.Core.Kinematics
{
    public static class ArticulatedKinematics
    {
        public static Result Compute(
            double psi,
            double alpha,
            Vector3 articulationWorldPosition,
            double wheelbaseFront,
            double wheelbaseRear,
            Vector3 antennaOffsetFrontFrame,
            Vector3 imuOffsetFrontFrame,
            Vector3 drawbarOffsetRearFrame)
        {
            Matrix3 rv = Rotation.Rz(psi);
            Matrix3 rff = Rotation.Rz(psi + alpha * 0.5);
            Matrix3 rrf = Rotation.Rz(psi - alpha * 0.5);

            Vector3 frontFrameWorld = articulationWorldPosition + rv * new Vector3(wheelbaseFront, 0, 0);
            Vector3 rearFrameWorld = articulationWorldPosition + rv * new Vector3(-wheelbaseRear, 0, 0);

            Vector3 antennaWorld = frontFrameWorld + rff * antennaOffsetFrontFrame;
            Vector3 imuWorld = frontFrameWorld + rff * imuOffsetFrontFrame;
            Vector3 drawbarWorld = rearFrameWorld + rrf * drawbarOffsetRearFrame;

            return new Result(frontFrameWorld, rff, rearFrameWorld, rrf, antennaWorld, imuWorld, drawbarWorld);
        }

        public readonly record struct Result(
            Vector3 FrontFrameWorldPosition,
            Matrix3 FrontFrameOrientation,
            Vector3 RearFrameWorldPosition,
            Matrix3 RearFrameOrientation,
            Vector3 AntennaWorldPosition,
            Vector3 ImuWorldPosition,
            Vector3 DrawbarWorldPosition);
    }
}
