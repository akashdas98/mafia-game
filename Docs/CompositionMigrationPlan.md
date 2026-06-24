# Composition Migration Plan

This document describes a staged migration from the current inheritance-heavy controller/input/helper design toward a more compositional Unity architecture. The goal is not to rewrite the project at once. The goal is to extract one capability at a time while keeping existing prefabs, gameplay, and Oblique Loft LOS work stable.

## Current Inheritance Hotspots

### Controller And Input Stack

Original shape:

- `Controller : MonoBehaviour`
- `CharacterController : Controller`
- `CarController : Controller`
- `InputHandler : MonoBehaviour`
- `CharacterInputHandler : InputHandler`
- `CarInputHandler : InputHandler`
- Old controller/input helper classes have been removed after routing moved to focused capability components and `PlayerInputRouter` / `VehicleInputRouter`.
- `CharacterController` and `CarController` have now been removed from the active runtime path.

Scaling risk:

- Features become tied to one owner type when behavior is hidden behind owner-specific helpers.
- Adding a new controllable entity tends to create a parallel set of base/helper classes.
- Plain C# helper objects with manual lifecycle forwarding make execution order and ownership implicit.
- Input is passed through string-keyed dictionaries, which makes feature extraction and renaming fragile.

### Item And Weapon Stack

Current shape:

- `Interactable : MonoBehaviour`
- `Item : Interactable`
- `Weapon : Item`
- `Gun : Weapon`
- `SemiAuto : Gun`
- `FullAuto : Gun`
- `Pistol : SemiAuto`
- `Melee : Weapon`

Scaling risk:

- Weapon behavior is encoded as class ancestry, so combinations such as burst fire, thrown weapons, charge weapons, weapon attachments, or AI-only weapons will push the hierarchy wider and deeper.
- Inventory classification uses `item is Weapon`, which couples storage rules to inheritance.
- Weapon tuning is set in code, such as `Pistol.Awake`, instead of reusable data/configuration.

### Entity Wiring

Current shape:

- Explicit serialized references wire stable owner relationships.
- Narrow local fallbacks use `GetComponent`, `GetComponentInChildren`, or parent/child hierarchy scans only at the specific call site that needs them.

Scaling risk:

- A dynamic entity index or universal refs object can become an implicit service locator if components use it for broad cross-feature access instead of focused local dependencies.

### Targeting And Vehicle Responsibilities

Original shape:

- `Target` owned visual targeting state, target picking, legacy LOS behavior, and the Oblique Loft bridge.
- `CarController` owned driving physics, animation, enter/exit possession, visibility changes, parenting, and input handler switching.

Current shape:

- `Target` presents shooter-side target state and delegates selection/LOS decisions to targeting strategies.
- `VehicleMotor` owns driving input and movement ticking.
- `VehicleAnimationController` owns vehicle animator parameters.
- `VehiclePossession` owns enter/exit, driver visibility/collision state, parenting, and input-handler switching.

Scaling risk:

- These classes are not inheritance-heavy, but they are responsibility-heavy. They should be decomposed through composition before more behavior is added.

## Target Architecture

Prefer capability components over type-specific helper inheritance.

Example target shape for a character entity:

- `CharacterMotor`
- `CharacterAnimationController`
- `InteractionSensor`
- `InventoryUser`
- `Inventory` child/prefab with item containers
- `WeaponUser`
- `TargetingPresenter` or `AimTarget`
- `PlayerInputRouter`

Example capability interfaces:

- `IMoveInputReceiver`
- `IAimInputReceiver`
- `IFireInputReceiver`
- `IInteractInputReceiver`
- `IInventoryInputReceiver`
- `IPossessable`
- `IItemPickupReceiver`
- `IEquippable`
- `IFireMode`

The intended direction:

- Input routers translate raw input into typed intent.
- Capability components own behavior.
- Entity composition decides what an object can do.
- Explicit serialized references define stable local ownership.
- Transition controller shells are removed once prefab routing and local owner APIs cover their old entry points.

## Migration Rules

