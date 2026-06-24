# Oblique Loft Collider

`ObliqueLoftCollider` is the custom 2.5D logic-volume collider component for 2D oblique/top-down blockers and static direct targets. Use it for simple mostly-static objects such as walls, buildings, trees, and signs. Shootable characters should use `SimpleTarget`, not Oblique Loft volumes.

## Technical Doc

### Component

File: `Assets/Scripts/ObliqueLoft/ObliqueLoftCollider.cs`

Core fields:

- `Use In Raycasts`: includes or excludes this collider from Oblique Loft LOS/raycast queries.
- `Show Generated Face Gizmos`: opt-in generated-face shading overlay.
- `Footprint`: local ground/depth polygon.
- `Slices`: vertical cross-section polygons at authored slice depths.
- `Generated Faces`: rebuilt logic-volume faces used by ray/face intersection.

Sprite-frame profile fields:

- `Use Sprite Frame Profiles`: enables runtime/editor profile application.
- `Target Sprite Renderer`: the single sprite renderer whose current sprite selects the active frame profile. For the normal workflow, put `ObliqueLoftCollider` on the same GameObject as the `SpriteRenderer` and leave this field blank so it resolves to itself. Assign it only for legacy/separate-object setups.
- `Sprite Frame Profiles`: stored manually authored profiles keyed by the target renderer's current sprite.

### Coordinate Model

Logic coordinates:

- `Vector3.x`: horizontal ground X.
- `Vector3.y`: vertical height.
- `Vector3.z`: ground/depth axis, mapped from Unity 2D scene Y.

Scene visualization projects logic `(x, y, z)` to Unity scene `(x, z + y, object z)` so height appears upward in the 2D Scene view.

Local X and local depth rotate and scale through the object's 2D local X/Y basis before becoming logic-space `X/Z`. Logical height uses transform Y scale. Unity object Z and transform Z scale are drawing/sorting concerns only; they are not logical height or logical depth.

Rotation is applied geometrically to the already-authored volume. It does not recalculate front/back edges or reinterpret slice setup.

### Authoring Model

Footprint and slices are separate concepts.

- The footprint describes the object's ground/depth shape.
- Footprint point count is independent from slice point count.
- Slices describe vertical cross-sections at different depths.
- `Slice Depth` is the Y position on the footprint that the slice belongs to.
- Slice point Y is editable visual/local Y in the 2D Scene view.
- Runtime slice height is `slicePoint.y - Slice Depth`, clamped to zero or above.
- All slices must have the same point count.
- Adding/deleting a slice point affects the same point index on every slice.
- Adding/deleting a footprint point affects only the footprint.

The footprint front and back must each have a horizontal edge. Pointy front/back footprint ends are invalid.

Each slice must have two distinct bottom connector points:

- if multiple points share lowest Y, use the farthest-left and farthest-right points in that lowest-Y set,
- if only one point is uniquely lowest, use that point plus the next-lowest point.

Slice polygons should stay simple/non-self-intersecting. During editor edits, self-intersection repair changes only the selected slice's connection order; it does not move point coordinates or reorder other slices.

### Generated Geometry

Before building faces, every slice is canonicalized from its two bottom connector points:

- the bottom-left connector becomes lane/index `0`,
- the builder chooses a connector-rooted direction,
- neighboring slice-pair lane order is optimized to avoid projected crossings and pinched side quads,
- raw dot indices are not treated as faithful cross-slice lanes.

For each neighboring canonical slice pair `A` and `B`, the builder creates one outward-wound quad per matching edge:

```text
A[i], B[i], B[j], A[j]
j = (i + 1) % vertexCount
```

The first and last slices become cap faces. Every generated face is wound outward from the generated volume center before normal-based classification.

Surface types come from face normals:

- mostly upward: `Top` or `SlopedTop`,
- mostly downward: `Bottom` or `SlopedBottom`,
- mostly depth-facing: `Front` or `Back`,
- mostly side-facing: `Side`.

### Raycast / LOS Runtime

A shot is a 3D logic ray from:

```text
shooter ground position + shoot height
```

to:

```text
target ground position + aimed target height
```

Final collision uses generated polygonal faces and ray/face intersection. A projected 2D overlap is never enough to count as a hit.

