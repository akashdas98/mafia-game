# MafiaGame Context

## Current Status

This repository is a Unity `6000.3.16f1` 2D oblique/top-down game project at:

`D:\Projects\Unity\Mafia Game\MafiaGame`

Current major work item: custom 2.5D / oblique-world LOS and bullet collision system from `KickoffPrompts/LOS-V3.txt`.

Current implementation state:

- Oblique Loft v1 exists beside the old LOS/depth-collider prototype.
- Targeting has been split into strategies: `TargetSelectionResolver`, `SimpleTargetingStrategy`, `ObliqueTargetingStrategy`, and `LegacyDepthTargetingStrategy`.
- `Target.cs` remains the shooter-side marker, highlight, debug, and strategy coordinator.
- Gameplay integration is opt-in through `useSimpleTargeting`, `useObliqueLoftLos`, and `drawObliqueLoftDebug`.
- Unity gameplay/editor validation is still required before calling the LOS feature complete.

Current estimated completion:

- Unity CLI/tooling setup: complete enough for current work.
- Oblique Loft Collider foundation: partially complete.
- Full LOS-V3 feature: not complete.
- Gameplay integration of Oblique Loft LOS: started, compile-clean, not Unity-validated.
- Unity Editor authoring workflow validation: not verified.

## Operating Rule

Before continuing LOS or collision work, check this file's `Status Ledger`, `Known Issues`, and `Next Recommended Steps`. Areas marked `Complete` should be maintained rather than reworked. Areas marked `Started` or `Open` are the next implementation targets.

Do not claim the Oblique Loft feature is done until gameplay routing, sample collider authoring, editor workflow testing, debug comparison, and migration validation are complete.

Keep this file compact. It is working memory, not a full changelog. Use Git history and durable docs under `Docs/` for detailed history.

## Status Ledger

| Area | Status | Do Next | Do Not Rework Unless |
| --- | --- | --- | --- |
| Root memory files | Complete, compact ledger format | Keep current status accurate and concise. | User requests another format. |
| Unity CLI wrapper | Complete, needs optional re-test after Unity closes | Use `.\tools\unity.ps1`; re-test `version`/`refresh` when safe. | Unity install path changes. |
| .NET SDK availability | Complete | Use `dotnet build Assembly-CSharp.csproj` for the fast runtime compile check. | SDK breaks or PATH is unavailable. |
| Existing LOS prototype inspection | Complete | Preserve old path while integrating new one. | Existing gameplay behavior changes or deeper migration begins. |
| Oblique Loft runtime data structures | Started, compile-clean, footprint collider sync added | Validate synced non-trigger footprint collider in Unity prefab/scene editing. | Validation/editor testing reveals design issues. |
| Surface generation from slices | Improved, compile-clean, connector-normalized dynamic volume-lane optimization added | Test generated faces in Unity; next improvement is footprint-boundary constraints for non-rectangular silhouettes. | Geometry edge cases fail. |
| Surface classification by normals | Improved, compile-clean, center-outward face winding fixed | Verify normals and labels visually in Scene view. | Classification is wrong for sample volumes. |
| Ray-face intersection | Started, compile-clean | Test with sample shot rays and authored volumes. | False hits/misses appear. |
| Closest-hit LOS query | Started, compile-clean | Validate through feature flag/fallback with samples. | API shape blocks `Target.cs` integration. |
| Scene/debug gizmos | Improved, compile-clean, generated face overlay uses translucent fills | Validate in Unity Scene view. | Gizmos are unreadable or mapped incorrectly. |
| Editor drawing workflow | Improved direct-manipulation v1, needs Editor verification | Verify point selection/drag/nudge/edge insert/delete in Unity. | User asks for a different authoring model. |
| Animation-frame hitbox design | Implemented in code, needs Unity validation | Validate parent/child `SimpleTarget` layer profile auto-detect while scrubbing layered animations. | User changes the desired authoring model. |
| Explicit wiring migration | Code-complete, compile-clean, needs Unity import/prefab validation | Verify migrated character/car/inventory prefab references in Unity. | Serialized references break after Unity import. |
| Simple target resolver | Started, compile-clean, drop-in prefab added | Add `Assets/Prefabs/SimpleTarget.prefab` to representative characters and test selected/intervening resolution. | User chooses a different character hit model. |
| Compatibility with existing LOS system | Started, simple target plus opt-in static blocker/direct-target bridge added | Test `Target.cs` with feature switches enabled on sample targets/blockers. | User decides to keep systems fully separate. |
| Sample object/prefab migration | Started, drop-in prefab added and Car V2 loft moved to `Sprites/Body`, needs Unity validation | Open `ObliqueLoftCollider.prefab` and `Car V2.prefab`, verify import, footprint sync, and target selection. | User asks to defer prefab changes. |
| Vehicle 16-direction movement | Complete in `VehicleMotor`, compile-clean, needs Play Mode verification | Verify zero root rotation, 22.5-degree heading steps, steering resistance, reverse steering, braking, and 4-way blend output. | User asks for continuous rotation or new directional sprites. |
| Old-vs-new debug comparison | Started, targetter-owned Oblique debug exists | Validate targetter-owned ray/hit labels and add fuller comparison only if still needed. | User no longer needs comparison tooling. |
| Composition migration | Code-complete through Phase 9 plus inventory child-prefab packaging; helper/broad controller cleanup complete; compile-clean | Validate latest prefab imports and play-test movement, interaction, weapon use, inventory, car possession, and driving. | Implementation priorities change. |

