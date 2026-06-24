# Animation Frame Hitbox Implementation

## Goal

Implement animation-frame-aware hitboxes using Unity's existing animation preview/scrubbing flow.

Status: implemented in code for parent/child `SimpleTarget` layer auto-detect profiles and optional single-target-renderer `ObliqueLoftCollider` sprite profiles; Unity Editor Animation-window validation is still required.

The user should not need a separate hitbox timeline. The user should be able to select an object, open Unity's normal Animation window, scrub to a sprite frame, and see/edit the relevant hitbox shape for that current frame.

There are three target surfaces:

- normal Unity colliders,
- `SimpleTarget`,
- `ObliqueLoftCollider`.

Keep these systems separate. Do not make `SimpleTarget` depend on Oblique Loft, and do not make normal Unity physics colliders depend on `SimpleTarget`.

## Important Design Rules

- The current project is Unity `6000.3.16f1`.
- Runtime logic must not depend on `UnityEditor`.
- Sprite art is not collision truth for Oblique Loft. Oblique Loft profiles are still authored logic volumes.
- `SimpleTarget` auto-detect is the exception: child `SimpleTargetLayer` helpers can trace current sprite alpha pixels because `SimpleTarget` is a flat visual hit face.
- Do not create a new timeline/editor window for v1. Use Unity's Animation window as the scrubber.
- Keep the existing `SimpleTarget` and `ObliqueLoftCollider` Scene UI. The visible/editable shape should change when the preview/current sprite frame changes.
- Layered character sprites are independent animators. Collision must be driven from the `Sprites` child hierarchy as a combined current-frame visual shape, not from one forced master animator.
- Do not reintroduce a master Animator, layered state sync, broad controller shell, service locator, or dynamic entity index.

## Object Hierarchy Convention

Anything with visible sprites should have a child GameObject named exactly:

```text
Sprites
```

For frame hitbox logic:

- `SimpleTarget` tracks the children under this `Sprites` object.
- `ObliqueLoftCollider` should normally be attached directly to the same GameObject as the one sprite renderer it tracks. `Target Sprite Renderer` exists for legacy/separate-object setups, but direct same-object placement keeps the normal inspector visible during Animation-window preview/scrubbing.
For `SimpleTarget`, the implementation should auto-find `Sprites` from the owning object hierarchy when the explicit field is blank. Search should be local to the owning object. Do not scan the whole scene to find a `Sprites` object for a component.

Suggested `SimpleTarget` resolver:

1. If serialized `spritesRoot` is assigned, use it.
2. Else search the component's children for a transform named `Sprites`.
3. Else search the component's parent/owner hierarchy for a child named `Sprites`.
4. Else fall back to sprite renderers under the component object itself.

## Normal Unity Colliders

For normal colliders, prefer Unity's built-in Sprite Custom Physics Shape workflow.

Do not build a custom normal-collider authoring system in v1 unless normal colliders fail to update in a concrete prefab.

Expected behavior:

- Artists/users can edit a sprite's Custom Physics Shape in Unity's Sprite Editor.
- Normal physics colliders can use Unity's sprite physics shape features.
- If a later prefab needs runtime collider syncing from the current sprite, add a small separate component such as `SpritePhysicsShapeColliderSync`. Do not fold that into `SimpleTarget`.

Do not block the `SimpleTarget` or Oblique Loft work on normal collider automation.

## SimpleTarget Auto-Detect Bake

SimpleTarget uses the current `HitCollider` at runtime:

- `SimpleTarget` uses the assigned/tagged `PolygonCollider2D`.
- `Ground Line Local Y` works as it does today.
- Existing prefabs keep working.

The editor adds an `Auto Detect Current Sprite Outlines` button:

- It reads all active `SpriteRenderer` children under the resolved `Sprites` object.
- It traces each renderer's current `Sprite` opaque alpha-pixel outline.
- It stores those outline points as per-sprite profiles on child `SimpleTargetLayer` components.
- It applies matching current layer profiles into the normal parent `PolygonCollider2D`.
- After baking, users can edit the collider manually.
- It does not require separate per-sprite shape authoring.

Each sprite layer object under `Sprites` can also have a `SimpleTargetLayer` helper with:

- `Auto Detect For Current Sprite`
- `Auto Detect For All Animator Frames`

The all-frames button scans that layer's local Animator clips for SpriteRenderer sprite keyframes and stores profiles for every sprite it finds.

### Included Renderers

