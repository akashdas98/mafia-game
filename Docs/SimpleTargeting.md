# Simple Targeting

`SimpleTarget` is the shootable-target side of the LOS system. Use it for characters and other flat animated targets. Do not model characters as Oblique Loft volumes; Oblique Loft is for simple mostly-static blockers such as walls, buildings, and trees.

The selected target and any character whose hit face crosses the visual shot line are resolved by the same simple-target query.

## Technical Doc

### Components

`SimpleTarget`

File: `Assets/Scripts/Common/SimpleTarget.cs`

Fields:

- `Use In Targeting`: includes or excludes this object from simple target resolution.
- `Ground Line Local Y`: local Y position used as the target's ground reference line.
- `Hit Collider`: the flat visual hit polygon used for simple target selection and interception.
- `Sprites Root`: optional explicit `Sprites` transform used by the editor `Auto Detect Current Sprite Outlines` button. Leave blank to auto-resolve.
- `Excluded Shape Renderers`: optional sprite renderers to ignore during auto-detection, such as shadows, VFX, selection rings, or decorative layers.
- `Apply Layer Profiles Automatically`: applies stored child layer profiles to the `Hit Collider` when current layer sprites change.

`SimpleTargetLayer`

File: `Assets/Scripts/Common/SimpleTargetLayer.cs`

Fields:

- `Simple Target`: parent target that owns the combined `Hit Collider`.
- `Sprite Renderer`: sprite layer renderer this component tracks.
- `Sprite Profiles`: per-sprite alpha-outline paths detected for this layer.

The parent SimpleTarget inspector automatically adds/configures `SimpleTargetLayer` components on active child sprite renderer objects under `Sprites`. Selecting a sprite layer object keeps the layer inspector visible, including the per-layer auto-detect buttons, and shows only that layer's current sprite profile outline in Scene view. Parent/root selection still shows the combined SimpleTarget hit collider outline and ground line.

`Target`

File: `Assets/Scripts/Common/Target.cs`

Relevant player targetter fields:

- `Aim Origin`: stable authored transform used as the logical shot-line origin, such as the character's shoulder.
- `Gun Point`: visual gun placement transform. Animation clips can move this per frame; logic still uses `Aim Origin`.
- `Allow Targeting Without Equipped Item`: test-only switch that allows aim/target selection without an equipped gun.
- `Use Simple Targeting`: enables the simple-target resolver.
- `Use Oblique Loft Los`: enables static Oblique Loft blocker checks before simple-target hits.
- `Draw Oblique Loft Debug`: draws the current static-blocker ray/hit debug and Scene view labels.

### Hit Shape

At runtime, `SimpleTarget` uses the assigned/tagged `PolygonCollider2D`.

- Resolution order is the assigned `Hit Collider`, then a child tagged `HitCollider`, then a tagged polygon collider in the owning root hierarchy.
- Every `PolygonCollider2D` path is treated as part of the flat hit shape.
- Path points are transformed from collider-local space to world space.
- The ground line spans the combined world-space min/max X across all paths.
- Ground Y is transformed through the hit collider transform, preserving the previous behavior.

The editor can bake the current sprite appearance into the `Hit Collider`.

`Auto Detect Current Sprite Outlines`:

- resolves the owning `Sprites` root,
- ensures child `SimpleTargetLayer` components exist on included sprite renderer objects,
- collects all active, enabled child `SpriteRenderer` components under that root,
- ignores renderers that are disabled, inactive in hierarchy, have no current sprite, or are listed in `Excluded Shape Renderers`,
- traces exact opaque alpha-pixel boundaries from the current sprite textures,
- stores each layer's result as a per-sprite profile on that layer's `SimpleTargetLayer`,
- applies all matching current layer profiles into the parent `Hit Collider`,
- does not boolean-union layered sprite paths,
- does not require separate per-sprite shape authoring.

After auto-detection, the generated `Hit Collider` is normal authored collider data. You can edit it manually, and runtime SimpleTarget queries do not keep reading sprite pixels.

`Auto Detect All Animator Frames`:

- scans each `SimpleTargetLayer`'s local `Animator`,
- reads all sprite keyframes targeting that layer's `SpriteRenderer`,
- traces and stores profiles for every sprite used by those clips,
- applies the profile matching the current sprite combination to the parent `Hit Collider`.

`Sprites` root resolution order:

1. Explicit `Sprites Root` if assigned.
2. A descendant named `Sprites` under the `SimpleTarget`.
3. A descendant named `Sprites` found by walking up the owner hierarchy.
4. If no `Sprites` root is found, child sprite renderers under the `SimpleTarget` object itself.

