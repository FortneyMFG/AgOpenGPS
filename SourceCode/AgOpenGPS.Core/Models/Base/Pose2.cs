using System;

namespace AgOpenGPS.Core.Models
{
    public struct Pose2
    {
        public Pose2(double x, double y, double yaw)
        {
            X = x;
            Y = y;
            Yaw = yaw;
        }

        public double X { get; }
        public double Y { get; }
        public double Yaw { get; }

        public XyCoord Position => new XyCoord(X, Y);

        public Pose2 WithYaw(double yaw)
        {
            return new Pose2(X, Y, yaw);
        }

        public Pose2 TranslateLocal(double localX, double localY)
        {
            XyCoord translated = ApplyLocal(localX, localY);
            return new Pose2(translated.X, translated.Y, Yaw);
        }

        public XyCoord ApplyLocal(double localX, double localY)
        {
            double c = Math.Cos(Yaw);
            double s = Math.Sin(Yaw);

            double worldX = X + (c * localX) + (s * localY);
            double worldY = Y + (-s * localX) + (c * localY);

            return new XyCoord(worldX, worldY);
        }

        public XyCoord ToLocal(double worldX, double worldY)
        {
            double dx = worldX - X;
            double dy = worldY - Y;

            double c = Math.Cos(Yaw);
            double s = Math.Sin(Yaw);

            double localX = (c * dx) - (s * dy);
            double localY = (s * dx) + (c * dy);

            return new XyCoord(localX, localY);
        }

        public static XyCoord Rotate(double yaw, double localX, double localY)
        {
            double c = Math.Cos(yaw);
            double s = Math.Sin(yaw);

            double worldX = (c * localX) + (s * localY);
            double worldY = (-s * localX) + (c * localY);

            return new XyCoord(worldX, worldY);
        }
    }
}