## Completed Foundation

- `AGENTS.md` and `CONTEXT.md` exist and must be read before work.
- `tools/unity.ps1` exists as the project-local Unity CLI wrapper.
- Unity project version is `6000.3.16f1`.
- `.NET SDK 8.0.421` is available at `C:\Program Files\dotnet\dotnet.exe`.
- `dotnet build Assembly-CSharp.csproj` is the current fast runtime compile check and has passed for the current runtime code path.
- Existing LOS prototype has been decomposed into focused targeting strategies while preserving old fallback behavior.
- New Oblique Loft runtime/editor code lives under `Assets/Scripts/ObliqueLoft/`.
- Oblique Loft docs live in `Docs/ObliqueLoftCollider.md` and `Assets/Scripts/ObliqueLoft/README.md`.
- SimpleTarget docs live in `Docs/SimpleTargeting.md`.
- AimTarget docs live in `Docs/AimTarget.md`.
- Architecture/composition docs live in `Docs/ArchitectureDiagrams.md`, `Docs/CompositionMigrationPlan.md`, `Docs/AnimationMigrationPlan.md`, and `Docs/AnimationAdapterArchitecture.md`.

## Current Architecture Notes

- `Target.cs` coordinates targeting state, marker display, highlight routing, debug switches, and strategy wiring.
- `TargetSelectionResolver` owns direct click/selection.
- `SimpleTargetingStrategy` owns selected/intervening flat `SimpleTarget` candidate construction and distance ordering.
- `ObliqueTargetingStrategy` owns static Oblique blockers, direct loft targeting, projected generated-face aim, and generated-face raycast filtering.
- `LegacyDepthTargetingStrategy` owns old `DepthCollider` / `HitCollider` / `EnclosureCollider` fallback.
- `SimpleTarget` represents shootable characters/flat targets as a current-frame hit polygon plus authored horizontal ground reference line.
- `ObliqueLoftCollider` is for mostly-static blockers/direct static targets such as walls, buildings, and trees. Cars and complex moving objects are ignored by Oblique Loft LOS for now.
- Runtime Oblique collision truth is generated 3D logic geometry, not sprites or normal Unity 2D collider overlap.
- Entity wiring should stay explicit through serialized references and narrow owner APIs. Do not reintroduce `Refs`, `EntityRefs`, broad service locators, old controller shells, or helper-controller subclasses.
- Character and vehicle control use typed input state objects routed through capability routers.
- Character animation uses component-specific animation adapters and `AnimatorParameterRelay`; layered visual animators keep independent state/time.
- Vehicle movement uses `VehicleMotor`'s 16-direction heading bucket while keeping the car root Z rotation at zero.

