# Oblique Loft Collider

## Getting Started

1. Add `Assets/Prefabs/ObliqueLoftCollider.prefab`, or add an `ObliqueLoftCollider` component to a simple mostly-static object that should block or receive custom oblique LOS hits, such as a wall, building, or tree.
2. In the inspector, click `Reset Box` if the collider is not already initialized.
3. Use `Edit Footprint` to edit the yellow-orange ground footprint.
4. Use `Edit Slice` to edit vertical cross-section slices:
   - front slice is cyan,
   - back slice is blue,
   - middle slices are green.
   - `Slice Depth` chooses which Y-depth on the footprint the selected slice belongs to.
   - middle slice connector handles can be dragged along valid non-horizontal footprint edges.
5. Footprint and slice outlines stay visible in both edit modes. The active edit mode only controls which points and edge handles can be changed.
6. Use the `Polygon Position` sliders to move the whole footprint polygon or the selected slice polygon without changing its shape.
7. Click-drag a point directly in Scene view. While dragging, points snap at sub-pixel proximity to nearby points and to nearby horizontal/vertical alignments in the same footprint or selected slice.
8. Use arrow keys to nudge the selected point by one configured step. Use `Shift + Arrow` for a larger nudge. Nudging does not snap.
9. Click an edge midpoint to insert a point.
10. Right-click a point and choose `Delete Point`, or press `Delete` / `Backspace` while a point is selected.
11. Add optional middle slices with `Add Middle Slice` where the vertical profile changes along depth.
12. Author the collider directly on the static blocker object. Oblique Loft collision is not bound to animation frames or `SpriteRenderer.sprite`.
13. To test gameplay targeting, enable `useSimpleTargeting` and `useObliqueLoftLos` on the player targetter's `Target` component. The old targeting path remains the fallback when the selected object has no `SimpleTarget` or direct `ObliqueLoftCollider`.
14. Enable `drawObliqueLoftDebug` on that same `Target` component to draw the current aim ray, closest hit point, hit normal, surface type, face id, hit object label, hit face outline/fill, and candidate/status text. Target and obstacle objects do not need any separate debug component; they only need `ObliqueLoftCollider` to participate in the new hit test.

## Concept

The visual sprite is not the collision truth. `ObliqueLoftCollider` defines a hidden 3D logic volume for a 2D oblique/top-down static blocker or static direct target.

The component is drop-in:

- It requires a `PolygonCollider2D` and creates one if missing.
- The `PolygonCollider2D` is forced to `isTrigger = false`.
- Its path is synchronized to the authored footprint whenever the loft collider rebuilds.
- Use the footprint as the solid ground collision shape; do not maintain a separate physical footprint polygon on the same object.

Logic coordinates:

- `X`: horizontal ground axis.
- `Y`: vertical height.
- `Z`: ground/depth axis, mapped from Unity 2D scene Y.

Scene gizmo display projects logic `(x, y, z)` to Unity scene `(x, z + y, object z)` so vertical height appears upward in the 2D Scene view.

Local X and local depth rotate and scale through the object's 2D local X/Y basis before becoming logic-space `X/Z`. Logical height uses transform Y scale. This means moving, rotating on Z, or scaling the object also moves, rotates, or scales the generated collider volume. The conversion avoids depending on an invertible Unity 3D transform matrix, so 2D prefabs with zero Z scale can still rotate their Oblique Loft volume correctly.

Unity object Z and transform Z scale are only drawing/sorting concerns for handles and gizmos. They are not logical height and they are not logical depth.

Rotation is applied as a plain geometric rotation of the already-authored volume. It does not recalculate which footprint edge is "front" or "back", and it does not reinterpret the slice setup. Author the correct collider for the sprite/frame first; the transform then rotates that authored volume as a whole.

A shot is a logic ray from:

```text
shooter ground position + shoot height
```

to:

```text
target ground position + aimed target height
```

The final hit test intersects that ray with generated polygonal faces. A projected 2D overlap is never enough to count as a hit.

## Authoring Model

Footprint and slices are separate concepts.

