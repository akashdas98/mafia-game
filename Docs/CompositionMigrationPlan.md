# Composition Migration Plan

This document describes a staged migration from the current inheritance-heavy controller/input/helper design toward a more compositional Unity architecture. The goal is not to rewrite the project at once. The goal is to extract one capability at a time while keeping existing prefabs, gameplay, and Oblique Loft LOS work stable.

## Current Inheritance Hotspots

### Controller And Input Stack

Current shape:

- `Base : MonoBehaviour`
- `Controller : Base`
- `CharacterController : Controller`
- `CarController : Controller`
- `InputHandler : Base`
- `CharacterInputHandler : InputHandler`
- `CarInputHandler : InputHandler`
- `ControllerHelper<T> where T : Controller`
- `CharacterControllerHelper : ControllerHelper<CharacterController>`
- `GunController : CharacterControllerHelper`
- `ItemsController : CharacterControllerHelper`
- `InputHandlerHelper<T> where T : InputHandler`
- `CharacterInputHandlerHelper : InputHandlerHelper<CharacterInputHandler>`
- `GunInputHandler : CharacterInputHandlerHelper`
- `ItemsInputHandler : CharacterInputHandlerHelper`

Scaling risk:

- Features become tied to one owner type, such as `GunController` requiring `CharacterController`.
- Adding a new controllable entity tends to create a parallel set of base/helper classes.
- Helpers are plain C# objects with manual lifecycle forwarding, so their execution order and ownership are implicit.
- Input is passed through string-keyed dictionaries, which makes feature extraction and renaming fragile.

### Item And Weapon Stack

Current shape:

- `Interactable : Base`
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

### Shared Base And Entity Lookup

Current shape:

- `Base` gives common `EntityRefs` access to controllers, input handlers, inventory, target, ammo, interactables, and items.
- `EntityRefs` dynamically indexes all child components.

Scaling risk:

- `Base` is convenient, but it encourages every gameplay component to inherit from the same root just to access entity lookup.
- `EntityRefs` can become an implicit service locator if components use it for broad cross-feature access instead of focused local dependencies.

### Targeting And Vehicle Responsibilities

Current shape:

- `Target` owns visual targeting state, target picking, legacy LOS behavior, and the Oblique Loft bridge.
- `CarController` owns driving physics, animation, enter/exit possession, visibility changes, parenting, and input handler switching.

Scaling risk:

- These classes are not inheritance-heavy, but they are responsibility-heavy. They should be decomposed through composition before more behavior is added.

## Target Architecture

Prefer capability components over type-specific helper inheritance.

Example target shape for a character entity:

- `EntityRefs`
- `CharacterMotor`
- `FacingAnimator`
- `InteractionSensor`
- `Inventory`
- `WeaponUser`
- `TargetingView`
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
- `EntityRefs` discovers local parts but does not become global ownership.
- Existing controllers remain as compatibility facades during migration.

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
5. Replace `InputHandlerHelper.GetInputs()` dictionary use with typed state accessors.
6. Update `GunInputHandler` to read aim/fire/mouse values from `CharacterInputState`.
7. Update `ItemsInputHandler` to read drop/scroll values from `CharacterInputState`.
8. Remove dictionary keys only after all readers are migrated.
9. Compile and play-test character and car input.

Exit criteria:

- No gameplay code reads `inputs["..."]`.
- Input names are refactor-safe.
- `InputManager` active handler switching still works.

## Phase 2: Explicit Feature Lifecycle

Purpose: make current helper ownership clear before converting helpers into components.

Migration steps:

1. Add small lifecycle interfaces, for example `IEntityFeature`, `IFixedTickFeature`, and `ILateTickFeature`, or use project-specific names like `Tick` and `FixedTick`.
2. Update `ControllerHelper<T>` and `InputHandlerHelper<T>` to implement the lifecycle interface temporarily.
3. Rename forwarded calls in owning classes where useful, or centralize helper ticking through a list.
4. Make `CharacterController` register `GunController` and `ItemsController` in a feature list.
5. Make `CharacterInputHandler` register `GunInputHandler` and `ItemsInputHandler` in a feature list.
6. Compile and verify update order remains unchanged.

