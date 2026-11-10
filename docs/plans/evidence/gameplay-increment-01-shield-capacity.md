# Shield Capacity Boost implementation evidence



Date: 2026-11-10. Runtime: Godot 4.7.2 .NET on Windows.



## Implementation



- `Scripts/GamePlaySystem/GameplayDefinitions.cs` resolves the supported ability/effect/Behaviour/targeting dependency chain from validated YAML into an immutable `ShieldBoostDefinition`. Other authored abilities remain unavailable for execution. Unsupported combinations in the selected chain fail loading.

- `ResourcePool.cs` stores current/base/effective capacity and stable owner contributions. Formula: `max(0, B + sum(flat) + B * sum(fraction))`. Sorted owner traversal makes application order irrelevant. Capacity increases do not heal; decreases clamp current. `ShipGameplayState.cs` owns the active instance, revision and deadline.

- `NetworkManager.Gameplay.cs` admits host and client requests through the same queue. It validates session, request bounds, control generation, caster identity, host-owned development grant, controlled allied target, and authoritative Euclidean distance. Current world movement has no wrapping topology. Constructed controlled ships are alive/allied in this increment; combat, death, condition providers and hostile factions are deferred.

- `SynchronizationCoordinator` expires Behaviours before processing requests on each 60 Hz host step. Duration milliseconds round upward to whole physics ticks. Expiry occurs at `tick >= deadline`; a same-tick application creates a fresh instance after expiration. Refresh preserves the existing instance/contribution and moves its deadline. Movement publication remains every second physics step. Dirty gameplay snapshots are also published at most every second physics step (30 Hz).

- Reliable gameplay snapshots contain entity identity, revision, current/effective Shield, Behaviour instance, deadline and definition ID. Initial join includes gameplay alongside topology and movement before readiness acknowledgement. Topology and full gameplay updates use the same ordered reliable stream; unknown/despawned entities are ignored and never created by gameplay. Newer revisions win; missed intermediate revisions need no replay because snapshots contain complete state. Session GUIDs and unique entity GUIDs exclude previous-session/deleted-entity state. Request caster generation excludes control-transfer races.

- Requests are bounded to 64-character ability IDs and 32-character entity IDs; the queue holds at most 32. Per-peer monotonic high-water marks plus a 32-result ring replay duplicates and reject evicted requests without executing them. Admission is limited to one new execution per six physics ticks. Snapshots reject malformed types, invalid/nonfinite pools, duplicate entities and more than 64 rows. The authored session maximum remains ten participants.

- The development loadout puts Shield Capacity Boost in hotbar slot one. Item/weapon activation routes remain intact. `PlayerStatsPanel` displays confirmed Shield, self/ally selection, selected capacity, countdown and activation feedback. Countdown is presentation only. HUD refresh is capped at 5 Hz and unchanged label/bar values are not reassigned. Session disposal unbinds the view and clears request state.



## Items and loaded content



Every item now supplies the numeric/string `Values` required by its category. The runtime loader rejects missing/extra/wrong-type/nonfinite values and stores read-only collections. Consumables require `AbilityIds`, with references validated against the ability catalogue; targeting belongs to the ability. This does not implement synchronized inventory or consumption.



Battery and weapon-fixture examples fill previously empty authored categories. Example item values are provisional development tuning, not an equipment-stat implementation. Ship tuning is explicitly 100 maximum / 40 current Shield.



Compatibility hashes the actual `FileLoader.CapturedInputs` after loading items, categories, ships, abilities, effects, Behaviours, targeting, attributes and conditions, including the user override text and schemas actually used. The nonexistent `EffectSchemas.yml` reference is removed. Legacy passive files are inactive and excluded; changing those files cannot change supported gameplay. Other authored catalogues are checked by the offline authoring validator but are not executable runtime features.



## Automated checks



- Installed `tools/gameplay-validation-requirements.txt` using Python pip.

- `python tools/validate_gameplay_data.py`: 125 definitions across ten catalogues; nine malformed-content checks rejected.

- `dotnet test sMPGWM.Tests --logger "console;verbosity=normal"`: 242 outer tests passed; the orchestration test additionally reports 158 engine tests passed, zero skipped. Existing explicit stress benchmark remains opt-in.

- Focused coverage includes owner isolation, flat/fraction arithmetic, no healing, clamping, refresh/deadline identity, duplicate replay/cache eviction, stale revisions, normal host session activation, ally selection, out-of-range rejection, expiration and teardown.

- Ten resource states over 3,600 simulation steps: 0.207 ms total and 840 allocated bytes in the measured loop (including stopwatch and expiry work). This is a focused gameplay-state measurement, not a whole-game performance claim. No forced GC is used.

- A ten-ship snapshot with active boost definitions serializes to 1,168 bytes (Godot Variant payload; RPC/transport framing excluded), below the 4 KiB test bound.
- The build retains the pre-existing `UiIcon.Size` member-hiding warning.



The initial gate exposed stale test version/count assumptions, an old ship-scene staging path and missing staged background assets. Tests/staging were updated to the current content and scene paths. No movement implementation was changed to satisfy those tests.



## ENet normal startup



Reproduce after building: `python tools/run_shield_slice.py`. `GODOT_BIN` can override the detected Godot 4.7.2 executable.



The opt-in `--shield-check-host` / `--shield-check-client` harness starts the ordinary main scene and session composition, then emits the existing first-hotbar-button signal. It supplies focus explicitly for headless execution; gameplay input policy and hotbar routing remain in the path. It selects self and other participants, records authoritative identities/deadlines, and checks that local Shield rises to 150 and returns to 100. Every added harness declaration/executable line has an individual `SHIELD-RUN-*` traceability marker referencing this scenario. Development hotbar binding lines use `SHIELD-LOADOUT-*` markers.



The controlled three-player run started host at 0 seconds and clients at 3 and 12 seconds. All three exited successfully. The late join received active boost state without resetting its deadline. Recorded current Shield remained 40 throughout. See `shield-slice-enet.txt` for the state trace. An earlier exploratory run ended the host before a late client's refresh expired; the harness window was corrected and the controlled run passed.



## Ten-player impaired transport



`python tools/run_shield_slice.py --players 10 --impaired` passed with host plus nine clients, all ten processes returning zero and no engine errors. Clients joined from 3 to 6.5 seconds, with the final client joining at 12 seconds. The seeded UDP proxy (seed 1042) applies 20?60 ms one-way delay, 1% packet loss and delay-induced reordering to client traffic. Verification covers self, host-to-client, client-to-host and client-to-client activation, shared refresh, late join, expiry and orderly disconnect. Trace: `shield-slice-enet-10-impaired.txt`. A cross-process trace check compared 6,640 observations across 58 entity revisions: no conflicting current/capacity/instance/deadline values, all current values 40, all effective capacities 100 or 150.



The first impaired run observed correct gameplay but its proxy stopped on a Windows UDP reset during disconnect. The proxy now tolerates resets from closed peers; the complete run was repeated successfully. Each process uses isolated temporary user data. The proxy and process driver carry per-line `SHIELD-PROCESS-*` traceability markers.



The graphical game was launched normally after verification and reached the start menu using D3D12/Forward+ on the RTX 4060 Ti, with no startup errors. It was left running.

## Remaining environment coverage



Steam initialized successfully in ordinary startup. Steam multiplayer transport has not been verified across separate authenticated Steam accounts; ENet evidence must not be treated as Steam transport evidence. Headless button-signal checks do not replace visual/manual pointer testing on multiple machines.

