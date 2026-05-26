# MafiaGame Context

## Current Status

This repository is a Unity `2021.3.33f1` 2D oblique/top-down game project at:

`D:\Projects\Unity\Mafia Game\MafiaGame`

Current major work item: custom 2.5D / oblique-world LOS and bullet collision system from `KickoffPrompts/LOS-V3.txt`.

Current estimated completion:

- Unity CLI/tooling setup: complete enough for current work.
- Oblique Loft Collider foundation: partially complete.
- Full LOS-V3 feature: not complete.
- Gameplay integration of Oblique Loft LOS: started as an opt-in static-blocker bridge plus unified `SimpleTarget` resolver, not Unity-validated.
- Unity Editor authoring workflow validation: not verified.

Current implementation state: an Oblique Loft v1 foundation exists beside the old LOS/depth-collider prototype. `Target.cs` owns the unified `SimpleTarget` candidate resolver, opt-in static Oblique Loft blocker checks, and its debug drawing switch. The old gameplay path remains the fallback when the selected object has no `SimpleTarget`.

## Operating Rule

Before continuing LOS or collision work, check the status ledger below. Areas marked `Complete` should be maintained rather than reworked. Areas marked `Started` or `Open` are the next implementation targets.

Do not claim the Oblique Loft feature is done until gameplay routing, sample collider authoring, editor workflow testing, debug comparison, and migration validation are complete.

## Status Ledger

| Area | Status | Do Next | Do Not Rework Unless |
| --- | --- | --- | --- |
| Root memory files | Complete, updated to AODS-style ledger | Keep status accurate after each change. | User requests a different format. |
| Unity CLI wrapper | Complete, needs optional re-test after Unity closes | Use `.\tools\unity.ps1`; re-test `version`/`refresh` when safe. | Unity install path changes. |
| .NET SDK availability | Complete | Use `dotnet build Assembly-CSharp.csproj` for the current fast runtime compile check. | SDK breaks or PATH is unavailable in a new shell. |
| Existing LOS prototype inspection | Complete | Preserve old path while integrating new one. | Existing gameplay behavior changes or deeper migration begins. |
| Oblique Loft runtime data structures | Started, compile-clean, footprint collider sync added | Validate synced non-trigger footprint collider in Unity prefab/scene editing. | Validation/editor testing reveals design issues. |
| Surface generation from slices | Improved, compile-clean, connector-normalized dynamic volume-lane optimization added | Test generated faces in Unity; next improvement is footprint-boundary constraints for non-rectangular silhouettes. | Geometry edge cases fail. |
| Surface classification by normals | Improved, compile-clean, center-outward face winding fixed | Verify normals and labels visually in Scene view. | Classification is wrong for sample volumes. |
| Ray-face intersection | Started, compile-clean | Test with sample shot rays and authored volumes. | False hits/misses appear. |
| Closest-hit LOS query | Started, compile-clean | Integrate through feature flag/fallback after sample validation. | API shape blocks `Target.cs` integration. |
| Scene/debug gizmos | Improved, compile-clean, generated face overlay uses translucent normal-colored fills | Validate in Unity Scene view. | Gizmos are unreadable or mapped incorrectly. |
| Editor drawing workflow | Improved direct-manipulation v1, needs Editor verification | Verify point selection/drag/nudge/edge insert/delete in Unity; refine any Scene view issues found. | User asks for a different authoring model. |
| Oblique Loft sprite-frame binding | Removed by user direction | Do not reintroduce collider profile binding to animation frames or `SpriteRenderer.sprite` unless explicitly requested. | User explicitly asks to restore it. |
| Entity refs migration | Started, compile-clean, needs Unity import/prefab validation | Open migrated character/car prefabs in Unity and verify `EntityRefs.Parts` auto-populates in prefab mode and play mode. | Lookup behavior fails after Unity import. |
| Simple target resolver | Started, compile-clean, drop-in prefab added | Add `Assets/Prefabs/SimpleTarget.prefab` to representative characters and test selected/intervening target resolution in Unity. | User chooses a different character hit model. |
| Compatibility with existing LOS system | Started, simple target plus opt-in static blocker/direct-target bridge added | Test `Target.cs` with `useSimpleTargeting`, `useObliqueLoftLos`, and `drawObliqueLoftDebug` enabled on sample targets/blockers. | User decides to keep systems fully separate. |
| Sample object/prefab migration | Started, drop-in prefab added and Car V2 loft moved to child, needs Unity validation | Open `Assets/Prefabs/ObliqueLoftCollider.prefab` and `Assets/Prefabs/Vehicle/Car V2.prefab`, verify import, footprint collider sync, and target selection. | User asks to defer prefab changes. |
| Vehicle 16-direction movement | Complete, compile-clean, needs Play Mode verification | In Play Mode, verify root rotation stays zero, heading changes in 22.5-degree steps, high-speed steering resistance, reverse steering, braking, and existing 4-way animation blending. | User asks for continuous rotation or new directional sprites. |
| Old-vs-new debug comparison | Started, targetter-owned Oblique debug exists | Validate targetter-owned ray/hit labels in Unity and add fuller old-vs-new comparison if still needed. | User no longer needs comparison tooling. |
| Composition migration planning | Complete, documentation-only | Use `Docs/CompositionMigrationPlan.md` when migrating inheritance-heavy controller/input/helper, weapon/item, targeting, vehicle, or `Base`/`EntityRefs` architecture. | Implementation priorities change. |

## Completed for Current Foundation

- Root `AGENTS.md` and `CONTEXT.md` exist and must be read before work.
- `tools/unity.ps1` exists as a project-local Unity CLI wrapper.
- Unity project version confirmed as `2021.3.33f1`.
- Matching Unity Editor found at `D:\Program Files\Unity\2021.3.33f1\Editor\Unity.exe`.
- `.NET SDK 8.0.421` installed through winget and available at `C:\Program Files\dotnet\dotnet.exe`.
- `dotnet build Assembly-CSharp.csproj` succeeds with 0 errors.
- Existing LOS prototype was inspected:
  - `Assets/Scripts/Common/Target.cs` owns current target selection and depth/hit collider LOS adjustment.
  - Legacy `Assets/Scripts/Common/Refs.cs` referenced `HitCollider`, `DepthCollider`, and `AimTarget`; it has since been replaced by dynamic `EntityRefs`.
  - `Assets/Scripts/Objects/Character/Helpers/ControllerHelpers/GunController.cs` supplies gun position and gun height.
  - `Assets/Scripts/Objects/Weapon/Gun/Gun.cs` is the gun base class.
