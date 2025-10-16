# 4WD (Articulated) Equipment Setup Overview

![Articulated equipment selection view](../SourceCode/GPS/btnImages/vehiclePageArticulated.png)

This guide explains how the AgOpenGPS WinForms client configures a four-wheel-drive (articulated) vehicle, how the user interface flows, and how the vehicle model ties into mapping, autosteer, and sensor inputs.

## Configuration UI flow

1. **Vehicle type and branding.** The `ConfigVehicleControl` user control lets operators choose the articulated platform and the matching brand artwork. The control switches the highlighted radio button based on the `VehicleConfig.Type`, loads the last saved brand from settings, and paints the preview image with the selected bitmap.【F:SourceCode/GPS/Forms/Config/ConfigVehicleControl.cs†L12-L145】

   ```csharp
   public ArticulatedBrand ArticulatedBrand {
       get => _articulatedBrand;
       private set {
           _articulatedBrand = value;
           pboxAlpha.BackgroundImage = ArticulatedBitmaps.GetFrontBitmap(_articulatedBrand);
       }
   }
   ```

   When the articulated option is active, the brand palette becomes visible, and the control persists the chosen logo back to `Settings.Default.setBrand_WDBrand` so the rest of the application can load the matching textures.【F:SourceCode/GPS/Forms/Config/ConfigVehicleControl.cs†L94-L133】

2. **Hitch geometry.** The configuration workflow routes the user through the hitch tab where the UI background image changes to match the current hitch style (front-fixed, rear-fixed, TBT, or trailing) and the numeric fields update the saved hitch lengths used by the kinematic model.【F:SourceCode/GPS/Forms/Settings/ConfigTool.Designer.cs†L139-L220】 Those lengths ultimately define the rigid drawbar segment drawn between the tractor and implement.【F:SourceCode/GPS/Classes/CVehicle.cs†L141-L160】

3. **Antenna pivot and offsets.** Dedicated numeric controls in the steering wizard write the antenna pivot distance into persisted settings, which the runtime vehicle configuration reads to locate the GNSS receiver relative to the articulation joint.【F:SourceCode/GPS/Forms/Settings/FormSteerWiz.cs†L31-L189】【F:SourceCode/GPS/Classes/CVehicle.cs†L51-L78】

## Visual pipeline and map integration

* **Texture binding.** At OpenGL initialization the main form loads tractor, harvester, and articulated textures from the brand libraries so the articulated front and rear halves render with the correct decals.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L39-L55】 The `VehicleTextures` helper defers bitmap creation until first use, exposing handles for the articulated front, rear, and wheel sprites.【F:SourceCode/GPS/Classes/VehicleTextures.cs†L7-L83】
* **Drawing the articulated model.** During each render cycle `CVehicle.DrawVehicle` rotates the scene into vehicle-heading space, draws the hitch, then renders the articulated halves about the pivot by rotating the front and rear groups in opposite directions by half of the measured steer angle.【F:SourceCode/GPS/Classes/CVehicle.cs†L141-L261】 This keeps the pivot centered over the articulation joint so coverage logs, section control, and the map all agree.
* **Camera placement.** The OpenGL viewport uses a perspective projection tied to the camera’s zoom and the `camera.SetLookAt` helper targets the pivot axle coordinate each frame, keeping the articulated chassis centered on the map.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L60-L140】 Adaptive mip-mapping on triangle strips reduces patch density as the camera pulls back.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L136-L145】

## Position, pivot, and map coupling

The positioning pipeline converts GNSS fixes into on-screen positions in several steps:

1. **Pivot axle derivation.** The `CalculatePositionHeading` routine subtracts the antenna-to-pivot offset from the GNSS fix to find the articulation joint, then projects the steer axle forward one wheelbase length to maintain the internal reference frame.【F:SourceCode/GPS/Forms/Position.designer.cs†L1278-L1291】
2. **Hitch and trailing tool kinematics.** The same routine pushes the hitch point rearward using the stored hitch length and, when trailing implements are enabled, applies “Torriem” jackknife recovery rules to converge the tank and tool pivot headings back behind the tractor.【F:SourceCode/GPS/Forms/Position.designer.cs†L1300-L1369】 These coordinates feed both the map polygons and section control logic.
3. **IMU fusion.** Whenever IMU heading data is available, the code computes an offset between IMU and GPS bearings, filters it into `imuGPS_Offset`, and applies it so the articulated frame follows the yaw sensor while still honoring GNSS drift corrections.【F:SourceCode/GPS/Forms/Position.designer.cs†L244-L284】 Roll is also injected as a lateral correction based on antenna height to keep the pivot and hitch positions aligned on side hills.【F:SourceCode/GPS/Forms/Position.designer.cs†L299-L347】
4. **Camera heading.** After the heading solution settles the camera yaw is updated so the 3D view always points in the driven direction, reinforcing the coupling between physical articulation and the map view.【F:SourceCode/GPS/Forms/Position.designer.cs†L286-L296】

## Autosteer, WAS, and articulated control

* **Vehicle controller state.** The `CVehicle` class caches Stanley and Pure Pursuit tuning, maximum steering limits, and slow-speed cutoffs directly from persisted settings so the autosteer algorithm works off the articulated geometry with correct delay and look-ahead distances.【F:SourceCode/GPS/Classes/CVehicle.cs†L22-L138】
* **Wheel-angle feedback.** Steering configuration screens expose WAS inversion, zeroing, and counts-per-degree controls; the values stored there drive the live wheel-angle sensor interpretation used by the autosteer module to split the steer angle across the articulated front and rear halves.【F:SourceCode/GPS/Forms/Settings/FormSteer.cs†L120-L199】【F:SourceCode/GPS/Forms/Settings/FormSteerWiz.Designer.cs†L3054-L3284】
* **Commanding articulation.** When rendering, `CVehicle.DrawVehicle` halves the measured steering command so each half of the articulated chassis rotates ±½θ around the joint, mirroring what the autosteer controller sends to the steering valve and giving operators immediate visual confirmation of the WAS feedback.【F:SourceCode/GPS/Classes/CVehicle.cs†L244-L260】

## Sensor placement summary

* **GNSS antenna.** Stored pivot and height offsets locate the antenna dot drawn above the articulation joint and ensure roll corrections translate the GNSS fix sideways when the chassis leans.【F:SourceCode/GPS/Classes/CVehicle.cs†L286-L293】【F:SourceCode/GPS/Forms/Position.designer.cs†L299-L347】
* **IMU.** IMU heading and roll arrive via UDP, are filtered, and override the GPS heading when valid so the vehicle follows the physical articulation instantly even when GNSS updates are sparse.【F:SourceCode/GPS/Forms/UDPComm.Designer.cs†L136-L198】【F:SourceCode/GPS/Forms/Position.designer.cs†L244-L347】
* **Hitch and implements.** Hitch lengths from the tool configuration update `mf.tool` and are used both for drawing and for calculating coverage polygons, keeping the map’s worked area aligned with the actual trailing implement.【F:SourceCode/GPS/Forms/Settings/ConfigTool.Designer.cs†L170-L209】【F:SourceCode/GPS/Classes/CVehicle.cs†L147-L160】
* **Camera.** The OpenGL camera distance is scaled with zoom and the draw loop positions it at the articulation pivot while tilt/rotation respond to operator inputs, so screenshots match the real articulated footprint.【F:SourceCode/GPS/Forms/OpenGL.Designer.cs†L60-L140】

