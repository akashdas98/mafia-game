# MafiaGame Agent Guide

## Architecture Overview

This repository is a Unity `6000.3.16f1` 2D oblique/top-down game project.

Top-level layout:

- `Assets/`: Unity runtime/editor assets, scenes, prefabs, scripts, sprites, tilemaps, and samples.
- `Assets/Scripts/Common/`: shared gameplay infrastructure such as input, inventory, target selection, and existing LOS logic.
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

- `VehicleMotor` owns car driving state, including the private 16-direction heading bucket. The car root transform must not be rotated for steering; runtime code keeps root Z rotation at zero.
- Existing car visuals are still 4-directional through the current Animator blend tree. Do not add 16-way car sprites or Oblique Loft sprite/profile binding unless the user explicitly asks.
- Car movement velocity should come from `VehicleMotor`'s current heading bucket, not `transform.right`.

Existing prototype:

- `Assets/Scripts/Common/Target.cs` is now the shooter-side targetter/marker coordinator. Direct click selection lives in `TargetSelectionResolver`; SimpleTarget resolution lives in `SimpleTargetingStrategy`; Oblique Loft targeting/blocking lives in `ObliqueTargetingStrategy`; old depth/hit fallback lives in `LegacyDepthTargetingStrategy`.
- Existing object targeting uses `EnclosureCollider`, `DepthCollider`, and `HitCollider` child colliders.
- Entity wiring should be explicit. Prefer serialized references for stable owner relationships and narrow owner APIs for cross-component communication. Do not reintroduce fixed universal refs, dynamic entity indexes, or broad service-locator lookup for gameplay wiring.
- Player input handlers use typed input state objects (`CharacterInputState`, `CarInputState`) rather than string-keyed input dictionaries. Do not add new gameplay reads from `Dictionary<string, float>` input keys. `CharacterInputHandler` is now the compatibility active input handler for player characters and forwards `CharacterInputState` into `PlayerInputRouter`, which routes to local capability interfaces. `CarInputHandler` now forwards `CarInputState` into `VehicleInputRouter`, which routes to `IVehicleInputReceiver`.
- The old plain C# controller helper layer has been removed. Do not add new `ControllerHelper`, `CharacterControllerHelper`, `GunController`, `ItemsController`, or `InputHandlerHelper` subclasses. New behavior should live on focused `MonoBehaviour` capability components and route input through typed input state plus capability routers.
- Character movement, animation, and nearby interaction tracking are extracted from broad controller ownership: `CharacterMotor` owns movement vector, speed, Rigidbody2D velocity, kinematic switching, and last-facing state; `CharacterAnimationController` ticks itself, invokes ordered animation adapters, and writes through `AnimationParameterWriter` to `AnimatorParameterRelay`; the relay broadcasts shared parameters to independent visible body/clothing child animators; `CharacterMovementAnimationAdapter` reads `CharacterMotor`; `CharacterAimAnimationAdapter` reads `WeaponUser` when enabled; `CharacterInteractor` owns nearby `Interactable` tracking and interaction execution. The old `CharacterController` and `CarController` shells have been removed from the active runtime path; do not reintroduce broad controller shells for new behavior.
- For new gameplay-driven animation expansion, follow `Docs/AnimationAdapterArchitecture.md`: gameplay components expose gameplay state only, component-specific animation adapters translate that state into animation parameters, and entity-specific animation controllers own the final writes to their entity-specific animator target such as the current `AnimatorParameterRelay` broadcast path for layered characters. Do not make gameplay components know animator parameter names.
- Layered character animation shares parameters, not forced state/time. Do not add a master Animator on the `Sprites` object, do not reintroduce `CharacterMasterAnimator`, `LayeredAnimatorSync`, or `MasterAnimationPreviewProbe`, and do not force all visual layer animators to mirror one common state/time. Each child layer Animator should decide independently how to respond to the shared parameter stream.
- Character Builder animation slots and override controllers are layer-scoped. The shared `Assets/Animations/Character/Base/LayerAnimatorTemplate.controller` is only a seed/fallback; generated layer templates live under `Assets/Animations/Character/Base/Templates/`, generated slot placeholders live under `Assets/Animations/Character/Base/Slots/<part-group>/`, and generated part override controllers should use the template for their own `CharacterPartGroup`.
- Weapon and inventory use are component-owned: `WeaponUser` owns equipped gun tracking, aim origin/visual gun point handling, trigger forwarding, and shoot-height/distance tuning; `InventoryUser` owns pickup/drop/cycle commands. `Inventory` owns inventory state and item container references and lives on the reusable `Inventory` child/prefab, not on the character root. `PlayerInputRouter` has explicit references to character capability components, and `Item` pickup resolves the receiver from the interacting character hierarchy. `Assets/Prefabs/Character/Character.prefab` is wired with `PlayerInputRouter`, and `Assets/Prefabs/Vehicle/Car V2.prefab` is wired with `VehicleInputRouter`. The old gun/item/input helper adapters have been removed after Unity validation.
- Inventory equip slots are capability-based: `Inventory` and `InventoryUser` classify equip-capable items through `IEquippable`, with `Weapon` as the current adapter. Do not add new inventory checks that depend on `item is Weapon`; weapon-specific behavior belongs in `WeaponUser` or weapon/fire-mode components.
- Gun stats and trigger behavior are component/config driven: `GunStats` stores current serialized gun tuning, and `Gun` delegates trigger behavior through `GunFireMode` components such as `SemiAutoFireMode` and `FullAutoFireMode`. Keep old weapon subclasses as compatibility adapters unless prefab migration explicitly removes them.
- `WeaponUser` supplies the logical aim origin and shoot height to `Target`. `Target` can reference an authored stable `Aim Origin` transform for the real shot line and an animation-frame-specific `Gun Point` transform for visual weapon placement; do not derive logic rays from the visual gun point. `Assets/Prefabs/AimTarget.prefab` includes `Marker`, `AimOrigin`, and `GunPoint` children; the `Target` root remains anchored to the character and only the marker sprite moves to the resolved target point.