- New Oblique Loft files were added under `Assets/Scripts/ObliqueLoft/`.
- Runtime additions include:
  - `ObliqueSurfaceType`
  - `ObliqueLoftSlice`
  - `ObliqueLoftFace`
  - `ObliqueRay`
  - `ObliqueRayHit`
  - `ObliqueLoftBuilder`
  - `ObliqueRaycaster`
  - `ObliqueLoftCollider`
  - `ObliqueLoftLos`
  - `SimpleTarget`
- Editor addition:
  - `Assets/Scripts/ObliqueLoft/Editor/ObliqueLoftColliderEditor.cs`
- Migration notes added:
  - `Assets/Scripts/ObliqueLoft/README.md`
- `Target.cs` now has a `useSimpleTargeting` resolver that uses one distance-sorted candidate path for the selected simple target and any simple target crossing the ground shot line. `useObliqueLoftLos` is now the static blocker test before each simple target or intended endpoint, and selected static `ObliqueLoftCollider` objects can be direct targets. Direct loft targeting can select projected generated faces outside the footprint collider, maps the cursor through the selected loft's projected generated faces before falling back to footprint inference, prefers overlapping projected faces that face the shooter, then extends the selected aim ray through the selected loft's logic bounds and accepts the closest generated face hit, including nearer faces on the same selected object. The old depth/hit collider path remains the fallback when no selected `SimpleTarget` or direct loft target exists.
- Oblique blocked target feedback uses a brighter green sprite tint, and hit-face debug drawing resolves generated faces by stored face id instead of assuming face id equals current list position.
- Surface generation now canonicalizes every slice from two distinct lowest connector points before lofting. If multiple points share the lowest Y, it uses the farthest-left/right points in that set; if only one point is uniquely lowest, it uses that point plus the next-lowest point. The left connector becomes lane/index `0`; raw dot indices are not treated as faithful cross-slice pairs. The builder keeps the connector lane anchored, dynamically optimizes neighboring slice-pair lane order to avoid projected crossing connections, penalizes pinched or near-zero side quads, favors broader volume-like side quads, then connects matching canonical lanes through depth.
- Generated faces are now wound outward from the generated volume center before normal classification. This fixes top/bottom/front/back labels that could be inverted by slice winding or inward face winding.
- Runtime and editor projection now agree on transform scale: local X uses transform X scale, while both logic height and logic depth use transform Y scale. Unity object Z and transform Z scale do not affect logical height/depth.
- Runtime transform handling treats position, Z rotation, and scale as geometric transforms of the already-authored volume. Local ground X/depth now convert through the object's 2D local X/Y basis rather than Unity's full 3D inverse matrix, so 2D prefabs with zero Z scale can still rotate the Oblique Loft volume correctly.
- `ObliqueLoftColliderEditor.cs` now supports selected `Slice Depth` editing, footprint and selected-slice polygon position sliders, selected-slice self-intersection repair on point drag/nudge, draggable middle-slice connector handles, adding/removing middle slices, sorting slices into front/middle/back order, and adding slice points on the selected slice footprint-depth position.
- Oblique Loft collider shapes are authored directly on static blocker/direct-target objects. The sprite-frame / `SpriteRenderer.sprite` collider profile binding modification was removed by user direction.
- `ObliqueLoftCollider` now requires/creates a `PolygonCollider2D` and synchronizes it to the authored footprint as a non-trigger solid/selectable ground collider during rebuilds.
- `TargetEditor.cs` adds selected-targetter Scene view labels for the current Oblique Loft closest hit surface type, face id, and hit object.
- Corrected authoring model: footprint point count is independent from slice point count; all slices must keep equal point counts; slice point insert/delete is synchronized across all slices by point index. Footprint front/back ends must have horizontal edges and pointy ends are invalid. Dragging or nudging an endpoint of a mandatory front/back footprint edge moves its paired endpoint to the same Y.
- `ObliqueLoftColliderEditor.cs` now supports color-coded footprint/slice outlines, point selection, immediate click-drag point movement, drag-only sub-pixel snapping to nearby points and horizontal/vertical alignments, arrow-key nudging without snapping, shift-arrow larger nudging, edge-click insertion, right-click delete menu, and Delete/Backspace point deletion. Footprint and all slice outlines stay visible in every edit mode; only the active mode's points/edge handles are editable.
- Default `ObliqueLoftCollider.ResetToBox` now creates a more visible 2D authoring shape: front slice depth is the lower footprint edge, back slice depth is the upper footprint edge, each editable slice polygon is stored at its own visual/local Y position, each slice bottom edge sits on its footprint depth, and slice height defaults to half the footprint depth. Runtime logic height is derived from `slicePoint.y - sliceDepth`; Unity object Z is only the gizmo drawing plane.
- Feature documentation for Oblique Loft Collider now exists at `Docs/ObliqueLoftCollider.md`, with getting-started steps near the top.
- Architecture recommendations by feature area now exist at `Docs/ArchitectureRecommendations.md`.
- A staged composition-over-inheritance migration plan now exists at `Docs/CompositionMigrationPlan.md`.
- `Refs` has been replaced by `EntityRefs`, a dynamic root-level entity part index. Character and car prefabs now point to `EntityRefs`; `Base`, controllers, input handlers, gun/item helpers, car possession, and `SimpleTarget` use dynamic part lookup or focused owner state instead of fixed universal fields.

## Active Gaps

1. Gameplay integration validation:
   `Target.cs` has a compile-clean unified `SimpleTarget` resolver and opt-in Oblique Loft static blocker/direct-target checks, but it has not been tested in Unity against real character prefabs and authored loft volumes. Default behavior remains old depth/hit collider logic when the selected object has no `SimpleTarget` or direct `ObliqueLoftCollider`.