## Active Gaps

1. Gameplay integration validation:
   `Target.cs` has a compile-clean unified `SimpleTarget` resolver and opt-in Oblique Loft checks, but it has not been tested in Unity against real character prefabs and authored loft volumes.

2. Editor workflow completeness:
   The Oblique Loft custom editor has direct manipulation for footprint/slice points and generated face visualization, but Scene view behavior still needs manual Unity verification.

3. Sample migration:
   Default `SimpleTarget` and `ObliqueLoftCollider` prefabs exist, and `Car V2` has been prefab-migrated, but representative scene/prefab setup still needs Unity import and play-mode validation.

4. Composition migration validation:
   Runtime code and prefab YAML are compile-clean, but migrated character/car/inventory prefab references and gameplay behavior need Unity spot-checks.

5. Footprint-constrained geometry:
   Generated faces use connector-normalized dynamic slice lanes and outward winding, but non-rectangular footprint silhouettes may still need a footprint-constrained builder pass.

## Pending

- Re-test `.\tools\unity.ps1 -Command version` and `.\tools\unity.ps1 -Command refresh` after Unity exits or the user confirms it is safe.
- Open Unity and verify custom editor import/compile.
- Create a sample `ObliqueLoftCollider` using `Reset Box`.
- Add `Assets/Prefabs/SimpleTarget.prefab` to at least one character prefab and verify selected/intervening target resolution.
- Play-test car driving and verify zero root rotation with 16-direction movement.
- Verify Oblique Loft point selection, dragging, arrow-key nudging, edge insertion, deletion, generated faces, normals, labels, and debug gizmos visually.
- Test feature-flagged `Target.cs` integration with Oblique Loft blockers.
- Validate targetter-owned Oblique Loft debug labels and add fuller old-vs-new comparison visuals only if needed.
- Open `Assets/Prefabs/Inventory.prefab`, `Assets/Prefabs/Character/Character.prefab`, and vehicle prefabs in Unity Prefab Mode to verify explicit references.
- Run `Tools/Character Builder/Rebuild Character Assets` in Unity and verify layer-scoped generated templates, slot clips, generated clips, and override controllers.

## Assumptions

- New LOS behavior should be introduced beside the existing prototype before replacing gameplay calls.
- Existing `DepthCollider` and `HitCollider` child objects should remain during migration.
- Cars and complex moving objects are intentionally ignored by Oblique Loft LOS for now.
- Characters should migrate to `SimpleTarget` flat hit polygons rather than Oblique Loft volumes.
- Logic depth maps from 2D scene Y into logic Z.
- For Oblique Loft v1, all slices must have the same vertex count and consistent winding.
- The footprint front/back must each have a horizontal edge. Pointy front/back footprint ends are invalid.
- The old LOS behavior should remain the fallback until the new system is validated.

## Known Issues

- Git dubious-ownership is resolved for the current shell user through a specific `safe.directory` entry for `D:/Projects/Unity/Mafia Game/MafiaGame`. No filesystem ownership or ACL changes were made.
- A Unity Editor process was observed running during earlier CLI testing. Do not force-close it without user confirmation.
- `dotnet build Assembly-CSharp.csproj` succeeds. Full solution/editor builds may still surface existing warnings from older scripts.
- `dotnet build MafiaGame.sln` and `dotnet build Assembly-CSharp-Editor.csproj` may fail before C# diagnostics because generated editor project restore/build paths disagree.
- `.\tools\unity.ps1 -Command refresh` has previously failed before import because Unity Licensing Client IPC timed out with return code 199.
- Unity Editor Scene view behavior, prefab import references, animation behavior, and Play Mode gameplay paths are not fully manually verified.
- Character Builder layer-scoped generated assets still need Unity menu validation.
- Non-rectangular Oblique Loft footprint silhouettes may still produce geometry-construction issues.

