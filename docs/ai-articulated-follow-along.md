# AI Follow-Along: Fixing Articulated 4WD Geometry

This walkthrough documents the exact conversation flow used to nudge Codex toward a minimal patch that corrects the articulated (4WD) antenna and hitch behaviour in AgOpenGPS v6. Each step explains the intent, keeps the copy-paste prompts collapsed by default, and highlights the portions that were hand-typed when steering the AI.

## Step 1 – Ask Codex for a deep-dive reference

Hand-typed focus: <mark>create me me a comprehensive screenshot of how the 4wd equipment setup works…</mark>

<details>
<summary>Copy-paste prompt</summary>

```text
create me me a comprehensive screenshot of how the 4wd equipement setup works, give me a doc that includes relevant code snippets on how the UI works, how the visualuals work, what the terms are, how it relates to the map, how it relates to was and autosteer, how the imu and hitch and everything else are positioned, how the camera is positioned, how the location of the pivot point is determined etc etc etc etc etc
```
</details>

## Step 2 – Capture Codex’s reference output (3m52s)

Hand-typed focus: <mark>Save the entire Codex summary for reference.</mark>

<details>
<summary>Codex response snapshot</summary>

```markdown
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
```
</details>

## Step 3 – Frame the community issue for ChatGPT

Hand-typed focus: <mark>the AOG community gave me a bit of a challenge…</mark>

<details>
<summary>Context prompt for ChatGPT</summary>

```text
the AOG community gave me a bit of a challenge, fix an issue with ai in existing V6... 

https://github.com/AgOpenGPS-Official/AgOpenGPS/issues/917 heres the issue they want me to tackle...

currentlly the steering looks good, the tractor drives real nice etc, so if we could not screw with that that would be great, the problem is with the positioning with the antenna, and hitch... the hitch needs to pivot with the rear half... the antenna needs to pivot/stay with/what ever you want to stay, its position relative to the front half... its location also needs to be possitioned relative to the position of the front axle, not the center of the tractor or where ever it is now... Can you create a breif to give to ai to try to just fix those things, with out screing up a bunch of other stuff?

can you give me a brief to give back to Codex?
```
</details>

## Step 4 – Tighten scope to geometry-only edits

Hand-typed focus: <mark>we dont need to edit UI…</mark>

<details>
<summary>Guidance prompt for a minimal patch brief</summary>

```text
we dont need to edit UI, its all correct already, same with settings and what not, also hitch length needs to be applied behind the rear axle, if the hitch has to start at the pivot/center of the vhiecle or what ever its called (origin maybe I have no idea) then make the hitch the length of the rear axle, liekly half of wheelbase+the hitch length... 

can you make a new brief and make sure it doesn't try to change net versions or anything? this should be a real small patch, hopefully a few lines of code
```
</details>

<details>
<summary>Resulting Codex brief</summary>

