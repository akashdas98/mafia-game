# Simple Targeting

## Purpose

`SimpleTarget` is the shootable-target side of the LOS system.

Use it for characters and other flat animated targets. Do not model characters as Oblique Loft volumes. Oblique Loft is for simple mostly-static blockers such as walls, buildings, and trees.

The selected target and any character that steps into the ground shot line are resolved by the same simple-target query.

## Components

### SimpleTarget

File: `Assets/Scripts/Common/SimpleTarget.cs`

Add `Assets/Prefabs/SimpleTarget.prefab` to each shootable character prefab, or add `SimpleTarget` manually.

Fields:

- `Use In Targeting`: includes or excludes this object from simple target resolution.
- `Ground Collider`: the ground/depth polygon used to find where the shot line enters the character's ground footprint.
- `Hit Collider`: the flat visual hit polygon used to test the computed impact point.

The drop-in prefab includes:

- `SimpleTarget` root object.
- `GroundCollider` child with a `PolygonCollider2D`, tagged `DepthCollider`.
- `HitCollider` child with a `PolygonCollider2D`, tagged `HitCollider`.
- The root `SimpleTarget` fields already assigned to those child colliders.

Edit the child polygon collider points to fit the character or object. The prefab can be nested under an entity root; `EntityRefs` can discover it automatically.

If `Ground Collider` or `Hit Collider` is blank, `SimpleTarget` tries to resolve them from:

1. Child objects tagged `DepthCollider` and `HitCollider`.
2. Tagged `PolygonCollider2D` parts discovered through `EntityRefs`.

This lets existing character prefabs work with minimal setup as long as their depth and hit collider child objects keep the expected tags.

### Target

File: `Assets/Scripts/Common/Target.cs`

Relevant fields on the player targetter:

- `Use Simple Targeting`: enables the simple-target resolver.
- `Use Oblique Loft Los`: enables static Oblique Loft blocker checks before simple-target hits.
- `Draw Oblique Loft Debug`: draws the current static-blocker ray/hit debug and Scene view labels.

## Shot Resolution

The selected object defines the intended shot:

```text
from = shooter ground position + gun height
to = selected target ground position + aimed target height
```

`Target.cs` then resolves actual impact in this order:

1. Find valid `SimpleTarget` candidates intersecting the intended ground shot line.
2. Include the selected target in the same candidate list.
3. Sort candidates by ground distance from the shooter.
4. For each candidate:
   - compute the impact height at that candidate's ground intersection,
   - test the impact point against the candidate's flat hit polygon,
   - if `Use Oblique Loft Los` is enabled, test static Oblique Loft blockers before that candidate,
   - accept the first unblocked candidate.
5. If no simple target is hittable, test static Oblique Loft blockers before the intended endpoint.
6. If no blocker exists, fall back to the selected target or the old targeting path when no selected `SimpleTarget` exists.

This means there is no separate "interception" system. A target is just a target, whether the player clicked it or it crossed the line.

## Integration Steps

1. Add `Assets/Prefabs/SimpleTarget.prefab` to a character prefab.
2. Move/scale/edit the `GroundCollider` and `HitCollider` child `PolygonCollider2D` shapes.
3. Keep the character's existing `EnclosureCollider`, `DepthCollider`, and `HitCollider` during migration.
4. On the player targetter's `Target` component, enable `Use Simple Targeting`.
5. Add `ObliqueLoftCollider` only to static blockers that should obstruct shots.
6. Enable `Use Oblique Loft Los` on the targetter when static blockers should participate.
7. Aim at one character with another character standing between shooter and target. The nearer unblocked `SimpleTarget` should become the actual target.
8. Place a static Oblique Loft blocker between shooter and candidate. The blocker should win before the candidate.

## Animation Frames

`SimpleTarget` currently uses whatever `PolygonCollider2D` is assigned at runtime.

For current prefabs, this can be the existing `HitCollider`. For per-animation-frame hit polygons, the integration point is still the `Hit Collider` field: update or swap the assigned polygon for the active frame before targeting resolves.

There is not yet a dedicated sprite-frame binding component for `SimpleTarget` hit polygons.

## Debugging

Useful checks:

- The chosen and actual target markers should differ when another valid simple target is closer on the shot line.
- If Oblique Loft blocks the shot, `Draw Oblique Loft Debug` should show the static blocker ray and hit face.
- If a character is not being considered, verify `Use In Targeting`, `Ground Collider`, `Hit Collider`, child collider tags, and the prefab's `EntityRefs` list.

## Current Limitations

- Unity play-mode behavior has not been validated on migrated character prefabs yet.
- Per-frame character hit-polygon binding is not implemented yet.
- Cars and complex moving objects are intentionally ignored by Oblique Loft LOS for now.
- The old targeting path remains as a fallback when the selected object has no `SimpleTarget`.
