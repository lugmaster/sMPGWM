# Story 3 — Extract the local PlayerController

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: implementation added; automated checks run; three-player user playtest pending.

*As a local player, my input and UI interaction are managed independently of network participants and their ships.*

## Agreed ownership

| Component | Responsibility |
|---|---|
| `GamePlayManager` | Create and dispose gameplay systems, coordinate startup/shutdown, and publish session state. |
| `PlayerController` | Own local input policy, input/action controllers, UI bindings, related subscriptions, and the local participant reference. |
| Inventory, equipment, hotbar, and message systems | Retain their existing behavior and lifecycle under `GamePlayManager`; connect to the local player through explicit references. |

Create exactly one session-scoped `PlayerController` as a child of `GamePlayManager`, with `InputController` beneath it. Remote participants never create local input or UI.

## Initialization and authority

Assemble all local-player children before assigning the local peer as multiplayer authority:

```csharp
playerController.AddChild(inputController);
// Add all other local-player children.
playerController.SetMultiplayerAuthority(peerId);
// Enable input only after initialization is complete.
```

Use the same initialization path on the host with peer ID 1. Assign authority recursively once the actual local peer ID is available and before input processing is enabled. Ensure initialization callbacks do not use unfinished authority setup. Any child added later must receive the local authority explicitly; an earlier assignment does not propagate to future children.

Authority assignment is separate from input gating. Retain explicit authority and input-policy checks. As agreed in Story 4, each controlled ship is a child of its `MpController`, outside the local `PlayerController` subtree. The revised first milestone applies input and movement locally through `PlayerController`; network simulation authority is deferred to separate synchronization work.

Initialize local-player coordination before networking can publish participant/session notifications. If the peer ID is not yet available, keep local gameplay input disabled until authority setup completes.

## Implementation plan

1. Extract `InputController`, `PlayerInputPolicy`, and `PlayerActionController` coordination from `GamePlayManager` into `PlayerController`.
2. Move pointer filtering, hotbar action routing, UI input availability, inventory-edit-session coordination, and their subscriptions into the local player. Pass explicit references to the existing systems and UI.
3. Preserve session-state and message behavior: pending acknowledgements participate in the same local input policy, and session activation still updates UI navigation and message availability.
4. Preserve input blocking for inactive sessions, blocking windows, pending acknowledgements, and focus loss. Reset held input when control is unavailable, while preserving the automatic-braking setting.
5. Provide `BindParticipant` and `UnbindParticipant`. Rebinding the same participant is harmless; unbinding clears the reference and held input. Story 4 wires participant creation/removal into these operations.
6. While no participant is bound, local menus and UI remain governed by existing session rules; no ship input is applied. Local input routing and movement are introduced in Story 7.
7. Explicitly shut down local-player bindings and subscriptions before disposing the referenced systems. Repeated cleanup is safe, and a new session creates fresh local controls.

## Acceptance criteria

- Exactly one local `PlayerController` and one `InputController` exist per running gameplay instance.
- On host and client, the player and its assembled children have the actual local peer as authority before input is enabled.
- Children added later receive the correct local authority explicitly.
- Remote participant creation never creates input controllers or local UI.
- Blocking windows, pending acknowledgements, focus loss, and inactive sessions preserve existing input restrictions and reset held input.
- Inventory editing, equipment, hotbar actions, menus, and messages retain existing behavior.
- Participant binding/unbinding is safe and does not recreate local controls.
- Leaving and restarting sessions does not duplicate subscriptions or retain stale participant references.

## Validation

Use one host and two clients for multiplayer scenarios. Each instance has three participant/ship representations but exactly one `PlayerController` and `InputController`. On H, bind only H; on A, bind only A; on B, bind only B. Creating either remote participant never creates local controls or changes the binding. Once Story 7 is integrated, input on each instance moves only its own ship; the other two ships remain at their spawn transforms. Exercise focus loss, blocking UI, and restart independently on all three instances.

Run existing input-policy/action tests and add focused lifecycle/binding coverage where needed. Verify authority on both host and client, including a child added after initial assignment. Smoke-test inventory, hotbar, menus, acknowledgement messages, focus loss, and session restart in Godot.

## Scope boundary

Participant lifecycle integration belongs to Story 4, local ship input and movement to Story 7, and camera following to Story 8. Multiplayer inventory ownership changes are outside this epic. `MovementPhysics` is unchanged by this extraction.

Dependencies: none. The participant-binding contract introduced here is completed by Story 4.