```text
Absolutely — here’s a minimal-impact, no-framework-change brief you can hand directly to Codex.
This version tells it exactly what to fix and what not to touch, and it makes clear this is a tiny geometric patch only.

Codex Brief — Small Patch: Fix Articulated Antenna & Hitch Positioning

(Do not change .NET versions, project structure, or build configuration)

🔒 Constraints

Do not modify project files, NuGet packages, target framework, or build settings.
This patch must compile in the current .NET Framework environment exactly as-is.

Do not edit UI forms, settings persistence, or existing logic unrelated to geometry.

Do not refactor or rename classes.
Only update the minimal math that positions the antenna and hitch in articulated (4WD) mode.

🎯 Goal

Fix the world-space position updates for articulated tractors so:

The antenna stays rigidly attached to the front half of the tractor
→ it should pivot and move with the front frame, not the pivot or center.
→ its position is defined relative to the front axle, not the vehicle center/pivot.

The hitch stays rigidly attached to the rear half of the tractor
→ it should pivot and move with the rear frame.
→ the hitch position should be located behind the rear axle by the configured hitch length.
(If geometry starts from the pivot, use total offset = rear-axle distance + hitch length.)

Everything else (steering, map rendering, tool behavior) currently looks correct and must remain unchanged.

📂 Expected file(s)

SourceCode/GPS/Classes/CVehicle.cs
(main draw + geometry math — preferred edit location)

If positioning math lives elsewhere (e.g. Position*.cs), update only the section that computes antenna and hitch world coordinates.

🧮 Current geometry (already correct, keep this)

double psiPivot;   // vehicle pivot heading
double alpha;      // articulation angle
double dFront;     // pivot → front axle
double dRear;      // pivot → rear axle
Vector2 pivotPos;  // world pivot position

double psiF = psiPivot + 0.5 * alpha;
double psiR = psiPivot - 0.5 * alpha;

Vector2 frontAxle = pivotPos + R(psiF) * new Vector2(+dFront, 0);
Vector2 rearAxle  = pivotPos + R(psiR) * new Vector2(-dRear,  0);

✅ Patch behavior

Replace any antenna/hitch world-position code that derives from pivotPos or a “center of tractor” with the correct frame-based expressions:

// ANTENNA — front half, relative to front axle
Vector2 antennaLocalFront = new(AntennaPivotDistance + dFront, 0); // ahead of front axle
Vector2 antennaWorld = frontAxle + R(psiF) * antennaLocalFront;

// HITCH — rear half, behind rear axle
Vector2 hitchLocalRear = new(-HitchLength, 0); // negative = behind rear axle
Vector2 hitchWorld = rearAxle + R(psiR) * hitchLocalRear;

// (optional if pipeline needs pivot-based point)
// Vector2 hitchWorld = pivotPos + R(psiR) * new Vector2(-(dRear + HitchLength), 0);


Keep lateral offsets or roll corrections exactly as they are now — just change the base reference from pivotPos to the correct axle frame.

No new variables or settings required; use existing AntennaPivotDistance, HitchLength, dFront, dRear, and alpha.

🧪 Acceptance check

At articulation = 0°, antenna sits forward of the front axle; hitch sits behind the rear axle.

When turning (articulating), antenna rotates with front half, hitch rotates with rear half.

Steering response, map behavior, and coverage polygons remain unchanged.

🧱 Deliverable

A single commit modifying only the math that calculates antenna and hitch world positions—likely under 10 lines of code changed—no dependency, target-framework, or project edits.
```
</details>

## Step 5 – Iterate after partial success (rear hitch fixed only)

Hand-typed focus: <mark>okay, so it seems the rear of the hitch goes to where its supposed to now…</mark>

<details>
<summary>Prompt targeting the remaining issues</summary>

```text
okay, so it seems the rear of the hitch goes to where its supposed to now... 

but the front of the hitch doesnt like to attach to the pivot (so the hitch doesn't like move properly... again the rear of it is where its supposed to go which is by far the important thing...)

and then the antenna doesn't move with the cab like it is supposed to, it is fixed much like the front of the hitch... 

Can you create a prompt to just hammer those two things specifically? without breakin the rear of the hitch or anything else
```
</details>

<details>
<summary>Focused Codex brief for hitch front & antenna</summary>