2. Editor workflow completeness:
   The current custom editor has reset/rebuild, validation display, color-coded outlines, point selection/dragging, selected `Slice Depth` editing, footprint and selected-slice polygon position sliders, selected-slice connection-order self-intersection repair on point drag/nudge, draggable ordered middle connector handles, faded connector lines from the slice's two distinct lowest connector points back to the footprint at that depth, middle slice add/remove, edge-click insertion, right-click/delete-key deletion, and arrow-key nudging. Slice repair now draws edges by stored connection order while drawing handles/labels by raw point index, so repaired lines can change without visually moving or relabeling dots. It still needs Unity Editor validation and any polish needed after real Scene view testing.

3. Unity Scene view validation:
   Gizmos, face labels, normals, and point editing have not been manually verified inside the Unity Editor.

4. Sample migration:
   `Assets/Prefabs/ObliqueLoftCollider.prefab` now exists as a default drop-in loft volume, and `Car V2` has its existing authored loft config moved from the root object to an `ObliqueLoftCollider` child object. No existing scene object has been migrated and the prefab changes have not been Unity-import validated.

5. Debug comparison:
   There is no complete old-vs-new comparison visualizer yet. `Target.cs` draws the old debug lines and now also owns the Oblique Loft ray/hit gizmos through `drawObliqueLoftDebug`; `TargetEditor` labels selected new-system hits.

6. Geometry robustness:
   The v1 ray/face intersection and triangle tests build and are deterministic, but they still need testing on real authored volumes, sloped faces, edge hits, and concave or invalid polygons. The builder now performs connector-normalized dynamic volume-lane optimization, so raw slice point order and winding are less likely to create wrong faces, crossing volume debug lines, or pinched side quads. The footprint still mostly validates authoring depth and front/back flatness; it does not yet use the footprint boundary to constrain or generate the intended side/depth silhouette for non-rectangular objects.

7. Simple target workflow:
   `SimpleTarget` can reuse tagged `DepthCollider` and `HitCollider` child polygons, tagged polygons discovered through `EntityRefs`, explicitly assigned polygons, or the new `Assets/Prefabs/SimpleTarget.prefab` with adjustable `GroundCollider` and `HitCollider` child polygons. No character prefab has been Unity play-tested yet. There is no dedicated sprite-frame binding component for character hit polygons yet.

8. Vehicle movement validation:
   `CarController` now compiles with 16-direction bucket steering and zero root rotation, but the driving feel, reverse steering, and existing 4-way Animator blend tree still need Unity Play Mode verification.

## Mandatory Update Protocol

After every meaningful change, update this file. A meaningful change includes a new feature, changed workflow, changed dependency/tooling, changed editor/runtime boundary, changed scene/prefab migration status, verification result, or completion-status change.

When a work area becomes complete, update both `Status Ledger` and `Active Gaps` so future turns do not keep reworking the same area. If an item is complete except Unity Editor validation, mark it `Complete, Needs Editor Verification` and make editor verification the only next action for that area.

Also update `AGENTS.md` if workflow rules, dependencies, project boundaries, or architecture guidance changed.

Feature docs under `Docs/` must be updated when feature behavior, setup steps, authoring workflow, runtime API, or user-facing usage changes. Do not use feature docs for ordinary progress tracking; keep progress in this file.

## Detailed Implemented Inventory

- `tools/unity.ps1`: project-local Unity CLI wrapper. Detects the editor version from `ProjectSettings\ProjectVersion.txt`, defaults to the local Unity `2021.3.33f1` install when present, supports `UNITY_EDITOR` and `-UnityPath`, and exposes `version`, `open`, `refresh`, `test-editmode`, and `test-playmode`.
- `Assets/Scripts/ObliqueLoft/ObliqueSurfaceType.cs`: surface type enum for generated faces.
- `Assets/Scripts/ObliqueLoft/ObliqueLoftSlice.cs`: serializable vertical slice data with depth and editable 2D profile points.
- `Assets/Scripts/ObliqueLoft/ObliqueLoftFace.cs`: generated face data with vertices, normal, surface type, and face index.
- `Assets/Scripts/ObliqueLoft/ObliqueRay.cs`: logic ray from shooter to target with helper for ground position plus height.
- `Assets/Scripts/ObliqueLoft/ObliqueRayHit.cs`: closest-hit result containing collider, object, point, distance, surface type, normal, and face index.
- `Assets/Scripts/ObliqueLoft/ObliqueLoftBuilder.cs`: validates footprint/slices, canonicalizes each slice from two distinct lowest connector points by choosing the higher-profile path between them, generates cap faces and connecting quads between neighboring canonical slice lanes, and classifies surfaces by dominant normal direction.
- `Assets/Scripts/ObliqueLoft/ObliqueRaycaster.cs`: ray-plane intersection, projected point-in-face containment, broad candidate filtering through projected bounds, and closest-hit selection.
- `Assets/Scripts/ObliqueLoft/ObliqueLoftCollider.cs`: Unity component storing footprint, slices, generated faces, validation errors, rebuild/reset helpers, coordinate conversion, projected bounds, synchronized non-trigger footprint `PolygonCollider2D`, and gizmo rendering. Runtime coordinate conversion applies object position/Z rotation/scale through the object's 2D local X/Y basis to local ground X/depth and maps height through transform Y scale.
- `Assets/Scripts/ObliqueLoft/ObliqueLoftLos.cs`: compatibility-oriented static APIs for `HasLineOfSight`, `CanHitTargetHeight`, and `TryGetClosestHit`.
- `Assets/Scripts/ObliqueLoft/Editor/ObliqueLoftColliderEditor.cs`: basic custom inspector and scene point editing for loft colliders.
- `Assets/Scripts/Common/Editor/TargetEditor.cs`: selected targetter label showing the latest Oblique Loft hit surface type, face id, and hit object in Scene view.
- `Assets/Scripts/ObliqueLoft/README.md`: explains current prototype relationship, runtime APIs, authoring v1, and migration notes.
- `Assets/Scripts/Common/EntityRefs.cs`: dynamic entity part lookup component. It auto-rebuilds a serialized parts list from child components, indexes concrete types, base component types, and interfaces, and replaces the old fixed `Refs` fields.
- `Assets/Scripts/Common/SimpleTarget.cs`: shootable flat target component with a ground/depth polygon and current-frame hit polygon. It can auto-resolve child `DepthCollider` / `HitCollider` tagged polygons or tagged polygon parts discovered through `EntityRefs`.
- `Assets/Prefabs/SimpleTarget.prefab`: default drop-in SimpleTarget root with adjustable `GroundCollider` and `HitCollider` child `PolygonCollider2D` shapes already assigned to the component fields.
- `Assets/Scripts/Common/Target.cs`: unified simple-target resolver through serialized `useSimpleTargeting`, opt-in static Oblique Loft blocker checks through `useObliqueLoftLos`, targetter-owned debug drawing through `drawObliqueLoftDebug`, and old LOS path fallback when no selected `SimpleTarget` exists.
- `Assets/Prefabs/ObliqueLoftCollider.prefab`: default drop-in Oblique Loft box volume with `ObliqueLoftCollider` and a non-trigger `PolygonCollider2D` footprint.
- `Assets/Prefabs/Vehicle/Car V2.prefab`: existing authored Oblique Loft footprint/slice config moved off the root and onto an `ObliqueLoftCollider` child object with a synced non-trigger footprint `PolygonCollider2D`.
- `Assets/Scripts/Objects/Car/CarController.cs`: car driving keeps the root transform rotation at zero and uses a private 16-direction heading bucket for velocity and Animator heading parameters. Steering speed still uses the existing `rotationSpeed`, `speedFactorThreshold`, and `speedFactorMultiplier` tuning.
- `Assembly-CSharp.csproj`: manually updated to include new Oblique Loft runtime files for `dotnet build` until Unity regenerates project files.
- Footprint and slice point counts are intentionally independent. Only slice point counts are synchronized across slices.
- `Docs/ObliqueLoftCollider.md`: durable feature documentation, current getting-started steps, authoring model, runtime API, integration notes, limitations, and file map.
- `Docs/SimpleTargeting.md`: durable feature documentation for the unified selected/intervening character targeting flow, `SimpleTarget` setup, static blocker interaction, and limitations.
- `Docs/EntityRefs.md`: durable documentation for the dynamic entity part lookup workflow, prefab behavior, runtime API, and migration notes from fixed `Refs`.
- `Docs/ArchitectureRecommendations.md`: significant architecture recommendations by feature area.
- `Docs/CompositionMigrationPlan.md`: staged migration plan from inheritance-heavy controllers, input handlers, helpers, item/weapon hierarchy, targeting, vehicle possession, and broad `Base` usage toward capability composition.