### Hit Tests

`SimpleTarget` exposes `TryGetHitPaths(out List<Vector2[]> paths)`.

Targeting methods operate over all current paths:

- `ContainsHitPoint`: true if the point is inside any current path.
- `TryGetFirstHitPolygonIntersection`: checks every edge of every current path and returns the closest intersection from the shot origin.
- `TryGetGroundBaseline`: computes the current ground line from the current path points.

This lets layered sprite detections behave as one combined target without merging the polygons internally.

### Ground Baseline

`Ground Line Local Y` is shared across all frames.

- ground Y comes from the hit collider transform,
- line width comes from the hit collider paths' combined min/max world X.

When you scrub an animation and press `Auto Detect Current Sprite Outlines`, the baked collider paths and baseline width update to the current frame/layer combination. The baseline height remains the authored `Ground Line Local Y`.

### Shot Resolution

The selected object defines the intended shot:

```text
from = aim origin
to = selected target ground position + aimed target height
```

`Target.cs` resolves actual impact in this order:

1. Find valid `SimpleTarget` candidates intersecting the intended visual shot line.
2. Include the selected target in the same candidate list.
3. Sort candidates by ground distance from the shooter.
4. For each candidate:
   - compute the hit-face impact point,
   - intersect the shooter ground line with the candidate's ground reference line,
   - derive hit height from the visual impact point above that ground point,
   - reject impacts inside the `Aim Origin` to `Gun Point` minimum radius,
   - if `Use Oblique Loft Los` is enabled, test static Oblique Loft blockers before that candidate,
   - accept the first unblocked candidate.
5. If no simple target is hittable, test static Oblique Loft blockers before the intended endpoint.
6. If no blocker exists, fall back to the selected target or old targeting path when no selected `SimpleTarget` exists.

There is no separate interception system. A target is just a target, whether the player clicked it or it crossed the line.

### Editor Preview And Gizmos

There is no custom SimpleTarget animation scrubber. Unity's Animation window changes current `SpriteRenderer.sprite` values while previewing; press `Auto Detect Current Sprite Outlines` to bake that current preview frame into the `Hit Collider`.

When the `SimpleTarget`, one of its children, an owning parent, a linked `SimpleTargetLayer`, or an included sprite layer renderer under `Sprites` is selected, Scene gizmos draw:

- the current flat hit face paths as blue outlines,
- the authored ground reference line in yellow-orange.

If `Apply Layer Profiles Automatically` is enabled and the current layer sprites have stored profiles, the Scene view drawer applies those profiles before drawing. This lets the visible collider follow Animation-window scrubbing while a child sprite layer object is selected.

When a `SimpleTargetLayer` object is selected, Scene view shows only that layer's current sprite profile outline. Dragging those points edits that layer's stored profile and reapplies the combined parent `Hit Collider`.

The custom SimpleTarget inspector shows:

- whether `Sprites` was found,
- how many sprite renderers are currently included,
- how many current `Hit Collider` paths are active,
- `Auto Detect Current Sprite Outlines` and `Auto Detect All Animator Frames` buttons.

Each `SimpleTargetLayer` inspector shows:

- parent target,
- tracked sprite renderer,
- current sprite,
- stored profile count,
- whether the current sprite has a profile,
- `Auto Detect For Current Sprite`,
- `Auto Detect For All Animator Frames`.

### Source Files

Runtime:

- `Assets/Scripts/Common/SimpleTarget.cs`
- `Assets/Scripts/Common/SimpleTargetingStrategy.cs`
- `Assets/Scripts/Common/Target.cs`

Editor:

- `Assets/Scripts/Common/Editor/SimpleTargetGizmoDrawer.cs`
- `Assets/Scripts/Common/Editor/TargetEditor.cs`

## Integration

### Manual Hit Collider Integration

1. Add `Assets/Prefabs/SimpleTarget.prefab` to a character prefab, or add `SimpleTarget` manually.
2. Assign `Hit Collider`, or keep a child polygon tagged `HitCollider`.
3. Edit the `PolygonCollider2D` points to fit the target.
4. Set `Ground Line Local Y`.
5. Keep existing `EnclosureCollider`, `DepthCollider`, and `HitCollider` objects during migration.
6. On the player targetter's `Target`, enable `Use Simple Targeting`.
7. Add `ObliqueLoftCollider` only to static blockers that should obstruct shots.
8. Enable `Use Oblique Loft Los` on the targetter when static blockers should participate.

