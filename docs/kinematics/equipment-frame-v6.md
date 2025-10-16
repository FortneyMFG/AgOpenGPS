# Equipment Frame of Reference in AgOpenGPS v6

## 1. Overview
AgOpenGPS v6 now models the "equipment" frame as a hierarchy of rigid poses rooted at the pivot point of the machine. Every update computes a `VehiclePoseSnapshot` containing the world pose of the pivot, the child front and rear frames, and sensor/tool offsets. The pivot pose is aligned with the fused heading (`fixHeading`), while the front and rear frames gain their own headings when articulation is enabled. Their local +Y axis points forward, +X points to the right-hand side, and +Z remains upward for layering in OpenGL. Visualisation starts by rotating the scene by `-fixHeading`, after which rendering and physics reuse the same pivot-relative coordinates.【F:SourceCode/GPS/Forms/Position.designer.cs†L1258-L1379】【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L1-L173】

The vehicle frame hierarchy is:

```
Pivot pose (fixHeading)
├── Front frame pose (front axle, yaw = fixHeading + δ/2)
└── Rear frame pose  (rear axle, yaw = fixHeading − δ/2)
    └── Hitch offset (implement attach point)
```

The implement/hitch frame inherits the rear frame pose, so trailing tools align with the rear axle heading rather than the global heading. All world coordinates are still expressed in the global field NE grid (`pn.fix`, `pivotAxlePos`, `hitchPos`), but these are recomputed from the shared hierarchy so physics, autosteer, and rendering agree.【F:SourceCode/GPS/Forms/Position.designer.cs†L1258-L1379】

## 2. Antenna Position: Physical and Visual
### Physical calculations
The antenna is treated as a local offset from the front frame origin. `VehiclePoseCalculator` decomposes the pivot→antenna vector into two transforms: a translation from the pivot to the front axle along `fixHeading`, and a rotation within the front frame by `fixHeading + δ/2`. When articulation is active the antenna sweeps with the front half; when disabled the helper collapses to the legacy rigid computation.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L37-L118】

### Visual representation
`CVehicle.DrawVehicle` queries `mf.VehiclePose` and draws the antenna point using the pivot-local antenna coordinates. Articulated tractors also translate and rotate their front texture by the computed front-frame pose so the icon tracks the front hood in the simulator.【F:SourceCode/GPS/Classes/CVehicle.cs†L139-L241】

### Current issue resolved
Previously, antenna offsets were applied only to the center frame, so articulated tractors left the antenna fixed at the hinge. The new hierarchy rotates the front frame by `δ/2` and translates by ±`wheelbase/2`, which keeps the antenna co-moving with the front cab both in world math and OpenGL. The helper also feeds the same coordinates to physics so GPS-to-pivot transforms stay consistent.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L37-L118】【F:SourceCode/GPS/Forms/Position.designer.cs†L1258-L1379】

#### Math sketch
For articulation angle `δ` and wheelbase `L`:

```math
frontOrigin = R(fixHeading) · \begin{bmatrix}0\\L/2\end{bmatrix}
antennaLocal = \begin{bmatrix}-offset\\AntennaPivot - L/2\end{bmatrix}
antennaWorld = pivot + frontOrigin + R(fixHeading + δ/2) · antennaLocal
```

### Expected behaviour
When the front frame pivots, the antenna is translated and rotated by the front pose so it mirrors the cab swing. The same pose is published to guidance and the draw layer, eliminating divergence between physics and visuals.【F:SourceCode/GPS/Classes/CVehicle.cs†L139-L241】

## 3. IMU Orientation and Rotation
IMU headings are converted to pivot headings through `ConvertImuHeadingToPivot`. When the articulation-aware frame is active the IMU yaw (measured on the front frame) is offset by `δ/2` before being stored in `fixHeading`, ensuring the fused heading represents the pivot frame used by autosteer and guidance.【F:SourceCode/GPS/Forms/Position.designer.cs†L252-L339】【F:SourceCode/GPS/Forms/Position.designer.cs†L1432-L1479】

If the IMU is co-located with the antenna (default), its pose equals the front frame pose. If mounted elsewhere, the same `VehiclePoseCalculator` logic can be extended with an additional local offset so that both roll and yaw corrections are mapped back through the front frame to the pivot.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L1-L173】

Ignoring this transform causes the IMU to inject front-frame yaw directly into `fixHeading`, leading to heading jumps whenever articulation changes. Subtracting `δ/2` stabilises the fused heading during rapid steering of articulated tractors.【F:SourceCode/GPS/Forms/Position.designer.cs†L252-L339】

## 4. Hitch Position and Behavior
The hitch is expressed as a rear-frame offset `(-antennaOffset, hitchLength + L/2)` so it inherits both the rear translation and yaw. `CalculatePositionHeading` stores the resulting world coordinates in `hitchPos`, while `CVehicle.DrawVehicle` uses the same pivot-local vector to draw the hitch link. For rigid implements the rear-frame heading replaces the earlier `fixHeading`, ensuring the tool aligns with the rear axle rather than the mid-hinge.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L69-L118】【F:SourceCode/GPS/Forms/Position.designer.cs†L1280-L1379】【F:SourceCode/GPS/Classes/CVehicle.cs†L139-L241】