- The footprint describes the object's ground/depth shape.
- Footprint point count is independent from slice point count.
- Slices describe vertical cross-sections at different depths.
- In the editor, `Slice Depth` is the Y position on the footprint that the slice uses as its depth source.
- Slice point Y values are editable visual/local Y positions in the 2D Scene view. Runtime logic height is derived from `slicePoint.y - Slice Depth`, so slice points cannot go below their connector depth.
- Each slice draws faded dotted connector lines from two distinct lowest slice points down to the footprint at the same depth, showing where that cross-section belongs on the bird's-eye footprint. If two or more points share the lowest Y, the connector uses the farthest-left and farthest-right points in that lowest-Y set. If only one point is uniquely lowest, the connector uses that point plus the next-lowest point.
- The two bottom connector points define the canonical anchor for generated geometry. They do not have to be neighboring points; if there are intermediate bottom-edge points, the builder chooses the path between connectors that passes through the higher slice profile.
- Generated face shading is separate from the authoring overlays. It draws translucent normal-colored collision face fills, and is controlled by `Show Generated Face Gizmos`.
- Middle slice connector handles move both connector endpoints together. They snap to the footprint's non-horizontal edges and cannot pass the previous or next slice depth.
- The front slice connector is locked to the bottom-most footprint depth. The back slice connector is locked to the top-most footprint depth.
- The bottom/front and top/back of the footprint must each have a horizontal edge. A pointy front or back end is invalid; add even a tiny horizontal edge if needed. Dragging or nudging either endpoint of those mandatory edges also moves its paired endpoint to the same Y so the edge stays horizontal.
- All slices must have the same number of points and the same winding/order.
- Inserting or deleting a point on a slice inserts or deletes that same point index on every slice.
- Dragging or nudging a slice point into a self-intersecting polygon automatically repairs only the selected slice by changing its stored line connection order between crossing edges. Point positions and point identities are not moved, and other slices are not reordered.
- Inserting or deleting a footprint point changes only the footprint.
- Select a middle slice in `Selected Slice`, then click `Remove Selected Middle Slice` to delete it. Front and back slices cannot be removed.
- `Footprint Position X/Y` moves every footprint point together.
- `Selected Slice Position X/Y` moves every point in the currently selected slice together. Downward slice movement stops at the selected slice's depth so the slice polygon shape is preserved instead of clamping individual points.

Simple box-like objects usually need:

- one rectangular footprint,
- one rectangular front slice,
- one rectangular back slice.

The default reset shape uses a rectangular footprint. The front slice depth is the lower footprint edge, and its bottom slice edge sits there. The back slice depth is the upper footprint edge, and its bottom slice edge sits there. The editable front and back slice polygons are stored at those different visual Y positions, and both rise about halfway up the footprint height, making the default box readable immediately in the 2D Scene view.

Objects whose vertical profile changes along depth should add middle slices where the profile changes.

## Simple Targets

Shootable characters should use `SimpleTarget`, not Oblique Loft volumes.

Detailed setup and runtime behavior are documented in `Docs/SimpleTargeting.md`.

`SimpleTarget` represents a flat current-frame target:

- `Ground Collider`: the ground/depth polygon used to find where the ground shot line enters the target.
- `Hit Collider`: the flat visual hit polygon for the current animation frame.
- If either field is left blank, the component tries child objects tagged `DepthCollider` / `HitCollider`, then tagged polygon collider parts discovered through `EntityRefs`.

`Assets/Prefabs/SimpleTarget.prefab` is available as a drop-in version with adjustable `GroundCollider` and `HitCollider` child polygons already assigned to the `SimpleTarget` fields.

`Target.cs` handles the selected character and accidental character interceptors through the same simple-target query:

1. The selected object defines the intended ground endpoint and target height.
2. All enabled valid `SimpleTarget` objects intersecting that ground line are collected.
3. Candidates are sorted by distance from the shooter.
4. Before each candidate is accepted, static `ObliqueLoftCollider` blockers are ray-tested before that candidate.
5. The first unblocked simple target becomes the actual hit. If a static blocker is closer than that target, the blocker becomes the actual hit.
6. If no simple target is hittable, static blockers are checked before the intended endpoint.

The selected character and another character stepping into the shot are not separate systems.

For static Oblique Loft objects, direct targeting is handled by the loft volume itself. Clicking the synchronized footprint collider can select the loft object, and clicking a projected generated face can also select it even when that visible face sits outside the footprint collider. The targetter checks the selected loft's projected generated faces under the cursor and reconstructs an aimed logic-space point on the clicked face. If several projected faces overlap under the cursor, it prefers the face oriented toward the shooter. If no projected face contains the cursor, it falls back to footprint-based ground/height inference. The selected aim line is then extended through the selected loft's logic bounds, and the shot ray tests all valid loft colliders, including the selected one. The closest generated face on that extended line becomes the actual hit. Any other loft collider closer than the selected loft still blocks first, and a nearer face of the selected loft can occlude a farther face on the same object.

## Generated Geometry

Before building faces, every slice is canonicalized from its two bottom connector points:

- the bottom-left connector becomes lane/index `0`,
- the builder chooses a connector-rooted direction and then optimizes the lane order against the previous slice,
- the optimizer keeps the connector lane anchored, treats projected crossing connections as hard failures, penalizes pinched or near-zero side quads, and favors broader volume-like side quads,
- the remaining points continue around the polygon in that same direction.

This makes slice point lanes dynamic and reactive to the authored slice state. Raw dot indices are not treated as faithful cross-slice pairs; connector-rooted optimized lane order is what controls volume construction.