## Pending

- Re-test `.\tools\unity.ps1 -Command version` and `.\tools\unity.ps1 -Command refresh` after the existing Unity Editor process exits or the user confirms it is safe.
- Open Unity and verify the custom inspector compiles/imports cleanly in the Editor.
- Create a sample `ObliqueLoftCollider` in a scene using `Reset Box`.
- Add `Assets/Prefabs/SimpleTarget.prefab` to at least one character prefab, adjust the child polygons, and verify selected/intervening target resolution.
- Play-test car driving and verify the root transform rotation remains zero while movement changes in 22.5-degree heading steps using the existing 4-way sprites.
- Verify point selection, dragging, arrow-key nudging, edge insertion, point deletion, generated faces, normals, labels, and shot debug gizmos visually.
- Test feature-flagged integration from `Target.cs` to `ObliqueLoftLos`.
- Validate targetter-owned Oblique Loft debug labels and add fuller old-vs-new comparison for a shot from gun to target if needed.
- Migrate at least one representative object/prefab to an authored loft collider for validation.

## Assumptions

- The user wants the new LOS system to be introduced beside the existing prototype before replacing gameplay calls.
- Existing `DepthCollider` and `HitCollider` child objects should remain during migration.
- `EntityRefs` should stay a dynamic entity-local part index, not a fixed universal reference type with feature-specific fields.
- Cars and complex moving objects are intentionally ignored by the Oblique Loft LOS path for now.
- Car driving uses fixed 16-direction movement buckets with the car root transform rotation kept at zero; the existing 4-way car Animator remains a temporary visual approximation.
- Characters should migrate to `SimpleTarget` flat hit polygons rather than Oblique Loft volumes.
- Logic depth maps from 2D scene Y into logic Z.
- For v1, all slices must have the same vertex count and consistent winding.
- The footprint front/back must each have a horizontal edge, even if tiny. Pointy front/back footprint ends are invalid.
- The old LOS behavior should remain the fallback until the new system is validated.

## Known Issues

- Git reports dubious ownership for this repository under the current shell user. Do not change global Git config unless the user asks.
- A Unity Editor process was observed running from earlier CLI testing. Do not force-close it without user confirmation.
- `dotnet build Assembly-CSharp.csproj` succeeds. A full rebuild may still surface existing warnings from older scripts:
  - `SceneDetails.name` hides `Object.name`.
  - `Highlighter.renderer` hides `Component.renderer`.
  - nullable annotations are used outside `#nullable` context in existing scripts.
  - `Target.enabled` hides `Behaviour.enabled`.
- The Oblique Loft editor workflow is improved and closer to the requested direct-manipulation model, but it has not been manually verified in Unity Editor.
- The new `SimpleTarget` resolver has only been validated by C# compilation so far; selected/intervening character behavior still needs Unity play-mode testing.
- The `EntityRefs` migration has been validated by C# compilation and prefab YAML updates, but Unity import/prefab-mode auto-population still needs manual verification.
- The 16-direction car movement change has been validated by runtime C# compilation only; Play Mode driving behavior and Animator blend output still need manual verification.
- The interrupted generated rotated-car-sprite attempt has been removed. Car animation clips now reference the original `Car_2_complete_32x32_1.png` sheet again and have no transform rotation curves. The directional car sprite regions inside that sheet have been physically rotated for the current no-root-rotation car movement: right unchanged, up 90 degrees counter-clockwise, left 180 degrees, and down 270 degrees counter-clockwise. Unity Editor import/Animator visual validation is still needed.
- Generated faces now use connector-normalized dynamic slice lanes, try to remove projected lane crossings and pinched side quads, and wind faces outward from the generated volume center before normal classification, but the footprint is still not an active source for side-surface construction. Non-rectangular footprint silhouettes may still need a footprint-constrained builder pass.
- Rotation is now applied geometrically through the collider transform for runtime raycasts and editor projection, but Scene view behavior under complex parent transforms still needs manual validation.
- `dotnet build MafiaGame.sln` and `dotnet build Assembly-CSharp-Editor.csproj` currently fail in this shell before C# diagnostics because the generated editor project restore path is inconsistent: restore emits `Temp\obj\Debug\Assembly-CSharp-Editor\project.assets.json`, while build expects `Temp\obj\Assembly-CSharp-Editor\project.assets.json`. Runtime `Assembly-CSharp.csproj` still compiles successfully.
- Unity Editor Scene view behavior has not been manually verified.
- `.\tools\unity.ps1 -Command refresh` currently fails before import because Unity Licensing Client IPC times out after 60 seconds with return code 199. This is an environment/licensing startup failure, not a script compile result.