1. Do not big-bang replace controllers or input handlers.
2. Keep current prefab behavior working after every phase.
3. Keep `InputManager` active-handler switching until a replacement exists.
4. Prefer adapter components that allow old and new code to run together temporarily.
5. Migrate one feature at a time and compile after each meaningful step.
6. Update docs and `CONTEXT.md` after each meaningful migration.
7. Do not change Oblique Loft LOS routing as part of this migration unless the phase explicitly touches targeting.

## Phase 0: Baseline And Guardrails

Purpose: make the current behavior measurable before changing architecture.

Steps:

1. Record the current component graph for character, car, weapon, and item prefabs.
2. Add or identify a smoke-test scene for character movement, aiming, pickup/drop, weapon cycling, car enter/exit, and car driving.
3. Add small compile-safe tests where practical for pure logic, especially input state conversion and inventory bounds.
4. Run `dotnet build Assembly-CSharp.csproj`.
5. In Unity, validate character movement, interact, aim/fire, item pickup/drop, weapon cycle, car enter/exit, and driving.

Exit criteria:

- Current behavior is documented.
- Known behavior gaps are separated from migration regressions.
- Runtime compile check passes.

## Phase 1: Typed Input State

Purpose: remove string-keyed input dictionaries before extracting more capabilities.

Migration steps:

1. Add typed structs/classes such as `CharacterInputState` and `CarInputState`.
2. Keep `InputData` as the raw Unity input snapshot from `InputManager`.
3. Change `CharacterInputHandler.SetInputs(InputData)` to populate `CharacterInputState`.
4. Change `CarInputHandler.SetInputs(InputData)` to populate `CarInputState`.
5. Done: replace old helper dictionary use with typed state routing.
6. Done: remove dictionary keys after all readers migrated.
7. Compile and play-test character and car input.

Exit criteria:

- No gameplay code reads `inputs["..."]`.
- Input names are refactor-safe.
- `InputManager` active handler switching still works.

## Phase 2: Explicit Feature Lifecycle

Purpose: make helper ownership clear before converting helpers into components.

Migration steps:

1. Done historically: add small lifecycle interfaces and centralize helper ticking while helpers still existed.
2. Superseded: character input helpers were removed after `PlayerInputRouter` validation.
3. Superseded: `GunController`, `ItemsController`, `ControllerHelper<T>`, `CharacterControllerHelper`, and the lifecycle interfaces were removed after Unity validation of the capability path.
4. Compile and verify update order remains unchanged.

Exit criteria:

- No plain C# controller helper lifecycle remains in gameplay code.
- New behavior uses focused `MonoBehaviour` lifecycle or explicit capability APIs.

## Phase 3: Character Capabilities

Purpose: extract character behavior into reusable components.

Migration steps:

1. Add `CharacterMotor` as a MonoBehaviour that owns movement vector, speed, Rigidbody2D velocity, and facing.
2. Superseded: `PlayerInputRouter` routes movement directly to `CharacterMotor`.
3. Move only the current locomotion parameter writing into `CharacterAnimationController` or a similarly focused component.
4. Done: `CharacterAnimationController` owns animation ticking directly through its own `Update`.
5. Add `InteractionSensor` or `Interactor` to own nearby interactable tracking.
6. Move `Controller` interactable list behavior into the interaction component for characters.
7. Done: `PlayerInputRouter` routes interaction directly to `CharacterInteractor`.
8. Compile and validate movement, animation, and interaction.
9. Done: remove the old `CharacterController` shell and its prefab component.

Exit criteria:

- Movement, animation, and interaction are independent character components.
- `CharacterController` is removed from the active runtime path.
- Feature components can be reused by non-player characters without inheriting from `CharacterController`.

Detailed animation design, aim pose integration, animator parameters, and frame-specific visual gun points are intentionally outside this phase. Use `Docs/AnimationMigrationPlan.md` for that follow-up work after the composition foundation is validated.

## Phase 4: Weapon User And Inventory Input

Purpose: remove character-specific gun/item helper coupling.

Migration steps:

1. Done: add a `WeaponUser` MonoBehaviour that owns equipped weapon aiming, gun positioning, trigger state forwarding, and shoot height/distance settings.
2. Done: move `GunController` logic into `WeaponUser`.
3. Done: replace direct `controller.gunController` input calls with `PlayerInputRouter` wiring to `WeaponUser`. `Target` is initialized by `WeaponUser` directly.
4. Done: add an `InventoryUser` focused inventory command component for cycle/drop/pickup operations.
5. Done: move `ItemsController` behavior into `InventoryUser`.
6. Superseded: aim/fire input now routes directly from `PlayerInputRouter` to `IAimInputReceiver` and `IFireInputReceiver`.
7. Superseded: inventory input now routes directly from `PlayerInputRouter` to `IInventoryInputReceiver`.
8. Done: update `Item.PickUp` to call `IItemPickupReceiver` instead of requiring `CharacterController`.
9. Done: audit the already-migrated character prefab/code against the interface routing change. The text prefab contains `CharacterMotor`, `CharacterAnimationController`, `CharacterInteractor`, `WeaponUser`, `InventoryUser`, and `PlayerInputRouter` wired by explicit serialized references, with inventory state on the `Inventory` child/prefab.
10. Done after Unity validation: remove `GunController`, `ItemsController`, `ControllerHelper<T>`, `CharacterControllerHelper`, and the helper lifecycle interfaces.
11. Compile and verify aim/fire, pickup/drop, and weapon cycling.

Exit criteria:

- Input, pickup, target initialization, and weapon/inventory commands are no longer tied to `CharacterControllerHelper`, `GunController`, or `ItemsController`.
- A different entity can use weapons or inventory by adding capability components.
- Shoot height and gun distance are serialized tuning values, not hardcoded helper fields.
- Runtime fallback wiring keeps required local components initialized when migration components are auto-added.

## Phase 5: Input Router Composition

Purpose: replace type-specific input helpers with capability routing.

Migration steps:

1. Started: introduce `PlayerInputRouter` for player-controlled character command routing. `CharacterInputHandler` remains the active `InputHandler` for now so `InputManager` active-handler switching stays stable. `Assets/Prefabs/Character/Character.prefab` is text-wired with `PlayerInputRouter`.
2. Done for characters: keep raw `InputData` translation in `CharacterInputHandler`, then forward the typed `CharacterInputState` to `PlayerInputRouter`.
3. Done for characters: route commands to local capability interfaces:
   - `IMoveInputReceiver` implemented by `CharacterMotor`
   - `IInteractInputReceiver` implemented by `CharacterInteractor`
   - `IAimInputReceiver` implemented by `WeaponUser`
   - `IFireInputReceiver` implemented by `WeaponUser`
   - `IInventoryInputReceiver` implemented by `InventoryUser`
4. Done for characters: keep `CharacterInputHandler` as an adapter that forwards to `PlayerInputRouter`.
5. Done for current car input: keep `CarInputHandler` as the active adapter and add `VehicleInputRouter`, with `VehicleMotor` implementing `IVehicleInputReceiver`. `Assets/Prefabs/Vehicle/Car V2.prefab` is text-wired with `VehicleInputRouter`.
6. Update `InputManager.SetInputHandler` only if the active-handler abstraction needs to become an active-router abstraction.
7. Compile and validate character and car control switching.
8. Done: remove old input helper classes after Unity validation confirmed the router path. `GunInputHandler`, `ItemsInputHandler`, `CharacterInputHandlerHelper`, and `InputHandlerHelper` are no longer in the project.

Exit criteria:

- Character and car input are routed by capability interfaces, not concrete entity types.
- Character/car possession still works.
- `GunInputHandler`, `ItemsInputHandler`, `CharacterInputHandlerHelper`, and `InputHandlerHelper` are deleted.

## Phase 6: Vehicle Possession And Movement Split

Purpose: separate car driving from entering/exiting and input ownership.

Migration steps:

