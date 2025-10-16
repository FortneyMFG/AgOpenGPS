//Please, if you use this, share the improvements

using AgOpenGPS.Core.Drawing;
using AgOpenGPS.Core.DrawLib;
using AgOpenGPS.Core.Models;
using OpenTK.Graphics.OpenGL;
using System;

namespace AgOpenGPS
{
    public class CVehicle
    {
        private readonly FormGPS mf;

        public int deadZoneHeading, deadZoneDelay;
        public int deadZoneDelayCounter;
        public bool isInDeadZone;

        //min vehicle speed allowed before turning shit off
        public double slowSpeedCutoff = 0;

        //autosteer values
        public double goalPointLookAheadHold, goalPointLookAheadMult, goalPointAcquireFactor, uturnCompensation;

        public double stanleyDistanceErrorGain, stanleyHeadingErrorGain;
        public double maxSteerAngle, maxSteerSpeed, minSteerSpeed;
        public double maxAngularVelocity;
        public double hydLiftLookAheadTime;

        public double hydLiftLookAheadDistanceLeft, hydLiftLookAheadDistanceRight;

        public bool isHydLiftOn;
        public double stanleyIntegralGainAB, purePursuitIntegralGain;

        //flag for free drive window to control autosteer
        public bool isInFreeDriveMode;

        //the trackbar angle for free drive
        public double driveFreeSteerAngle = 0;

        public double modeXTE, modeActualXTE = 0, modeActualHeadingError = 0;
        public int modeTime = 0;

        public double functionSpeedLimit;

        public CVehicle(FormGPS _f)
        {
            //constructor
            mf = _f;

            VehicleConfig = new VehicleConfig();

            VehicleConfig.AntennaHeight = Properties.Settings.Default.setVehicle_antennaHeight;
            VehicleConfig.AntennaPivot = Properties.Settings.Default.setVehicle_antennaPivot;
            VehicleConfig.AntennaOffset = Properties.Settings.Default.setVehicle_antennaOffset;

            VehicleConfig.Wheelbase = Properties.Settings.Default.setVehicle_wheelbase;

            slowSpeedCutoff = Properties.Settings.Default.setVehicle_slowSpeedCutoff;

            goalPointLookAheadHold = Properties.Settings.Default.setVehicle_goalPointLookAheadHold;
            goalPointLookAheadMult = Properties.Settings.Default.setVehicle_goalPointLookAheadMult;
            goalPointAcquireFactor = Properties.Settings.Default.setVehicle_goalPointAcquireFactor;

            stanleyDistanceErrorGain = Properties.Settings.Default.stanleyDistanceErrorGain;
            stanleyHeadingErrorGain = Properties.Settings.Default.stanleyHeadingErrorGain;

            maxAngularVelocity = Properties.Settings.Default.setVehicle_maxAngularVelocity;
            maxSteerAngle = Properties.Settings.Default.setVehicle_maxSteerAngle;

            isHydLiftOn = false;

            VehicleConfig.TrackWidth = Properties.Settings.Default.setVehicle_trackWidth;

            stanleyIntegralGainAB = Properties.Settings.Default.stanleyIntegralGainAB;

            purePursuitIntegralGain = Properties.Settings.Default.purePursuitIntegralGainAB;
            VehicleConfig.Type = (VehicleType)Properties.Settings.Default.setVehicle_vehicleType;

            hydLiftLookAheadTime = Properties.Settings.Default.setVehicle_hydraulicLiftLookAhead;

            deadZoneHeading = Properties.Settings.Default.setAS_deadZoneHeading;
            deadZoneDelay = Properties.Settings.Default.setAS_deadZoneDelay;

            isInFreeDriveMode = false;

            //how far from line before it becomes Hold
            modeXTE = 0.2;

            //how long before hold is activated
            modeTime = 1;

            functionSpeedLimit = Properties.Settings.Default.setAS_functionSpeedLimit;
            maxSteerSpeed = Properties.Settings.Default.setAS_maxSteerSpeed;
            minSteerSpeed = Properties.Settings.Default.setAS_minSteerSpeed;

            uturnCompensation = Properties.Settings.Default.setAS_uTurnCompensation;
        }

        public int modeTimeCounter = 0;
        public double goalDistance = 0;

        public VehicleConfig VehicleConfig { get; }