**Pseudocode**
```csharp
rearPose = pivotPose.TranslateLocal(0, -wheelbase/2).WithYaw(fixHeading - delta/2);
hitchLocal = new XyCoord(-antennaOffset, hitchLength + wheelbase/2);
hitchWorld = rearPose.ApplyLocal(hitchLocal.X, hitchLocal.Y);
```

## 5. Root Cause Analysis of Previous Problems
1. **Single-origin transforms:** Only the pivot was updated; antenna and hitch offsets stayed in the root frame, so articulated geometry never propagated to sensors or tools.【F:SourceCode/GPS/Forms/Position.designer.cs†L1248-L1314】
2. **Mismatched frames:** Rendering rotated texture halves but still drew antenna/hitch from origin (0,0). The new snapshot shares pivot-local coordinates between physics and OpenGL.【F:SourceCode/GPS/Classes/CVehicle.cs†L139-L241】
3. **IMU heading misuse:** IMU yaw fed directly into `fixHeading`, meaning the pivot heading instantly jumped with articulation. Applying the `δ/2` correction keeps heading continuous.【F:SourceCode/GPS/Forms/Position.designer.cs†L252-L339】
4. **Debug visibility:** Developers lacked feedback on computed positions. A debug overlay now prints pivot, front, rear, antenna, and hitch world coordinates while also logging them once per second.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L456-L515】【F:SourceCode/GPS/Forms/Position.designer.cs†L1292-L1357】

Key classes to review: `VehiclePoseCalculator`, `FormGPS.CalculatePositionHeading`, `CVehicle.DrawVehicle`, and the new debug overlay in `OpenGL.Designer`. These encapsulate all geometry transforms.

## 6. Plan to Fix the Issue
1. **Introduce hierarchy:** `VehiclePoseSnapshot` captures pivot, front, rear, antenna, hitch, and IMU poses each frame.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L1-L173】
2. **Compute articulation offsets:** The calculator splits the steering angle evenly across front/rear frames and translates ±`wheelbase/2` from the pivot before applying local sensor offsets.【F:SourceCode/AgOpenGPS.Core/Models/Vehicle/VehiclePoseSnapshot.cs†L37-L118】
3. **Recompute antenna/hitch transforms:** `CalculatePositionHeading` stores the world positions and pivot-relative coordinates for reuse downstream.【F:SourceCode/GPS/Forms/Position.designer.cs†L1258-L1379】
4. **Update IMU processing:** `ConvertImuHeadingToPivot` subtracts half the articulation angle when the toggle is enabled, so the fused heading always refers to the pivot.【F:SourceCode/GPS/Forms/Position.designer.cs†L252-L339】
5. **Synchronise visualization:** `CVehicle.DrawVehicle` draws hitch lines, articulated halves, and the antenna using the same pivot-local coordinates generated for physics.【F:SourceCode/GPS/Classes/CVehicle.cs†L139-L241】
6. **Validate geometry:** The debug overlay lists pivot, front, rear, antenna, and hitch world coordinates for every frame; unit tests cover the transform math (see §7).【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L456-L515】

## 7. Validation and Testing
- **Visual check:** Run the simulator and confirm the antenna icon tracks the front hood through left/right articulation.
- **Rear frame alignment:** Watch the hitch marker as the tractor turns; it should trail with the rear frame instead of the pivot.
- **IMU stability:** Enable the articulation toggle and verify heading remains smooth when steering quickly.
- **Debug overlay/log:** Observe the multi-line overlay or review the periodic log entries to ensure pivot/front/rear/antenna/hitch coordinates update coherently.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L456-L515】
- **Unit tests:** `VehiclePoseCalculator` tests cover rigid vs articulated scenarios, ensuring front/rear/antenna/hitch coordinates and yaw splits are correct.【F:SourceCode/AgOpenGPS.Core.Tests/Models/Vehicle/VehiclePoseCalculatorTests.cs†L1-L119】

## 8. Appendix
### Coordinate Frame Diagram
```
          Front frame (ψ_f)
             ^ Y_f
             |
     antenna *----> X_f (right)
             |
 pivot o---- Rear frame (ψ_r)
             |
             * hitch
```

### Transformation Formulas
```
ψ_f = fixHeading + δ/2
ψ_r = fixHeading − δ/2
frontOrigin = pivot + R(fixHeading) · [0, L/2]^T
rearOrigin  = pivot + R(fixHeading) · [0, −L/2]^T
antennaWorld = frontOrigin + R(ψ_f) · [−offset, AntPivot − L/2]^T
hitchWorld   = rearOrigin  + R(ψ_r) · [−offset, Hitch + L/2]^T
```

### Configuration
The articulation-aware frame model is enabled per vehicle via the new "Articulation-aware frame math" toggle in the vehicle configuration UI. The setting is stored in `setVehicle_useArticulatedFrameModel` and applied during vehicle initialisation.【F:SourceCode/GPS/Forms/Config/ConfigVehicleControl.cs†L36-L209】【F:SourceCode/GPS/Properties/Settings.cs†L150-L216】