1. Done for first slice: add `VehicleMotor` for speed, acceleration, braking, steering bucket, Rigidbody2D velocity, and driving/heading state.
2. Done: route vehicle input directly to `VehicleMotor`.
3. Done: add `VehicleAnimationController` so vehicle animation parameters are owned by a vehicle-specific animation component.
4. Done: add `VehiclePossession` for enter/exit.
5. Done: move driver parenting, visibility, Rigidbody mode switching, and input handler switching into the possession component.
6. Deferred: add explicit exit anchors or a simple exit-position strategy when vehicle placement rules are designed.
7. Done: route car interaction to `VehiclePossession.Enter`, and route exit input through `VehicleMotor.Exit` to `VehiclePossession`.
8. Compile and validate enter, exit, driving, braking, reversing, and animation.
9. Done: remove the old `CarController` shell and its prefab component.

Exit criteria:

- Car movement and possession are separate components.
- Possession can be reused for other vehicles or seats.
- `CarController` is removed from the active runtime path.
- Vehicle animation parameter writing is no longer hardcoded in `CarController`.

## Phase 7: Item And Weapon Composition

Purpose: avoid deeper weapon inheritance as more weapon behavior is added.

Migration steps:

1. Done for current gun path: add serialized `GunStats` config for base damage, distance falloff, mag size, fire speed, and fire rate.
2. Started: add `IEquippable` for equip/unequip behavior. `Weapon` implements it as the current adapter.
3. Add `WeaponBehaviour` or `DamageDealer` for weapon-level behavior.
4. Done for current gun path: add fire-mode components:
   - `SemiAutoFireMode`
   - `FullAutoFireMode`
   - `BurstFireMode`
   - future charge or thrown modes
5. Done: change `Gun` / `SemiAuto` / `FullAuto` to delegate trigger behavior to fire-mode components while keeping the old subclasses as adapters.
6. Done for current pistol behavior: replace `Pistol : SemiAuto` hardcoded `Awake` tuning with serialized `GunStats` defaults on the gun path.
7. Done for current inventory path: update `Inventory` and `InventoryUser` classification to use `IEquippable` instead of `item is Weapon`.
8. Done for current character path: move `Inventory` state/storage onto the `Inventory` child/prefab with explicit `Weapons` and `Misc` container references; keep `InventoryUser` on the character as the command capability.
9. Keep old weapon subclasses as adapters until prefabs are migrated.
10. Compile and verify equip, unequip, fire, and inventory behavior.

Exit criteria:

- New weapons are configured by data and components, not by adding another subclass level.
- Inventory rules do not depend on `Weapon` inheritance.
- Existing weapons still work.

## Phase 8: Targeting Decomposition

Purpose: keep targeting and LOS maintainable while Oblique Loft integration grows.

Current targeting roles to preserve:

- `AimTarget` / `Target` is the shooter-side targetter and marker presenter.
- `SimpleTarget` is the shootable-target component for characters and other flat animated targets. It owns a flat current-frame hit polygon plus an authored horizontal ground reference line.
- `ObliqueLoftCollider` is for mostly-static blockers and direct static targets. Complex moving objects such as cars are intentionally out of scope for Oblique Loft LOS right now.
- The old depth/hit/enclosure path remains the fallback while migrated prefabs are validated.

Migration steps:

1. Done for first slice: add `TargetingResult`, a pure targeting result type that describes intended target, actual hit object, hit point, derived ground point, target height, blocker, LOS mode, and debug data.
2. Done for first slice: extract the selected-object and direct-click selection pass from `Target` into `TargetSelectionResolver` while preserving current highlighter ownership resolution.
3. Done: extract the `SimpleTarget` candidate pass into `SimpleTargetingStrategy`. It keeps the current ordering: selected target and intervening flat targets are one distance-sorted candidate list, and a nearer unblocked candidate wins. Static Oblique blocker checks are still called through a `Target` callback while that strategy is extracted separately.
4. Done: extract static Oblique Loft blocker/direct-target tests into `ObliqueTargetingStrategy`. It wraps the existing `ObliqueLoftLos`, projected generated-face aim, generated-face raycast, minimum-radius filtering, and debug result data instead of reimplementing the geometry layer.
5. Done: extract the old depth/hit/enclosure fallback into `LegacyDepthTargetingStrategy`, including the selected DepthCollider target line, old interposing DepthCollider scan, and old HitCollider point/intersection checks.
6. Done for current routing: keep `Target` focused on shooter-side state, marker display, highlight routing, debug drawing switches, and wiring to the resolver/strategies.
7. Keep `AimOrigin` as the logical shot origin and `GunPoint` as visual placement only; do not let targeting decomposition depend on animation-frame visual gun position for real ray math.
8. Keep old-vs-new fallback behavior unchanged until SimpleTarget and sample loft volumes are Unity-validated.
9. Compile and validate selected SimpleTargets, intervening SimpleTargets, static Oblique blockers, direct loft targeting through footprint/projected faces, legacy fallback targets, highlights, and debug drawing.