Use `ObliqueLoftLos` for compatibility-style queries:

```csharp
bool clear = ObliqueLoftLos.HasLineOfSight(shooterGround, shootHeight, targetGround, targetHeight);
bool hit = ObliqueLoftLos.TryGetClosestHit(shooterGround, shootHeight, targetGround, targetHeight, out ObliqueRayHit result);
```

`ObliqueRayHit` includes:

- `Collider`
- `HitObject`
- `Point`
- `Distance`
- `SurfaceType`
- `Normal`
- `FaceIndex`

### Sprite Frame Profiles

Sprite-frame profiles are optional. Use them only when one object needs different authored Oblique Loft volumes for different current sprites.

Oblique Loft does not have automatic sprite-outline mode. Sprite physics shapes are flat 2D outlines and cannot define footprint depth, slice depths, vertical profiles, or logic height.

The normal setup is one `ObliqueLoftCollider` component on the same GameObject as one `SpriteRenderer`. This keeps the Oblique Loft inspector visible while Unity's Animation window is previewing and scrubbing that sprite object. If a future layered object really needs independent Oblique Loft authoring per visual layer, add separate Oblique Loft collider components to those layer GameObjects instead of using one collider to merge multiple layers.

Profile data is stored in `ObliqueLoftSpriteFrameProfile` and includes:

- current `Sprite`,
- target `SpriteRenderer` reference,
- footprint points,
- cloned slice data, including slice point order.

Profiles are matched by target renderer and sprite first. If no renderer-specific profile is found, the collider can fall back to a sprite-only match for old profile data.

When `Use Sprite Frame Profiles` is enabled:

- the component watches the target sprite renderer's current sprite,
- when the current sprite changes, the editor saves the previous sprite's current shape,
- if the new sprite has a matching profile, it applies that footprint/slice data,
- if the new sprite has no profile yet, it creates one by copying the last live shape,
- Scene view and inspector edits are saved back to the current sprite profile automatically,
- applying a profile calls `Rebuild()`,
- `Rebuild()` regenerates faces and syncs the footprint `PolygonCollider2D`,
- no manual create/capture/apply step is required while authoring.

`Target Sprite Renderer` auto-assignment order:

1. SpriteRenderer on the same GameObject.
2. SpriteRenderer in a parent.
3. SpriteRenderer in a child.

Direct same-object setup is preferred because selecting the animated sprite object shows the real `ObliqueLoftCollider` inspector and Scene handles while scrubbing. Legacy/separate-object setup is still supported: when the target sprite renderer object is selected in the hierarchy, the separate Oblique Loft collider remains visible/editable in Scene view through the bridge overlay, but its inspector will not be the active inspector.

### Source Files

Runtime:

- `Assets/Scripts/ObliqueLoft/ObliqueLoftCollider.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftSpriteFrameProfile.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftSlice.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftFace.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftBuilder.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueRay.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueRayHit.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueRaycaster.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftLos.cs`

Editor:

- `Assets/Scripts/ObliqueLoft/Editor/ObliqueLoftColliderEditor.cs`
- `Assets/Scripts/Common/Editor/TargetEditor.cs`

Targeting bridge:

- `Assets/Scripts/Common/Target.cs`
- `Assets/Scripts/Common/ObliqueTargetingStrategy.cs`

## Integration

### Static Collider Integration

1. Add `ObliqueLoftCollider` directly to the sprite/blocker GameObject. The component is available from `Add Component > Oblique Loft > Oblique Loft Collider`.
2. Click `Reset Box` if the collider has no valid starting shape.
3. Edit the footprint and slices with the Scene handles.
4. Leave `Use Sprite Frame Profiles` disabled.
5. Use the synchronized footprint `PolygonCollider2D` as the object's solid/selectable ground footprint.
6. Do not hand-author a different physical footprint collider for the same loft object.
7. On the player targetter's `Target`, enable `Use Oblique Loft Los` when static blockers should participate.
8. Enable `Draw Oblique Loft Debug` to inspect ray, hit point, hit normal, surface label, face id, and hit object.

### Sprite Profile Integration

