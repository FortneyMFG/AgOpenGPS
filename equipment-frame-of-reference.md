# Equipment Frame of Reference in AgOpenGPS v6

## 1. Overview
The AgOpenGPS (AOG) "equipment" frame is built around the vehicle's instantaneous pivot point—the location where the implement hitch attaches on a rigid tractor, or the articulation joint on a hinged machine. Rendering begins by rotating the entire scene by the negative of the fused vehicle heading (`fixHeading`), aligning the vehicle's local +Y axis with the forward direction in global field coordinates.【F:SourceCode/GPS/Classes/CVehicle.cs†L141-L168】 This rotation places the equipment frame origin at the vehicle's center of articulation, expressed in OpenGL units that correspond to meters in the field model. From there:

| Axis | Positive Direction | Notes |
|------|--------------------|-------|
| +X   | Vehicle's right-hand side | Lateral offsets such as antenna shift use `-VehicleConfig.AntennaOffset` for left/right displacement.【F:SourceCode/GPS/Classes/CVehicle.cs†L286-L293】 |
| +Y   | Forward along the pivot-to-steer-axle line | Texture transforms translate ±`Wheelbase/2` to render front and rear halves, assuming the origin sits midway between them.【F:SourceCode/GPS/Classes/CVehicle.cs†L244-L260】 |
| +Z   | Upward out of the field plane | Primarily used for visual layering (e.g., antenna point at z=0.1).【F:SourceCode/GPS/Classes/CVehicle.cs†L286-L293】 |

The global field frame uses easting/northing in meters, with `pn.fix` carrying GPS positions and `fixHeading` giving vehicle yaw in radians.【F:SourceCode/GPS/Forms/Position.designer.cs†L241-L356】 Transforming between global and equipment frames subtracts the pivot offset (`AntennaPivot`) along the heading direction to derive the pivot axle (`pivotAxlePos`).【F:SourceCode/GPS/Forms/Position.designer.cs†L1283-L1297】

This equipment frame must remain consistent with:

* **Vehicle frame(s):** For rigid tractors, the equipment frame coincides with the rear axle frame. For articulated machines, the front and rear halves are drawn by translating ±`Wheelbase/2` and rotating by ±½ the modeled steer angle; however, offset points like the antenna are still tied to the root frame instead of their respective halves.【F:SourceCode/GPS/Classes/CVehicle.cs†L244-L293】
* **Implement/hitch frame:** The hitch position is computed from the same pivot-based origin by extending `tool.hitchLength - AntennaPivot` along the heading, meaning the implement frame assumes a rigid trailer lined up with the global heading rather than the rear-half orientation.【F:SourceCode/GPS/Forms/Position.designer.cs†L1300-L1314】
* **Global field system:** GPS fixes (`pn.fix`) are corrected for antenna lateral/vertical offsets by rotating them into the global NE plane before deriving the equipment pivot, ensuring that navigation logic uses world coordinates while visualization renders relative to the local frame.【F:SourceCode/GPS/Forms/Position.designer.cs†L289-L348】

## 2. Antenna Position: Physical and Visual
### Physical calculations
AOG applies antenna offsets in the world frame. Each GPS fix is first rotated by `-gpsHeading` to shift it by `AntennaOffset` (lateral) and roll-corrected by the antenna height. The same corrections adjust historical points during heading initialization.【F:SourceCode/GPS/Forms/Position.designer.cs†L289-L348】 After offsetting, the code backs out the pivot position by subtracting `AntennaPivot` along `fixHeading`, effectively translating from antenna to pivot coordinates.【F:SourceCode/GPS/Forms/Position.designer.cs†L1283-L1297】

### Visual representation
In the OpenGL renderer, the antenna icon is drawn at a fixed local coordinate `(-AntennaOffset, AntennaPivot, 0.1)`, independent of articulation or steering.【F:SourceCode/GPS/Classes/CVehicle.cs†L286-L293】 Because only the overall root matrix is rotated by `fixHeading`, the antenna remains locked to the vehicle origin instead of following the front half of an articulated tractor.

### Current issue and expected behavior
On articulated machines, the simulated steer angle is split evenly across front and rear halves for texture rotation, yet the antenna's translation/rotation is never recomputed relative to the front frame. The expected behavior is:

1. Translate from the articulation joint to the front-frame IMU/antenna mount using the front-frame geometry (wheelbase front half, lateral offset).
2. Rotate that vector by the articulation angle to align with the actual front-frame heading.
3. Apply the root rotation so the antenna swings out with the front hood when the tractor articulates.

The required transformation belongs alongside the other kinematic updates—`CalculatePositionHeading` for physics and `DrawVehicle` for visualization—so that the antenna's local transform is derived from the same articulation-aware matrix before `GLW.DrawPointLayered` is issued.【F:SourceCode/GPS/Forms/Position.designer.cs†L1278-L1314】【F:SourceCode/GPS/Classes/CVehicle.cs†L244-L293】 Currently, no articulation angle is injected into those calculations; the code bases offsets solely on `fixHeading`, so the antenna neither swings nor translates as the front half pivots.

