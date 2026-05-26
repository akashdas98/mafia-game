# Entity Refs

## Purpose

`EntityRefs` is the dynamic part lookup component for an entity prefab or scene object.

Use it on the root object of a gameplay entity. It discovers components on the root and its children, then lets scripts ask for the part they need by type instead of reading a fixed universal refs field.

## Runtime Component

File: `Assets/Scripts/Common/EntityRefs.cs`

`EntityRefs` rebuilds its `Parts` list in edit-time validation, when enabled, when child objects change, and in `Awake`.

It indexes:

- the component's concrete type,
- its base component classes,
- its implemented interfaces.

That means no new field or support code is needed when a new component type is added to an entity. If a child object gets an `Interactable`, `BoxCollider2D`, `ObliqueLoftCollider`, or another custom component, `EntityRefs` can discover it automatically.

## Usage

For code attached to the entity, use the `Base.EntityRefs` property or `TryGetPart`.

```csharp
public class CarInteractable : Interactable
{
  public override void Interact(GameObject player)
  {
    if (TryGetPart(out CarController carController))
    {
      carController.Enter(player);
    }
  }
}
```

For helper classes that are not `MonoBehaviour` subclasses, use the owning component's `EntityRefs`.

```csharp
if (inputHandler.EntityRefs != null &&
    inputHandler.EntityRefs.TryGet(out CharacterController controller))
{
  controller.gunController.PullTrigger();
}
```

## Prefabs

Add `EntityRefs` to the root of the prefab or scene object.

When editing a prefab in Unity, the `Parts` list is visible on the `EntityRefs` component. It is rebuilt automatically when Unity validates the component and when child objects are added or removed. The context menu command `Rebuild Parts` is available if a manual refresh is needed after unusual editor operations, such as adding a component to an existing child without changing the hierarchy.

Existing character and car prefabs have been migrated from the old fixed `Refs` component to `EntityRefs`.

## Migration Notes

The old `Refs` component exposed fixed fields such as controller, animator, inventory, input handler, depth collider, hit collider, and aim target. Those fixed fields are gone.

Current lookup rules:

- Controllers and input handlers find their sibling or child parts through `EntityRefs`.
- `InputManager` assigns itself to the active `InputHandler` at runtime.
- `Controller.CurrentSceneDetails` stores the current scene trigger directly on the controller.
- `SimpleTarget` resolves hit/depth polygons from explicit fields, child tags, or tagged polygon colliders discovered through `EntityRefs`.

Prefer focused APIs when state is owned by a specific component. Use `EntityRefs` for discovering entity parts, not as a replacement for every runtime state variable.
