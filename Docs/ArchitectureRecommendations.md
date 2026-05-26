# Architecture Recommendations

This document records significant architecture observations by feature area. It is intentionally selective: minor style issues and speculative rewrites are omitted.

## Cross-Cutting Architecture

### Current Shape

The project uses a lightweight pattern:

- `Controller` subclasses own gameplay behavior.
- `InputHandler` subclasses receive input from `InputManager`.
- `ControllerHelper` and `InputHandlerHelper` split feature-specific behavior such as guns and items.
- `EntityRefs` acts as a dynamic part lookup index for each entity object.

This is workable for a small project, but responsibilities are beginning to blur as targeting, inventory, input, and vehicle possession interact.

For a staged composition-over-inheritance migration plan, see `Docs/CompositionMigrationPlan.md`.

### Recommended Changes

1. Replace string-keyed input dictionaries with typed input state.

   Current helpers read values like `inputs["aim"]`, `inputs["scroll"]`, and `inputs["actionInput"]`. This is fragile and makes refactors error-prone. A typed `CharacterInputState` and `CarInputState` would preserve the current design while removing string coupling.

2. Make helper ownership explicit.

   Helpers are plain C# classes created by controllers/input handlers. That is fine, but the lifecycle is implicit. Consider a small `IControllerFeature` / `IInputFeature` interface with `Tick`, `FixedTick`, and `Dispose` naming so feature helpers are intentionally lifecycle-managed.

3. Keep `EntityRefs` as discovery, not ownership.

   `EntityRefs` removes fixed universal fields, but it can still become an implicit service locator if overused. Use it to discover sibling/child parts on an entity; keep mutable runtime state on the component that owns it and expose focused APIs for higher-level behavior.

## Character Feature

### Current Shape

`CharacterController` owns movement, animation, interaction, and feature helper lifecycle. `CharacterInputHandler` translates raw input into movement/interact/gun/item behavior.

### Recommended Changes

1. Split character locomotion from character orchestration.

   Movement and animation are currently embedded in `CharacterController`. If character behavior grows, introduce a `CharacterMotor` or `CharacterMovementController` for Rigidbody movement and facing state. Keep `CharacterController` as the feature orchestrator.

2. Move interactable selection into a focused interaction feature.

   `Controller` tracks interactables globally. That works for character and maybe vehicles, but interaction rules are character-centric. A `CharacterInteractionController` helper would make it easier to add priority, filtering, prompts, or interaction UI later.

3. Avoid public lowercase feature properties.

   `gunController` and `itemsController` are public properties but named like fields. Prefer `GunController` and `ItemsController` properties or explicit methods to reduce accidental direct coupling.

## Input Feature

### Current Shape

`InputManager` polls Unity input and forwards `InputData` to the active `InputHandler`. This allows switching control from character to car.

### Recommended Changes

1. Keep the active input handler idea.

   This is a useful pattern for character/car possession. It should remain.

2. Add null guards and explicit active-owner semantics.

   `InputManager.Update` assumes `inputHandler` exists. `SetInputHandler` assumes the previous handler exists. Add null-safe handling before more input modes are introduced.

3. Replace input dictionaries with typed state.

   This is the highest-value input improvement. It affects `CharacterInputHandler`, `GunInputHandler`, `ItemsInputHandler`, and `CarInputHandler`.

## Gun, Targeting, And LOS Feature

### Current Shape

`GunController` positions the equipped gun and forwards aim/fire inputs. `Target.cs` owns target selection, old depth/hit collider LOS adjustment, target highlighting, and now an opt-in Oblique Loft bridge.

### Recommended Changes

1. Extract targeting query logic from `Target`.

   `Target` is both a scene marker/renderer and the targeting service. It now contains selection, old LOS, new LOS bridge, and highlighting. Introduce a non-MonoBehaviour service such as `TargetingResolver` with:

   - chosen target selection,
   - old LOS fallback,
   - Oblique Loft LOS query,
   - result object/position.

   Keep `Target` responsible for displaying the marker and highlight result.

2. Make LOS mode an explicit strategy.

   Instead of `Target` directly owning both systems long-term, use an interface such as `ITargetingOcclusionResolver` or a simple enum-backed strategy:

   - `LegacyDepthColliderResolver`
   - `ObliqueLoftResolver`
   - fallback chain resolver

   This would make old-vs-new comparison and eventual replacement much cleaner.

3. Move gun height/distance settings into serialized configuration.

   `GunController` currently hardcodes `gunDistance = 1f` and `gunHeight = 1.5f`. These are gameplay tuning values and should be serialized on a component or weapon config.

4. Keep Oblique Loft runtime independent.

   The new `ObliqueLoft` runtime layer is currently separate from `UnityEditor` and should stay that way. Editor code should remain under `Editor`.

## Inventory And Item Feature

### Current Shape

`Inventory` stores weapons/misc items and manages equipped weapon state. `ItemsController` handles pickup/drop/cycle.

### Recommended Changes

1. Fix inventory index boundary before it becomes a runtime bug.

   `SetEquippedWeaponIndex` checks `index > weapons.Count`, but valid max index is `weapons.Count - 1`. The current condition allows `index == weapons.Count`, which can lead to invalid access when calling `GetEquippedWeapon().gameObject.SetActive(true)`.

2. Centralize equip/unequip side effects.

   `Weapon` has `Equip`/`Unequip`, but inventory currently toggles GameObject active state directly. Use weapon equip/unequip methods consistently when changing equipped weapon.

3. Avoid hard-coded hierarchy lookup for dropped item placement.

   `ItemsController.DropItem` finds `TileGrid/Items` by string path. This is brittle. Prefer a serialized scene drop parent on `SceneDetails` or a dedicated item container reference.

## Vehicle Feature

### Current Shape

`CarController` handles driving physics, animation, entering/exiting, camera/input transfer, and driver visibility. Driving now keeps the car root transform rotation at zero and tracks facing with a private 16-direction heading bucket. Velocity comes from the current bucket, while the existing 4-direction Animator blend tree receives the current heading vector as a temporary visual approximation.

### Recommended Changes

1. Split vehicle possession from vehicle movement.

   Enter/exit logic changes parent, visibility, Rigidbody mode, and input handler. Movement logic controls speed/turning. These are separate concerns. A `VehicleSeat` or `VehiclePossessionController` would reduce coupling.

2. Keep driving tuning serialized.

   Many car tuning fields are public. That is fine for quick iteration, but use `[SerializeField] private` with grouped headers later to avoid accidental external mutation.

3. Make exit placement explicit.

   `Exit` currently unparents the driver but does not choose a safe exit position. When collision and LOS volumes become more accurate, vehicle exit should use a defined exit anchor.

## Building And Visibility Feature

### Current Shape

Only `BuildingFadeOut` is visible in the current script set.

### Recommended Changes

Keep building visibility separate from LOS collision truth. Buildings may fade visually, but `ObliqueLoftCollider` should remain the logic collision source. Avoid coupling fade state to hit blocking unless gameplay explicitly requires it.

## Recommended Priority

1. Validate and stabilize the Oblique Loft editor workflow in Unity.
2. Extract targeting resolution out of `Target.cs` before the old/new LOS bridge grows further.
3. Replace string-keyed input dictionaries with typed input states.
4. Fix the inventory equipped-index boundary.
5. Split vehicle possession from vehicle movement when vehicle behavior grows.