### Math sketch
Consider an articulation angle `δ` (positive turning left) with total wheelbase `L`. If the front-frame pivot-to-antenna vector in its local frame is `\(p_f = (x_f, y_f)\)`, then the world-relative vector after articulation is:

```math
R(δ/2) = \begin{bmatrix}\cos(δ/2) & -\sin(δ/2) \\ \sin(δ/2) & \cos(δ/2)\end{bmatrix},
\quad t_f = R(δ/2) \cdot p_f + \begin{bmatrix}0 \\ L/2\end{bmatrix}
```

The antenna's local pose should be `t_f` relative to the articulation joint before the global `fixHeading` rotation is applied. Visually, the renderer should push a matrix, apply the front-half translation `(+0, +L/2)`, rotate by `-δ/2`, then draw the antenna point.

```
Global -> pivot (fixHeading) -> [rear frame] -> articulation -> [front frame] -> antenna
```

## 3. IMU Orientation and Rotation
During heading initialization, the IMU heading is aligned to GPS by computing `imuGPS_Offset` and adding it to the raw IMU heading to form `imuCorrected`, which becomes `fixHeading` whenever gyro data is available.【F:SourceCode/GPS/Forms/Position.designer.cs†L247-L284】 This implicitly assumes the IMU shares the antenna's point of reference: offsets are applied to GPS fixes prior to heading fusion, but no additional transform accounts for the IMU's physical displacement.

For articulated tractors, the IMU is typically mounted on the front frame (cab). Without transforming IMU orientation and position through the articulation joint, several problems arise:

* **Heading jumps:** Because `fixHeading` feeds both steering logic and rendering, any mismatch between the IMU's frame and the pivot causes sudden changes when articulation changes quickly, especially if the IMU is effectively measuring the front-frame heading while the physics expect a pivot-centered heading.
* **Roll/tilt drift:** Roll corrections assume the antenna height is measured relative to the pivot axis, yet the roll distance is simply `sin(roll) * -AntennaHeight` applied to the GPS point.【F:SourceCode/GPS/Forms/Position.designer.cs†L337-L348】 If the IMU is offset laterally or longitudinally, those corrections should rotate about its mount point before mapping back to the pivot.

To handle arbitrary IMU locations, derive the IMU pose in the appropriate frame (front or rear). For a front-mounted IMU on an articulated machine, transform its local offset `p_{imu}` with the same `R(δ/2)` rotation used for the antenna so that heading smoothing works on the pivot-centered yaw while maintaining the correct orientation difference when computing corrections.

## 4. Hitch Position and Behavior
`CalculatePositionHeading` determines the hitch position by extending from the antenna-corrected fix along `fixHeading` using `(tool.hitchLength - AntennaPivot)`.【F:SourceCode/GPS/Forms/Position.designer.cs†L1300-L1314】 This assumes the hitch lies on the same centerline as the GPS point regardless of articulation. The rendering code then draws rigid hitch lines without additional articulation transforms.【F:SourceCode/GPS/Classes/CVehicle.cs†L141-L168】 Consequently, the hitch appears fixed relative to the root even when the rear frame should swing opposite the front frame.

The correct approach is to treat the rear frame as a separate child frame:

* Rear-frame heading: `ψ_r = fixHeading + δ/2`
* Rear-frame offset from pivot: `t_r = R(ψ_r) * (0, -L/2)`
* Hitch offset in rear frame: `p_h = (x_h, y_h)`
* World hitch position: `p_world = pivot + R(ψ_r) * p_h`

### Pseudocode
```csharp
// pivotPose: world pose at articulation joint
// delta: articulation angle (front-left positive)
// wheelbase: distance between axle centers
Pose rearPose = pivotPose.TranslateRotate(0, -wheelbase / 2, +delta / 2);
Vector3 hitchLocal = new Vector3(hitchOffsetX, hitchOffsetY, hitchOffsetZ);
Vector3 hitchWorld = rearPose.Apply(hitchLocal);
```

Rendering should mirror this transform so that the hitch graphic follows the rear-half rotation before tool kinematics (e.g., trailing tank) are simulated.