```text
Absolutely. Here’s a tight, surgical prompt you can paste into Codex. It targets exactly two fixes and forbids everything else.

Codex Patch Prompt — Fix 2 Things Only (Articulated)

Do not change .NET versions, project files, packages, UI, settings, steering, or rear hitch math.
Touch only the minimal geometry/draw code needed to fix:

1) Front-of-hitch anchor

Problem: On articulated (4WD), the front of the hitch is not attached, so the hitch bar doesn’t move correctly.

Required behavior: The rear end of the hitch must remain as-is (it’s correct and rigid to the rear half). The front end of the hitch must attach to the pivot (articulation joint) and rotate with the rear-frame heading (ψR) so the bar moves correctly during articulation.

Expected math (preserve the existing rear endpoint!)
// already correct, DO NOT CHANGE:
Vector2 hitchRearWorld = ToWorld(rearAxle, psiR, new(-HitchLength, 0)); 
// (behind rear axle; this endpoint is correct today)

// FIX the front endpoint (anchor) so it’s at the pivot and rotates properly:
Vector2 hitchFrontWorld = pivotPos; 
// draw the hitch bar from hitchFrontWorld → hitchRearWorld


If the draw code requires both points in the same local frame, compute both in world (above) and feed directly to the line draw.
If a transform is needed, ensure front endpoint uses pivotPos (not a stale cached point or world origin).

Acceptance: When turning, the hitch bar keeps its rear end glued to the rear-frame point and the front end stays pinned at the pivot, so the whole bar articulates correctly.

2) Antenna must move with the cab (front frame)

Problem: The antenna appears fixed (like the broken hitch front), not moving with the front half.

Required behavior: Antenna is rigid to the front frame; position is relative to the front axle and rotates with front heading (ψF).

Expected math

double psiF = psiPivot + 0.5 * alpha;
Vector2 frontAxle = ToWorld(pivotPos, psiF, new(+dFront, 0));

// Convert existing pivot-based distance to a front-axle-relative X on the fly.
// (Do NOT change settings/UI; just compute correctly here.)
double axFront = AntennaPivotDistance + dFront; // ahead of front axle
double ayFront = AntennaLateralOffset;          // keep existing lateral sign

Vector2 antennaWorld = ToWorld(frontAxle, psiF, new(axFront, ayFront));


Use antennaWorld everywhere the antenna dot or GNSS back-solve expects the antenna’s world position.

Acceptance: During articulation, the antenna moves/rotates with the front half (cab). Distances from front axle remain constant.

Files to change (likely)

SourceCode/GPS/Classes/CVehicle.cs (hitch line endpoints, antenna point used for drawing)

or the specific Position/Draw helper where antenna/hitch world points are computed

Do not edit steering logic, WAS, controllers, tow-between/jackknife, camera, or UI strings.

Safety checks (optional)

After your change, the rear hitch endpoint world position must be bit-for-bit identical to before (log it pre/post in debug if needed).

The front hitch endpoint must equal pivotPos.

(antennaWorld - frontAxle) length stays constant across articulation frames.

Test steps

α = 0°:

Antenna sits ahead of front axle; hitch bar front at pivot, rear behind rear axle.

α ≠ 0° (turn):

Antenna rotates with front; hitch bar rear stays glued to rear frame; front stays at pivot; bar articulates cleanly.

No change in steering feel, coverage, or rear hitch endpoint location.

Deliverable: Single small commit changing only the hitch front endpoint anchor and the antenna world-position computation. No other diffs.
```
</details>

## Step 6 – Address lateral translation & IMU rotation

Hand-typed focus: <mark>okay were closer... the rear axle seems like it wants to go to the right angle…</mark>

<details>
<summary>Prompt covering lateral translation + IMU</summary>

```text
okay were closer... the rear axle seems like it wants to go to the right angle, but when the tractor steers the pivot effectively translates left/right (which is probably accurate, the center of the axles stay in their position left/right) 

Which means both the hitch and the antenna need to translate left/right with the pivot in the center of the tractor... should hopefully be a little bit of trig based on the angle and half the wheel base... 

I hope that make sense, to be clear when the tractor steers left the pivot moves to the right... 

also the point the implment connects too is still correct, its just the visual hitch element that needs to move... 

The annetenna as a whole needs to move though.... the visual and the calcualted antenna position,  the IMU for that matter as well needs to figure for the rotation of the front half... so the like antenna/imu object will probably need to rotate? another prompt back to codex:
```
</details>

<details>
<summary>Codex brief for pivot translation and IMU</summary>

