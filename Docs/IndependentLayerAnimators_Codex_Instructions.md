# Codex Task: Restore Independent Layer Animators and Remove Master Animator Architecture

## Context

This Unity project has a character sprite-builder pipeline for layered 2D character animation.

The recent experiment introduced a `MasterCharacterAnimator`, `LayeredAnimatorSync`, and `MasterAnimationPreviewProbe` so that one master Animator on the `Sprites` object could drive all child layer Animators. This is now being rolled back because it removes the kind of layer independence required by the game.

The desired final architecture is:

```text
CharacterRoot
├── CharacterAnimationController
├── AnimatorParameterRelay
├── SpriteBuilder
└── Sprites
    ├── Body
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    ├── Face
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    ├── Hair
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    ├── UpperClothing
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    ├── LowerClothing
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    ├── Shoes
    │   ├── SpriteRenderer
    │   ├── Animator
    │   └── CharacterSpriteLayer
    └── Weapon
        ├── SpriteRenderer
        ├── Animator
        └── CharacterSpriteLayer
```

There should be **no master Animator on `Sprites`**.

Each visual layer has its own Animator and can independently decide how to use the shared animation parameters.

Example of required independence:

```text
Lower body is walking.
Upper body starts shooting.
Lower body keeps its existing walk animation time.
Lower body does not restart because shooting began.
```

This cannot be achieved with one master Animator forcing all layers into the same state/time.

---

## High-Level Goal

Restore the previous independent-layer animation architecture:

```text
CharacterAnimationController
→ AnimationParameterWriter
→ AnimatorParameterRelay
→ all layer Animators receive shared parameters
→ each layer Animator independently decides what state to enter / continue
```

Keep the sprite-builder asset pipeline:

```text
filenames
→ parsed contexts
→ slot clips
→ generated sprite clips
→ generated override controllers
→ catalog
→ SpriteBuilder applies override controllers to layer Animators
```

Remove the master Animator control/sync/probe system.

---

## Non-Goals

Do not build a custom animation scrubber.

Do not generate special layered preview clips.

Do not use a master Animator as a runtime or editor control point.

Do not force all layer Animators to the same state/time.

Do not remove the independent child layer Animators.

Do not remove the suffix-based filename/sample-rate changes. Those should remain.

---

## Desired Runtime Flow

```text
CharacterAnimationController.Tick()
→ asks animation adapters to contribute parameters
→ AnimationParameterWriter writes to AnimatorParameterRelay
→ AnimatorParameterRelay broadcasts parameters to child layer Animators
→ Body / Face / Hair / Clothing / Shoes / Weapon each evaluate their own Animator graph independently
```

Shared parameters are okay. Shared forced state/time is not.

For example:

```text
Parameter IsMoving = true goes to all layers.
Parameter IsShooting = true goes to all layers.

UpperClothing Animator may respond to IsShooting.
LowerClothing Animator may ignore IsShooting and continue walking.
Shoes Animator may only respond to IsMoving.
Face Animator may respond to expression/aim/shoot parameters separately.
```

---

## Files / Systems to Remove or Stop Using

Remove these components from the character prefab / scene hierarchy:

```text
CharacterMasterAnimator
LayeredAnimatorSync
MasterAnimationPreviewProbe
Animator component on Sprites, if it was only added for the master Animator
```

Remove or leave unused temporarily until compile is stable:

```text
Assets/Scripts/Character/CharacterMasterAnimator.cs
Assets/Scripts/Character/LayeredAnimatorSync.cs
Assets/Scripts/Character/MasterAnimationPreviewProbe.cs
```

After all code references are removed and the project compiles, delete these scripts if unused.

---

## Restore `AnimationParameterWriter`

`AnimationParameterWriter` should target `AnimatorParameterRelay`, not `CharacterMasterAnimator`.

Use this final implementation:

```csharp
public sealed class AnimationParameterWriter
{
  private AnimatorParameterRelay relay;

  public AnimationParameterWriter(AnimatorParameterRelay relay)
  {
    this.relay = relay;
  }

  public void SetRelay(AnimatorParameterRelay relay)
  {
    this.relay = relay;
  }

  public void SetBool(string parameterName, bool value)
  {
    relay?.SetBool(parameterName, value);
  }

  public void SetInteger(string parameterName, int value)
  {
    relay?.SetInteger(parameterName, value);
  }

  public void SetFloat(string parameterName, float value)
  {
    relay?.SetFloat(parameterName, value);
  }

  public void SetTrigger(string parameterName)
  {
    relay?.SetTrigger(parameterName);
  }

  public void ResetTrigger(string parameterName)
  {
    relay?.ResetTrigger(parameterName);
  }
}
```