New system:

- Runtime logic must live outside `UnityEditor`.
- Editor-only authoring code must stay under an `Editor` folder or editor assembly boundary.
- The visual sprite is not collision truth. Collision truth is generated 3D logic geometry.
- Oblique Loft LOS is now scoped to simple mostly-static blockers/direct static targets such as walls, buildings, and trees. Complex moving objects such as cars should be ignored by LOS unless the user explicitly revisits that decision.
- Actual shootable targets use `SimpleTarget`: a flat 2D current-frame hit polygon plus an authored horizontal ground reference line height. They do not require a separate ground/depth polygon. The selected character and any character whose hit face crosses the shot line are resolved by the same simple-target query.
- SimpleTarget sprite auto-detect is editor/profile-assisted, not a broad runtime remeshing system. The parent `SimpleTarget` owns the combined `Hit Collider`; child `SimpleTargetLayer` components under `Sprites` store per-sprite alpha-outline profiles and expose layer-local auto-detect buttons. The parent and child buttons trace exact opaque alpha-pixel outlines from active child `SpriteRenderer` sprites, apply matching layer profiles into the normal `Hit Collider`, and then users can manually edit the collider. It must not require separate per-sprite shape authoring.
- Oblique Loft collider shapes are authored directly on the object. Do not bind Oblique Loft collision profiles to animation frames or `SpriteRenderer.sprite`.
- Oblique Loft sprite/frame profile authoring is optional and editor-assisted. Prefer placing one `ObliqueLoftCollider` component directly on the same GameObject as its single target `SpriteRenderer` so the normal collider inspector stays visible while Unity's Animation window previews/scrubs that sprite object. When `Use Sprite Frame Profiles` is enabled, scrubbing to a new sprite saves the previous sprite's current shape, loads an existing shape for the new sprite when present, or creates a new profile by copying the last live shape when missing; normal Scene/inspector edits are saved to the current sprite profile automatically. The `Target Sprite Renderer` field remains for legacy/separate-object setups. Do not auto-generate Oblique Loft volumes from sprite outlines. If layered Oblique Loft authoring is ever needed, use multiple separate `ObliqueLoftCollider` components, one per layer object.
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
- `Target.cs` first resolves `SimpleTarget` candidates along the intended visual shot line, sorted by derived ground distance. Oblique Loft colliders are tested as static blockers before each simple target candidate and before the final intended endpoint. Static `ObliqueLoftCollider` objects can be selected either through their synchronized footprint collider or through projected generated faces under the cursor. Direct targeting maps the cursor through the selected loft's projected generated faces, prefers overlapping projected faces that face the shooter, then extends that aim ray through the loft bounds and raycasts all valid loft colliders, including the selected loft. The closest generated face on that line becomes the hit, so other colliders or nearer faces of the same selected object can occlude farther faces.

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
- Unity project version currently detected from `ProjectSettings/ProjectVersion.txt`: `6000.3.16f1`.
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
- Keep `CONTEXT.md` compact. It is a current working ledger, not a full historical changelog.
- Keep `Recent Changes` to the latest high-signal entries only, generally around 5-10 items, and consolidate or remove older entries once their facts are represented in the status ledger, known issues, next steps, durable docs, or Git history.
- Update `AGENTS.md` if architecture boundaries, workflow rules, dependencies, tools, or feature guidance changed.
- Update the relevant feature document under `Docs/` when a feature behavior, setup step, authoring workflow, runtime API, or user-facing usage changes. This is feature documentation, not progress tracking.
- Keep the `Status Ledger`, `Active Gaps`, `Known Issues`, `Next Recommended Steps`, and `Recent Changes` in `CONTEXT.md` accurate.

A meaningful change includes a new feature, changed workflow, changed dependency/tooling, changed editor/runtime boundary, changed scene/prefab migration status, verification result, or completion-status change.

## Git Notes

Git safe-directory has been configured for this exact repository path for the current shell user:

`D:/Projects/Unity/Mafia Game/MafiaGame`

Do not use `takeown`, `icacls`, `chown`, or other ownership/ACL changes to fix Git access unless the user explicitly asks. The safe-directory entry is the intended non-ownership-changing fix.

## Do Not

- Do not call the entire Oblique Loft feature complete until gameplay integration, sample object migration, editor workflow validation, and old-vs-new debug comparison are done.
- Do not simplify the Oblique Loft system into normal Unity 2D colliders.
- Do not route existing gameplay through new LOS code without fallback or explicit user approval.
- Do not force-close Unity Editor processes unless the user confirms it is safe.
- Do not edit generated/cache Unity folders for normal feature work.