        public double UpdateGoalPointDistance()
        {
            double xTE = Math.Abs(modeActualXTE);
            double goalPointDistance = mf.avgSpeed * 0.05 * goalPointLookAheadMult;

            double LoekiAheadHold = goalPointLookAheadHold;
            double LoekiAheadAcquire = goalPointLookAheadHold * goalPointAcquireFactor;

            if (xTE <= 0.1)
            {
                goalPointDistance *= LoekiAheadHold;
                goalPointDistance += LoekiAheadHold;
            }

            else if (xTE > 0.1 && xTE < 0.4)
            {
                xTE -= 0.1;

                LoekiAheadHold = (1 - (xTE / 0.3)) * (LoekiAheadHold - LoekiAheadAcquire);
                LoekiAheadHold += LoekiAheadAcquire;

                goalPointDistance *= LoekiAheadHold;
                goalPointDistance += LoekiAheadHold;
            }
            else
            {
                goalPointDistance *= LoekiAheadAcquire;
                goalPointDistance += LoekiAheadAcquire;
            }

            if (goalPointDistance < 2) goalPointDistance = 2;
            goalDistance = goalPointDistance;

            return goalPointDistance;
        }

        public void DrawVehicle()
        {
            GL.Rotate(glm.toDegrees(-mf.fixHeading), 0.0, 0.0, 1.0);
            double pivotX = mf.pivotAxlePos.easting;
            double pivotY = mf.pivotAxlePos.northing;
            double sinFixHeading = Math.Sin(mf.fixHeading);
            double cosFixHeading = Math.Cos(mf.fixHeading);
            double frontHeading = mf.steerAxlePos.heading;
            double sinFrontHeading = Math.Sin(frontHeading);
            double cosFrontHeading = Math.Cos(frontHeading);
            double frontAxleX = mf.steerAxlePos.easting;
            double frontAxleY = mf.steerAxlePos.northing;
            double pivotToFrontDistance = Math.Sqrt(
                (frontAxleX - pivotX) * (frontAxleX - pivotX)
                + (frontAxleY - pivotY) * (frontAxleY - pivotY));
            if (glm.IsZero(pivotToFrontDistance))
            {
                pivotToFrontDistance = VehicleConfig.Type == VehicleType.Articulated
                    ? 0.5 * VehicleConfig.Wheelbase
                    : VehicleConfig.Wheelbase;
                frontAxleX = pivotX + sinFrontHeading * pivotToFrontDistance;
                frontAxleY = pivotY + cosFrontHeading * pivotToFrontDistance;
            }

            XyCoord TransformWorldToVehicleLocal(double worldX, double worldY)
            {
                double dx = worldX - pivotX;
                double dy = worldY - pivotY;
                double x = cosFixHeading * dx - sinFixHeading * dy;
                double y = sinFixHeading * dx + cosFixHeading * dy;
                return new XyCoord(x, y);
            }

            //mf.font.DrawText3D(0, 0, "&TGF");
            if (mf.isFirstHeadingSet && !mf.tool.isToolFrontFixed)
            {
                double hitchFrontX = pivotX;
                double hitchFrontY = pivotY;
                double hitchRearX = mf.hitchPos.easting;
                double hitchRearY = mf.hitchPos.northing;
                XyCoord hitchFrontLocal = TransformWorldToVehicleLocal(hitchFrontX, hitchFrontY);
                XyCoord hitchRearLocal = TransformWorldToVehicleLocal(hitchRearX, hitchRearY);

                XyCoord[] vertices;
                if (!mf.tool.isToolRearFixed)
                {
                    vertices = new XyCoord[]
                    {
                        hitchRearLocal, hitchFrontLocal
                    };
                }
                else
                {
                    double dirX = hitchRearX - pivotX;
                    double dirY = hitchRearY - pivotY;
                    double length = Math.Sqrt(dirX * dirX + dirY * dirY);
                    double offsetX = 0;
                    double offsetY = 0;
                    if (!glm.IsZero(length))
                    {
                        double scale = 0.35 / length;
                        offsetX = -dirY * scale;
                        offsetY = dirX * scale;
                    }

                    XyCoord rearLeft = TransformWorldToVehicleLocal(hitchRearX + offsetX, hitchRearY + offsetY);
                    XyCoord frontLeft = TransformWorldToVehicleLocal(pivotX + offsetX, pivotY + offsetY);
                    XyCoord rearRight = TransformWorldToVehicleLocal(hitchRearX - offsetX, hitchRearY - offsetY);
                    XyCoord frontRight = TransformWorldToVehicleLocal(pivotX - offsetX, pivotY - offsetY);

                    vertices = new XyCoord[]
                    {
                        rearLeft, frontLeft,
                        rearRight, frontRight
                    };
                }
                LineStyle backgroundLineStyle = new LineStyle(4, Colors.Black);
                LineStyle foregroundLineStyle = new LineStyle(1, Colors.HitchRigidColor);
                LineStyle[] layerStyles = { backgroundLineStyle, foregroundLineStyle };
                GLW.DrawLinesPrimitiveLayered(layerStyles, vertices);
            }

            //draw the vehicle Body
            if (!mf.isFirstHeadingSet && mf.headingFromSource != "Dual")
            {
                GL.Color4(1, 1, 1, 0.75);
                mf.ScreenTextures.QuestionMark.Draw(new XyCoord(1.0, 5.0), new XyCoord(5.0, 1.0));
            }

            //3 vehicle types  tractor=0 harvestor=1 Articulated=2
            ColorRgba vehicleColor = new ColorRgba(VehicleConfig.Color, (float)VehicleConfig.Opacity);
            if (VehicleConfig.IsImage)
            {
                if (VehicleConfig.Type == VehicleType.Tractor)
                {
                    //vehicle body
                    GLW.SetColor(vehicleColor);

                    AckermannAngles(
                        -(mf.timerSim.Enabled ? mf.sim.steerangleAve : mf.mc.actualSteerAngleDegrees),
                        out double leftAckermann,
                        out double rightAckermann);
                    XyCoord tractorCenter = new XyCoord(0.0, 0.5 * VehicleConfig.Wheelbase);
                    mf.VehicleTextures.Tractor.DrawCentered(
                        tractorCenter,
                        new XyDelta(VehicleConfig.TrackWidth, -1.0 * VehicleConfig.Wheelbase));

                    //right wheel
                    GL.PushMatrix();
                    GL.Translate(0.5 * VehicleConfig.TrackWidth, VehicleConfig.Wheelbase, 0);
                    GL.Rotate(rightAckermann, 0, 0, 1);

                    XyDelta frontWheelDelta = new XyDelta(0.5 * VehicleConfig.TrackWidth, -0.75 * VehicleConfig.Wheelbase);
                    mf.VehicleTextures.FrontWheel.DrawCenteredAroundOrigin(frontWheelDelta);

                    GL.PopMatrix();

                    //Left Wheel
                    GL.PushMatrix();

                    GL.Translate(-VehicleConfig.TrackWidth * 0.5, VehicleConfig.Wheelbase, 0);
                    GL.Rotate(leftAckermann, 0, 0, 1);

                    mf.VehicleTextures.FrontWheel.DrawCenteredAroundOrigin(frontWheelDelta);

                    GL.PopMatrix();
                    //disable, straight color
                }
                else if (VehicleConfig.Type == VehicleType.Harvester)
                {
                    //vehicle body

                    AckermannAngles(
                        mf.timerSim.Enabled ? mf.sim.steerAngle : mf.mc.actualSteerAngleDegrees,
                        out double leftAckermannAngle,
                        out double rightAckermannAngle);
                    ColorRgba harvesterWheelColor = new ColorRgba(Colors.HarvesterWheelColor, (float)VehicleConfig.Opacity);
                    GLW.SetColor(harvesterWheelColor);
                    //right wheel
                    GL.PushMatrix();
                    GL.Translate(VehicleConfig.TrackWidth * 0.5, -VehicleConfig.Wheelbase, 0);
                    GL.Rotate(rightAckermannAngle, 0, 0, 1);
                    XyDelta forntWheelDelta = new XyDelta(0.25 * VehicleConfig.TrackWidth, 0.5 * VehicleConfig.Wheelbase);
                    mf.VehicleTextures.FrontWheel.DrawCenteredAroundOrigin(forntWheelDelta);
                    GL.PopMatrix();

                    //Left Wheel
                    GL.PushMatrix();
                    GL.Translate(-VehicleConfig.TrackWidth * 0.5, -VehicleConfig.Wheelbase, 0);
                    GL.Rotate(leftAckermannAngle, 0, 0, 1);
                    mf.VehicleTextures.FrontWheel.DrawCenteredAroundOrigin(forntWheelDelta);
                    GL.PopMatrix();

                    GLW.SetColor(vehicleColor);
                    mf.VehicleTextures.Harvester.DrawCenteredAroundOrigin(
                        new XyDelta(VehicleConfig.TrackWidth, -1.5 * VehicleConfig.Wheelbase));
                    //disable, straight color
                }
                else if (VehicleConfig.Type == VehicleType.Articulated)
                {
                    double modelSteerAngle = 0.5 * (mf.timerSim.Enabled ? mf.sim.steerAngle : mf.mc.actualSteerAngleDegrees);
                    GLW.SetColor(vehicleColor);

                    XyDelta articulated = new XyDelta(VehicleConfig.TrackWidth, -0.65 * VehicleConfig.Wheelbase);
                    GL.PushMatrix();
                    GL.Translate(0, -VehicleConfig.Wheelbase * 0.5, 0);
                    GL.Rotate(modelSteerAngle, 0, 0, 1);
                    mf.VehicleTextures.ArticulatedRear.DrawCenteredAroundOrigin(articulated);
                    GL.PopMatrix();

                    GL.PushMatrix();
                    GL.Translate(0, VehicleConfig.Wheelbase * 0.5, 0);
                    GL.Rotate(-modelSteerAngle, 0, 0, 1);
                    mf.VehicleTextures.ArticulatedFront.DrawCenteredAroundOrigin(articulated);
                    GL.PopMatrix();
                }
            }
            else
            {
                GL.Color4(1.2, 1.20, 0.0, VehicleConfig.Opacity);
                GL.Begin(PrimitiveType.TriangleFan);
                GL.Vertex3(0, VehicleConfig.AntennaPivot, -0.0);
                GL.Vertex3(1.0, -0, 0.0);
                GL.Color4(0.0, 1.20, 1.22, VehicleConfig.Opacity);
                GL.Vertex3(0, VehicleConfig.Wheelbase, 0.0);
                GL.Color4(1.220, 0.0, 1.2, VehicleConfig.Opacity);
                GL.Vertex3(-1.0, -0, 0.0);
                GL.Vertex3(1.0, -0, 0.0);
                GL.End();

                GL.LineWidth(3);
                GL.Color3(0.12, 0.12, 0.12);
                GL.Begin(PrimitiveType.LineLoop);
                {
                    GL.Vertex3(-1.0, 0, 0);
                    GL.Vertex3(1.0, 0, 0);
                    GL.Vertex3(0, VehicleConfig.Wheelbase, 0);
                }
                GL.End();
            }
            if (mf.camera.camSetDistance > -75 && mf.isFirstHeadingSet)
            {
                //draw the bright antenna dot
                double axFront = VehicleConfig.AntennaPivot - pivotToFrontDistance;
                double ayFront = VehicleConfig.AntennaOffset;

                double forwardX = sinFrontHeading;
                double forwardY = cosFrontHeading;
                double leftX = -cosFrontHeading;
                double leftY = sinFrontHeading;

                double antennaWorldX = frontAxleX + forwardX * axFront + leftX * ayFront;
                double antennaWorldY = frontAxleY + forwardY * axFront + leftY * ayFront;
                XyCoord antennaLocal = TransformWorldToVehicleLocal(antennaWorldX, antennaWorldY);

                PointStyle antennaBackgroundStyle = new PointStyle(16, Colors.Black);
                PointStyle antennaForegroundStyle = new PointStyle(10, Colors.AntennaColor);
                PointStyle[] layerStyles = { antennaBackgroundStyle, antennaForegroundStyle };
                GLW.DrawPointLayered(layerStyles, antennaLocal.X, antennaLocal.Y, 0.1);
            }

            if (mf.bnd.isBndBeingMade && mf.bnd.isDrawAtPivot)
            {
                if (mf.bnd.isDrawRightSide)
                {
                    GL.LineWidth(2);
                    GL.Color3(0.0, 1.270, 0.0);
                    GL.Begin(PrimitiveType.LineStrip);
                    {
                        GL.Vertex3(0.0, 0, 0);
                        GL.Color3(1.270, 1.220, 0.20);
                        GL.Vertex3(mf.bnd.createBndOffset, 0, 0);
                        GL.Vertex3(mf.bnd.createBndOffset * 0.75, 0.25, 0);
                    }
                    GL.End();
                }
                //draw on left side
                else
                {
                    GL.LineWidth(2);
                    GL.Color3(0.0, 1.270, 0.0);
                    GL.Begin(PrimitiveType.LineStrip);
                    {
                        GL.Vertex3(0.0, 0, 0);
                        GL.Color3(1.270, 1.220, 0.20);
                        GL.Vertex3(-mf.bnd.createBndOffset, 0, 0);
                        GL.Vertex3(-mf.bnd.createBndOffset * 0.75, 0.25, 0);
                    }
                    GL.End();
                }
            }

            //Svenn Arrow
            if (mf.isSvennArrowOn && mf.camera.camSetDistance > -1000)
            {
                //double offs = mf.curve.distanceFromCurrentLinePivot * 0.3;
                double svennDist = mf.camera.camSetDistance * -0.07;
                double svennWidth = svennDist * 0.22;
                LineStyle svenArrowLineStyle = new LineStyle(mf.ABLine.lineWidth, Colors.SvenArrowColor);
                GLW.SetLineStyle(svenArrowLineStyle);
                XyCoord[] vertices = {
                    new XyCoord(svennWidth, VehicleConfig.Wheelbase + svennDist),
                    new XyCoord(0, VehicleConfig.Wheelbase + svennWidth + 0.5 + svennDist),
                    new XyCoord(-svennWidth, VehicleConfig.Wheelbase + svennDist)
                };
                GLW.DrawLineStripPrimitive(vertices);
            }
            GL.LineWidth(1);
        }

        private void AckermannAngles(double wheelAngle, out double leftAckermannAngle, out double rightAckermannAngle)
        {
            leftAckermannAngle = wheelAngle;
            rightAckermannAngle = wheelAngle;
            if (wheelAngle > 0.0)
            {
                leftAckermannAngle *= 1.25;
            }
            else
            {
                rightAckermannAngle *= 1.25;
            }
        }

    }
}