## Important Deviations From LOS-V3

- The full feature is not complete yet.
- Existing gameplay has only an opt-in bridge to the new Oblique Loft system; it is not enabled by default and has not been Unity-tested. The Oblique debug switch now belongs to the targetter rather than a separate target/obstacle component.
- The editor workflow now uses direct point/edge manipulation from default polygons rather than blank click-to-close drawing. This matches the user's corrected preferred workflow more closely, but still needs Unity Editor validation.
- No existing scene object has been migrated to authored Oblique Loft collider volumes yet. A default `Assets/Prefabs/ObliqueLoftCollider.prefab` exists, and `Car V2` has been prefab-migrated to a child loft object, but both need Unity import validation.
- Character and car prefabs have been migrated from legacy fixed `Refs` to dynamic `EntityRefs`, but the migration has not been verified inside Unity prefab mode.
- No old-vs-new LOS comparison panel/tool has been completed yet.

## Next Recommended Steps

1. Verify the Oblique Loft inspector and direct-manipulation controls inside Unity Editor.
2. Fix any Scene view UX issues found with point picking, edge insertion, synchronized slice deletion, and keyboard nudging.
3. Open `Assets/Prefabs/ObliqueLoftCollider.prefab` and `Assets/Prefabs/Vehicle/Car V2.prefab`, then verify the non-trigger `PolygonCollider2D` matches the footprint after import and after footprint edits.
4. Open migrated character/car prefabs and verify `EntityRefs.Parts` auto-populates after validation.
5. Add `Assets/Prefabs/SimpleTarget.prefab` to one character prefab and test selected target resolution.
6. Add a second `SimpleTarget` character between shooter and selected target and verify the nearer unblocked character is selected as the actual hit.
7. Create one sample static scene object with `ObliqueLoftCollider` and verify it blocks before a simple target when `useObliqueLoftLos` is enabled.
8. Click/select a static `ObliqueLoftCollider` object through both its footprint collider and projected generated faces, then verify projected face aiming can directly target front, back, side, top, and sloped faces when they are not occluded by closer lofts or nearer faces on the same selected loft.
9. Validate the targetter-owned Oblique Loft debug ray/hit labels and add fuller old-vs-new comparison visuals if needed.
10. Play-test car driving and verify zero root rotation, 16-direction movement steps, high-speed steering resistance, reverse steering, braking, gradual stop, and the existing 4-way Animator blend tree.

## Recent Changes