1. Add or select the `ObliqueLoftCollider` on the same GameObject as the animated `SpriteRenderer`.
2. Enable `Use Sprite Frame Profiles`.
3. Leave `Target Sprite Renderer` blank when the component is on the same sprite object; it will auto-assign itself. Assign the field only for a legacy/separate-object setup.
4. Scrub Unity's Animation window to the desired frame.
5. Edit footprint and slices with the normal Oblique Loft Scene UI.
6. Keep the sprite object selected while scrubbing so the Oblique Loft inspector and Scene handles stay visible.
7. Scrub to another sprite frame and repeat. A new frame starts from the last seen shape unless it already has its own saved shape.

Layered Oblique Loft objects should be rare. If they exist, add separate Oblique Loft collider components to each sprite layer GameObject that needs its own volume.

### Gameplay Integration

`Target.cs` has an opt-in bridge:

- `useObliqueLoftLos = false` keeps the old depth/hit collider targeting path.
- `useSimpleTargeting = true` enables selected/intervening SimpleTarget resolution.
- `useObliqueLoftLos = true` uses valid static `ObliqueLoftCollider` hits as blockers before simple targets or before the intended endpoint.
- A selected `ObliqueLoftCollider` can be hit directly through its synchronized footprint collider or projected generated faces.
- If sprite-frame profiles are enabled on a loft object, the current matching profile is applied before normal rebuild/raycast use.
- If the new system is disabled or no new-system target exists, the old system remains the fallback.

Shootable characters should use `SimpleTarget`; detailed setup is in `Docs/SimpleTargeting.md`.

## Usage

### Use A Static Oblique Loft Collider

1. Select the sprite/blocker object with `ObliqueLoftCollider`.
2. Click `Reset Box` if needed.
3. Use `Edit Footprint` to edit the yellow-orange ground footprint.
4. Use `Edit Slice` to edit vertical cross-section slices.
5. Add middle slices when the vertical profile changes along depth.
6. Use `Show Generated Face Gizmos` only when inspecting generated face fills.
7. Leave `Use Sprite Frame Profiles` disabled.
8. Aim through the object with `Draw Oblique Loft Debug` enabled and verify the hit face/normal.

### Use Sprite Frame Profiles

1. Select the animated sprite object with `ObliqueLoftCollider`.
2. Enable `Use Sprite Frame Profiles`.
3. Confirm `Target Sprite Renderer` resolves to the same object's `SpriteRenderer`, or assign it only for a legacy/separate-object setup.
4. Scrub Unity's Animation window to the desired sprite frame.
5. Edit the live footprint and slices with the existing Scene UI.
6. Keep the same sprite object selected while scrubbing; the collider inspector remains visible because the component is on that object.
7. Scrub to another sprite frame and repeat. If the frame has no saved shape yet, it copies the last live shape automatically.
8. Use `Delete Current Sprite Profile` only when the current sprite profile should be removed and recreated from the last live shape.

If no profile exists for the current target sprite, authoring creates one from the last live shape. It does not auto-generate a volume from sprite pixels.

### Use Multiple Visual Layers

Do not use one Oblique Loft collider to merge multiple sprite layers. For the rare case where several visual layers each need Oblique Loft behavior, add one Oblique Loft collider component to each layer object.

### Validate Gameplay

1. Enable `Use Oblique Loft Los` on the player targetter's `Target`.
2. Enable `Draw Oblique Loft Debug`.
3. Aim at or through the object on several animation frames.
4. Confirm the correct current sprite profile is applied.
5. Confirm generated face fills, hit labels, and footprint collider match the current profile.
6. Confirm old targeting fallback still works when no direct SimpleTarget or Oblique Loft target is selected.

### Current Limitations

- Unity Editor Scene view behavior still needs manual validation.
- Animation-window scrubbing with sprite-frame profiles needs Unity validation.
- The editor is direct-manipulation v1, not a specialized authoring window.
- Geometry assumes valid closed polygons, equal point counts across slices, and two distinct bottom connector points on every slice.
- The footprint validates depth/shape authoring, but does not yet generate or constrain all side/depth surfaces for complex non-rectangular silhouettes.
- Missing sprite profiles leave the current live loft shape unchanged by design.
- Old-vs-new debug comparison is partial; targetter-owned Oblique debug exists, but there is no full comparison panel.