---

## Restore `CharacterAnimationController`

`CharacterAnimationController` should use `AnimatorParameterRelay`, not `CharacterMasterAnimator`.

Expected structure:

```csharp
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
  [SerializeField] private AnimatorParameterRelay animatorRelay;
  [SerializeField] private MonoBehaviour[] animationAdapters;

  private readonly List<MonoBehaviour> adapterBehaviours = new();
  private readonly List<IAnimationParameterContributor> contributors = new();
  private AnimationParameterWriter writer;
  private bool adaptersCached;

  public void Initialize()
  {
    Initialize(null, null);
  }

  public void Initialize(CharacterMotor motor, WeaponUser weaponUser)
  {
    if (animatorRelay == null)
    {
      animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    }

    RefreshAdapterListIfNeeded();
    InitializeKnownAdapters(motor, weaponUser);
  }

  public void Tick()
  {
    Initialize();

    if (animatorRelay == null)
    {
      return;
    }

    if (writer == null)
    {
      writer = new AnimationParameterWriter(animatorRelay);
    }
    else
    {
      writer.SetRelay(animatorRelay);
    }

    for (int i = 0; i < contributors.Count; i++)
    {
      MonoBehaviour adapter = adapterBehaviours[i];

      if (adapter != null && adapter.isActiveAndEnabled)
      {
        contributors[i].Contribute(writer);
      }
    }
  }

  private void Update()
  {
    Tick();
  }

  private void RefreshAdapterListIfNeeded()
  {
    if (adaptersCached)
    {
      return;
    }

    if (animationAdapters == null || animationAdapters.Length == 0)
    {
      animationAdapters = FindLocalAnimationAdapters();
    }

    adapterBehaviours.Clear();
    contributors.Clear();

    if (animationAdapters == null)
    {
      adaptersCached = true;
      return;
    }

    foreach (MonoBehaviour adapter in animationAdapters)
    {
      if (adapter is IAnimationParameterContributor contributor)
      {
        adapterBehaviours.Add(adapter);
        contributors.Add(contributor);
      }
    }

    adaptersCached = true;
  }

  private MonoBehaviour[] FindLocalAnimationAdapters()
  {
    MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
    List<MonoBehaviour> foundAdapters = new();

    foreach (MonoBehaviour behaviour in behaviours)
    {
      if (behaviour is IAnimationParameterContributor)
      {
        foundAdapters.Add(behaviour);
      }
    }

    return foundAdapters.ToArray();
  }

  private void InitializeKnownAdapters(CharacterMotor motor, WeaponUser weaponUser)
  {
    if (animationAdapters == null)
    {
      return;
    }

    foreach (MonoBehaviour adapter in animationAdapters)
    {
      if (adapter is CharacterMovementAnimationAdapter movementAdapter)
      {
        movementAdapter.Initialize(motor);
      }
      else if (adapter is CharacterAimAnimationAdapter aimAdapter)
      {
        aimAdapter.Initialize(weaponUser);
      }
    }
  }

  private void Reset()
  {
    animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    animationAdapters = FindLocalAnimationAdapters();
    adaptersCached = false;
  }

  private void OnValidate()
  {
    if (animatorRelay == null)
    {
      animatorRelay = GetComponentInChildren<AnimatorParameterRelay>(true);
    }

    if (animationAdapters == null || animationAdapters.Length == 0)
    {
      animationAdapters = FindLocalAnimationAdapters();
    }

    adaptersCached = false;
  }
}
```

If this exact file already exists, preserve local improvements but restore the relay-based dependency.

---

## Restore `AnimatorParameterRelay`

Ensure `AnimatorParameterRelay` exists and broadcasts parameters to child layer Animators.

It should find layer Animators under the character's `Sprites` hierarchy and exclude any non-layer Animator if present.

Since the final architecture has no master Animator on `Sprites`, this is simpler: all relevant Animators under `Sprites` are child layer Animators.

Add validation if useful:

```text
If a follower Animator does not have a parameter being set, log a warning with the parameter name instead of Unity's hash-only error.
```

Do not make this warning fatal, because different layers may intentionally not use every parameter. It may be better as optional/disabled if noisy.

---

## Revert `SpriteBuilderEditor`

`SpriteBuilderEditor` should not create or assign any master Animator components.

Remove logic that ensures or adds:

```text
Animator on Sprites
CharacterMasterAnimator
MasterAnimationPreviewProbe
LayeredAnimatorSync
MasterCharacterAnimator.controller assignment
```

The Apply button should go back to simply:

```csharp
if (GUILayout.Button("Apply Sprite Build"))
{
  builder.Apply();
  EditorUtility.SetDirty(builder);
}
```

If there is an auto-apply block, it should also just do:

```csharp
if (GUI.changed)
{
  builder.AutoSelectMissingOrInvalidOptions();
  builder.Apply();
  EditorUtility.SetDirty(builder);
}
```

Do not use `AssetDatabase` here for assigning a master controller.

---

## Revert `SpriteBuilder`

`SpriteBuilder` should not reference:

```text
CharacterMasterAnimator
LayeredAnimatorSync
MasterAnimationPreviewProbe
```

Remove fields such as:

```csharp
[SerializeField] private CharacterMasterAnimator masterAnimator;
[SerializeField] private LayeredAnimatorSync layeredAnimatorSync;
```

Remove methods such as:

```text
EnsureMasterAnimatorReferences()
```

`SpriteBuilder.Apply()` should only apply selected catalog entries to the actual visual layer objects:

```text
Body
Face
Hair
UpperClothing
LowerClothing
Shoes
Weapon
```

It should not configure a master object.

Expected responsibility:

```text
SpriteBuilder.Apply()
→ Resolve catalog entry for each selected part
→ For animated entries, assign generated override controller to that layer's Animator
→ For static entries, assign static sprite to that layer's SpriteRenderer
```

---

## Revert `CharacterBuilderConstants`

The controller should no longer be called `MasterCharacterAnimator.controller`.

Use a neutral/template name.

Recommended final constant:

```csharp
public const string LayerTemplateControllerPath =
    BaseRoot + "/LayerAnimatorTemplate.controller";
```

Then rename the Unity asset:

```text
Assets/Animations/Character/Base/MasterCharacterAnimator.controller
```

to:

```text
Assets/Animations/Character/Base/LayerAnimatorTemplate.controller
```

Do the rename inside Unity so the `.meta` GUID is preserved.

If less risky, keep the old asset name temporarily but rename the code concept later. However, final desired naming is `LayerAnimatorTemplate.controller` because it is no longer a master controller.

Search and replace references:

```text
MasterControllerPath
MasterCharacterAnimator.controller
BaseControllerPath
BaseCharacterLayer.controller
```

Final code should use:

```text
LayerTemplateControllerPath
LayerAnimatorTemplate.controller
```

---

## Update `CharacterBuilderMenu`

This editor menu should remain responsible for asset generation only:

```text
Tools → Character Builder → Create / Update Slot Clips
Tools → Character Builder → Rebuild Character Assets
```

It should:

```text
1. Create/update slot clips.
2. Generate sprite animation clips from PNGs.
3. Generate override controllers based on the layer template controller.
4. Build/update the character sprite catalog.
```

It should not create or assign master components on character prefabs.

Rename helper methods:

```text
EnsureMasterController → EnsureLayerTemplateController
masterController variable → layerTemplateController
missingMasterSlotCount → missingTemplateSlotCount
```

Generated override controllers should use:

```csharp
RuntimeAnimatorController layerTemplateController = EnsureLayerTemplateController();
```

Then:

```csharp
AnimatorOverrideController overrideController =
    new AnimatorOverrideController(layerTemplateController);
```

or equivalent existing logic.

---

## Remove Probe Curves From Slot Clips

The master preview probe system is no longer needed.

If code was added to slot clips to animate:

```text
MasterAnimationPreviewProbe.previewSlotHashA
MasterAnimationPreviewProbe.previewSlotHashB
MasterAnimationPreviewProbe.previewTime
```

remove that generation code.

Slot clips can return to being placeholder/template clips.

If the current placeholder clips contain probe curves, update the builder so `Create / Update Slot Clips` clears those probe curves.

The slot clips are still needed as override keys in the template controller.

---

## Keep Suffix Filename / Sample Rate Changes

Do not revert the filename format.

Keep this final format:

```text
<gender>_<bodyType>_<context...>_<partGroup>_<variant...>_single.png
<gender>_<bodyType>_<context...>_<partGroup>_<variant...>_sheet.png
<gender>_<bodyType>_<context...>_<partGroup>_<variant...>_sheet_<sampleRate>.png
```

Rules:

```text
_single = single-frame source
_sheet = animated sheet using default sample rate
_sheet_<sampleRate> = animated sheet using explicit sample rate
```

Keep parser support for:

```text
_sheet
_sheet_12
_sheet_8
_sheet_24
```

Keep generated real sprite clips using `parsed.sampleRate`.

Keep placeholder slot clips using `CharacterBuilderConstants.DefaultSampleRate`.

---

## Animator Controller Strategy

Initial short-term approach:

```text
Use one shared LayerAnimatorTemplate.controller for all layer override controllers.
```

This is acceptable as a starting point.

Long-term better approach:

```text
Assets/Animations/Character/Base/Templates/
    BodyLayer.controller
    FaceLayer.controller
    HairLayer.controller
    UpperClothingLayer.controller
    LowerClothingLayer.controller
    ShoesLayer.controller
    WeaponLayer.controller
```

Each part group can eventually have its own layer template controller.

This allows true controller-graph independence per layer type.

For now, do not implement per-layer templates unless easy and safe. The immediate task is to remove the master system and restore independent runtime Animators.

---

## Expected Independence After Migration

Example behavior after migration:

```text
LowerClothing Animator:
- Receives IsMoving = true
- Keeps playing walk_e
- Ignores Shoot trigger/parameter

UpperClothing Animator:
- Receives IsMoving = true
- Receives IsShooting = true
- Enters shooting/aiming state

Shoes Animator:
- Receives IsMoving = true
- Keeps walking

Weapon Animator:
- Receives Shoot trigger
- Plays weapon shoot/recoil
```

No system should force all layer Animators into a single common state/time.

---

## Preview Workflow After Migration

Preview is per layer, using Unity's built-in Animation window.

Examples:

```text
To preview upper-body shooting:
select UpperClothing or Weapon layer and preview its generated/controller animation.

To preview walking legs:
select LowerClothing or Shoes layer and preview walking.

To see all parts together:
use Play Mode or a controlled scene test where AnimatorParameterRelay broadcasts the params.
```

No master layered preview feature is required.

---

## Cleanup Search Checklist

After migration, search the project for these strings:

```text
CharacterMasterAnimator
LayeredAnimatorSync
MasterAnimationPreviewProbe
MasterControllerPath
MasterCharacterAnimator.controller
BaseControllerPath
BaseCharacterLayer.controller
SetMasterAnimator
masterAnimator
previewSlotHashA
previewSlotHashB
previewTime
```

Expected:

```text
CharacterMasterAnimator / LayeredAnimatorSync / MasterAnimationPreviewProbe:
  zero references, unless files are intentionally left unused temporarily.

SetMasterAnimator:
  zero references.

masterAnimator:
  zero references in character animation runtime code, unless unrelated.

previewSlotHashA / previewSlotHashB / previewTime:
  zero references after removing probe system.
```

Also search:

```text
AnimatorParameterRelay
animatorRelay
SetRelay
```

Expected:

```text
AnimatorParameterRelay:
  used by CharacterAnimationController and AnimationParameterWriter.

animatorRelay:
  used by CharacterAnimationController.

SetRelay:
  used by CharacterAnimationController / AnimationParameterWriter.
```

---

## Acceptance Criteria

1. Project compiles with no references to removed master/probe/sync classes.

2. Character prefab has:

```text
CharacterAnimationController
AnimatorParameterRelay
SpriteBuilder
Sprites child with Body/Face/Hair/UpperClothing/LowerClothing/Shoes/Weapon children
```

3. `Sprites` has no master Animator component unless it is used for something unrelated.

4. Each visual layer child has:

```text
SpriteRenderer
Animator
CharacterSpriteLayer
```

5. `CharacterAnimationController` writes to `AnimatorParameterRelay`.

6. `AnimationParameterWriter` writes to `AnimatorParameterRelay`.

7. `AnimatorParameterRelay` broadcasts parameters to all child layer Animators.

8. `SpriteBuilder.Apply()` assigns generated override controllers/static sprites to layer objects.

9. Running:

```text
Tools → Character Builder → Create / Update Slot Clips
Tools → Character Builder → Rebuild Character Assets
```

still works.

10. Applying the sprite build still assigns correct override controllers to each selected visual layer.

11. In Play Mode, different layers can independently respond to the same parameter stream.

12. Lower-body walking can continue while upper-body shooting begins, provided the lower-body Animator graph ignores the shooting parameter/trigger and remains in its locomotion state.

---

## Important Design Principle

Do not reintroduce a single animation authority that forces all layers to the same state/time.

The system should share **parameters**, not **state**.

Correct:

```text
Shared params → independent layer state machines
```

Incorrect:

```text
One master state/time → all layers forced to mirror it
```
