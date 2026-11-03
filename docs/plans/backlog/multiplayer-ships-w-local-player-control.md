**Epic: YAML-defined multiplayer ships with local player control**

Enable every participant, including the host, to spawn and locally control a ship defined in YAML. Each running game has one local `PlayerController`, one `MpController` per participant, and a separate ship entity for each participant.

Stories 1–7 form the first deliverable epic scope. Completing their implementation reaches a major milestone and requires a user playtest before proceeding to follow-up work. The local ships plan now includes camera following as Story 8. Movement synchronization has its own five-story plan. Movement effects and broader lifecycle verification (original Stories 10–11) are out of current scope and retained for later reconsideration; none blocks this first milestone.

**Stories 1–8 are covered by separate story files**, indexed in the [Stories 1–8 summary plan](../multiplayer-ships-story-summary.md). Covered means documented, not implemented; Story 7 still has refinement items open.

The first milestone processes input locally each frame and moves the locally controlled ship in local physics updates. Movement synchronization is separate follow-up work, documented as five stories under the [separate synchronization plan](../multiplayer-synchronization-story-summary.md). That follow-up uses host-authoritative movement, local client prediction/reconciliation, and remote interpolation. Cross-peer movement/effect consistency, prediction/reconciliation, combat, a ship-selection screen, and multiplayer inventory synchronization are outside the first milestone. Participant and ship spawning remain networked through Story 6.

**Architectural contract**

| Component | Responsibility |
|---|---|
| `GamePlayManager` | Assemble gameplay systems and coordinate session lifecycle. |
| `PlayerController` | Local input policy, gameplay commands, UI bindings, and camera target. |
| `MpManager` | Participant registry, creation, removal, and local-participant notification. |
| `MpController` | Participant identity and parent of its currently controlled ship. |
| `NetworkManager` | Existing connection and join protocol; route validated gameplay messages. |
| `ShipRegistry` | Central source for all loaded ship-type data, indexed by the exact YAML name. |
| `ShipFactory` | Construct configured ship entities. |
| `ShipController` | Physical ship body, movement, and per-instance state. |
| `ShipVisuals` | Render animation from applied ship state. |
| `CameraFollow` | Follow the locally controlled ship without inheriting its rotation. |

`ShipData` is immutable configuration shared by instances. `ShipState` is mutable state belonging to one ship. Neither remote ships nor their controllers read local input.

`MpController` is a participant `Node`; its controlled `ShipController` is a child `CharacterBody2D`. The separate local `PlayerController` owns input authority and routes local input directly to its participant's ship. Remote ships do not consume local input or run local-player movement updates. Ship changes preserve participant identity and reparent ships as needed; association changes do not implicitly destroy ships.

Ship identity uses the YAML `name` value verbatim, with case-sensitive comparison. The planned `PlayerStartupData.ShipId` default is `fighter`, matching the packaged YAML name. References to a ship ID throughout this epic mean this name; there is no separate YAML `id` field. Renaming a ship changes its identity.

**Refinement progress**

All multiplayer scenarios use at least three players: one host (H) and two clients (A and B). After all joins complete, every instance contains three participants and three ship entities, with one ship beneath each participant. All may use the same `fighter` type; each ship has its own entity ID and mutable state. The following stationary-remote behavior describes only the Stories 1–7 milestone; the separate synchronization plan replaces it with synchronized movement.

| Running instance | H's ship | A's ship | B's ship |
|---|---|---|---|
| Host H | Locally controlled | Present, stationary | Present, stationary |
| Client A | Present, stationary | Locally controlled | Present, stationary |
| Client B | Present, stationary | Present, stationary | Locally controlled |

Remote ships remain at their approved spawn transforms, including when their owners move on another instance. They retain their render/collision representation but do not run movement updates or get displaced by local collisions. Movement begins for each instance's own ship in Story 7; Stories 1–6 establish the data, entities, spawning, and local binding. Multiplayer join, late-join, duplicate-delivery, disconnect, and restart scenarios must cover both clients, not just a host/client pair.

- Stories 1–6: refinement complete; implementation pending. See the linked stories below.
- Story 7: refinement in progress, revised to local input and movement.
- Story 8 — camera following (formerly Story 9): refinement complete; implementation pending.
- Synchronization (formerly Story 8): refinement documented in five separate stories; implementation pending. Implementation checks and playtest tuning remain explicit.
- Original Stories 10–11 — movement effects and broader lifecycle verification: out of current scope; retained as future ideas after the active plans work.