For auto-detect, include all relevant sprite renderers under `Sprites`.

A renderer should be included only if:

- the renderer exists,
- the renderer is active in hierarchy,
- the renderer is enabled,
- the renderer has a non-null `sprite`.

Add an optional serialized exclusion list only if easy:

```csharp
[SerializeField] private SpriteRenderer[] excludedShapeRenderers;
```

This allows excluding shadows, VFX, selection rings, or purely decorative layers later.

Do not require the user to assign every layer manually. The default should cover all child sprite renderers under `Sprites`.

### Reading Sprite Alpha Outlines

Use the current sprite texture:

- `sprite.texture`
- `sprite.rect`
- `sprite.pivot`
- `sprite.pixelsPerUnit`

Trace the boundary edges of opaque alpha pixels and preserve each closed outline loop as a separate path.

Do not boolean-union the paths. That is unnecessary and risky.

SimpleTarget hit logic should work over multiple paths:

- `ContainsHitPoint(point)`: true if the point is inside any path.
- `TryGetFirstHitPolygonIntersection(from, to)`: test every path edge and return the nearest intersection.
- `TryGetGroundBaseline(...)`: compute min/max X across every point in every included path.
- Gizmo drawing: draw every path.

### Sprite Local To Collider Transform

Sprite alpha outline points are converted into sprite renderer local space using the sprite pivot and pixels-per-unit.

For each outline point:

```csharp
Vector2 world = renderer.transform.TransformPoint(localPoint);
Vector2 colliderLocal = hitCollider.transform.InverseTransformPoint(world);
```

Write the resulting collider-local points into `PolygonCollider2D.SetPath`.

### Missing Sprite Alpha Outlines

If a sprite has no usable opaque pixels:

- skip that sprite for auto-detection,
- optionally log or expose a validation warning in the inspector,
- do not generate a bounding rectangle automatically unless the user asks for that later.

Reason: SimpleTarget auto-detect should follow the current visible sprite pixels without separate shape authoring.

If all included sprites have no usable opaque pixels, the button should leave the current collider unchanged and show a warning.

### Ground Line After Auto-Detect

`Ground Line Local Y` remains one shared setting across all frames.

After baking, the ground line width comes from all `HitCollider` paths:

```text
left.x  = lowest world X of all current hit collider path points
right.x = highest world X of all current hit collider path points
left.y/right.y = world Y of Ground Line Local Y
```

This must work for both single-sprite and layered-sprite objects.

For layered sprites:

- body, clothing, hair, face, shoes, etc. all contribute to min X and max X if they are included renderers,
- the line spans the combined current frame/layer width,
- the line height does not jump per sprite frame,
- the line height is still authored once by `Ground Line Local Y`.

For the ground Y conversion, keep the existing mental model: `Ground Line Local Y` is local to the SimpleTarget/hitbox setup. A practical implementation can use:

```csharp
float groundY = transform.TransformPoint(new Vector2(0f, groundLineLocalY)).y;
```

Keep the old hit-collider transform conversion for ground Y.

### SimpleTarget API Shape

Refactor `SimpleTarget` internally so targeting code does not care where paths came from.

Add an internal/current-frame path method:

```csharp
public bool TryGetHitPaths(out List<Vector2[]> paths)
```

Behavior:

- returns every path from the current `HitCollider`.

Then update existing methods to use paths:

- `TryGetHitPolygon` may remain for compatibility, but new code should use `TryGetHitPaths`.
- `ContainsHitPoint`
- `TryGetFirstHitPolygonIntersection`
- `TryGetGroundBaseline`

Do not rewrite `SimpleTargetingStrategy` unless necessary. It should keep calling the same public methods if possible.

### SimpleTarget Scene Gizmos

Update `SimpleTargetGizmoDrawer`:

- draws every current hit collider path,
- ground baseline spans combined min X to max X across all paths,
- drawing should update when Unity Animation window preview changes the current sprite renderers.

No separate scrub UI is needed.

### SimpleTarget Inspector

Add inspector fields:

- `Hit Collider`
- `Sprites Root`
- optional `Excluded Shape Renderers`
- auto-detect status:
  - number of included renderers,
  - number of current hit collider paths,
  - warning if no `Sprites` root was found,
  - button to bake current sprite outlines,
  - button to detect all animator-frame sprites.

Keep the existing `Hit Collider` field visible and editable.

Add child inspectors for `SimpleTargetLayer` so selecting a sprite layer keeps the layer-local SimpleTarget profile buttons visible.

