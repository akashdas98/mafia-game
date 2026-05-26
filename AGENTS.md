# MafiaGame Agent Guide

## Architecture Overview

This repository is a Unity `2021.3.33f1` 2D oblique/top-down game project.

Top-level layout:

- `Assets/`: Unity runtime/editor assets, scenes, prefabs, scripts, sprites, tilemaps, and samples.
- `Assets/Scripts/Common/`: shared gameplay infrastructure such as `Base`, `EntityRefs`, input, inventory, target selection, and existing LOS logic.
- `Assets/Scripts/Objects/`: gameplay object controllers for characters, cars, weapons, buildings, and helpers.
- `Assets/Scripts/ObliqueLoft/`: new custom 2.5D logic-volume LOS/collision system started from `KickoffPrompts/LOS-V3.txt`.
- `Assets/Scripts/ObliqueLoft/Editor/`: editor-only tooling for authoring Oblique Loft volumes.
- `Packages/`: Unity package manifest and lock file.
- `ProjectSettings/`: Unity project settings.
- `Docs/`: durable feature documentation and architecture notes. Update these when feature behavior, setup steps, or user-facing workflows change.
- `tools/`: project-local helper scripts, including Unity CLI wrapper.

Generated/cache folders such as `Library`, `Temp`, `Logs`, `UserSettings`, `.vs`, `bin`, and `obj` should not be hand-edited unless the user explicitly requests it.

## Required Startup Context

At the start of every task in this repository:

1. Read this `AGENTS.md`.
2. Read `CONTEXT.md`.
3. Use the `Status Ledger`, `Known Issues`, and `Next Recommended Steps` in `CONTEXT.md` before deciding what to do.
4. For LOS/oblique collision work, read `KickoffPrompts/LOS-V3.txt` before coding.

If `AGENTS.md` or `CONTEXT.md` changes during a task, re-read the changed file before continuing.

## Feature Boundaries

The current major feature is the custom Oblique Loft Collider LOS system.

Vehicle movement:

- `CarController` driving uses a private 16-direction heading bucket state. The car root transform must not be rotated for steering; runtime code keeps root Z rotation at zero.
- Existing car visuals are still 4-directional through the current Animator blend tree. Do not add 16-way car sprites or Oblique Loft sprite/profile binding unless the user explicitly asks.
- Car movement velocity should come from the current heading bucket, not `transform.right`.

Existing prototype:

- `Assets/Scripts/Common/Target.cs` currently owns target picking and old LOS adjustment.
- Existing object targeting uses `EnclosureCollider`, `DepthCollider`, and `HitCollider` child colliders.
- Entity part lookup uses `EntityRefs`, a dynamic component index on the root entity object. Do not add new fixed universal reference fields for new systems; components should be discovered by type or exposed through focused owner APIs.
- `GunController` supplies gun position and shoot height.

New system:

