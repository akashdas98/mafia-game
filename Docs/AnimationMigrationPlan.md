# Animation Migration Plan

This document describes the intended migration for gameplay animation after the composition foundation work. It covers the character animation issues discussed around `CharacterAnimationController`, `AimTarget`, and animation-frame-specific gun points, and it also accounts for rigid animation code in vehicles.

The goal is not to replace the current layered sprite animation system immediately. The goal is to make animation state explicit, composable, and driven by capability state instead of each gameplay controller manually setting animator parameters every frame.

For the next implementation pass, use `Docs/AnimationAdapterArchitecture.md`. That document is the implementation guide for component-specific animation adapters: gameplay components expose gameplay state, adapters translate that state into animation parameters, and entity animation controllers own the final animator writes.

## Current State

The character prefab is built from multiple sprite layers:

- Body
- Face
- Hair
- Upper clothing
- Lower clothing
- Shoes
- Optional weapon layer

The character prefab has independent visible child animators for the body/clothing layers. Gameplay parameters are written to `AnimatorParameterRelay`, which broadcasts shared parameters to the child layer animators without forcing a shared state or normalized time.

Character Builder generated animation assets are also layer-scoped. The old shared `LayerAnimatorTemplate.controller` remains as a seed/fallback only; generated part override controllers now target `Base/Templates/<part-group>Layer.controller`, and their placeholder slots come from `Base/Slots/<part-group>/slot_<part-group>_<context>.anim`. This lets each layer keep its own authored state machine and only expose the slots it actually needs.

To intentionally reapply shared state-machine edits, edit `LayerAnimatorTemplate.controller` with generic `Base/Slots/slot_<context>.anim` motions, then run `Tools -> Character Builder -> Maintenance -> Rebuild Layer Templates From Seed`. The maintenance command deletes and recreates the per-layer template controllers from the seed, retargets their motions to each layer's slot clips, and rebuilds generated character assets.

Current locomotion flow:

- `CharacterInputHandler` reads movement input into `CharacterInputState`.
- `PlayerInputRouter` routes movement directly to `CharacterMotor`.
- `CharacterMotor.FixedUpdate` applies movement to the Rigidbody2D.
- `CharacterAnimationController.Update` invokes ordered animation adapters and applies their parameters through `AnimatorParameterRelay`.
- `CharacterMovementAnimationAdapter` reads `CharacterMotor` and writes current locomotion parameters:
  - `LastFacing`
  - `Horizontal`
  - `Vertical`
  - `Magnitude`

Current aim/weapon flow:

- `AimTarget` prefab owns `Marker`, `AimOrigin`, and `GunPoint` children.
- `Target` uses `AimOrigin` as the stable logical shot origin.
- `GunPoint` is a visual point for placing the equipped gun or weapon sprite.
- `WeaponUser` owns equipped gun tracking, aim origin / gun point usage, trigger forwarding, and shoot-height/distance tuning directly.
- `WeaponUser` exposes current gameplay aim state. `CharacterAimAnimationAdapter` translates it into animator-facing parameters:
  - `IsAiming`
  - `AimDirection`
  - `AimAngle`
  - `AimBucket`
  - `HasEquippedWeapon`
  - `IsTriggerHeld`

Current vehicle animation flow:

- `VehicleMotor` owns driving state, heading bucket state, and Rigidbody2D velocity.
- Vehicle animation no longer depends on broad vehicle controller animation code; `VehicleMotor` and `VehicleAnimationController` own the driving-animation path.
- `VehicleMotor` implements `IVehicleInputReceiver` and applies vehicle movement from its own `FixedUpdate`.
- `VehicleAnimationController` owns the vehicle `Animator` reference and writes:
  - `Horizontal`
  - `Vertical`
  - `Driving`
- This removes the old direct `CarController.Animate()` coupling for the current vehicle path. Future vehicle animation work should extend the vehicle-specific animation controller instead of putting parameter writes back into the broad gameplay controller.

## Problems To Fix

The old `Animate()` pattern is too rigid because it implies one owner should push every parameter every frame. That works for basic movement or driving, but it becomes limiting once aiming, weapons, hit reactions, reloads, vehicle entry, stealth, crouch, AI state, vehicle damage, doors, sirens, or future object states need animation.

Specific problems:

- Broad character or vehicle controller shells should not be reintroduced as animation decision owners.
- A single method that sets every parameter in `Update()` will become a large switchboard.
- Locomotion animation, vehicle driving animation, aiming pose, weapon pose, and action animation have different sources of truth.
- Aim origin and gun point authoring are animation-related, but logical targeting must still use `AimOrigin`, not the visual `GunPoint`.
- Per-frame gun points may eventually belong to the equipped weapon or animation clip data, but for now they live under `AimTarget`.