For every neighboring canonical slice pair `A` and `B`, the builder creates one outward-wound quad per matching edge:

```text
A[i], B[i], B[j], A[j]
j = (i + 1) % vertexCount
```

The first and last slices become cap faces. Every generated face is wound outward from the generated volume center before classification. Surface labels come from those outward normals, so upward normals classify as top, downward normals classify as bottom, lower-depth normals classify as front, and higher-depth normals classify as back.

Each generated face stores:

- vertices,
- normal,
- surface type,
- face index,
- owning collider through the hit result.

Surface type is calculated from the face normal:

- mostly upward: `Top` or `SlopedTop`,
- mostly downward: `Bottom` or `SlopedBottom`,
- mostly depth-facing: `Front` / `Back`,
- mostly side-facing: `Side`.

## Runtime API

Use `ObliqueLoftLos` for compatibility-style LOS queries:

```csharp
bool clear = ObliqueLoftLos.HasLineOfSight(
  shooterGroundPosition,
  shootHeight,
  targetGroundPosition,
  targetHeight
);

bool hit = ObliqueLoftLos.TryGetClosestHit(
  shooterGroundPosition,
  shootHeight,
  targetGroundPosition,
  targetHeight,
  out ObliqueRayHit result
);
```

`ObliqueRayHit` includes:

- `Collider`
- `HitObject`
- `Point`
- `Distance`
- `SurfaceType`
- `Normal`
- `FaceIndex`

## Current Gameplay Integration

`Target.cs` has an opt-in bridge:

- `useObliqueLoftLos = false` keeps the old depth/hit collider targeting path.
- `useSimpleTargeting = true` enables the unified simple-target resolver for selected characters and intervening characters.
- `useObliqueLoftLos = true` uses valid static `ObliqueLoftCollider` hits as blockers before simple targets or before the intended endpoint, and allows a selected `ObliqueLoftCollider` object to be hit directly.
- `drawObliqueLoftDebug = true` draws the Oblique Loft debug ray, selected-hit label, status text, and hit face overlay from the targetter itself.
- When Oblique LOS blocks a shot, the resolved hit object uses a faded green Oblique highlight and the hit generated face gets an extra green outline/fill in Scene view.
- The blocked-object sprite highlight is intentionally brighter than a passive tint so a blocked Oblique hit is visible during aiming.
- Hit face overlays resolve the generated face by its stored face id, not by current list position.
- If the new system is disabled, the old system remains the fallback.
- Before raycasting, the targetter rebuilds discovered loft colliders so stale generated faces do not hide authored changes.
- When the selected target has no `SimpleTarget`, the old targeting path remains the fallback.

This preserves the existing targeting system while allowing test objects to use the new logic volume path.

## Current Limitations

- Unity Editor Scene view behavior still needs manual validation.
- `Assets/Prefabs/ObliqueLoftCollider.prefab` is available as a drop-in default box loft, but prefab import and Scene view behavior still need Unity validation.
- Old-vs-new debug comparison is partial: old debug lines and the new targetter-owned Oblique Loft debug drawing both live in `Target.cs`, but there is not yet a dedicated comparison panel.
- The editor is direct-manipulation v1. It is not a full specialized authoring window.
- Geometry assumes valid closed polygons, equal point counts across slices, and two distinct bottom connector points on every slice.
- Current generated geometry is connector-normalized slice-loft geometry: the builder caps the first/last slice, connects optimized matching canonical slice lanes after trying to avoid projected lane crossings and pinched side quads, and winds every generated face outward from the generated volume center before normal classification. The footprint validates depth/shape authoring, but it is not yet used to generate or constrain side surfaces along the full footprint boundary, so non-rectangular footprints or mismatched slice bottom widths can still produce faces that do not match the intended object silhouette.
- `SimpleTarget` frame-profile authoring is manual for now. The component can use tagged depth/hit collider children or explicit collider fields, but there is not yet a dedicated sprite-frame polygon binding component for character hit polygons.
- Oblique Loft has no sprite-frame binding. Author one collider shape directly on each static blocker object.

## Files

- Runtime:
  - `Assets/Scripts/ObliqueLoft/ObliqueLoftCollider.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueLoftSlice.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueLoftFace.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueLoftBuilder.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueRay.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueRayHit.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueRaycaster.cs`
  - `Assets/Scripts/ObliqueLoft/ObliqueLoftLos.cs`
- Editor:
  - `Assets/Scripts/Common/Editor/TargetEditor.cs`
  - `Assets/Scripts/ObliqueLoft/Editor/ObliqueLoftColliderEditor.cs`
- Existing targeting bridge:
  - `Assets/Scripts/Common/Target.cs`
  - `Assets/Scripts/Common/SimpleTarget.cs`