## SimpleTarget Runtime Behavior

SimpleTarget does not read sprite pixels at runtime.

The editor/child buttons bake sprite alpha profiles into child layer components and apply matching profiles into the normal `HitCollider`. Runtime queries use that collider data.

## Oblique Loft Frame Profiles

Oblique Loft does not have auto mode.

Oblique Loft must stay manually authored because a sprite outline cannot define:

- ground footprint,
- slice depths,
- vertical slice polygons,
- logic height,
- generated face topology.

Add sprite/frame profiles for `ObliqueLoftCollider`.

The component tracks exactly one `Target Sprite Renderer`. The preferred setup is attaching the component to that same sprite renderer object. If a rare layered object needs independent Oblique Loft volumes for multiple visual layers, add multiple separate Oblique Loft collider components, one per layer object.

### Oblique Loft Target Sprite Renderer

Add a serialized `Target Sprite Renderer` reference directly to `ObliqueLoftCollider`.

Auto-assignment order:

1. SpriteRenderer on the same GameObject.
2. SpriteRenderer in a parent.
3. SpriteRenderer in a child.

Keep all editor code in `Assets/Scripts/ObliqueLoft/Editor/`.

### Oblique Loft Profile Key

For v1, key Oblique Loft profiles by the target `SpriteRenderer` and current `Sprite`.

Each profile stores:

- the source `Sprite`,
- the target `SpriteRenderer` reference,
- footprint points,
- slices:
  - name,
  - depth,
  - points,
  - point order.

The renderer reference is only the single target renderer for this collider. It is not a multi-layer selection system.

Suggested serializable type names:

```csharp
[Serializable]
public class ObliqueLoftSpriteFrameProfile
{
  public Sprite sprite;
  public SpriteRenderer renderer;
  public string rendererPath;
  public List<Vector2> footprint;
  public List<ObliqueLoftSlice> slices;
}
```

Existing profile data can keep a renderer path for backward compatibility, but new authoring is driven by the assigned target renderer field.

### Oblique Loft Current Frame Selection

The editor needs a way to decide which current sprite profile is active.

Required behavior:

- The active current frame is the assigned target renderer's current sprite.
- The user can select the target sprite renderer object in the hierarchy and still see/edit this collider in Scene view.
- The existing Oblique Loft Scene handles edit the currently selected profile's shape.

Do not make the editor guess between multiple current layer sprites.

### Oblique Loft Profile Apply/Capture

When the target renderer's current sprite changes:

1. Find a matching profile by target renderer identity and sprite.
2. If no renderer-specific profile exists, optionally fall back to sprite-only match.
3. If a profile exists, apply its footprint/slices to the live `ObliqueLoftCollider` and call `Rebuild()`.
4. If no profile exists, create one by copying the current live shape from the previous/last seen frame.

When the user edits the live Oblique Loft shape in the existing editor:

- record the edit into the active current sprite profile,
- if no profile exists yet for that current sprite, create it from the edited live shape,
- keep using Unity Undo for editor changes.

Do not require explicit create/capture/apply buttons for the normal workflow. A delete button is acceptable as a recovery tool for the current sprite profile.

### Oblique Loft Scene UI

Keep the current Oblique Loft UI:

- edit footprint,
- edit slices,
- add/remove middle slice,
- drag/nudge points,
- edge insert/delete,
- generated face gizmos.

Only change what data those handles are editing:

- if a frame profile is active, handles edit that profile's live-applied shape,
- if no profile is active, handles edit the shared/default shape.

Do not build a separate Oblique Loft frame editor.

## Unity Animation Window Preview Behavior

The implementation should work when Unity's Animation window changes sprite frames in editor preview.

Expected flow:

1. User selects an animated object or prefab instance.
2. User opens Unity Animation window.
3. User scrubs to a frame.
4. SpriteRenderer sprites under `Sprites` update in preview.
5. User clicks `Auto Detect Current Sprite Outlines`.
6. `SimpleTarget` writes the combined current sprite alpha outlines into the normal `HitCollider`.
7. `ObliqueLoftCollider` editor sees the assigned target renderer's current sprite, loads that profile if it exists, or creates it from the last live shape if missing. Selecting that target renderer also keeps the collider visible/editable in Scene view.

Implementation note:

- In editor code, use `SceneView.duringSceneGui`, `EditorApplication.update`, or inspector repaint hooks if needed so profile changes are noticed while scrubbing.
- Do not mutate animation clips while scrubbing.
- Only profile data changes when the user edits/captures hitbox data.