Exit criteria:

- Helper lifecycle is explicit.
- Adding/removing a helper does not require custom one-off forwarding code in multiple places.

## Phase 3: Character Capabilities

Purpose: extract character behavior into reusable components.

Migration steps:

1. Add `CharacterMotor` as a MonoBehaviour that owns movement vector, speed, Rigidbody2D velocity, and facing.
2. Have `CharacterController.MoveToward` delegate to `CharacterMotor`.
3. Move animation parameter writing into `CharacterAnimator` or a similarly focused component.
4. Have `CharacterController` delegate animation to the new component.
5. Add `InteractionSensor` or `Interactor` to own nearby interactable tracking.
6. Move `Controller` interactable list behavior into the interaction component for characters.
7. Keep `CharacterController.InteractWith` as a compatibility facade that delegates to `Interactor`.
8. Compile and validate movement, animation, and interaction.
9. After prefab validation, remove duplicated state from `CharacterController`.

Exit criteria:

- Movement, animation, and interaction are independent character components.
- `CharacterController` is thinner and mostly delegates.
- Feature components can be reused by non-player characters without inheriting from `CharacterController`.

## Phase 4: Weapon User And Inventory Input

Purpose: remove character-specific gun/item helper coupling.

Migration steps:

1. Add a `WeaponUser` MonoBehaviour that owns equipped weapon aiming, gun positioning, trigger state forwarding, and shoot height/distance settings.
2. Move `GunController` logic into `WeaponUser` while keeping `GunController` as a temporary adapter if needed.
3. Replace direct `controller.gunController` calls with `EntityRefs.Get<WeaponUser>()` or a focused serialized reference.
4. Add an `InventoryUser` or focused inventory command component for cycle/drop/pickup operations.
5. Move `ItemsController` behavior into `InventoryUser`.
6. Update `GunInputHandler` to call `IAimInputReceiver` and `IFireInputReceiver` instead of `CharacterController.gunController`.
7. Update `ItemsInputHandler` to call `IInventoryInputReceiver`.
8. Update `Item.PickUp` to call an item pickup capability instead of requiring `CharacterController`.
9. Compile and verify aim/fire, pickup/drop, and weapon cycling.

Exit criteria:

- Guns/items are no longer tied to `CharacterControllerHelper`.
- A different entity can use weapons or inventory by adding capability components.
- Shoot height and gun distance are serialized tuning values, not hardcoded helper fields.

## Phase 5: Input Router Composition

Purpose: replace type-specific input helpers with capability routing.

Migration steps:

1. Introduce `PlayerInputRouter` as the active `InputHandler` for player-controlled entities.
2. Let the router translate `InputData` into typed commands.
3. Route commands to local capability interfaces:
   - `IMoveInputReceiver`
   - `IInteractInputReceiver`
   - `IAimInputReceiver`
   - `IFireInputReceiver`
   - `IInventoryInputReceiver`
4. Keep `CharacterInputHandler` as an adapter that forwards to `PlayerInputRouter`.
5. Keep `CarInputHandler` as an adapter or add a `VehicleInputRouter`.
6. Update `InputManager.SetInputHandler` only if the active-handler abstraction needs to become an active-router abstraction.
7. Compile and validate character and car control switching.
8. Remove old input helper classes after all routes are migrated.

Exit criteria:

- Input is routed by capabilities, not concrete entity type.
- Character/car possession still works.
- `GunInputHandler` and `ItemsInputHandler` are deleted or reduced to adapters with no feature logic.

## Phase 6: Vehicle Possession And Movement Split

Purpose: separate car driving from entering/exiting and input ownership.

Migration steps:

