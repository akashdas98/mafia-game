# Architecture Diagrams

These diagrams show the component ownership architecture after the composition migration. The important rule is that stable owner relationships are serialized explicitly on the owning prefab or component. There is no runtime entity index, universal refs object, broad lookup service, or transition controller layer in the architecture.

If a Markdown Mermaid preview fails, the text under each diagram is the same architecture in plain form.

## Character Entity Wiring

```mermaid
flowchart TB
  Root["Character root prefab"]

  Motor["CharacterMotor"]
  Anim["CharacterAnimationController"]
  Relay["AnimatorParameterRelay"]
  Layers["Independent body/clothing child Animators"]
  MoveAdapter["CharacterMovementAnimationAdapter"]
  AimAdapter["CharacterAimAnimationAdapter"]
  Interactor["CharacterInteractor"]
  WeaponUser["WeaponUser"]
  InventoryUser["InventoryUser"]
  Router["PlayerInputRouter"]
  InventoryObject["Inventory child/prefab"]
  Inventory["Inventory"]
  Weapons["Weapons container"]
  Misc["Misc container"]
  AimTarget["AimTarget child with Target"]

  Root --> Motor
  Root --> Anim
  Root --> Relay
  Root --> Layers
  Root --> MoveAdapter
  Root --> AimAdapter
  Root --> Interactor
  Root --> WeaponUser
  Root --> InventoryUser
  Root --> Router
  Root --> InventoryObject
  Root --> AimTarget
  InventoryObject --> Inventory
  InventoryObject --> Weapons
  InventoryObject --> Misc

  Anim -. ordered adapter list .-> MoveAdapter
  Anim -. ordered adapter list .-> AimAdapter
  Anim -. parameter writes .-> Relay
  Relay -. broadcasts shared parameters .-> Layers
  MoveAdapter -. reads state .-> Motor
  AimAdapter -. reads state .-> WeaponUser

  Router -. serialized refs .-> Motor
  Router -. serialized refs .-> Interactor
  Router -. serialized refs .-> WeaponUser
  Router -. serialized refs .-> InventoryUser

  WeaponUser -. serialized ref .-> Inventory
  WeaponUser -. serialized ref .-> AimTarget
  InventoryUser -. serialized ref .-> Inventory
```

Plain view:

- Character root owns the character capability components.
- `CharacterAnimationController` ticks itself, invokes its ordered animation adapter list, and owns final character parameter writes through `AnimatorParameterRelay`.
- `AnimatorParameterRelay` broadcasts shared parameters to independent visible layer animators; it does not force a common state/time.
- Character Builder generated override controllers are also independent per layer: each part group uses its own template controller under `Base/Templates/` and its own slot placeholders under `Base/Slots/<part-group>/`.
- `CharacterMovementAnimationAdapter` reads `CharacterMotor`; `CharacterAimAnimationAdapter` reads `WeaponUser`.
- Enable or disable the aim adapter component to control whether aim parameters are written.
- `PlayerInputRouter` has explicit fields for movement, interaction, weapon, and inventory receivers.
- `Inventory` lives on the `Inventory` child/prefab with assigned `Weapons` and `Misc` item containers.
- `WeaponUser` and `InventoryUser` use assigned `Target` / `Inventory` references, with narrow local hierarchy fallback only for missing local fields.

## Character Input Flow

```mermaid
flowchart LR
  InputManager["InputManager"]
  Handler["CharacterInputHandler"]
  State["CharacterInputState"]
  Router["PlayerInputRouter"]

  InputManager --> Handler
  Handler --> State
  State --> Router

  Router -->|move| CharacterMotor
  Router -->|interact| CharacterInteractor
  Router -->|aim and fire| WeaponUser
  Router -->|pickup drop cycle| InventoryUser
```

Input is translated into typed state before gameplay sees it. Gameplay components receive commands through focused capability interfaces and explicit router fields.

## Weapon And Inventory Flow

