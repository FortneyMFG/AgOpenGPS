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

        public readonly struct Result
        {
            public Result(
                Vector3 frontFrameWorldPosition,
                Matrix3 frontFrameOrientation,
                Vector3 rearFrameWorldPosition,
                Matrix3 rearFrameOrientation,
                Vector3 antennaWorldPosition,
                Vector3 imuWorldPosition,
                Vector3 drawbarWorldPosition)
            {
                FrontFrameWorldPosition = frontFrameWorldPosition;
                FrontFrameOrientation = frontFrameOrientation;
                RearFrameWorldPosition = rearFrameWorldPosition;
                RearFrameOrientation = rearFrameOrientation;
                AntennaWorldPosition = antennaWorldPosition;
                ImuWorldPosition = imuWorldPosition;
                DrawbarWorldPosition = drawbarWorldPosition;
            }

            public Vector3 FrontFrameWorldPosition { get; }

            public Matrix3 FrontFrameOrientation { get; }

            public Vector3 RearFrameWorldPosition { get; }

            public Matrix3 RearFrameOrientation { get; }

            public Vector3 AntennaWorldPosition { get; }

            public Vector3 ImuWorldPosition { get; }

            public Vector3 DrawbarWorldPosition { get; }
        }
    }
}