```text
Got it — here’s a laser-focused Codex patch prompt that fixes exactly what you’re seeing now:

the visual hitch’s front endpoint must translate left/right with the pivot (so the bar moves right when steering left, etc.)

the antenna (and IMU) must both translate with the pivot and rotate with the front half (ψF)

Nothing else changes. Rear hitch endpoint and implement attach point stay as-is.

Codex Patch Prompt — Articulated: Pivot L/R Translation + Antenna/IMU Rotation

Do not change .NET versions, project files, packages, UI, settings, steering, controllers, or implement attach-point math.
Touch only the minimal geometry/draw code to fix these two visuals/points.

Fix 1) Hitch bar’s front endpoint must follow the pivot (with lateral translation)

Problem: When steering, the pivot translates sideways; the rear hitch endpoint is correct, but the front of the hitch bar doesn’t move with the pivot, so the bar doesn’t articulate correctly.

Required:

Keep the rear endpoint exactly as it is now (already correct).

Set the front endpoint of the visual hitch bar to the current pivot position (world), so it translates left/right with the pivot.

Expected code shape (world coordinates):

// headings & axles are already correct elsewhere:
double psiF = psiPivot + 0.5 * alpha;
double psiR = psiPivot - 0.5 * alpha;

Vector2 frontAxle = pivotPos + R(psiF) * new Vector2(+dFront, 0);
Vector2 rearAxle  = pivotPos + R(psiR) * new Vector2(-dRear,  0);

// Rear hitch endpoint (ALREADY CORRECT) — DO NOT CHANGE:
Vector2 hitchRearWorld = rearAxle + R(psiR) * new Vector2(-HitchLengthBehindRearAxle, 0);

// FRONT hitch endpoint must be the pivot (this makes the bar translate L/R with steering):
Vector2 hitchFrontWorld = pivotPos;

// Draw the bar from hitchFrontWorld → hitchRearWorld (both in world coords)
DrawHitchBar(hitchFrontWorld, hitchRearWorld);


If current code builds the hitch in a “vehicle center” or old local frame, bypass it: compute the two world points above and feed them directly to the line draw. The implement attach point remains untouched (it is already using the correct rear endpoint).

Acceptance: Steer left → the pivot shifts right; the front of the hitch bar follows that shift, the rear stays glued to the rear frame point. The bar visually articulates.

Fix 2) Antenna (and IMU) must translate with pivot and rotate with front half

Problem: Antenna appears fixed (and IMU by implication), not moving with the cab/front frame.
Required: Antenna & IMU positions must:

Translate with the current pivot (so they follow the lateral “slide” when articulating), and

Rotate with the front frame heading ψF,

Be positioned relative to the front axle (not vehicle center).

Expected code shape:

// Already computed above:
double psiF = psiPivot + 0.5 * alpha;
Vector2 frontAxle = pivotPos + R(psiF) * new Vector2(+dFront, 0);

// Convert any pivot-based distance to front-axle-relative before placing:
double axFront = AntennaPivotDistance + dFront; // ahead of FRONT axle
double ayFront = AntennaLateralOffset;          // keep existing lateral sign convention

Vector2 antennaWorld = frontAxle + R(psiF) * new Vector2(axFront, ayFront);
Vector2 imuWorld     = frontAxle + R(psiF) * IMUOffsetFront; // if you have a separate IMU offset

// Use antennaWorld everywhere you draw the antenna dot / compute GNSS back-solve.
// If you render an IMU sprite/gizmo, rotate it by psiF so it visually follows the cab:
DrawAntenna(antennaWorld);
DrawIMU(imuWorld, psiF);


Key point: both antenna and IMU derive from frontAxle and rotate by psiF. Because frontAxle itself is constructed from pivotPos and psiF, they naturally inherit the pivot’s left/right translation.

Files to change (likely)

SourceCode/GPS/Classes/CVehicle.cs (hitch line endpoints; antenna/IMU draw points)

or whichever drawing/position helper currently sets the hitch bar endpoints and antenna/IMU world positions

Do not change:

Implement attach point logic (rear endpoint stays identical)

Steering/Stanley/PID/WAS/valve/camera

Any UI strings, settings, or persistence

Quick self-checks (manual)

α = 0°:

Antenna sits ahead of the front axle; IMU sits where expected on cab; hitch bar front at pivot, rear behind rear axle.

α ≠ 0° (turn):

Pivot slides sideways. Hitch front follows pivot; rear stays at rear frame point.

Antenna & IMU translate with pivot and rotate with ψF (cab).

Implement attach point unchanged.

No change in steering feel, coverage, or implement positioning.

Deliverable: One small commit updating only the hitch front endpoint anchor (to pivotPos) and the antenna/IMU world-position (to frontAxle + rotation by ψF). No other diffs.
```
</details>

## Step 7 – Final nudge on translation direction

Hand-typed focus: <mark>that seemed to translate the hitch/antenna left/right the wrong direction</mark>

<details>
<summary>Quick corrective prompt</summary>

```text
that seemed to translate the hitch/antenna left/right the wrong direction,
```
</details>

---

By following this chronology you can replay the entire AI-assisted iteration without losing track of the exact prompts that guided Codex toward the articulated geometry fix.
