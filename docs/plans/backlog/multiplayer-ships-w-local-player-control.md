**Epic: YAML-defined multiplayer ships with local player control**

Enable every participant, including the host, to spawn and locally control a ship defined in YAML. Each running game has one local `PlayerController`, one `MpController` per participant, and a separate ship entity for each participant.

Stories 1–7 form the first deliverable epic scope. Completing their implementation reaches a major milestone and requires a user playtest before proceeding to follow-up work. Stories 8–11 remain listed as follow-up planning; camera following, movement effects, and broader lifecycle verification do not block this first milestone.

**Stories 1–7 are covered by separate story files**, indexed in the [Stories 1–7 summary plan](multiplayer-ships-story-summary.md). Covered means documented, not implemented; Story 7 still has refinement items open.

The first milestone processes input locally each frame and moves the locally controlled ship in local physics updates. Movement synchronization is separate follow-up work, documented as five stories under the [Story 8 synchronization plan](../multiplayer-synchronization-story-summary.md). That follow-up uses host-authoritative movement, local client prediction/reconciliation, and remote interpolation. Cross-peer movement/effect consistency, prediction/reconciliation, combat, a ship-selection screen, and multiplayer inventory synchronization are outside the first milestone. Participant and ship spawning remain networked through Story 6.

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

All multiplayer scenarios use at least three players: one host (H) and two clients (A and B). After all joins complete, every instance contains three participants and three ship entities, with one ship beneath each participant. All may use the same `fighter` type; each ship has its own entity ID and mutable state. The following stationary-remote behavior describes only the Stories 1–7 milestone; Story 8 replaces it with synchronized movement.

| Running instance | H's ship | A's ship | B's ship |
|---|---|---|---|
| Host H | Locally controlled | Present, stationary | Present, stationary |
| Client A | Present, stationary | Locally controlled | Present, stationary |
| Client B | Present, stationary | Present, stationary | Locally controlled |

Remote ships remain at their approved spawn transforms, including when their owners move on another instance. They retain their render/collision representation but do not run movement updates or get displaced by local collisions. Movement begins for each instance's own ship in Story 7; Stories 1–6 establish the data, entities, spawning, and local binding. Multiplayer join, late-join, duplicate-delivery, disconnect, and restart scenarios must cover both clients, not just a host/client pair.

- Stories 1–6: refinement complete; implementation pending. See the linked stories below.
- Story 7: refinement in progress, revised to local input and movement.
- Story 8: refinement discussion finished and documented in five separate synchronization stories; implementation pending. Implementation checks, tuning values, and the measured playtest envelope are explicitly retained in those plans.
- Stories 9–11: follow-up work awaiting refinement after the Stories 1–7 milestone and user playtest.

The local-control scope correction also updates references in Stories 2–6; the former host-only movement assumption is superseded for the first milestone. Story 8 subsequently introduces host-authoritative simulation together with immediate client prediction, not host-only responsiveness.

Refinement complete means the implementation plan is agreed, not that the story has been implemented.

---

**Story 1 — Load typed ship definitions through the existing loading pipeline**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 1 — Load typed ship definitions](multiplayer-ships-story-01-load-typed-definitions.md).

---

**Story 2 — Build the central `ShipRegistry` and include it in compatibility checks**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 2 — Central ShipRegistry](multiplayer-ships-story-02-ship-registry.md).

---

**Story 3 — Extract the local `PlayerController`**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 3 — Local PlayerController](multiplayer-ships-story-03-local-player-controller.md).

---

**Story 4 — Separate participant identity from the ship body**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 4 — Participant identity and ship ownership](multiplayer-ships-story-04-participant-and-ship.md).

---

**Story 5 — Instantiate ship entities from `ShipData`**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 5 — Construct configured ships](multiplayer-ships-story-05-ship-construction.md).

---

**Story 6 — Extend the existing multiplayer spawn protocol**

Refinement complete; implementation pending.

Agreed implementation plan: [Story 6 — Multiplayer spawn protocol](multiplayer-ships-story-06-spawn-protocol.md).

---

**Story 7 — Control the local ship through PlayerController**

Covered in a separate story file; refinement in progress, implementation pending.

Implementation plan: [Story 7 — Local ship control](multiplayer-ships-story-07-local-ship-control.md).

---

**Story 8 — Synchronize ship movement (refined and split)**

*As a participant, I see consistent ship movement across all game instances.*

Refinement discussion finished; plans documented, implementation pending. This remains outside the first local-control milestone and follows its required user playtest.

Whole plan: [Multiplayer synchronization summary](../multiplayer-synchronization-story-summary.md). The numbering below belongs to the synchronization plan and does not renumber original Stories 9–11.

