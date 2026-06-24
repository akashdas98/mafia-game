# AimTarget

## Purpose

`AimTarget` is the shooter-side targeting prefab. It owns the player's `Target` component, the visible target marker, the stable logical aim origin, and the visual gun placement point.

It is not a shootable target. Use `SimpleTarget` for characters or objects that can be hit, and use `ObliqueLoftCollider` for mostly-static blockers or direct static targets.

## Files

- Prefab: `Assets/Prefabs/AimTarget.prefab`
- Targeting logic: `Assets/Scripts/Common/Target.cs`
- Weapon integration: `Assets/Scripts/Character/WeaponUser.cs`
- Shootable target docs: `Docs/SimpleTargeting.md`
- Static blocker docs: `Docs/ObliqueLoftCollider.md`

## Prefab Structure

`Assets/Prefabs/AimTarget.prefab` is structured as:

```text
AimTarget
  Marker
  AimOrigin
  GunPoint
```

The root `AimTarget` object has the `Target` component. Keep this root anchored under the character. Runtime target marker rendering must not move the root.

`Marker` has the assigned `SpriteRenderer`. `Target.Render()` moves this child to the resolved target or hit point and toggles its visibility.

`AimOrigin` is the stable logical origin for target lines and Oblique Loft rays. Put it at the character's shoulder or equivalent rotation point. Keep it stable across animation frames unless the real logical shooter origin is intentionally changing.

`GunPoint` is the visual gun placement point. Put it at the hand, muzzle, or current visual hold point. Animation clips can key this child per frame so the equipped weapon follows the sprite art.

## Target Component Fields

On the `Target` component:

- `Sprite`: assign the `Marker` child `SpriteRenderer`.
- `Aim Origin`: assign the `AimOrigin` child transform.
- `Gun Point`: assign the `GunPoint` child transform.
- `Allow Targeting Without Equipped Item`: test-only switch for selecting targets without an equipped item. Trigger pulls still require an equipped gun.
- `Use Simple Targeting`: enables the `SimpleTarget` resolver.
- `Use Oblique Loft Los`: enables static Oblique Loft blocker checks.
- `Draw Oblique Loft Debug`: draws the current Oblique Loft aim ray and hit debug from the targetter.

The prefab has `Sprite`, `Aim Origin`, and `Gun Point` wired by default.

## Runtime Behavior

Real shot and LOS logic is measured from `AimOrigin` to the selected target. The visual gun point does not define the real ray.

`WeaponUser` uses the authored points this way:

- `GetAimOriginPosition()` uses `Target.Aim Origin` when assigned.
- `GetGunVisualPosition()` uses `Target.Gun Point` when assigned.
- `GetGunHeight()` derives shoot height from the authored aim origin relative to the character root when available.
- `SetGunPosition()` places the equipped gun visual at `GunPoint`.

The minimum targeting radius is:

```text
distance(AimOrigin, GunPoint)
```

Clicks and resolved hits inside that radius are ignored. This applies to direct clicks, `SimpleTarget` hits, legacy hit/depth targets, and Oblique Loft face hits. The intent is to prevent targeting inside the character's own arm/gun reach.

## Character Integration

Use the prefab as a child of the character root.

1. Add or keep `Assets/Prefabs/AimTarget.prefab` under the character.
2. Verify the `Target` component has `Marker`, `AimOrigin`, and `GunPoint` assigned.
3. Keep `AimTarget` root positioned so the authored points line up with the character.
4. Ensure the character root's `WeaponUser` has its `Target` field assigned to the child `AimTarget` component.
5. The character's `WeaponUser` initializes the targetter with `target.Initialize(this)`.
6. `PlayerInputRouter` sends aim input to `WeaponUser` through `IAimInputReceiver`.

For the current character prefab setup, the prefab instance should inherit the `Marker`, `AimOrigin`, and `GunPoint` children from `Assets/Prefabs/AimTarget.prefab`. Unity import/prefab validation is still required after the prefab YAML changes.

## Animation Usage

Animate `GunPoint.localPosition` when the hand or gun location changes per animation frame. This is the current integration point for frame-specific visual gun placement.

Do not animate `AimOrigin` for visual polish. Moving it changes real targeting math, shot height, Oblique Loft rays, and the minimum targeting radius. Only animate it if the intended logical shoulder/origin genuinely changes.

Do not put `AimOrigin` or `GunPoint` under `Marker`. `Marker` moves to the target point during aiming, so anything under it would also move away from the character.

Slight visual mismatch is acceptable: `GunPoint` can be a little off the origin-to-target line because sprite art may not be exact. The real target line still uses `AimOrigin` to target.

## Fallbacks

If `Aim Origin` is not assigned, targeting falls back to the old character-position-plus-gun-height origin.

If `Gun Point` is not assigned, gun placement falls back to the old fixed-distance visual point along the aim line.

If `Allow Targeting Without Equipped Item` is enabled, target selection can run without an equipped item for testing. Shooting still requires an equipped gun.

## Relationship To Other Systems

`AimTarget` / `Target` is the targetter. It chooses and displays where the shooter is aiming.

`SimpleTarget` is the shootable-target component. It supplies the current flat hit polygon and authored ground reference line for characters or other flat targets.

`ObliqueLoftCollider` is the static blocker or direct static target volume. `Target` can ray-test it after building a shot from `AimOrigin`.

The selected target and any target crossing the shot line are resolved by `Target.cs`; `AimTarget` is only the shooter-side object that owns that process.

## Common Problems

If the whole `AimTarget` object moves to the clicked target point, the prefab or code path is wrong. Only `Marker` should move.

If the gun appears at the target instead of the character, check that `GunPoint` is not parented under `Marker`.

If close-range targets cannot be selected, check the distance between `AimOrigin` and `GunPoint`. That distance is the no-target radius.

If the target marker is invisible, verify the `Target.Sprite` field points to the `Marker` child's `SpriteRenderer`.

If targeting works without a gun in test scenes but should not in normal play, disable `Allow Targeting Without Equipped Item`.

## Future Direction

`GunPoint` currently lives on `AimTarget` so the character can provide frame-specific visual placement. It can later move to the equipped gun object when weapons own their own per-frame hold/muzzle data. The real shot origin should still remain a stable logical origin supplied to the targetter.
