# Story 6 — Extend the existing multiplayer spawn protocol

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: implementation added; automated checks run; three-player user playtest pending.

*As a joining player, I receive my ship and the current ships of existing participants.*

## Agreed startup contract

`PlayerStartupData` provides the player's requested ship type through `ShipId`. Replace its placeholder `Message` payload and have `PlayerStartupData.Create()` populate `ShipId` with `"fighter"` for now. Keep this as the startup-data contract so it can be extended later; do not hardcode the choice independently in the network or factory layers.

The ship ID identifies a definition in `ShipRegistry`, using the exact YAML `name`. The packaged name is already `fighter`, matching this default. Lookup remains case-sensitive with no implicit alias or normalization.

The request carries the type ID, not the ship's full configuration. All peers resolve the approved ID through their compatible local registry. A separate host-generated entity ID identifies an individual ship instance.

## Message contracts

| Message | Contents |
|---|---|
| Spawn request | Serialized `PlayerStartupData`, initially containing `ShipId`. |
| Host-approved spawn record | Participant peer ID, unique ship entity ID, ship definition ID, position, rotation, and velocity. |
| Initial spawn snapshot | Host-approved records for the current participants and their ships, including the joining participant. |

Use validated primitive payloads and retain disabled arbitrary-object decoding. Advance protocol compatibility when changing the message contracts.

## Implementation plan

1. Update `PlayerStartupData` construction, serialization, parsing, and callers to use `ShipId`. Keep the default in its creation path; host and client both supply their startup data through the existing network startup configuration.
2. On the host, derive the requesting participant from the actual sender, validate `ShipId` against `ShipRegistry`, allocate a unique entity ID, and choose the initial transform. The initial policy uses host-selected spaced positions and zero velocity; collision-aware placement can be refined later.
3. Route the host's own startup data through the same approval/construction path using a local call. Clients cannot select authoritative entity IDs, ownership, stats, or spawn locations.
4. For the first local-control milestone, build initial snapshots from the host-approved spawn records. Use the same record format for incremental spawn announcements. Current moving-state snapshots require synchronization and are covered by the separate synchronization plan; the host does not yet know the remote players' locally simulated transforms.
5. Preserve the existing reliable sequence: spawn request, initial snapshot, readiness acknowledgement, then join completion. Keep later additions/removals ordered consistently with the snapshot and completion messages.
6. Make duplicate delivery idempotent. Repeated records must not create new entities or reset existing ships to old spawn positions. Conflicting identity/type records fail explicitly; deliberate future ship replacement uses an explicit lifecycle operation rather than masquerading as a duplicate spawn.
7. Apply the initial snapshot and complete local participant binding and authority setup before acknowledging readiness. Complete loading only after the local participant and ship exist and the existing join-completion conditions are met.
8. Preserve Story 5's construction-failure policy: clean unfinished nodes and call `GameFlowManager.QuitToStartMenuBlocking(reason)` on the failing instance. A host construction failure ends the hosted session. Invalid client startup requests are rejected before construction, with a reason for the joining client, rather than terminating an otherwise healthy host session.

## Acceptance criteria

- With one host and two clients, all three instances contain the same three participant IDs and three ship entity IDs, one child ship per participant, each resolving `fighter` locally.
- Both clients receive the host and each other; the host receives both clients. Spawn delivery must not omit client-to-client visibility.
- On each instance, the two remote ships remain at their approved spawn transforms. Only its own ship is enabled for local movement when Story 7 is integrated; no movement replication is required.
- `PlayerStartupData.Create()` supplies `ShipId == "fighter"`, and serialization round trips preserve it exactly.
- The packaged registry contains `fighter`; requested unknown IDs are rejected without silently substituting a default.
- Host-local and remote participant creation use the same approved construction path.
- Clients cannot provide authoritative ownership, entity IDs, stats, or spawn transforms.
- Initial and incremental records construct equivalent configured entities.
- Late joiners receive the current participant/ship roster and approved spawn records. Current moving-state synchronization is explicitly covered by the separate synchronization plan.
- Duplicate delivery neither duplicates entities nor resets their current state.
- Invalid requests and malformed host records produce controlled failures with reasons.
- Construction failures invoke the blocking return-to-menu flow specified in Story 5.
- Loading does not complete until the local participant, ship, binding, and input authority setup are ready.

## Validation

Update startup-payload tests for the ship ID and invalid types/values. Test host-side lookup and ownership derivation, approved-record round trips, duplicate records, and conflicting identities. Use at least one host H and two clients A/B for all multiplayer scenarios:

- Start H, join A, then join B: B receives H/A/B in its snapshot, while H and A gain B. Verify all three entity IDs and parent relationships on every instance. Repeat with the client join order reversed and with overlapping joins.
- Late join after local movement is available: move H on H and A on A, then join B. B sees their approved spawn transforms, not their unsynchronized moved positions. Existing instances must not reset their own moved ship when B joins.
- Apply repeated spawn records for H, A, and B: no duplicates, local retargeting, or movement resets.
- Disconnect A, then repeat with B: only the departed participant/ship disappears from the remaining instances. Host loss returns both clients through the existing exit flow.
- Reject an invalid request from one client while the other remains joined; verify no phantom participant or ship remains. Construction failures still follow Story 5's blocking exit policy.
- Restart the three-player session and verify a fresh roster without stale entities or bindings.

Story 6 verifies spawning and binding. Once Story 7 is integrated, exercise input on H, A, and B independently: each instance moves exactly its own ship and retains two stationary remote ships. Live movement-state synchronization is not required for this milestone.

## Scope boundary

Ship-selection UI and additional player startup fields come later. Continuous movement replication and current moving-state late-join snapshots belong to the separate synchronization plan. Host approval of spawn data does not decide movement simulation authority. This story establishes the creation protocol for participant-controlled ships; future abandoned-ship synchronization and player-facing ship switching can extend it separately.

Dependencies: [Story 2](multiplayer-ships-story-02-ship-registry.md), [Story 4](multiplayer-ships-story-04-participant-and-ship.md), and [Story 5](multiplayer-ships-story-05-ship-construction.md).