Exit criteria:

- `Target` no longer owns all targeting decisions directly.
- SimpleTarget, Oblique Loft, and legacy targeting are explicit strategies with the same gameplay ordering as the current bridge.
- Future LOS comparison tooling is easier to add.
- `AimTarget` remains a shooter-side presentation/integration prefab, not a shootable target.

## Phase 9: Remove Shared Lookup Coupling

Purpose: stop requiring gameplay components to inherit from `Base` or depend on a dynamic entity index just for local wiring.

Migration steps:

1. Done: add explicit serialized fields for stable required dependencies on controllers, routers, weapon/inventory users, vehicle components, and target prefabs.
2. Done: `InputHandler`, `Interactable`, `Inventory`, `Target`, and `Ammo` now inherit directly from `MonoBehaviour`; the unused `Base` and `Controller` scripts have been removed.
3. Done: remove `EntityRefs`, `EntityRefsExtensions`, runtime references to them, compile entries, and prefab components.
4. Done: replace input-router capability discovery with explicit router fields.
5. Done: replace `WeaponUser` and `InventoryUser` dynamic lookup with explicit `Target` / `Inventory` references and local component fallback.
6. Done: replace vehicle dynamic lookup with explicit vehicle/input-handler/animator references.
7. Done: keep only narrow local hierarchy fallback where the owning object is inherently dynamic, such as item pickup receiver resolution or SimpleTarget tagged child collider lookup.
8. Compile and validate prefabs after each group of components is migrated.

Exit criteria:

- `Base` is no longer the default parent for gameplay components.
- Required dependencies are explicit.
- No dynamic entity index remains in runtime architecture.

## Suggested Order

1. Phase 0: Baseline And Guardrails.
2. Phase 1: Typed Input State.
3. Phase 2: Explicit Feature Lifecycle.
4. Phase 3: Character Capabilities.
5. Phase 4: Weapon User And Inventory Input.
6. Phase 5: Input Router Composition.
7. Phase 6: Vehicle Possession And Movement Split.
8. Phase 7: Item And Weapon Composition.
9. Phase 8: Targeting Decomposition.
10. Phase 9: Remove Shared Lookup Coupling.

The highest-value early migration is typed input state because it lowers risk for every later input and capability change. The highest-value structural migration is extracting `WeaponUser` and `CharacterMotor`, because those remove the strongest coupling from `CharacterController`.

See `Docs/AnimationMigrationPlan.md` for the dedicated follow-up plan for entity-specific animation controller components, character animation, vehicle animation, aim pose, and frame-specific visual gun points. The composition plan should only keep the minimal animation extraction needed to decouple broad gameplay controllers; detailed animator behavior belongs in the animation plan.

## Validation Checklist For Each Migration

Run this after each meaningful migration:

1. `dotnet build Assembly-CSharp.csproj`.
2. Check character movement and animation.
3. Check interact and item pickup.
4. Check weapon equip, aim, fire, drop, and cycle.
5. Check car enter/exit and input switching.
6. Check car movement, braking, reversing, and animation.
7. Check targeting and LOS behavior if the migration touched guns, targeting, or Oblique Loft.
8. Update `CONTEXT.md`.
9. Update relevant docs if behavior, setup, workflow, or public API changed.