## Target Architecture

Animation should be split into three layers of responsibility:

1. State producers
2. Entity-specific animation state aggregation
3. Optional animation-authored point providers

State producers are gameplay capability components. Examples:

- `CharacterMotor` produces movement vector and facing.
- `WeaponUser` produces aiming, equipped weapon, trigger, and weapon visual placement state.
- `VehicleMotor` produces vehicle speed, heading, braking, reversing, and driving state.
- Future `InventoryUser` / `ActionUser` / `DamageReceiver` components can produce action, pickup, reload, hit, or death state.

Animation state aggregation should be owned by focused, entity-specific components, not by broad gameplay controllers. The current `CharacterAnimationController` is the transition point for character animation and now invokes ordered animation adapters. `VehicleAnimationController` now plays the same role for cars.

As animation state grows, do not keep expanding one central state struct with copied fields from every gameplay component. Use component-specific animation adapters instead. A gameplay component such as `WeaponUser` should expose normal gameplay state; a `CharacterAimAnimationAdapter` can translate that state into `IsAiming`, `AimHorizontal`, `AimVertical`, `HasWeapon`, and `TriggerHeld` animator values.

Do not add a universal animation controller component just to wrap animator calls. That layer is too thin to carry its own abstraction right now. Each entity-specific animation controller should directly own the animation parameters and animator references it needs for that entity. Simple animated objects that only play a self-contained clip or a small local Animator state machine do not need this extra component layer. If a later prefab changes from layered child animators to one actual `Animator`, handle that inside that entity's animation controller migration, not through this plan.

Animation-authored point providers are optional. Characters need this for visual points such as `GunPoint`; other entities may need named points for doors, muzzle flashes, wheels, lights, or interact anchors.

## Proposed Components

### CharacterAnimationController

Long-term role:

- Own the current character animation snapshot.
- Pull state from local capabilities.
- Apply only stable, shared character animator parameters.
- Own the character's current animator plumbing, including the `AnimatorParameterRelay` parameter broadcast target.
- Invoke ordered `IAnimationParameterContributor` adapters and own the final writes through `AnimationParameterWriter`.
- Avoid owning gameplay decisions.

It should stay focused on character animation coordination, not movement, targeting, inventory, or weapon logic.

### VehicleAnimationController

Current and long-term role:

- Own the current vehicle animation snapshot.
- Pull or receive heading/speed/driving state from `VehicleMotor`.
- Own the vehicle's animator reference and vehicle-specific parameter writes.
- Avoid owning vehicle physics, input, possession, or steering decisions.

Initial parameters should preserve current behavior:

- `Horizontal`
- `Vertical`
- `Driving`

Do not fold this into `CharacterAnimationController`. Vehicle animation and character animation may both write parameters named `Horizontal` and `Vertical`, but their state semantics are different.

### Entity-Specific Animation Controllers

Use entity-specific controllers for state aggregation:

- `CharacterAnimationController`
- `VehicleAnimationController`
- future `DoorAnimationController`, `BuildingAnimationController`, or `WeaponAnimationController` only when those entities need real animation state logic.

Use this pattern when an entity has gameplay-driven animation state that would otherwise leak into a broad gameplay controller. Examples include characters, vehicles, or interactive objects whose animation depends on movement, aiming, driving, damage, open/closed state, or similar gameplay state. Simple animated objects that only play self-contained animations can keep that behavior on their Animator without adding an entity-specific animation controller.

Do not create one generic `AnimationController` that becomes a type-switch over every entity's state, and do not add a universal animator wrapper unless there is a concrete nontrivial behavior shared by several entity animation controllers.

### CharacterAnimationState

Implemented earlier as a small serializable state object, but no longer the preferred extension point.

Current fields:

- `Vector2 Movement`
- `int LastFacing`
- `bool IsAiming`
- `Vector2 AimDirection`
- `bool HasEquippedWeapon`
- `bool IsTriggerHeld`
- `bool IsReloading`
- `string ActionState` or a small enum for non-locomotion actions

This gives the animation layer one coherent input instead of scattered parameter writes.

Do not grow this type indefinitely. New animation-affecting gameplay features should prefer the adapter pattern in `Docs/AnimationAdapterArchitecture.md`, especially when the source state is already owned by a focused component. The current character path no longer needs this type for movement/aim parameter output.

### CharacterAimPose

Add later if aiming animation becomes more than a boolean/direction.

Likely role:

- Convert target position and origin position into aim direction.
- Provide facing override while aiming.
- Provide weapon/hand pose state to animation.
- Keep visual pose separate from targeting truth.

`AimOrigin` remains the logical rotation/origin point. `GunPoint` remains visual placement only.

### AnimationPointProvider

Add later when `GunPoint` needs to become frame-specific per animation instead of a static child under `AimTarget`.

Possible role:

- Expose named points such as `AimOrigin`, `GunPoint`, `LeftHand`, `RightHand`.
- Resolve current-frame authored points from animation events, sprite metadata, child transforms, or weapon data.
- Let `WeaponUser` ask for visual placement points without owning animator internals.

Do not move to this until the current `AimTarget` child-point approach is validated and the need is concrete.

## Animator Parameters

Current required character locomotion parameters stay:

- `LastFacing`
- `Horizontal`
- `Vertical`
- `Magnitude`

Likely next parameters:

- `IsAiming`
- `AimHorizontal`
- `AimVertical`
- `AimAngle`
- `AimBucket`
- `HasWeapon`
- `TriggerHeld`

Current vehicle parameters stay:

- `Horizontal`
- `Vertical`
- `Driving`

Rules:

- Add parameters only when an animator controller or blend tree consumes them.
- Keep parameter names centralized in animation code.
- Do not let input handlers write animator parameters directly.
- Do not let weapon/item classes write layered character animator parameters directly.
- Do not let vehicle physics/input controllers write vehicle animator parameters directly after `VehicleAnimationController` exists.
- For layered characters, the active parameter-write path is `CharacterAnimationController` -> `AnimationParameterWriter` -> `AnimatorParameterRelay` -> independent child layer animators. Share parameters, not forced state/time.
- For generated layered character assets, keep slot placeholders and layer templates per `CharacterPartGroup`; do not make generated part override controllers depend on one shared slot list.

## Aim Origin And Gun Point Rules

Logical targeting rules:

- Real target lines are measured from `AimOrigin`.
- Obstacle checks and target resolution should continue to use the logical origin and shoot height.
- Anything inside the `AimOrigin` to `GunPoint` radius remains disallowed for targeting.

Visual animation rules:

- `GunPoint` can be slightly off the exact origin-target line because it is visual.
- `GunPoint` should follow animation pose.
- For now, `GunPoint` is authored as a child of `AimTarget`.
- Later, `GunPoint` can be supplied by an equipped weapon or frame-specific animation point provider.

The animation migration must preserve this split. Do not derive logical shot rays from the visual weapon point.

## Migration Phases

### Phase A: Stabilize Current Character Locomotion Extraction

Status: superseded by adapter implementation; Unity validation remaining.

Steps:

1. Done: keep `CharacterMotor` as the movement source.
2. Done: keep `CharacterAnimationController` applying locomotion parameters through `AnimatorParameterRelay`.
3. Validate the character prefab in Unity after text wiring.
4. Confirm layered body/clothing animators still receive movement parameters.
5. Confirm entering/exiting cars does not leave stale movement animation.

Exit criteria:

- Movement and idle animation match pre-migration behavior.
- Layered animators receive shared locomotion parameters while keeping independent state machines.
- No runtime auto-added animation components are needed on the character prefab.

### Phase B: Introduce Explicit Character Animation State

Status: code-complete, Unity validation remaining.

Completed steps:

1. Added `CharacterAnimationState` during the first migration pass.
2. Superseded central movement/aim state copying with component-specific animation adapters.
3. Existing locomotion parameter output is intended to stay unchanged through `CharacterMovementAnimationAdapter`.
4. Removed the old external `SetMovement` transition call.
5. Runtime compile passes.

Remaining validation:

1. Play-test movement and idle behavior.

Exit criteria:

- Character movement animation has one focused adapter input.
- Existing animator controllers do not need to change yet.
- The old `CharacterController.Animate` transition method is gone.

### Phase C: Add Aim Animation State

Status: code-complete for adapter-based aim parameter output; animator consumption remains opt-in.

Completed steps:

1. `WeaponUser` exposes whether the character is aiming.
2. `WeaponUser` exposes aim direction from `AimOrigin` to the current aim point.
3. `CharacterAimAnimationAdapter` writes aim parameters when that component is enabled.
4. The character prefab includes the aim adapter disabled by default until animator controllers consume those parameters.
5. `GunPoint` remains visual placement only.

Exit criteria:

- Aiming can affect animation without changing targeting math.
- Aim state is broadcast through `AnimatorParameterRelay` to visible layered animators, which may consume or ignore it independently.
- Input handlers still do not touch animators.

### Phase D: Extract Vehicle Animation