The local-control scope correction also updates references in Stories 2–6; the former host-only movement assumption is superseded for the first milestone. The separate synchronization plan subsequently introduces host-authoritative simulation together with immediate client prediction, not host-only responsiveness.

Refinement complete means the implementation plan is agreed, not that the story has been implemented.

---

**Story 1 — Load typed ship definitions through the existing loading pipeline**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 1 — Load typed ship definitions](../multiplayer-ships-story-01-load-typed-definitions.md).

---

**Story 2 — Build the central `ShipRegistry` and include it in compatibility checks**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 2 — Central ShipRegistry](../multiplayer-ships-story-02-ship-registry.md).

---

**Story 3 — Extract the local `PlayerController`**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 3 — Local PlayerController](../multiplayer-ships-story-03-local-player-controller.md).

---

**Story 4 — Separate participant identity from the ship body**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 4 — Participant identity and ship ownership](../multiplayer-ships-story-04-participant-and-ship.md).

---

**Story 5 — Instantiate ship entities from `ShipData`**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 5 — Construct configured ships](../multiplayer-ships-story-05-ship-construction.md).

---

**Story 6 — Extend the existing multiplayer spawn protocol**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 6 — Multiplayer spawn protocol](../multiplayer-ships-story-06-spawn-protocol.md).

---

**Story 7 — Control the local ship through PlayerController**

Covered in a separate story file; refinement in progress, implementation pending.

Implementation plan: [Story 7 — Local ship control](../multiplayer-ships-story-07-local-ship-control.md).

---

**Separate synchronization plan — formerly Story 8 (refined and split)**

*As a participant, I see consistent ship movement across all game instances.*

Refinement discussion finished; plans documented, implementation pending. This remains outside the first local-control milestone and follows its required user playtest.

Whole plan: [Multiplayer synchronization summary](../multiplayer-synchronization-story-summary.md). The numbering below belongs to the synchronization plan. Camera following is now Story 8 in the local ships plan; original Stories 10–11 remain deferred ideas.

| Synchronization story | Refinement status |
|---|---|
| [1 — Network timing and snapshot scheduling](../multiplayer-synchronization-story-01-network-timing-and-snapshot-scheduling.md) | Finished and documented; implementation pending. |
| [2 — Client input delivery and authoritative host movement](../multiplayer-synchronization-story-02-client-input-and-host-movement.md) | Finished and documented; implementation pending. |
| [3 — Local prediction, reconciliation, and correction smoothing](../multiplayer-synchronization-story-03-prediction-and-reconciliation.md) | Finished and documented; implementation pending. |
| [4 — Remote ship interpolation](../multiplayer-synchronization-story-04-remote-ship-interpolation.md) | Finished and documented; implementation pending. |
| [5 — Synchronization lifecycle and network-condition verification](../multiplayer-synchronization-story-05-lifecycle-and-verification.md) | Finished and documented; implementation pending. |

The host simulates all ships at 60 Hz and publishes full movement snapshots after every second completed physics step (30 Hz). Clients send tick-addressed input batches at 30 Hz, predict locally at 60 Hz, reconcile meaningful state differences, and interpolate remote ships. Joining is fresh, with current-state baselines for existing players; persistence is deferred. Keep existing colliders and movement; custom crash response and advanced collision prediction await playtest findings.

The plans retain explicit implementation checks for Godot replay integration, movement-state completeness, bounded timing/history recovery, and playtest-tuned thresholds. Synchronization lifecycle coverage belongs to synchronization Story 5; original Story 11 is deferred broader integration and regression work. Its deferral does not remove cleanup or validation required by the active stories.

Dependencies: Stories 6 and 7.

---

**Story 8 — Follow the local ship with a persistent camera (formerly Story 9)**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 8 — Camera following](../multiplayer-ships-story-08-camera-follow.md), included in the [local ships summary](../multiplayer-ships-story-summary.md).

Use one persistent local `Camera2D` with a fixed top-down angle. Follow the controlled ship's displayed position using Godot's built-in position smoothing. Retarget on control changes, snap on assignment/replacement/teleport, and clean up safely on removal or session exit. Remote ships never change the target.

Dependencies: Stories 3–5 and 7. Synchronization presentation integrates when available; it does not block local following.