1. Add `VehicleMotor` for speed, acceleration, braking, steering bucket, Rigidbody2D velocity, and animation direction.
2. Have `CarController` delegate driving methods to `VehicleMotor`.
3. Add `VehicleSeat` or `PossessableVehicle` for enter/exit.
4. Move driver parenting, visibility, Rigidbody mode switching, and input handler switching into the possession component.
5. Add explicit exit anchors or a simple exit-position strategy.
6. Keep `CarController.Enter` and `CarController.Exit` as compatibility facades during migration.
7. Compile and validate enter, exit, driving, braking, reversing, and animation.
8. Remove duplicated movement/possession state from `CarController`.

Exit criteria:

- Car movement and possession are separate components.
- Possession can be reused for other vehicles or seats.
- `CarController` is thin or removed after prefab migration.

## Phase 7: Item And Weapon Composition

Purpose: avoid deeper weapon inheritance as more weapon behavior is added.

Migration steps:

1. Add data assets or serialized configs for weapon stats: base damage, distance falloff, mag size, fire rate, reload time.
2. Add `EquippableItem` for equip/unequip behavior.
3. Add `WeaponBehaviour` or `DamageDealer` for weapon-level behavior.
4. Add fire-mode components or strategy objects:
   - `SemiAutoFireMode`
   - `FullAutoFireMode`
   - `BurstFireMode`
   - future charge or thrown modes
5. Change `Gun` to delegate trigger behavior to a fire-mode component.
6. Replace `Pistol : SemiAuto` tuning with a pistol config.
7. Update `Inventory` classification to use capabilities such as `IEquippable` or item category data instead of `item is Weapon`.
8. Keep old weapon subclasses as adapters until prefabs are migrated.
9. Compile and verify equip, unequip, fire, and inventory behavior.

Exit criteria:

- New weapons are configured by data and components, not by adding another subclass level.
- Inventory rules do not depend on `Weapon` inheritance.
- Existing weapons still work.

## Phase 8: Targeting Decomposition

Purpose: keep targeting and LOS maintainable while Oblique Loft integration grows.

Migration steps:

1. Add a pure targeting result type that describes chosen object, hit point, blocker, LOS mode, and debug data.
2. Extract target resolution from `Target` into a non-MonoBehaviour service such as `TargetingResolver`.
3. Extract legacy depth/hit-collider LOS into a resolver strategy.
4. Keep Oblique Loft LOS in its existing runtime layer and expose it through a resolver strategy.
5. Let `Target` own display/highlight/debug drawing only.
6. Keep old-vs-new fallback behavior unchanged until sample loft volumes are validated.
7. Compile and validate selected targets, intervening targets, blockers, direct loft targeting, and debug drawing.

Exit criteria:

- `Target` no longer owns all targeting decisions directly.
- Legacy and Oblique Loft LOS are explicit strategies.
- Future LOS comparison tooling is easier to add.

## Phase 9: Reduce `Base` Coupling

Purpose: stop requiring gameplay components to inherit from `Base` just for entity lookup.

Migration steps:

1. Keep `EntityRefs` as the entity-local component index.
2. Add a small extension/helper for components that need local lookup without inheriting from `Base`.
3. Move new components to inherit directly from `MonoBehaviour`.
4. Replace broad `EntityRefs.Get<T>()` access with serialized references for stable required dependencies where practical.
5. Use `EntityRefs` mostly for optional sibling capabilities and prefab migration compatibility.
6. Gradually remove `Base` inheritance from components that do not need shared behavior.
7. Compile and validate prefabs after each group of components is migrated.

Exit criteria:

- `Base` is no longer the default parent for every gameplay component.
- Required dependencies are explicit.
- `EntityRefs` remains useful without becoming hidden global wiring.

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
10. Phase 9: Reduce `Base` Coupling.

The highest-value early migration is typed input state because it lowers risk for every later input and capability change. The highest-value structural migration is extracting `WeaponUser` and `CharacterMotor`, because those remove the strongest coupling from `CharacterController`.

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