### Sprite Outline Auto-Detect Integration

Use the auto-detect button for layered animated characters when drawing the initial SimpleTarget polygon by hand would be too much authoring work.

1. Make sure the character has a child GameObject named exactly `Sprites`.
2. Put all visual sprite-layer children under `Sprites`, such as `Body`, `Face`, `Hair`, `UpperClothing`, `LowerClothing`, `Shoes`, and `Weapon`.
3. Make sure each layer that should contribute to the target outline has a `SpriteRenderer`.
4. Add `SimpleTarget` to the character or a stable child object.
5. Assign `Hit Collider`, or let auto-detect create one if none exists.
6. Leave `Sprites Root` blank if auto-resolve finds the `Sprites` child, or assign it explicitly.
7. Add shadows, VFX, selection markers, or other non-body renderers to `Excluded Shape Renderers` if they live under `Sprites` but should not count as hit area.
8. Select the parent `SimpleTarget` once so it can attach missing `SimpleTargetLayer` components under `Sprites`.
9. Scrub Unity's Animation window to the frame you want to bake.
10. Click `Auto Detect Current Sprite Outlines`, or select one sprite layer and click `Auto Detect For Current Sprite`.
11. Use `Auto Detect All Animator Frames` on the parent or child layer when you want to pre-generate profiles from animator clips.
12. Select a sprite layer and edit that layer's current outline points if needed, or select the parent/root to preview the combined result.
13. Set `Ground Line Local Y` once.

### AimTarget Integration

Use the `AimTarget` prefab's built-in `AimOrigin` and `GunPoint` children, or assign equivalent transforms manually.

- `AimOrigin` is the stable logical shot origin.
- `GunPoint` is only visual weapon placement and can move per frame.
- Do not derive logic rays from `GunPoint`.
- The radius from `AimOrigin` to `GunPoint` is the minimum targeting radius.

Dedicated setup notes for that prefab are in `Docs/AimTarget.md`.

## Usage

### Use Manual SimpleTarget

1. Select the object with `SimpleTarget`.
2. Assign or edit the `HitCollider` polygon.
3. Set `Ground Line Local Y` to the visual ground/reference height.
4. Select the object, child, or owning parent in Scene view to see the combined hit outline and ground line. Select a `SimpleTargetLayer` object to see only that layer's current outline.
5. Aim directly at the target and through another target to validate selected/intervening behavior.

### Auto-Detect From Layered Sprites

1. Select the object with `SimpleTarget`.
2. Assign `Sprites Root` only if auto-resolve does not find `Sprites`.
3. Add decorative renderers to `Excluded Shape Renderers` if they should not count as hittable shape.
4. Scrub Unity's Animation window to the frame you want to capture.
5. Click `Auto Detect Current Sprite Outlines` on the parent, or select a layer under `Sprites` and click `Auto Detect For Current Sprite`.
6. To pre-generate all layer profiles from animation clips, click `Auto Detect All Animator Frames` on the parent or on each layer.
7. Select a sprite layer while scrubbing; if that frame already has stored profiles, confirm the blue SimpleTarget outline follows the current frame.
8. Select a sprite layer and edit that layer's current outline points if needed, or select the parent/root to preview the combined result.
9. Adjust `Ground Line Local Y` once so the baseline sits at the desired ground height.

Auto-detect stores profiles on child `SimpleTargetLayer` components and applies matching current profiles into the parent `HitCollider`. Manual edits to the collider can be preserved by not re-running detection for that sprite, or by treating the edited collider as the current authored shape.

### Validate Gameplay

1. Enable `Use Simple Targeting` on the player's `Target`.
2. Aim directly at an animated target.
3. Aim through one target toward another target.
4. Confirm the closest unblocked SimpleTarget is selected.
5. With `Use Oblique Loft Los` enabled, place a loft blocker between shooter and target and confirm the blocker wins.
6. If an auto-detected character is not considered, verify the generated `Hit Collider` paths and `Use In Targeting`.

### Current Limitations

- Unity play-mode behavior has not been validated on migrated character prefabs yet.
- Unity Animation-window preview plus auto-detect behavior needs manual validation on real layered character prefabs.
- Auto-detect uses opaque texture pixels exactly. This can create many collider points on detailed sprites.
- Cars and complex moving objects are intentionally ignored by Oblique Loft LOS for now.
- The old targeting path remains as a fallback when the selected object has no `SimpleTarget`.