---
**Original Story 10 — Animate ships from applied movement state (out of scope)**

Deferred for reconsideration after the active ships and synchronization plans work. The outline below is retained as a future idea, not an active implementation commitment.

*As a local player, I can see when my ship is thrusting or using its afterburner.*

Implementation:

- Produce presentation state from simulation: applied thrust direction, turning, and afterburner activation.
- Keep presentation state available for future synchronization; replicating its state belongs to the separate synchronization plan.
- Have `ShipVisuals` consume that state.
- Begin with shared thruster effects suitable for the static texture currently defined by the schema.
- Keep animation selection and playback local to rendering.

Acceptance criteria:

- The locally controlled ship displays effects corresponding to its applied movement.
- Requested but unavailable actions do not show active effects.
- Coasting does not automatically display active thrust.
- Effects return to idle when local input becomes neutral or control is disabled.
- Animation code never reads local input directly.

Validation: state-to-effect checks on each instance's locally controlled ship.

Dependencies: Stories 5 and 7.

---

**Original Story 11 — Complete lifecycle integration and regression verification (out of scope)**

Deferred for reconsideration after the active plans work. Required cleanup and verification in each active story, including synchronization Story 5, remain in scope. The outline below is retained for future refinement.

*As a player, I can join, leave, and start another session without stale ships or controls.*

Implementation:

- Integrate participant, ship, camera, and input cleanup.
- Verify rollback after partial spawn or join failure.
- Exercise ship replacement through a test harness; a respawn gameplay system is not required.
- Update existing spawning smoke tests and relevant UI/compatibility tests.
- Document system ownership, startup order, and local input/state contracts.

Acceptance criteria:

- A host and two clients each see three participants and three ships.
- Each instance controls and follows only its own ship.
- A late joiner receives the participant/ship roster and spawn configuration; live movement state is covered by the separate synchronization plan when integrated.
- Disconnect removes the correct participant and ship.
- Host loss returns clients through the existing session-exit flow.
- Starting another session does not reuse old entities, input, camera targets, or subscriptions.
- Invalid ship data and incompatible peers fail cleanly.
- Existing inventory, hotbar, menus, and messages still work.

Future dependencies: local ships Stories 1–8 and movement effects if included. Revisit this outline when reactivating it; synchronization-specific lifecycle verification already belongs to synchronization Story 5.

---

**Delivery sequence**

| Milestone | Stories | Reviewable result |
|---|---|---|
| Data foundation | 1–2 | Central `ShipRegistry` with validated ship types participating in compatibility checks. |
| Local architecture | 3–5 | Separate local player, participants, and configurable ship entities. |
| Spawning and local control | 6–7 | Host and clients spawn ships and each locally controls its own ship. |
| Major milestone and required user playtest | 1–7 complete | Three-player spawning and local control are ready for hands-on user evaluation. |
| Local camera follow-up | Local ships Story 8 | Fixed-angle camera following with native position smoothing. |
| Deferred future ideas | Original Stories 10–11 | Movement effects and broader lifecycle verification; out of current scope. |
| Separate synchronization follow-up | Synchronization Stories 1–5 (formerly epic Story 8) | Implement the documented host-authoritative movement, prediction, interpolation, and synchronization lifecycle plan. |

The first major milestone is reached when Stories 1–7 are implemented: a host and two clients can join using YAML-defined ships, every instance contains all three ships, and each player locally controls only their own ship while the other two remain stationary. This describes the delivery target, not the current implementation status.

**Required user playtest after Stories 1–7**

Pause progression to follow-up stories and have the user test the milestone with at least one host and two clients. Automated checks do not replace this hands-on test.

- Verify all three ships spawn on every instance, including when the second client joins late.
- On each instance, check local movement, aiming, braking, and afterburner behavior; the other two ships must remain stationary at their spawn transforms.
- Check input blocking and focus loss, and verify remote joins do not change the locally controlled ship.
- Exercise disconnect and session restart to check the lifecycle behavior already required by Stories 1–7.
- Record the user's findings and resolve milestone-blocking issues before choosing the next follow-up work.

Camera following from local ships Story 8 is the next follow-up and is not required for this first milestone playtest. Movement effects (original Story 10) are out of current scope. Remote motion and cross-peer collision consistency remain outside acceptance until synchronization is separately refined and implemented.