## Important Deviations From LOS-V3

- The full feature is not complete yet.
- Existing gameplay has only an opt-in bridge to Oblique Loft LOS and is not enabled as the only/default path.
- The Oblique debug switch belongs to the targetter.
- The editor workflow uses direct point/edge manipulation from default polygons rather than blank click-to-close drawing.
- Character and car prefabs have moved away from fixed refs and transitional dynamic entity indexes, but Unity prefab-mode validation is still required.
- No complete old-vs-new LOS comparison panel/tool exists yet.

## Next Recommended Steps

1. Verify the Oblique Loft inspector and direct-manipulation controls inside Unity Editor.
2. Fix any Scene view UX issues found with point picking, edge insertion, synchronized slice deletion, and keyboard nudging.
3. Open `Assets/Prefabs/ObliqueLoftCollider.prefab` and `Assets/Prefabs/Vehicle/Car V2.prefab`, then verify the non-trigger `PolygonCollider2D` matches the footprint after import and edits.
4. Open migrated character/car/inventory prefabs and verify explicit serialized references on routers, users, vehicle components, and animation adapters.
5. Add `Assets/Prefabs/SimpleTarget.prefab` to one character prefab and test selected target resolution.
6. Add a second `SimpleTarget` character between shooter and selected target and verify the nearer unblocked character is selected as the actual hit.
7. Create one sample static scene object with `ObliqueLoftCollider` and verify it blocks before a simple target when `useObliqueLoftLos` is enabled.
8. Click/select a static `ObliqueLoftCollider` through both its footprint collider and projected generated faces, then verify projected face aiming against front, back, side, top, and sloped faces.
9. Validate targetter-owned Oblique Loft debug labels and add fuller old-vs-new comparison visuals if needed.
10. Play-test car driving and verify zero root rotation, 16-direction steps, steering resistance, reverse steering, braking, gradual stop, and existing 4-way Animator blending.
11. Spot-check aim/fire, pickup/drop, weapon cycle, no-gun test targeting, and close-range minimum radius in Unity.
12. Validate character animation adapter ordering and parameter relay behavior in Unity.

## Recent Changes

- 2026-06-24: Added `.gitignore` entries for generated Character Builder outputs (`GeneratedClips`, `OverrideControllers`), Unity recovery scenes, and `debug.log`. Rebuild generated character outputs through `Tools/Character Builder/Rebuild Character Assets` when needed.
- 2026-06-24: Resolved Git dubious-ownership for the current shell user by adding this exact repo path to global `safe.directory`. Verified `git status --short` now works. No ownership or ACL changes were made.
- 2026-06-24: Compacted `CONTEXT.md` from a long historical changelog into a concise working ledger. Detailed implementation history should be retrieved from Git and durable docs instead of being appended here indefinitely.
- 2026-05-27: Moved aim authoring points into `Assets/Prefabs/AimTarget.prefab`. `Target` now keeps `AimOrigin` and `GunPoint` stable while only the marker sprite moves to the resolved hit position.
- 2026-05-27: Added authored `Aim Origin` and visual `Gun Point` support to `Target` / `WeaponUser`, including close-range rejection around the origin-to-gun-point radius.
- 2026-05-27: Fixed `Highlighter.OnValidate` prefab/edit-mode material errors by avoiding `Renderer.material` and using sprite color or material property blocks.
- 2026-05-27: Added a test-only `Allow Targeting Without Equipped Item` switch to `Target`.
- 2026-05-27: Fixed runtime Cinemachine follow assignment and made `BuildingFadeOut` tolerate missing camera/follow references.
- 2026-05-27: Added an authored SimpleTarget ground reference line and kept SimpleTarget as a flat 2D hit face at user direction.
- 2026-05-27: Moved SimpleTarget visualization into an editor drawer and tuned visible hit-face drawing.
- 2026-05-27: Fixed Oblique Loft slice insertion and connection-order handling so new slice points attach to the intended selected-slice edge.