```mermaid
flowchart TB
  InventoryUser["InventoryUser on character"] --> Inventory["Inventory on child prefab"]
  Inventory --> Containers["Weapons/Misc containers"]
  Inventory --> Equippable["IEquippable"]
  Equippable --> Weapon["Weapon"]
  Weapon --> Gun["Gun"]
  Gun --> GunStats["GunStats"]
  Gun --> FireMode["GunFireMode"]
  FireMode --> SemiAuto["SemiAutoFireMode"]
  FireMode --> FullAuto["FullAutoFireMode"]

  WeaponUser["WeaponUser"] -. assigned .-> Inventory
  WeaponUser -. assigned .-> Target["Target on AimTarget"]
  WeaponUser -->|place visual gun| Gun
  WeaponUser -->|trigger| Gun
```

Inventory rules depend on `IEquippable`, not concrete weapon subclasses. Gun trigger behavior is delegated to fire-mode components.

## Aim And Targeting Flow

```mermaid
flowchart TB
  WeaponUser["WeaponUser"] -->|Aim position| Target["Target on AimTarget"]

  Target --> Selection["TargetSelectionResolver"]
  Target --> Simple["SimpleTargetingStrategy"]
  Target --> Oblique["ObliqueTargetingStrategy"]
  Target --> Legacy["LegacyDepthTargetingStrategy"]

  Selection --> Selected["selected object"]
  Simple --> SimpleTarget["SimpleTarget flat hit polygon"]
  Oblique --> Loft["ObliqueLoftCollider static volume"]
  Legacy --> Old["Depth Hit Enclosure colliders"]

  Target --> Highlighter["Highlighter"]
  Target --> Marker["Marker child"]
```

`Target` is the shooter-side targetter and marker presenter. Selection and LOS decisions are delegated to strategies. The old depth/hit/enclosure path remains as fallback.

## AimTarget Points

```mermaid
flowchart TB
  AimRoot["AimTarget root"]
  Target["Target component"]
  Marker["Marker visual target sprite"]
  Origin["AimOrigin logical ray origin"]
  GunPoint["GunPoint visual weapon placement"]

  AimRoot --> Target
  AimRoot --> Marker
  AimRoot --> Origin
  AimRoot --> GunPoint

  WeaponUser -->|real shot line starts here| Origin
  WeaponUser -->|equipped gun visual moves here| GunPoint
  Target -->|resolved hit point| Marker
```

Targeting math uses `AimOrigin`. `GunPoint` is visual and can be animation-frame specific. The marker moves to the resolved target point; the `AimTarget` root stays anchored to the character.

## SimpleTarget And Oblique Loft

```mermaid
flowchart LR
  Shot["Shot from AimOrigin to intended target"]
  SimpleTarget["SimpleTarget flat hit polygon and ground line"]
  Loft["ObliqueLoftCollider 3D logic volume"]
  Footprint["synchronized footprint PolygonCollider2D"]
  Slices["authored footprint and slices"]
  Faces["generated polygon faces"]
  Raycast["logic ray face intersection"]

  Shot --> SimpleTarget
  Shot --> Loft
  Loft --> Footprint
  Loft --> Slices
  Slices --> Faces
  Faces --> Raycast
```

Use `SimpleTarget` for animated or moving shootable targets. Use `ObliqueLoftCollider` for mostly-static blockers and direct static targets.

## Vehicle Wiring

```mermaid
flowchart TB
  Root["Vehicle root prefab"]
  Motor["VehicleMotor"]
  Anim["VehicleAnimationController"]
  Possession["VehiclePossession"]
  Handler["CarInputHandler"]
  Router["VehicleInputRouter"]

  Root --> Motor
  Root --> Anim
  Root --> Possession
  Root --> Handler
  Root --> Router

  Anim -. pulls state .-> Motor

  Handler -. serialized ref .-> Router
  Router -. serialized ref .-> Motor
  Possession -. serialized ref .-> Handler
```

`VehicleMotor` owns driving state and receives typed vehicle input. `VehicleAnimationController` ticks itself and pulls state from `VehicleMotor` before writing animator parameters. `VehiclePossession` owns enter/exit and input switching.