## Data Ownership

SimpleTarget auto-detect:

- no per-frame data is stored,
- current shape is baked from `Sprites` child renderers and their current sprite alpha outlines into the normal `HitCollider`.

SimpleTarget runtime:

- existing/baked `PolygonCollider2D` remains the data source.

Oblique Loft:

- per-sprite profile data is stored on the `ObliqueLoftCollider`,
- applying a profile mutates the live authored footprint/slices,
- runtime raycasting uses the currently applied live shape.

Normal colliders:

- use Unity's own sprite physics/collider workflow.

## Runtime Behavior

SimpleTarget:

- Play Mode targeting should work from the baked `HitCollider`.
- pixel tracing is editor-only.
- target selection/intersection should continue through `Target.cs` and `SimpleTargetingStrategy` without new strategy classes.

Oblique Loft:

- runtime should apply the profile matching the target renderer's current sprite before raycasts.
- simplest v1: in `Update` or before `Rebuild`/raycast, detect current target renderer/sprite key and apply if changed.
- do not apply profiles for cars/complex moving objects unless the object explicitly has an Oblique Loft collider and profile binding enabled.
- preserve `useInRaycasts`.

## Validation And Edge Cases

SimpleTarget auto-detect must handle:

- no `Sprites` object found,
- `Sprites` exists but no active child SpriteRenderers,
- active SpriteRenderers with null sprites,
- sprites with no opaque pixels,
- multiple outline paths per sprite,
- multiple active layers with different transforms,
- disabled renderer excluded from shape,
- ground line width changing after the current frame is baked into the collider.

Oblique Loft profiles must handle:

- one current sprite renderer,
- target renderer is missing,
- target renderer has null sprite,
- current sprite has no profile yet,
- profile exists but validates with errors,
- profile application must call `Rebuild()`,
- profile data must preserve slice point order.

## Acceptance Criteria

SimpleTarget auto-detect:

- Selecting/scrubbing an animation frame, then clicking `Auto Detect Current Sprite Outlines`, changes the drawn SimpleTarget outline to match all current child sprite alpha outlines under `Sprites`.
- Single-sprite and layered-sprite objects use the same code path.
- Ground baseline width spans the lowest X to highest X of all baked collider path points.
- Ground baseline height remains shared and does not change per frame.
- Existing manual SimpleTarget prefabs still work unchanged.
- `Target.cs` can still select direct SimpleTargets and intervening SimpleTargets.

Oblique Loft frame profiles:

- Existing Oblique Loft authoring UI still works.
- A single-sprite object can store/load different Oblique Loft shapes for different current sprites.
- A layered object should use separate Oblique Loft collider components per layer object if multiple layers need volumes.
- Applying a profile rebuilds generated faces and the synced footprint collider.
- Existing static Oblique Loft objects without profiles still work unchanged.

Normal colliders:

- No custom normal-collider authoring is added unless needed.
- Documentation says to use Unity Sprite Custom Physics Shape for normal colliders.

Verification:

- Runtime compile check passes with `dotnet build Assembly-CSharp.csproj`.
- Unity Editor validation is still required for Animation window scrubbing and Scene view handle behavior.

## Files Likely To Change

Runtime:

- `Assets/Scripts/Common/SimpleTarget.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftCollider.cs`
- `Assets/Scripts/ObliqueLoft/ObliqueLoftSlice.cs` if deep-copy/profile helpers are needed

Editor:

- `Assets/Scripts/Common/Editor/SimpleTargetGizmoDrawer.cs`
- `Assets/Scripts/ObliqueLoft/Editor/ObliqueLoftColliderEditor.cs`

Docs:

- `Docs/SimpleTargeting.md`
- `Docs/ObliqueLoftCollider.md`
- `CONTEXT.md`
- `AGENTS.md` only if architecture/tooling rules change

## Do Not Do

- Do not add a separate animation-frame hitbox timeline.
- Do not require manual SimpleTarget per-frame editing for layered characters.
- Do not use only one child sprite for SimpleTarget auto-detect.
- Do not force layered animators to share one state/time.
- Do not auto-generate Oblique Loft volumes from sprite outlines.
- Do not merge multiple Oblique Loft sprite layers into one collider. If layered Oblique Loft is needed, use separate collider objects/components per layer.
- Do not remove the old targeting fallback path.
- Do not route normal physics colliders through SimpleTarget.