| Synchronization story | Refinement status |
|---|---|
| [1 — Network timing and snapshot scheduling](../multiplayer-synchronization-story-01-network-timing-and-snapshot-scheduling.md) | Finished and documented; implementation pending. |
| [2 — Client input delivery and authoritative host movement](../multiplayer-synchronization-story-02-client-input-and-host-movement.md) | Finished and documented; implementation pending. |
| [3 — Local prediction, reconciliation, and correction smoothing](../multiplayer-synchronization-story-03-prediction-and-reconciliation.md) | Finished and documented; implementation pending. |
| [4 — Remote ship interpolation](../multiplayer-synchronization-story-04-remote-ship-interpolation.md) | Finished and documented; implementation pending. |
| [5 — Synchronization lifecycle and network-condition verification](../multiplayer-synchronization-story-05-lifecycle-and-verification.md) | Finished and documented; implementation pending. |

The host simulates all ships at 60 Hz and publishes full movement snapshots after every second completed physics step (30 Hz). Clients send tick-addressed input batches at 30 Hz, predict locally at 60 Hz, reconcile meaningful state differences, and interpolate remote ships. Joining is fresh, with current-state baselines for existing players; persistence is deferred. Keep existing colliders and movement; custom crash response and advanced collision prediction await playtest findings.

The plans retain explicit implementation checks for Godot replay integration, movement-state completeness, bounded timing/history recovery, and playtest-tuned thresholds. Synchronization lifecycle coverage belongs to synchronization Story 5; original Story 11 retains broader integration and regression verification. Original Stories 9–11 are not marked refinement-complete by this split.

Dependencies: Stories 6 and 7.

---

**Story 9 — Follow the local ship with a persistent camera**

*As a player, my camera stays centered above my current ship.*

Implementation:

- Add `CameraFollow` under the local player setup.
- Subscribe to `ControlledShipChanged`.
- Follow global position while keeping the view upright.
- Place the camera in the viewport that renders the gameplay world.
- Follow the locally displayed ship position to avoid camera/ship jitter. Network interpolation is deferred to Story 8.
- Snap immediately on initial assignment and replacement.
- Clear the target before ship removal; hold the last position while no ship is assigned.

Acceptance criteria:

- Each gameplay instance has one active local gameplay camera.
- Remote spawns never change its target.
- Ship rotation does not rotate the camera.
- Replacement retargets the existing camera.
- Despawn and session cleanup leave no invalid target access.
- Camera motion and ship rendering remain aligned.

Validation: host/client visual checks, replacement, and disconnect scenarios.

Dependencies: Stories 4, 5, and 7.

---

**Story 10 — Animate ships from applied movement state**

*As a local player, I can see when my ship is thrusting or using its afterburner.*

Implementation:

- Produce presentation state from simulation: applied thrust direction, turning, and afterburner activation.
- Keep presentation state available for future synchronization; replicating it is deferred to Story 8.
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

**Story 11 — Complete lifecycle integration and regression verification**

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
- A late joiner receives the participant/ship roster and spawn configuration; live remote movement state is deferred to Story 8.
- Disconnect removes the correct participant and ship.
- Host loss returns clients through the existing session-exit flow.
- Starting another session does not reuse old entities, input, camera targets, or subscriptions.
- Invalid ship data and incompatible peers fail cleanly.
- Existing inventory, hotbar, menus, and messages still work.

Dependencies: Stories 1–7, 9, and 10. This is follow-up lifecycle verification; it does not block the Stories 1–7 milestone. Deferred Story 8 does not block this follow-up either.

---

**Delivery sequence**

| Milestone | Stories | Reviewable result |
|---|---|---|
| Data foundation | 1–2 | Central `ShipRegistry` with validated ship types participating in compatibility checks. |
| Local architecture | 3–5 | Separate local player, participants, and configurable ship entities. |
| Spawning and local control | 6–7 | Host and clients spawn ships and each locally controls its own ship. |
| Major milestone and required user playtest | 1–7 complete | Three-player spawning and local control are ready for hands-on user evaluation. |
| Presentation and lifecycle follow-up | 9–11 | Local camera, movement effects, and broader lifecycle verification. |
| Separate synchronization follow-up | 8, split into synchronization Stories 1–5 | Implement the documented host-authoritative movement, prediction, interpolation, and synchronization lifecycle plan. |

The first major milestone is reached when Stories 1–7 are implemented: a host and two clients can join using YAML-defined ships, every instance contains all three ships, and each player locally controls only their own ship while the other two remain stationary. This describes the delivery target, not the current implementation status.

**Required user playtest after Stories 1–7**

Pause progression to follow-up stories and have the user test the milestone with at least one host and two clients. Automated checks do not replace this hands-on test.

- Verify all three ships spawn on every instance, including when the second client joins late.
- On each instance, check local movement, aiming, braking, and afterburner behavior; the other two ships must remain stationary at their spawn transforms.
- Check input blocking and focus loss, and verify remote joins do not change the locally controlled ship.
- Exercise disconnect and session restart to check the lifecycle behavior already required by Stories 1–7.
- Record the user's findings and resolve milestone-blocking issues before choosing the next follow-up work.

Camera following and movement effects from Stories 9–10 are not required for this playtest. Remote motion and cross-peer collision consistency remain outside acceptance until synchronization is separately refined and implemented.
