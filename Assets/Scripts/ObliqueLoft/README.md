# Oblique Loft Collider V1

This folder contains the transition-friendly v1 of the custom 2.5D LOS/collision system.

## Relevant Existing Prototype

- `Assets/Scripts/Common/Target.cs` currently owns target selection and LOS adjustment.
- Existing targeting uses `EnclosureCollider`, `DepthCollider`, and `HitCollider` child objects.
- `GunController` supplies the current gun position and shoot height.
- The existing gameplay path is preserved. The new static-blocker system is available beside it through `ObliqueLoftLos`.
- `Target.cs` owns the opt-in gameplay bridge and Oblique Loft debug drawing. Static blocker/direct-target objects need `ObliqueLoftCollider`; shootable characters use `SimpleTarget`.

## Runtime Use

Add `Assets/Prefabs/ObliqueLoftCollider.prefab` or add `ObliqueLoftCollider` to any simple mostly-static object that needs custom 3D logic collision. The component keeps a non-trigger `PolygonCollider2D` synchronized to its footprint, so the footprint is also the solid/selectable 2D ground collider. Keep the old `DepthCollider` and `HitCollider` on existing prefabs while migrating so current gameplay remains unchanged.

The compatibility entry points are:

```csharp
bool clear = ObliqueLoftLos.HasLineOfSight(shooterGround, shootHeight, targetGround, targetHeight);
bool canHit = ObliqueLoftLos.CanHitTargetHeight(shooterGround, shootHeight, targetGround, targetHeight);
bool hit = ObliqueLoftLos.TryGetClosestHit(shooterGround, shootHeight, targetGround, targetHeight, out ObliqueRayHit result);
```

The logic ray uses:

- `Vector3.x`: ground X
- `Vector3.y`: height
- `Vector3.z`: ground/depth Y from the 2D scene

Scene authoring projects logic `(x, y, z)` onto visible 2D as `(x, z + y)`. Local X/depth are transformed through the object's 2D local X/Y basis into logic X/Z, and logic height uses transform Y scale. Unity object Z and transform Z scale are only drawing/sorting concerns; they are not part of logic height or logic depth. Rotation on Z is a geometric rotation of the already-authored volume; it does not recalculate front/back or slice layout.

Oblique Loft collider shapes are authored directly on static blocker/direct-target objects. There is no sprite-frame or `SpriteRenderer.sprite` binding for Oblique Loft colliders.

Shootable characters should use `Assets/Prefabs/SimpleTarget.prefab` or `Assets/Scripts/Common/SimpleTarget.cs`. A `SimpleTarget` has a ground/depth polygon and a flat hit polygon. The prefab includes adjustable `GroundCollider` and `HitCollider` child polygons already assigned to the component. `Target.cs` resolves the selected character and any intervening character with the same distance-sorted simple-target query. Static Oblique Loft objects can be selected directly through their synchronized footprint collider or through a projected generated face under the cursor. Direct targeting maps the cursor through the selected loft's projected generated faces, prefers overlapping face projections that face the shooter, then falls back to footprint inference if needed. The aim line is extended through the loft bounds so the closest generated face on the line is hit. Oblique Loft still checks closer static blockers before that direct target, and nearer faces of the same selected loft can occlude farther faces.

## Authoring V1

1. Add `Assets/Prefabs/ObliqueLoftCollider.prefab` or add an `ObliqueLoftCollider` component.
2. Use `Reset Box` in the custom inspector for a valid starting volume.
3. Edit the footprint and slice points in Scene view.
4. Use `Slice Depth` as the Y-depth on the footprint that the slice belongs to. Slice point Y is its editable visual/local Y in the 2D Scene view; runtime height is derived from `slicePoint.y - Slice Depth`. Slice connectors stem from two distinct lowest slice points: farthest-left/right among the lowest-Y tie set, or the unique lowest point plus the next-lowest point. Middle slice connector handles drag along valid non-horizontal footprint edges. Front/back connectors are locked to the bottom/top of the footprint.
5. Footprint and slice outlines are always visible. The current edit mode only controls which points and edge handles are editable.
6. Use `Footprint Position X/Y` to move every footprint point together, or `Selected Slice Position X/Y` to move every point in the selected slice together. Slice Y movement stops at that slice's depth so the shape stays intact.
7. Points can be click-dragged directly. Dragging snaps at sub-pixel proximity to nearby points and horizontal/vertical alignments in the same footprint or selected slice; arrow-key nudging does not snap.
8. The footprint front and back must each have a horizontal edge, even if it is only the minimum 1px length. Pointy front/back footprint ends are invalid. Dragging or nudging either endpoint of a mandatory front/back edge moves its paired endpoint to the same Y.
9. Keep all slices at the same vertex count. If dragging or nudging a slice point makes that slice self-intersect, the editor repairs only the selected slice by changing its stored line connection order between crossing edges. It does not move point positions, swap point identities, or reorder other slices.
10. Each slice needs distinct farthest-left and farthest-right bottom connector points. They do not have to be neighboring points; the builder canonicalizes each slice from those connectors, keeps the connector lane anchored, then dynamically optimizes the neighboring slice-pair lane order to avoid projected crossing connections, penalize pinched side quads, and favor broader volume-like side quads.
11. Select a middle slice in `Selected Slice`, then use `Remove Selected Middle Slice` to delete it.
12. Use `Rebuild` after manual data edits.

Validation errors appear in the inspector. `Show Generated Face Gizmos` is an optional debug view for translucent normal-colored generated collision face fills; it is separate from the footprint/slice authoring overlays and is off by default for new reset boxes.
Generated faces are wound outward from the generated volume center before classification, so top/bottom/front/back labels follow logic-space normal direction instead of debug draw order or slice winding.

## Migration Notes

- Start Oblique Loft migration with simple static blockers that already have `DepthCollider` and `HitCollider` children.
- Create an `ObliqueLoftCollider` volume that matches the same logical ground footprint and height.
- Add `Assets/Prefabs/SimpleTarget.prefab` to characters and adjust its `GroundCollider` / `HitCollider` child polygons, or add `SimpleTarget` manually and assign current-frame ground and hit polygons.
- Compare old debug lines from `Target.cs` with the targetter-owned Oblique Loft debug ray before routing gameplay calls fully to `ObliqueLoftLos`.
- With `useSimpleTargeting` enabled, `Target.GetActualTarget` uses one simple-target resolver for the selected target and characters in the way. With `useObliqueLoftLos` also enabled, valid static loft colliders can block before each simple target or before the intended endpoint. A selected loft collider can also be hit directly by selecting/aiming at the selected loft's projected generated faces and extending that aim line through its logic bounds; other lofts still block first if closer, and the selected loft's own nearer faces can block farther faces. Blocked Oblique hits use a faded green object highlight and an extra green hit-face overlay.
- Do not migrate cars or complex moving objects to Oblique Loft for normal LOS unless the design changes again.
