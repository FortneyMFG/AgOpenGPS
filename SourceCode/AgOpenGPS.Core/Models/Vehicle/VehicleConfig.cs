namespace AgOpenGPS.Core.Models
{
    public enum VehicleType
    {
        Tractor = 0,
        Harvester = 1,
        Articulated = 2
    }

    /// <summary>
    /// Vehicle level configuration shared across the WinForms and WPF clients.
    /// Coordinate conventions (global invariants):
    /// <list type="bullet">
    /// <item><description>World axes: X east, Y north. Heading increases counter-clockwise (CCW).</description></item>
    /// <item><description>Local +X runs forward, +Y runs to the operator's left for every frame.</description></item>
    /// <item><description>Articulation angle α = frontHeading − rearHeading (positive when the front rotates CCW relative to the rear).</description></item>
    /// <item><description>Pivot heading ψp is the bisector: frontHeading = ψp + α/2, rearHeading = ψp − α/2.</description></item>
    /// <item><description>Pivot distance is positive when a sensor is ahead of the pivot axle, negative when behind.</description></item>
    /// </list>
    /// These notes are duplicated in the articulated setup UI so that geometry, UI strings and debug overlays
    /// remain aligned.
    /// </summary>
    public class VehicleConfig
    {
        public VehicleType Type { get; set; }

        public bool IsImage { get; set; }
        public ColorRgb Color { get; set; }
        public double Opacity { get; set; }

        public double AntennaHeight { get; set; }
        public double AntennaPivot { get; set; }
        public double AntennaOffset { get; set; }

        public double Wheelbase { get; set; }
        public double TrackWidth { get; set; }

        public double PivotToFrontAxle { get; set; }
        public double PivotToRearAxle { get; set; }
        public double AntennaPivotFromFrontAxle { get; set; }

        // Articulated specific frame offsets. Front mounted sensors are stored in the front frame, rear mounted
        // drawbar/tool references are stored in the rear frame. The core geometry routines treat these as the
        // single source of truth and never mix coordinate spaces implicitly.
        public double ArticulationLimitDegrees { get; set; }

        public Vector2D AntennaOffsetFront { get; set; } = Vector2D.Zero;

        public Vector2D? ImuOffsetFront { get; set; }

        public Vector2D? CameraOffsetFront { get; set; }

        public Vector2D HitchOffsetRear { get; set; } = Vector2D.Zero;
    }
}