- Runtime logic must live outside `UnityEditor`.
- Editor-only authoring code must stay under an `Editor` folder or editor assembly boundary.
- The visual sprite is not collision truth. Collision truth is generated 3D logic geometry.
- Oblique Loft LOS is now scoped to simple mostly-static blockers/direct static targets such as walls, buildings, and trees. Complex moving objects such as cars should be ignored by LOS unless the user explicitly revisits that decision.
- Actual shootable targets use `SimpleTarget`: a flat 2D current-frame hit polygon plus a ground/depth polygon. The selected character and any character that crosses the shot line are resolved by the same simple-target query.
- Oblique Loft collider shapes are authored directly on the object. Do not bind Oblique Loft collision profiles to animation frames or `SpriteRenderer.sprite`.
- `ObliqueLoftCollider` is drop-in: it requires and synchronizes a non-trigger `PolygonCollider2D` to the authored footprint so the ground footprint is solid and selectable. Do not hand-author a different physical footprint collider for the same loft object.
- Position, rotation, and scale are transform data applied geometrically to the already-authored volume. Rotation must rotate the footprint, slices, faces, and normals as a whole; it must not recalculate front/back edges or reinterpret slice depth after authoring.
- A shot is a 3D logic ray from shooter ground position plus shoot height to target ground position plus aimed target height.
- Final collision must use generated polygonal faces and ray/face intersection, not plain Unity 2D raycast overlap.
- Footprint points are independent from slice points. The footprint represents the ground/depth shape only.
- Current surface generation canonicalizes every slice from its bottom-left/bottom-right connector points, chooses connector-rooted lanes, then optimizes each neighboring slice-pair lane order. The optimizer keeps the connector lane anchored, treats projected crossing connections as hard failures, penalizes pinched or near-zero side quads, and favors broader volume-like quads before connecting faces through depth. Every generated face is then wound outward from the generated volume center before normal-based classification. The footprint boundary still does not construct or constrain side/depth surfaces by itself, so non-rectangular footprint issues can still be geometry-construction issues before they are raycaster issues.
- In the authoring UI, slice depth means the Y position on the footprint that a vertical cross-section belongs to. Avoid alternate labels for this concept.
- Slice point Y is the editable visual/local Y position in the 2D oblique scene. Runtime logic height is derived as `slicePoint.y - sliceDepth`, clamped to zero or above. Dotted slice connectors must stem from two distinct lowest slice points. If two or more points share the lowest Y, use the farthest-left and farthest-right points in that lowest-Y set; if only one point is uniquely lowest, use that point and the next-lowest point.
- Middle slice connector handles move both connector endpoints together along valid non-horizontal footprint edges and must stay ordered between neighboring slice depths.
- Front and back connectors are locked to the bottom-most and top-most footprint depths. The footprint must have a horizontal front edge and a horizontal back edge, each at least 1px long; pointy front/back footprint ends are invalid. When editing either endpoint of those mandatory edges, move its paired endpoint to the same Y so the edge remains horizontal.
- Slice point counts must stay synchronized across all front/back/middle slices. Adding or deleting a point on one slice must add or delete the same point index across all slices.
- Slice polygons should stay simple/non-self-intersecting during editor edits. If a dragged or nudged slice point causes self-intersection, the editor repairs only the selected slice by reversing the stored connection-order segment between crossing edges. This changes which dots connect to which other dots; it must not move point coordinates, swap point identities in `EditablePoints`, or reorder other slices.
- Each slice must have two distinct bottom connector points. They do not have to be neighboring polygon points; runtime canonicalization chooses the higher-profile path between them so generated faces do not drop to zero when a bottom edge has inserted/intermediate points.
- Oblique shot/ray debug for player aiming is controlled by the targetter's `Target.drawObliqueLoftDebug` switch. Target and obstacle objects should only need `ObliqueLoftCollider` to participate in new LOS tests.
- `Target.cs` first resolves `SimpleTarget` candidates along the intended ground shot line, sorted by distance. Oblique Loft colliders are tested as static blockers before each simple target candidate and before the final intended endpoint. Static `ObliqueLoftCollider` objects can be selected either through their synchronized footprint collider or through projected generated faces under the cursor. Direct targeting maps the cursor through the selected loft's projected generated faces, prefers overlapping projected faces that face the shooter, then extends that aim ray through the loft bounds and raycasts all valid loft colliders, including the selected loft. The closest generated face on that line becomes the hit, so other colliders or nearer faces of the same selected object can occlude farther faces.

Transition rule:

- Do not delete or blindly replace the existing `Target.cs` LOS path.
- Introduce new Oblique Loft behavior beside the old system first.
- Route gameplay through the new system only behind an explicit feature flag/fallback or after sample loft volumes are validated.

## Coordinate Rules

Oblique logic coordinates:

- `Vector3.x`: horizontal ground X.
- `Vector3.y`: vertical height.
- `Vector3.z`: ground/depth axis, mapped from 2D scene Y.

Scene visualization projects logic `(x, y, z)` to Unity scene `(x, z + y, object z)` so height is visible as upward movement in the 2D Scene view. Local ground X/depth are transformed through the object transform into logic X/Z so position, rotation, and scale affect the volume. Logic height uses transform Y scale. Unity object Z and transform Z scale are only drawing/sorting concerns and must not contribute to logical height or ground depth.

## Coding Rules