Status: code-complete for the current car path, Unity validation remaining.

Completed steps:

1. `VehicleAnimationController` exists.
2. The old output is preserved: heading becomes `Horizontal` / `Vertical`, and nonzero speed becomes `Driving`.
3. The vehicle animation controller reads state from `VehicleMotor`.
4. The vehicle animator reference is owned by `VehicleAnimationController`.
5. Car root rotation behavior is unchanged: runtime root Z rotation stays zero.

Remaining validation:

1. Play-test driving, braking, reversing, and idle animation.
2. Confirm prefab serialized references survive Unity import.

Exit criteria:

- `CarController` no longer writes animator parameters directly.
- Vehicle animation state is reusable for other vehicle prefabs.
- Character animation and vehicle animation follow the same component pattern without sharing one broad state controller.

### Phase E: Frame-Specific Visual Points

Status: deferred by design until a real animation needs per-frame hand/gun offsets.

Steps:

1. Keep `AimTarget.GunPoint` until it becomes insufficient.
2. Add `AnimationPointProvider` only when a real animation needs per-frame hand/gun offsets.
3. Make `WeaponUser` ask the provider for visual gun placement.
4. Fall back to `AimTarget.GunPoint` if no provider exists.
5. Keep `AimOrigin` stable for logic.

Exit criteria:

- Visual weapon placement can change by animation frame.
- Targeting ray origin stays stable.
- Weapon visuals can eventually move out of `AimTarget` without changing target resolution.

### Phase F: Remove Compatibility Animation Routing

Status: code-complete for current direct animation calls and broad-controller shell removal; Unity validation remaining.

Completed steps:

1. The old `CharacterController` shell has been removed from the active runtime path.
2. The old `CarController` shell has been removed from the active runtime path.
3. Entity-specific animation controllers tick through normal `MonoBehaviour.Update`.
4. Duplicated movement/facing/heading animation state was removed from compatibility code.
5. Movement, possession, weapon, inventory, and interaction ticking now live on their focused capability components.

Exit criteria:

- Character animation timing and parameter writes belong to `CharacterAnimationController`.
- Vehicle animator parameter writes belong to `VehicleAnimationController`.
- Animation behavior is reusable by player characters, NPCs, scripted characters, and vehicles without one giant generic controller.

## Non-Goals For This Migration

- Do not rebuild all animator controllers immediately.
- Do not move Oblique Loft or SimpleTarget collision truth into sprite animation.
- Do not bind Oblique Loft collider shapes to animation frames.
- Do not replace the layered clothing/body sprite setup.
- Do not introduce per-frame gun point metadata until a concrete animation requires it.
- Do not make a single generic animation controller that owns every entity's semantics.
- Do not add entity-specific animation controller components to simple animated objects that can be handled by their own Animator or clip without gameplay-state coordination.
- Do not introduce a master Animator that forces all character layers into one common state/time. Keep character animation writes entity-specific through `CharacterAnimationController` and broadcast only shared parameters through `AnimatorParameterRelay`.

## Validation Checklist

Run after each animation migration step:

1. `dotnet build Assembly-CSharp.csproj`.
2. Open the character prefab in Unity and confirm serialized references.
3. Confirm the character root has `CharacterMovementAnimationAdapter` enabled and listed before `CharacterAimAnimationAdapter` on `CharacterAnimationController`.
4. Enable `CharacterAimAnimationAdapter` only when the layered animator controllers have matching aim parameters.
5. Play-test idle and walking in all movement directions.
6. Confirm every visible character layer receives shared parameters and can keep independent state/time.
7. Aim with and without an equipped weapon if test targeting is enabled.
8. Confirm the logical target ray starts from `AimOrigin`.
9. Confirm the equipped gun visual follows `GunPoint`.
10. Confirm targets inside the `AimOrigin` to `GunPoint` radius are rejected.
11. Enter and exit a car, then confirm movement animation resets correctly.
12. Drive a car and confirm `Horizontal`, `Vertical`, and `Driving` behavior matches the pre-migration animator output.

## Relationship To Composition Migration

This animation migration belongs after the current composition foundation:

- Phase 1 typed input state keeps animation independent from raw input names.
- Phase 2 explicit helper lifecycle makes update ordering visible.
- Phase 3 `CharacterMotor` gives animation a clean movement source.
- Phase 4 `WeaponUser` gives animation a clean aim/weapon source.
- Phase 6 `VehicleMotor` gives vehicle animation a clean speed/heading/driving source.

Do the Unity prefab validation for these phases before deeper animation work. Otherwise animation bugs may be prefab wiring problems rather than architecture problems.