## 5. Root Cause Analysis of Current Problems
1. **Transforms stop at the pivot:** GPS offsets and pivot translations are computed, but no articulation-aware transform is created for dependent points like the antenna or hitch. The code calls `GLW.DrawPointLayered` and hitch drawing routines with fixed local coordinates that ignore articulation.【F:SourceCode/GPS/Classes/CVehicle.cs†L141-L168】【F:SourceCode/GPS/Classes/CVehicle.cs†L286-L293】
2. **Single-origin assumption:** Hitch and pivot calculations extend directly from the GPS fix, assuming a rigid centerline, so the rear frame's yaw never diverges from `fixHeading`.【F:SourceCode/GPS/Forms/Position.designer.cs†L1283-L1314】
3. **Visual/physical mismatch:** Physically, `pn.fix` is corrected for offsets, but visualization uses OpenGL transforms without referencing those updated positions. Articulation angles enter only when rotating texture halves, not when computing offset points, so the simulation's geometry disagrees with the rendered model.
4. **Class boundaries:** `CVehicle` handles drawing, `Position` manages kinematics, and tool classes depend on `hitchPos`. None of these recompute offsets after articulation, so the bug spans `CVehicle`, `Position`, and downstream classes like `tool`/`Hitch` that read the stale positions.

Key areas to inspect and refactor include `CVehicle.DrawVehicle`, `Position.CalculatePositionHeading`, `tool`/`hitchPos` calculations, and IMU handling around `imuCorrected`.

## 6. Plan to Fix the Issue
1. **Establish frame hierarchy:** Define explicit poses for the pivot, front frame, rear frame, antenna, IMU, and hitch. Introduce helper functions (e.g., `VehiclePose.ComputeChildPose`) so front/rear transforms share the same articulation math.
2. **Calculate articulation offsets:** Use the articulation angle `δ` to derive front/rear headings (`±δ/2`) and translate ±`wheelbase/2` from the pivot before applying local offsets.
3. **Recompute antenna/hitch transforms:** Store antenna offsets relative to their frame (front for antenna/IMU, rear for hitch). Recalculate world positions before physics updates and before drawing so both layers reference the same articulated geometry.
4. **Update sensor processing:** When parsing IMU and GPS data, transform measurements back to the pivot using the new frame hierarchy. For example, subtract the articulated front-frame offset from IMU-based heading, and use that to refine `fixHeading`.
5. **Synchronize visualization:** Modify `CVehicle.DrawVehicle` to push matrices per frame, applying the same translations/rotations used in the physics model before drawing textures, antenna, and hitch sprites.
6. **Validate with scenarios:** Simulate left/right articulation and verify the antenna sweeps outward correctly while the hitch trails the rear frame. Ensure autosteer modules consume the updated positions without breaking existing rigid vehicle behavior.

### Checklist
- [ ] Add pose structs/functions supporting hierarchical transforms.
- [ ] Inject articulation-aware offsets when updating `pivotAxlePos`, `steerAxlePos`, `hitchPos`, and any sensor mounts.
- [ ] Refactor rendering to reuse the computed poses for antenna/hitch drawing.
- [ ] Update IMU offset handling so front-mounted sensors feed pivot-based heading estimates.
- [ ] Regression-test rigid tractors to ensure unchanged behavior.

### Diagram
```
          Front Frame (δ/2)
             ^ Y_f
             |
    antenna *
             |
      [Front axle]
              \
               \
 pivot o-------- Rear axle (−δ/2)
             |
             * hitch
```

## 7. Validation and Testing
* Verify in the simulator that the antenna icon tracks the front frame through full articulation sweeps.
* Observe the hitch in tight turns: it should follow the rear frame yaw and extend/retract correctly relative to the implement.
* Confirm autosteer path following by checking that the implement trail matches expected geometry for various hitch lengths.
* Review IMU logs for continuous heading without spikes when the articulation angle changes rapidly.
* Run before/after simulations capturing `pn.fix`, `pivotAxlePos`, and new articulated offsets to confirm transformations are applied consistently.

## 8. Appendix
### Coordinate frame reference
| Frame | Origin | +X | +Y |
|-------|--------|----|----|
| Global field | Earth-fixed easting/northing (`pn.fix`) | East | North |
| Pivot (equipment) | Articulation joint (`pivotAxlePos`) | Right | Forward |
| Front frame | Front axle midpoint | Right in cab | Forward along hood |
| Rear frame | Rear axle midpoint | Right | Rearward towards implement |
| Implement | Hitch point | Implement right | Along implement tongue |

### Transformation formulas
* Pivot from antenna: `pivot = gpsFix - R(fixHeading) * (0, AntennaPivot)`
* Hitch (correct): `hitch = pivot + R(fixHeading + δ/2) * (rearOffset + hitchLocal)`
* Antenna (correct): `antenna = pivot + R(fixHeading - δ/2) * (frontOffset + antennaLocal)`

### Configuration storage
Antenna geometry (`AntennaHeight`, `AntennaPivot`, `AntennaOffset`), wheelbase, and track width are loaded from `Properties.Settings.Default` into `VehicleConfig` during `CVehicle` construction.【F:SourceCode/GPS/Classes/CVehicle.cs†L46-L103】 These values are serialized in `vehicle.config` and reused across both physical calculations and visualization layers.