- Prefer existing project patterns over introducing new architecture.
- Keep files focused and avoid giant systems.
- Preserve existing gameplay behavior unless the user explicitly asks for replacement.
- Keep Unity `.meta` files with created, moved, or deleted assets.
- Avoid direct edits to generated `.csproj` and `.sln` files unless the task specifically requires it.
- Use serializable data structures for Unity-authored collider data.
- Use deterministic geometry and explicit validation over physics-engine magic.
- Do not use `UnityEditor` APIs from runtime scripts.
- Do not add complex remeshing or triangulation for Oblique Loft v1 unless the user asks for it.
- Oblique Loft editor UX should preserve direct manipulation: click/select points, drag points, click edges to insert points, right-click points for actions, and keyboard delete/nudge for selected points.
- Editable footprint/slice points should support immediate click-drag movement without requiring a prior selection click. Mouse dragging should snap at sub-pixel proximity to nearby points and horizontal/vertical alignments in the same point set; arrow-key nudging should remain exact and unsnapped.
- Oblique Loft authoring overlays should always show both the footprint and all slices. The active edit mode should only decide which points and edge handles are editable.
- Generated face shading is a separate opt-in debug view from the authoring overlays; keep it off by default for clean editing. It should draw translucent normal-colored face fills rather than extra face wireframe lines.

## Tooling Rules

- Use `.\tools\unity.ps1` for Unity command-line work in this repository.
- Unity editor path currently detected: `D:\Program Files\Unity\2021.3.33f1\Editor\Unity.exe`.
- The wrapper supports `version`, `open`, `refresh`, `test-editmode`, and `test-playmode`.
- `.NET SDK 8.0.421` is installed at `C:\Program Files\dotnet\dotnet.exe`.
- If a shell cannot find `dotnet`, refresh PATH from machine/user environment variables or open a new terminal.
- `dotnet build Assembly-CSharp.csproj` is the current fast runtime compile check. Use the full SDK path `C:\Program Files\dotnet\dotnet.exe` if the shell cannot find `dotnet`.

## Testing Rules

- For C# runtime compile checks, run `dotnet build Assembly-CSharp.csproj`.
- `dotnet build MafiaGame.sln` and `dotnet build Assembly-CSharp-Editor.csproj` may fail in the current shell before C# diagnostics because generated editor project restore output and build input paths disagree.
- For Unity import/editor validation, use `.\tools\unity.ps1 -Command refresh` only when no existing Unity Editor process is holding the project open or when the user confirms it is safe.
- For play/edit tests, use `.\tools\unity.ps1 -Command test-editmode` or `.\tools\unity.ps1 -Command test-playmode` when tests exist.
- Treat editor UX and Scene view authoring as not verified until tested inside Unity Editor.
- Existing warnings in older scripts are not automatically blockers, but do not introduce new errors.

## Mandatory Update Protocol

After every meaningful change:

- Update `CONTEXT.md` with what changed, why it changed, current status, verification, known issues, and next steps.
- Update `AGENTS.md` if architecture boundaries, workflow rules, dependencies, tools, or feature guidance changed.
- Update the relevant feature document under `Docs/` when a feature behavior, setup step, authoring workflow, runtime API, or user-facing usage changes. This is feature documentation, not progress tracking.
- Keep the `Status Ledger`, `Active Gaps`, `Known Issues`, `Next Recommended Steps`, and `Recent Changes` in `CONTEXT.md` accurate.

A meaningful change includes a new feature, changed workflow, changed dependency/tooling, changed editor/runtime boundary, changed scene/prefab migration status, verification result, or completion-status change.

## Git Notes

Git may report this repository as having dubious ownership for the current shell user. Do not modify global Git configuration unless the user asks for it.

## Do Not

- Do not call the entire Oblique Loft feature complete until gameplay integration, sample object migration, editor workflow validation, and old-vs-new debug comparison are done.
- Do not simplify the Oblique Loft system into normal Unity 2D colliders.
- Do not route existing gameplay through new LOS code without fallback or explicit user approval.
- Do not force-close Unity Editor processes unless the user confirms it is safe.
- Do not edit generated/cache Unity folders for normal feature work.
