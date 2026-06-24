# Animation Adapter Architecture

This document defines the next animation architecture direction for gameplay-driven animation state. It exists so implementation can proceed without re-litigating where animation parameters, gameplay state, and animator plumbing belong.

## Goal

Gameplay components should own gameplay state. They should not know animator parameter names or write animator parameters directly.

Animation adapters should translate gameplay state into animation meaning for a specific entity. Entity animation controllers should coordinate those adapters and perform the final writes to the current animator plumbing.

## Core Shape

```text
Gameplay component
  owns normal gameplay state

Component-specific animation adapter
  reads one gameplay component
  translates that state into animator-facing values

Entity animation controller
  gathers adapters for one entity
  owns final writes to an entity-specific animator target
```

Example for character aim:

```text
WeaponUser
  owns IsAiming, AimDirection, HasEquippedWeapon, IsTriggerHeld

CharacterAimAnimationAdapter
  reads WeaponUser
  writes IsAiming, AimHorizontal, AimVertical, HasWeapon, TriggerHeld

CharacterAnimationController
  invokes character animation adapters
  sends values to AnimatorParameterRelay
```

## Ownership Rules

- Gameplay components expose gameplay state only.
- Gameplay components do not reference animator parameter names.
- Gameplay components do not call `Animator`, `AnimatorParameterRelay`, or animation writers directly.
- Animation adapters may know animator parameter names.
- Animation adapters should be scoped to one meaningful gameplay source or feature area.
- Entity animation controllers own the final animator write path.
- Keep animation controllers entity-specific: `CharacterAnimationController`, `VehicleAnimationController`, future `DoorAnimationController`, etc.
- Do not add a universal animation controller that type-switches over unrelated entity semantics.
- For layered characters, share animator parameters through `AnimatorParameterRelay`; do not force visual layers to mirror one master state/time.

## Interfaces

Use a small contributor interface for adapters:

```csharp
public interface IAnimationParameterContributor
{
  void Contribute(AnimationParameterWriter writer);
}
```

Use a writer wrapper so adapters do not depend on the current animator plumbing:

```csharp
public sealed class AnimationParameterWriter
{
  public void SetBool(string parameterName, bool value);
  public void SetInteger(string parameterName, int value);
  public void SetFloat(string parameterName, float value);
  public void SetTrigger(string parameterName);
  public void ResetTrigger(string parameterName);
}
```

The current character implementation wraps `AnimatorParameterRelay`. Adapter code should remain independent from whether the final target is a relay, a direct `Animator`, or another entity-specific writer later.

## Controller Responsibilities

`CharacterAnimationController` should:

- Own references to the current entity-specific animator writer target.
- Find or serialize character animation adapters.
- Cache adapter references; do not search every frame.
- Invoke adapters in a deterministic order.
- Continue writing stable locomotion parameters during the transition, either directly or through a locomotion adapter.
- Avoid copying every adapter-owned value into one large `CharacterAnimationState`.

The controller should not:

- Own aim logic.
- Own weapon logic.
- Own damage/reload/stealth/action gameplay logic.
- Duplicate every state field exposed by every gameplay component.

## Adapter Responsibilities

Adapters should:

- Read gameplay state from one focused source component.
- Translate that state into animation parameters for the owning entity.
- Be allowed to contain animator parameter names.
- Be optional and removable without breaking gameplay.
- Prefer serialized references with local fallback in `Reset` / `OnValidate` / initialization.

Adapters should not:

- Mutate gameplay state.
- Make gameplay decisions.
- Read raw input.
- Move transforms for gameplay truth.
- Directly call `AnimatorParameterRelay` or `Animator`.

## Example Adapters

### CharacterMovementAnimationAdapter

Source component:

- `CharacterMotor`

Parameters:

- `LastFacing`
- `Horizontal`
- `Vertical`
- `Magnitude`

### CharacterAimAnimationAdapter

Source component:

- `WeaponUser`

Parameters:

- `IsAiming`
- `AimHorizontal`
- `AimVertical`
- `AimAngle`
- `AimBucket`
- `HasWeapon`
- `TriggerHeld`

`AimAngle` is continuous degrees from the logical `AimOrigin` direction, normalized to `0..360`, where `0` is right and angles increase counter-clockwise. `AimBucket` maps that continuous direction to 8 directional animation buckets:

```text
0 = right
1 = up-right
2 = up
3 = up-left
4 = left
5 = down-left
6 = down
7 = down-right
```

### VehicleDrivingAnimationAdapter

Source component:

- `VehicleMotor`

Parameters:

- `Horizontal`
- `Vertical`
- `Driving`

This can be added later if vehicle animation also needs contributor composition. It does not have to be part of the first character-focused implementation.

## Conflict Rules

For the first implementation, avoid conflicts by convention:

- Two adapters should not write the same parameter unless the entity animation controller documents the order.
- If conflicts become useful later, add an explicit priority field or ordered serialized list.
- Default ordering should be deterministic, preferably the serialized adapter list order.

## Suggested First Implementation

Status: implemented in code; Unity validation remains.

1. Done: added `IAnimationParameterContributor`.
2. Done: added `AnimationParameterWriter` that wraps `AnimatorParameterRelay`.
3. Done: added `CharacterMovementAnimationAdapter`.
4. Done: added `CharacterAimAnimationAdapter`.
5. Done: changed `CharacterAnimationController` to cache an ordered list of `MonoBehaviour` adapters implementing `IAnimationParameterContributor`.
6. Done: kept existing locomotion behavior wired through `CharacterMovementAnimationAdapter`.
7. Done: replaced the `writeAimParameters` switch with enabling/disabling `CharacterAimAnimationAdapter`.
8. Done: wired the character prefab with movement and aim adapters. The movement adapter is enabled; the aim adapter is present but disabled by default.
9. Done: `dotnet build Assembly-CSharp.csproj` passes.
10. Unity validation remains separate: verify movement animation, independent layered animator behavior, aiming parameter output, weapon visuals, and car enter/exit.

## Current Character Animator Plumbing

`AnimatorParameterRelay` lives on the character root and broadcasts shared parameters to the visible child layer animators under `Sprites`. The visible body, face, hair, clothing, shoe, and optional weapon child animators each keep their own override controller and state machine. `CharacterAnimationController` writes parameters through `AnimationParameterWriter` to the relay; no master Animator owns or forces child state/time.

This restores the required layer independence: lower-body layers can continue walking while upper-body or weapon layers react to aim/shoot parameters, as long as those layer controllers are authored to ignore or consume the relevant parameters.

Character Builder now keeps animation slots and template controllers layer-scoped. The shared `Assets/Animations/Character/Base/LayerAnimatorTemplate.controller` is only a seed/fallback for newly created layer templates. The active generated structure is:

- `Assets/Animations/Character/Base/LayerAnimatorTemplate.controller`
- `Assets/Animations/Character/Base/Templates/<part-group>Layer.controller`
- `Assets/Animations/Character/Base/Slots/slot_<context>.anim`
- `Assets/Animations/Character/Base/Slots/<part-group>/slot_<part-group>_<context>.anim`
- `Assets/Animations/Character/OverrideControllers/<part-identity>.overrideController`

Generated override controllers use the template for their own `CharacterPartGroup`, so body, face, hair, clothing, shoes, and weapon layers can have different state machines and different slot coverage while still receiving the same shared parameter stream.

When a shared state-machine change should be reapplied to every layer, edit `LayerAnimatorTemplate.controller` using the generic base `slot_<context>.anim` motions, then run `Tools -> Character Builder -> Maintenance -> Rebuild Layer Templates From Seed`. That command recreates the per-layer template controllers from the seed, retargets generic slot motions to each layer's `slot_<part-group>_<context>.anim` placeholders, and rebuilds generated character assets so override controllers point at the refreshed templates.

Editor preview is per layer through Unity's normal Animation/Animator windows. There is no custom master layered preview path.

## Migration Notes

`CharacterAnimationState` can remain temporarily for transition or debugging, but it should stop growing into a copy of every gameplay component's state. New animation features should prefer adapters.

Existing animator parameter names should not change unless the animator controllers are updated in the same pass.

Simple animated objects do not need this architecture. Use adapters only when gameplay-driven animation state needs coordination.

## Acceptance Criteria

- Gameplay components do not gain animator parameter-name fields.
- `CharacterAnimationController` no longer needs direct `WeaponUser` state copying for aim parameters.
- Adding a new animation-affecting component requires adding an adapter, not editing a central state struct for every value.
- Runtime compile passes.
- Docs and architecture notes describe adapters as the intended path for gameplay-driven animation expansion.