- 2026-05-26: Added `Docs/CompositionMigrationPlan.md`, a staged composition-over-inheritance migration plan. It inventories rigid inheritance and responsibility hotspots across controller/input helpers, item/weapon/gun classes, broad `Base`/`EntityRefs` usage, targeting, and vehicle possession, then defines phase-by-phase and migration-by-migration steps with validation criteria. Linked it from `Docs/ArchitectureRecommendations.md` and updated this context file. Documentation-only change; no compile run required.
- 2026-05-19: Improved direct static `ObliqueLoftCollider` selection/aiming again. `Target.cs` now adds valid lofts whose projected generated faces contain the cursor to the chosen-target list, so direct loft targeting is not limited to the synchronized ground footprint collider. When several projected faces overlap under the cursor, direct aiming scores candidates by whether their normal faces the shooter before constructing the extended ray. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the existing 9 warnings; Unity play/editor validation is still required.
- 2026-05-19: Improved direct static `ObliqueLoftCollider` aiming so the selected targetter first projects generated faces into the 2D aiming view and reconstructs the clicked face's logic-space point with barycentric interpolation. Footprint-based aim inference remains the fallback when no generated face projection contains the cursor. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the existing 9 warnings; Unity play/editor validation is still required.
- 2026-05-19: Updated direct static `ObliqueLoftCollider` targeting so the selected aim ray extends through the selected loft's logic bounds before raycasting all valid loft colliders. This lets direct loft targets use the full generated volume instead of stopping at the inferred aim point, while closer blockers and nearer faces of the same selected loft still occlude farther faces. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors; Unity play/editor validation is still required.
- 2026-05-18: Created root `AGENTS.md` and `CONTEXT.md` to establish persistent project instructions and progress tracking.
- 2026-05-18: Added `tools/unity.ps1` Unity CLI wrapper for Unity `2021.3.33f1`; parser validation passed. Initial Unity CLI tests launched Unity but returned immediately before wait handling was added. Existing Unity process should not be force-closed without user confirmation.
- 2026-05-18: Loaded `KickoffPrompts/LOS-V3.txt` as the active kickoff prompt for LOS work.
- 2026-05-18: Inspected existing LOS prototype in `Target.cs`, `Refs.cs`, `GunController.cs`, and `Gun.cs`.
- 2026-05-18: Added compile-clean Oblique Loft runtime/editor foundation under `Assets/Scripts/ObliqueLoft/` and migration notes in `Assets/Scripts/ObliqueLoft/README.md`.
- 2026-05-18: Installed `.NET SDK 8.0.421` using winget. Confirmed `dotnet --info` and `dotnet --version`.
- 2026-05-18: Ran `dotnet build MafiaGame.sln`; build succeeded with 0 errors and 9 existing warnings.
- 2026-05-18: Rewrote `AGENTS.md` and `CONTEXT.md` into the AODS-style guide/status-ledger format so the exact current progress state is remembered.
- 2026-05-18: Improved `ObliqueLoftColliderEditor` with selected slice depth editing, middle slice add/remove, sorted front/middle/back naming, and correct selected-depth slice point insertion. Added an opt-in `Target.cs` bridge through `useObliqueLoftLos` that tries valid `ObliqueLoftCollider` hits and falls back to old depth/hit collider logic by default. Updated `Assembly-CSharp.csproj` so `dotnet build MafiaGame.sln` includes the new runtime files; build passed with 0 errors and the same 9 existing warnings.
- 2026-05-18: Added `ObliqueShotDebugEditor` so selected debug shots label closest hit surface type, face id, and hit object in Scene view. `dotnet build MafiaGame.sln` still passes with 0 errors and the same 9 existing warnings. Unity refresh/import was attempted and failed before import due to Unity Licensing Client IPC timeout/return code 199.
- 2026-05-18: Final incremental `dotnet build MafiaGame.sln` after context update passed with 0 warnings and 0 errors.
- 2026-05-18: Reworked `ObliqueLoftColliderEditor` to match the corrected authoring model: default collider starts editable, footprint points are independent, slice insert/delete is synchronized across all slices, colors are footprint yellow-orange/front cyan/back blue/middle green, points are selectable/draggable, arrows nudge selected points, edge clicks insert points, right-click offers delete, and Delete/Backspace deletes selected points. Updated `AGENTS.md` and `CONTEXT.md`; `dotnet build MafiaGame.sln` passed with 0 warnings and 0 errors.
- 2026-05-18: Added `Docs/ObliqueLoftCollider.md` with getting-started steps and current feature documentation. Added `Docs/ArchitectureRecommendations.md` with significant architecture recommendations by feature area. Updated `AGENTS.md` and `CONTEXT.md` so feature docs are updated when feature behavior or user-facing workflows change.
- 2026-05-18: Verified docs by reading back `Docs/ObliqueLoftCollider.md` and `Docs/ArchitectureRecommendations.md`. Ran `dotnet build MafiaGame.sln`; build passed with 0 warnings and 0 errors.
- 2026-05-18: Adjusted Oblique Loft scene projection so logic height draws upward in the 2D Scene view instead of into Unity Z. Changed default reset slice height to half the footprint depth so the front slice starts at the lower footprint edge and the back slice starts at the upper footprint edge with visible height. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, and `CONTEXT.md`. `dotnet build MafiaGame.sln` passed, including `Assembly-CSharp-Editor`, with 0 errors and 9 existing warnings.
- 2026-05-18: Moved Oblique Loft shot debug ownership into `Target.cs`: `drawObliqueLoftDebug` now controls the current aim ray/hit gizmos and selected-targetter Scene label, while target/obstacle objects only need `ObliqueLoftCollider`. Removed standalone `ObliqueShotDebug` runtime/editor scripts, added `Assets/Scripts/Common/Editor/TargetEditor.cs`, cleared stale cached debug rays when Oblique targeting is disabled or no target is selected, and updated `Docs/ObliqueLoftCollider.md` plus this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors and the same 9 existing warnings; solution/editor-project build is currently blocked by generated editor-project restore path mismatch before C# compile diagnostics.
- 2026-05-18: Corrected Oblique Loft slice authoring for the 2D oblique illusion: slice position is now presented as `Slice Depth`, point dragging inverses the `(x, z + y)` projection correctly, slice point Y is clamped to non-negative height, Unity object Z no longer contributes to logical height, faded dotted connector lines show slice point `0` and the last point attached back to the footprint at the same depth, and the raw serialized slice list is hidden from the custom inspector. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and `CONTEXT.md`. Runtime `Assembly-CSharp.csproj` builds with 0 errors and the same 9 existing warnings; editor-project dotnet build remains blocked before C# diagnostics by generated project-reference/restore issues, so Unity Editor import still needs validation.
- 2026-05-18: Added first-pass connector-driven slice depth behavior: middle slice connector handles drag both connectors together, snap to non-horizontal footprint edges, and clamp between neighboring slices; front/back connectors lock to footprint min/max depths. This was later simplified so pointy front/back footprint ends are invalid instead of becoming special slices. Runtime `Assembly-CSharp.csproj` builds with 0 errors and the same 9 existing warnings; Unity Scene view validation is still required.
- 2026-05-18: Fixed flat footprint edge endpoint detection for slice connectors. Horizontal bottom/top footprint edges now return both edge endpoints instead of collapsing to one X value, so a default rectangular collider keeps normal front/back rectangular slices on the lower/upper footprint edges. `Reset Box` now immediately runs slice normalization. Runtime `Assembly-CSharp.csproj` builds with 0 errors and the same 9 existing warnings; Unity Scene view validation is still required.
- 2026-05-18: Fixed Unity 2021 editor compile error in `ObliqueLoftColliderEditor` by replacing unsupported `Handles.DiamondHandleCap` with `Handles.RectangleHandleCap` for the slice-depth connector handle. Runtime `Assembly-CSharp.csproj` builds with 0 warnings and 0 errors, and the latest Unity editor log tail no longer shows the previous `DiamondHandleCap` error.
- 2026-05-18: Corrected front/back default slice placement to match the visual authoring model. Slice point Y is now stored as editable visual/local Y, runtime slice height is derived as `slicePoint.y - sliceDepth`, `ResetToBox` writes the front rectangle on the lower footprint edge and the back rectangle on the upper footprint edge, and normalization splits already-collapsed boundary slices onto their own footprint depths. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors; Unity editor log tail shows script import requests and no new `error CS` lines for the edited files.
- 2026-05-18: Cleaned up Oblique Loft authoring visibility. The custom editor now always draws the footprint and all slices regardless of whether `Edit Footprint` or `Edit Slice` is active; edit mode only controls which handles can be changed. Generated face gizmos, which were the extra pink/deep-blue/etc. collision-face and normal debug lines, are now labeled `Show Generated Face Gizmos` and default off on reset boxes. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors and only existing warnings.
- 2026-05-18: Removed the zero-area slice concept. `ObliqueLoftSliceKind` was deleted, all slices are normal polygon slices again, and `ObliqueLoftBuilder` now requires every slice to have at least three points and matching vertex counts. Footprint validation now requires the bottom/front and top/back depths to each have a horizontal edge of at least 1px length; pointy footprint ends are invalid instead of creating special slices. Added an explicit `Remove Selected Middle Slice` button beside the selected-slice controls; it is enabled only for middle slices. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors and only existing warnings.
- 2026-05-18: Made mandatory footprint front/back flat edges active during editing instead of validation-only. Selecting a footprint point now records whether it is part of the mandatory front or back edge; dragging or arrow-nudging either endpoint moves the paired endpoint to the same Y, and normalization still guarantees a minimum 1px horizontal edge if an end collapses. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors.
- 2026-05-18: Fixed a slice-editing regression where selected slice point indices were being reused as preferred footprint point indices during normalization, causing footprint points to move unexpectedly while editing slices. Mandatory footprint-edge enforcement now uses the selected point index only when the active selection is a footprint point, and slice selection clears stored footprint edge-pair state. Runtime `Assembly-CSharp.csproj` builds with 0 errors.
- 2026-05-18: Improved point editing ergonomics. Editable footprint and slice points now use direct draggable handles, so click-drag moves a point without a prior selection click. Mouse dragging snaps at sub-pixel proximity to nearby points and nearby horizontal/vertical alignments within the same footprint or selected slice. Arrow-key nudging intentionally remains exact and unsnapped. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors.
- 2026-05-18: Updated slice connector source rules. Dotted slice connectors now derive from the slice's two bottom-most points every draw; if more than two points share the bottom Y, the connector uses the farthest-left and farthest-right bottom points. This replaces the old fixed point-0/last-point connector assumption. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors.
- 2026-05-18: Hardened the Oblique Loft LOS bridge after runtime testing feedback. `Target.cs` now rebuilds all discovered `ObliqueLoftCollider` instances before candidate filtering, exposes a debug status string, and treats `useObliqueLoftLos` with no valid obstacle candidates as clear in the Oblique path instead of silently falling back to old depth/hit logic. `TargetEditor` now includes the status text in the Scene label. Updated `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors and only existing warnings.
- 2026-05-18: Improved Oblique LOS hit feedback and face hit robustness. `ObliqueRaycaster` now intersects generated faces through triangle-fan ray tests instead of relying only on projected point-in-polygon containment. Blocked Oblique hits resolve highlighters by searching collider object, parents, and children; use a faded green `Highlighter.HighlightObliqueBlocked`; and draw the hit generated face with an extra green outline/fill through targetter debug. Updated `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 errors and only existing warnings.
- 2026-05-18: Brightened the Oblique blocked sprite highlight and made targetter/editor hit-face overlays resolve faces by stored `FaceIndex` rather than list index. Documented that the current slice-loft-only builder likely explains wrong-face highlights because the footprint is not yet used to construct or constrain side/depth surfaces. Runtime `Assembly-CSharp.csproj` builds with 0 errors and 9 existing warnings.
- 2026-05-18: Changed `ObliqueLoftBuilder` to canonicalize every slice from its bottom connector points before face construction. Generated faces now connect matching canonical lanes through depth instead of trusting raw slice point indices. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Latest runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-18: Fixed over-strict connector validation that could leave `Generated Faces` at zero. Bottom-left and bottom-right connector points no longer have to be neighboring polygon points; the builder now chooses the higher-profile path between them during canonicalization. Latest runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-18: Fixed a runtime/editor projection mismatch that could make generated volume faces appear outside authored slices on scaled objects. `ObliqueLoftCollider.LocalToLogicWorld` now maps depth through transform Y scale rather than transform Z scale, and the custom editor footprint/slice handle projection now uses the same transform X/Y scale. Updated feature docs and memory files. Latest runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-18: Added `Polygon Position` inspector sliders to `ObliqueLoftColliderEditor`. `Footprint Position X/Y` translates all footprint points together, and `Selected Slice Position X/Y` translates all points in the selected slice together while clamping downward motion at that slice's depth to preserve shape. Updated `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 warnings and 0 errors.
- 2026-05-18: Fixed slice connector selection when only one point is uniquely lowest. Editor dotted connectors and runtime canonicalization now use two distinct lowest points: farthest-left/right among a lowest-Y tie set, or the unique lowest point plus the next-lowest point. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Latest runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-18: Added automatic slice self-intersection repair for editor point drag/nudge. This first version sorted the selected slice around its center and applied the same point permutation to every slice; it was too aggressive and was replaced immediately after user feedback.
- 2026-05-18: Corrected slice self-intersection repair. It now repairs only the selected slice, does not move point positions, and only reverses the point-order segment between crossing edges so the connecting lines stop intersecting. Other slices are not reordered. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` builds with 0 warnings and 0 errors.
- 2026-05-18: Reworked slice self-intersection repair to avoid swapping `EditablePoints` entries. `ObliqueLoftSlice` now stores a separate serialized point connection order. Editor drawing, edge insertion, and runtime face building use that connection order. Repair changes only the selected slice's connection order, so dots keep their coordinates and point identities. Latest runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-18: Finished the editor-side part of connection-order repair. Slice polygon edges and edge-insert buttons use the stored connection order, while point handles and labels are drawn by raw point index so the dots do not appear to move or change identity after repair. Fixed slice-point deletion so the connection order is normalized after the backing point list is shortened, avoiding deleted indices being reintroduced. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the 9 existing warnings.
- 2026-05-18: Updated `ObliqueLoftBuilder` so generated volume connections are not faithful to raw dot indices. It now canonicalizes from connector dots, chooses the next slice direction with fewer projected lane crossings, and untangles remaining projected lane intersections before building faces. Updated docs, README, `AGENTS.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the 9 existing warnings.
- 2026-05-18: Fixed generated face winding in `ObliqueLoftBuilder`. Front/back caps and connecting quads now use outward winding, so normal-based classification no longer swaps top with bottom or front with back. Updated `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the 9 existing warnings.
- 2026-05-18: Hardened generated face orientation further by winding every face outward from the generated volume center, independent of slice winding. Replaced the generated-face wireframe gizmo with an editor-only translucent normal-colored face fill overlay controlled by `Show Generated Face Gizmos`; footprint and slice outlines remain the visible editable line overlays. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the 9 existing warnings.
- 2026-05-18: Replaced the simple crossing untangle pass with dynamic volume-lane optimization in `ObliqueLoftBuilder`. For each neighboring slice pair, the builder keeps the connector lane anchored, scores possible lane-order reversals, treats projected crossing connections as hard failures, penalizes pinched/near-zero side quads, and favors broader volume-like side quads. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the 9 existing warnings.
- 2026-05-18: Added sprite/animation-frame binding for Oblique Loft colliders. `ObliqueLoftSpriteFrameBinding` captures footprint/slice profiles keyed by `SpriteRenderer.sprite`, applies matching profiles in play mode as animated frames change, and leaves missing profiles unchanged by default. Added inspector capture/apply buttons, deep-copy helpers for slices and collider shapes, documented the workflow, and updated project memory. Runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-19: Scoped Oblique Loft LOS to simple mostly-static blockers and added `SimpleTarget` for shootable characters. `Target.cs` now resolves the selected character and intervening characters through one distance-sorted simple-target candidate path, then uses `useObliqueLoftLos` only for static blocker checks before each candidate or final endpoint. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, `CONTEXT.md`, and `Assembly-CSharp.csproj`. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and 9 existing warnings.
- 2026-05-19: Added `Docs/SimpleTargeting.md` as the dedicated integration and behavior document for `SimpleTarget`, including setup steps, shot resolution order, Oblique Loft static blocker interaction, animation-frame notes, debugging, and limitations. Linked it from `Docs/ObliqueLoftCollider.md` and updated this context file.
- 2026-05-19: Removed the earlier Oblique Loft sprite/animation-frame binding modification at user direction. Deleted `ObliqueLoftSpriteFrameBinding` and its custom editor, removed the runtime compile include, removed leftover `ObliqueLoftCollider.SetShape` and `ObliqueLoftSlice` clone/copy helpers that only supported that binding, and updated docs/context so Oblique Loft collider shapes are authored directly on static blocker objects with no `SpriteRenderer.sprite` profile binding.
- 2026-05-19: Replaced the fixed `Refs` component with dynamic `EntityRefs`. `Base` now exposes `EntityRefs`; controllers, input handlers, gun/item helpers, car possession, animation lookup, and `SimpleTarget` resolve entity parts through dynamic type lookup or focused owner state. Character, Car, and Car V2 prefabs now reference `EntityRefs` instead of the deleted `Refs` script. Added `Docs/EntityRefs.md`, updated architecture/targeting docs, `AGENTS.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and 9 existing warnings.
- 2026-05-19: Made `ObliqueLoftCollider` more drop-in. It now requires and synchronizes a non-trigger `PolygonCollider2D` to the authored footprint, added `Assets/Prefabs/ObliqueLoftCollider.prefab`, and updated `Target.cs` so a selected static loft collider can be a direct target while closer lofts still block first. Updated `AGENTS.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and 9 existing warnings.
- 2026-05-19: Migrated `Assets/Prefabs/Vehicle/Car V2.prefab` so its existing authored Oblique Loft footprint/slice data lives on a child object named `ObliqueLoftCollider` instead of on the car root. The root no longer has the loft component or footprint polygon collider, and `EntityRefs.parts` now references the child loft component/collider. Runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors; Unity prefab import validation is still required.
- 2026-05-19: Repaired a YAML formatting error introduced during the `Car V2` loft-child migration where `validationErrors: []` and the next Unity document marker were written on the same line. This caused Unity parser failure and dangling Body component messages. The prefab document marker is now on its own line and runtime `Assembly-CSharp.csproj` build passes with 0 warnings and 0 errors; Unity prefab import should be retried.
- 2026-05-19: Added `Assets/Prefabs/SimpleTarget.prefab` as a drop-in SimpleTarget root with adjustable `GroundCollider` and `HitCollider` child polygon colliders wired into the component fields. Updated `Docs/SimpleTargeting.md`, `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors.
- 2026-05-19: Fixed Oblique Loft runtime transform conversion so local ground X/depth use the object's 2D local X/Y basis. This makes authored volumes rotate with object Z rotation, including under 2D prefabs that have zero Z scale. Updated `Docs/ObliqueLoftCollider.md`, `Assets/Scripts/ObliqueLoft/README.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the existing 9 warnings.
- 2026-05-19: Restored the lost `ObliqueLoftCollider` child on `Assets/Prefabs/Vehicle/Car V2.prefab` with the previously authored Car V2 footprint/slice config and a synced non-trigger footprint `PolygonCollider2D`. `EntityRefs.parts` again references the child loft component and collider. Runtime `Assembly-CSharp.csproj` build passed with 0 warnings and 0 errors; Unity prefab import validation is still required.
- 2026-05-19: Reverted the attempted `CarController` runtime rotation-source change at user direction. Car driving is back to the original `transform.Rotate` / `transform.right` path, and the animation-direction parameters are unchanged. The Oblique Loft docs/README rigidbody-transform sync note from that attempted fix was removed. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the existing 9 warnings.
- 2026-05-19: Implemented 16-direction car movement without rotating the car root. `CarController` now tracks a private heading bucket, moves from that bucket's vector, accumulates steering progress in 22.5-degree steps with the existing speed-resistance tuning, steers toward the opposite input heading while reversing, and feeds the current bucket vector to the existing 4-way Animator blend tree. Updated `AGENTS.md`, `Docs/ArchitectureRecommendations.md`, and this context file. Runtime `Assembly-CSharp.csproj` build passed with 0 errors and the existing 9 warnings; Unity Play Mode driving validation is still required.
- 2026-05-19: Repaired the interrupted car visual asset adjustment. Removed generated `Assets/Graphics/Vehicles/CarDirectional` sprites, restored all `Assets/Animations/Car/*.anim` clips to the original `Car_2_complete_32x32_1.png` sprite references with empty rotation/euler curves, and restored `Assets/Prefabs/Vehicle/Car.prefab` from HEAD after it was truncated during the interrupted attempt. `Car.prefab` and `Car V2.prefab` both point at the original right-facing idle sprite by default. Unity Animator/import validation is still required.
- 2026-05-19: Rotated the directional car sprites directly inside `Assets/Graphics/Vehicles/Car_2_complete_32x32_1.png` and updated its sprite-slice rects while preserving sprite names/internal IDs. Idle/drive right frames remain unrotated; idle/drive up are rotated 90 degrees counter-clockwise; idle/drive left are rotated 180 degrees; idle/drive down are rotated 270 degrees counter-clockwise. The car animation clips still reference the same original sheet sprites and do not use transform rotation curves. Unity Animator/import validation is still required.
