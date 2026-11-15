# Weapons | EnergyPerShot and Reloading

Status: implemented on 2026-11-15; build/run verification and remaining coverage limits recorded below.
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)
Story: 6 of 7
Dependencies: Story 5 (implemented; its recorded verification limitations still apply).

## Outcome

Enforce authored supply on the host and synchronize the result. Laser consumes energy per accepted shot; Vulcan consumes magazine rounds and reloads from unlimited reserves. Owner replicas receive authoritative supply state independently of cosmetic events. Visualization is outside this story.

## Current project state

Source review, not a new runtime verification run:

| Area | Already present | Remaining work |
| --- | --- | --- |
| Definitions | `light_weapon.yml` version 3.0.0: laser cost 4, rate 3/s; Vulcan capacity 60, reload 1500 ms, rate 15/s. Continuous demand is 2 and 1 respectively. Typed energy/ammunition supply definitions exist. | Consume these definitions; no schema migration or rebalance. |
| Firing | `WeaponSimulation` admits host shots after movement, checks power and configuration, and uses item-owned `WeaponCadence`. | Gate acceptance atomically on supply; progress reloads independently of held fire. |
| Item lifetime | `ItemEntity.WeaponState` currently contains cadence only. `ItemContainerState`, `ItemStore`, and world restoration preserve it. | Extend item-owned state with rounds/reload and revision; initialize only genuinely new items. |
| Replication | `NetworkManager.WeaponState` publishes reliable absolute equipped-owner cadence. Item baselines carry cadence; story 5 records protocol 12. | Extend existing streams/baselines, not a second competing state system. Shot-only change detection and sequence ordering cannot represent reload-only changes. |
| Resources | Runtime regeneration, continuous demand, resource revisions, and power conditions exist. Gameplay publication still uses world-level dirty publication. | Add atomic full-cost spending and changed-entity publication for ready peers. |
| Input | `PrimaryFire` and `SecondaryFire` use current context arbitration. No reload input is exposed by `InputController`. | Add configurable reload action through existing binding/input infrastructure; preserve current firing actions. |

The epic contains historical input names, protocol assumptions, and story statuses. Current source and this story's explicit scope take precedence for this increment; do not recreate earlier stories from the epic checklist.

## Refinement decisions (2026-11-15)

- User clarification: visualization is currently out of scope. Do not add ammo displays, reload bars, energy-starvation feedback, or an in-flight weapon HUD. The configurable reload action remains in scope through the existing binding menu.
- User delegated the recovery/control-change policy to established networking practice. Adopt host-owned simulation deadlines and persistent absolute state. An existing reload continues while its weapon remains equipped on a powered, enabled ship, even without a ready controlling player. Readiness gates new commands and firing, not the lifetime of an already accepted action.
- Recovery returns current rounds and the original reload deadline, or the completed/cancelled result. It never restarts the reload. Switching control leaves the old ship's reload in place; moving the weapon to another ship cancels it because that is an unequip. Destroying the item/world ends its state normally.

The underlying patterns are established: tick deadlines avoid transmitting a changing countdown ([Photon TickTimer](https://doc.photonengine.com/fusion/v2/manual/fusion-types/ticktimer)), and persistent state must synchronize to late joiners independently of command/event RPCs ([Unity state and event synchronization](https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/ways-synchronize/)). Continuing an equipped weapon's reload across control loss is this project's application of those patterns, not a claim that every game uses the same cancellation rules. Keep the existing GodotSteam transport and project codecs.

## Scope and behavior

### Shot admission and energy

- Add `ShipRuntime.TrySpendEnergy` as a production operation: require the full authored cost, mutate once, and refresh affected resource revisions/power conditions and movement availability. Failure changes no energy, accepted-shot sequence, muzzle selection, or effect output.
- Preserve zero initial stored energy and continuous equipped demand. Insufficient shot charge is a weapon availability reason, not automatically ship-wide energy depletion.
- Use generation from the same physics tick. Resolve weapons in ascending slot order across both groups, rechecking power and affordability after every payment. A weapon in both groups pays and fires once.
- Commit supply and cadence as one admission: no spending on cooldown, stale input, disabled systems, or rejected shots. Hits and misses pay equally. Starvation must not produce catch-up bursts or reset an unexpired cooldown.

### Magazine and reload lifecycle

- Store rounds and reload start/deadline on the item, alongside existing cadence. Ammo changes never use item counts or increment inventory/container/equipment revisions.
- New Vulcan starts with 60 rounds. Every accepted shot consumes one; the last round starts automatic reload at that shot's tick. An empty equipped weapon also starts reload when its group is held and power is available.
- Convert authored milliseconds to 60 Hz physics ticks, rounding up so completion never occurs early (1500 ms = 90 ticks). These are simulation ticks, not 30 Hz publication ticks.
- Manual reload targets all equipped eligible ammunition weapons, including weapons assigned to neither group. Ignore full magazines, energy weapons, and weapons already reloading. Partial magazines keep their rounds until completion; firing is blocked during reload. Duplicate requests cannot restart or extend it.
- Complete an eligible reload before firing on its deadline tick. Completion fills to authored capacity; UI never performs this transition.
- Unequip, fixture/weapon-system removal, and power loss cancel reload without filling. Check cancellation before completion, including on the deadline tick. Preserve rounds, shot sequence, and unexpired cooldown through transfer/re-equip; a new slot occupant never inherits state.
- Restored equipped reloads resume in the preserved tick domain after validation; unequipped surviving items retain their rounds/cadence without an active reload. Do not initialize restored items as new weapons.
- Focus loss, trigger release, connection recovery, and control reassignment stop or gate firing but do not cancel a valid equipped reload. After power returns, an empty held weapon can start a fresh reload; a partial magazine requires a new manual request. Story 7 integrates death cancellation.

### Requests, state, and recovery

- Add a configurable `ReloadWeapons` action, default R, using existing binding infrastructure and context arbitration. The current loader requires matching action sets; handle the new action explicitly while preserving existing user bindings. Send one request per press; suppress during UI/binding capture, targeting, focus loss, or unavailable control. Held input consumed by a blocked context must not leak into a later reload.
- Use bounded reliable idempotent requests carrying session, numeric ship reference, control generation, request sequence, expected equipment revision, and affected item identities. Validate and enqueue at receipt; apply inside authoritative simulation after equipment changes. Host-local requests follow the same path. Duplicates return a stable result; stale requests cannot reload replacement occupants.
- Extend existing absolute owner state with rounds, reload start/deadline, and an independent monotonic state revision. Publish reload start, cancellation, and completion even when no shot occurs. Reject stale state even when shot sequence is unchanged; validate the whole batch before mutation.
- Preserve cadence/sequence and binding validation. Coalesce current values at 30 Hz; use arrays/scalars and predefined positional schemas. Reuse bounded buffers and recalculate payload ceilings. Bump the actual protocol version when changing wire formats (12 at this review).
- Extend complete item/join/recovery baselines and world capture, including unequipped items. Install identities before dependent state and preserve readiness/released-trigger gates and cosmetic baseline cutoffs.
- Optimize gameplay publication to changed entity rows for ready peers. Keep complete baselines and complete snapshots for non-ready joiners, whose current path rejects partial gameplay rows. Energy spending must converge independently of shot effects.

### Presentation boundary

Preserve existing firing effects and equipment/group controls. No supply visualization is implemented here. Replicate authoritative rounds and reload start/deadline so a later visualization story can consume them without changing gameplay ownership. Do not add a presentation-only progress field or tick a replicated countdown.

## Detailed implementation plan

The following records the implementation design authorized by the user. Type names and wire layouts below are proposed concrete designs; final names may follow existing conventions without changing the contracts.

### 1. Establish item-owned runtime state

Introduce `WeaponRuntimeState` in `WeaponSystem`, composing the existing `WeaponCadence`. Change `ItemEntity.WeaponState` to hold this complete state; retain the tested cadence algorithm rather than duplicating it. Update existing callers, capture, and restore together.

| State | Purpose and invariant |
| --- | --- |
| `Revision` | Monotonic per-item version, independent of shot sequence and container revisions. Increment for each committed supply/cadence transition; no increment for a no-op. |
| Existing cadence | Preserve next eligible tick, accepted-shot sequence, and last accepted tick. Keep fractional scheduling and no-catch-up behavior. |
| `Rounds` | Ammunition items: integer between zero and authored capacity. Energy items: canonical zero. |
| `ReloadStartTick`, `ReloadDeadlineTick` | Both inactive sentinels when idle; otherwise a valid start and checked future deadline. Energy items never have an active reload. |

Create ammunition items full only on genuine creation. Replica installation and restoration explicitly install captured state rather than relying on constructor defaults. Deep-copy the entire runtime state in `ItemContainerState` and `ItemStore`; leave mutable execution state outside `ItemEntity` placement/value equality.

Precompute reload duration in ticks when preparing executable bindings: `max(1, ceil(milliseconds * 60 / 1000))`, with overflow and deadline bounds validated. Continue limiting execution to Laser Mk1 and Vulcan Mk1; typed state does not authorize firing medium/heavy mocks.

### 2. Add atomic energy spending

Implement `ShipRuntime.TrySpendEnergy` using the existing resource pool. Validate the cost, check affordability before mutation, refresh derived power conditions, increment runtime revision once, and update configuration revision only when movement availability changes. Failed spending leaves resources and revisions untouched.

Keep existing resource generation, battery replacement behavior, and continuous equipment demand. After shot spending changes movement availability, refresh the ship's movement configuration before publication; do not run movement twice. Guard revision/deadline arithmetic before committing a shot so an overflow cannot leave a half-paid admission.

### 3. Integrate reload lifetime with equipment and components

Add start, cancel, and complete operations to item runtime state. Route automatic and manual starts through the same operation. Repeated start/cancel/complete attempts are idempotent, and completion clears its deadline so it cannot fill again on subsequent ticks.

Integrate cancellation with the authoritative equipment transaction's successful commit, before observers/snapshots can see the moved item. Use `EquipmentController`/loadout preparation hooks; never mutate during a transfer preview or failed transaction. This covers fixture removal and an unequip/re-equip within the same tick, which a final-snapshot comparison would miss. Group-only edits do not cancel reloads.

The existing component lifecycle has no `Weapons` flag. Add an explicit weapon-availability bit to `ShipComponentKind` and honor it in admission and reload maintenance to implement the epic's weapon-system removal rule. Update component-mask validation, capture/restore, replication, and tests consistently. Removing this capability cancels reloads but retains item rounds and cooldown; reattachment does not refill.

### 4. Split reload maintenance from shot admission

Extend `GameplaySimulation` and `WeaponSimulation` with a pre-movement reload phase. Process existing reloads for equipped weapons on world ships independently of player readiness. Keep new reload requests and automatic starts gated by valid authority/input as appropriate.

| Order within a 60 Hz host step | Work |
| --- | --- |
| 1 | Apply queued item transfers and observe current equipment/group revisions; cancelled outgoing bindings cannot survive a successful transfer. |
| 2 | Commit configuration and process existing ability commands in their established order. |
| 3 | Advance resources once; reconcile power/weapon availability and cancel invalid reloads before testing deadlines. |
| 4 | Apply validated manual reload requests, then complete eligible deadlines. A duplicate request on a completion tick cannot restart the active reload. |
| 5 | Refresh movement configuration and simulate movement once. |
| 6 | Admit shots in stable ship and ascending slot order, rechecking power after every accepted payment. |
| 7 | Finish the tick and publish coalesced state at the existing 30 Hz cadence. |

For each candidate shot, validate readiness, binding, held group, cooldown, reload, power, and full supply. Prepare the existing hitscan result, then commit payment and cadence exactly once before emitting the accepted event. A failed admission cannot advance muzzle/spread sequence. Emptying a magazine starts reload on that shot's tick. If energy spending removes power, cancel all remaining active reloads on that ship immediately, including weapons in earlier slots; refresh movement configuration for publication.

Keep the existing sorted weapon bindings: `WeaponGroupConfiguration` already guarantees ascending slot order. Do not add a recurring sort or scan the entire item store each tick.

### 5. Implement bounded reliable reload commands

Add dedicated reload request/result handling to `NetworkManager` and a bounded simulation queue. Use the existing entity-index map, not string ship identifiers. Proposed positional request: `[session, shipIndex, controlGeneration, requestSequence, equipmentRevision, itemIds[]]`; item IDs are unique, ordered by equipped slot, and limited to nine supported weapon bindings.

The client identifies equipped ammunition items; the host decides which currently need reload. A full, energy, or already-reloading item is a no-op. Reject an invalid binding or malformed request before changing any weapon. Validate ownership, session, generation, and configuration again when dequeuing, after equipment changes. A stale request cannot act on replacement items. Manual reload does not require group membership.

Use the bounded `ActivationHistory` approach as a reference, with reload-specific outcomes. Initial limits: 32 queued requests globally, 32 retained results per peer, and at most one admitted new request per peer per six physics ticks. Duplicates consult retained outcomes before rate limiting; pending duplicates are not enqueued again. Evicted old requests return an old-request result and never execute. Queue overflow returns a stable busy result. Validate positive bounded sequences before indexing history.

Host-local reload uses the same queue and validation. Requests/replies carry enough binding identity to ignore late acknowledgements. Clear pending commands on peer removal, recovery, control-generation changes, and session disposal; keep an appropriate sequence high-water mark within the session so delayed requests cannot replay after a reset. None of this command history is restored as gameplay state.

### 6. Wire configurable reload input

Add `ReloadWeapons` to `config/config.cfg`, the required action catalogue, and relevant project input defaults. Sample a fresh press through `InputController`/`PlayerController`, using the existing flight context, pointer/targeting filter, and release-after-block behavior. Honour both configured binding slots. Send a reliable command once; do not add reload to the held-fire mask or prediction replay.

Add a narrow upgrade for otherwise valid configurations missing only the new action. Preserve existing bindings and metadata. Use R when it is free in the flight context; if a user already assigned R there, leave the new action unbound through the existing binding UI rather than replacing their mapping. Choose a free menu order for the inserted entry. Continue rejecting unrelated invalid/missing configuration instead of introducing a general permissive fallback. Preserve the established explicit settings-save path.

### 7. Extend owner replication and protect baselines

Replace `_publishedWeaponStateTicks` shot-based detection in `NetworkManager.WeaponState` with item identity plus runtime revision tracking, scoped to the current owner binding. Coalesce reliable absolute state at 30 Hz, including reload-only transitions. Update publication markers only after a successful send; invalidate them on binding changes/baseline resets.

Proposed owner row: `[itemId, stateRevision, nextEligibleTick, shotSequence, lastAcceptedTick, rounds, reloadStartTick, reloadDeadlineTick]`. Keep session, ship/control binding, equipment revision, and completed host tick in the envelope. Capacity and duration remain in definitions; do not resend them on every shot. Bound rows to the equipped weapon ceiling and validate the complete batch, types, identities, supply mode, tick bounds, and revisions before mutation. Equal revisions must represent identical state; lower revisions cannot roll state back.

Extend `ItemSnapshotCodec` with the same canonical runtime-state representation for inventory and equipped items. Compare runtime revisions when confirming an already-known item: a delayed full item snapshot must not replace a newer supply state merely because its container revision is still valid. Preserve authoritative placement while retaining newer runtime state where appropriate. A fresh join/recovery baseline must install a coherent complete state before readiness.

Extend world restoration validation for magazine bounds, reload deadlines in the captured tick domain, supply mode, and equipped/system/power relationships. Reject active reloads on unequipped items rather than silently refilling or accepting invalid saved state. Restore all surviving weapon items, including inventory items. No per-shot stream for private remote inventories is needed.

### 8. Publish only changed gameplay rows to ready peers

In `NetworkManager.Gameplay`, collect entities whose runtime revisions changed and send only those rows to ready recipients. Retain complete `GameplaySnapshot()` output for initial/recovery baselines and non-ready peers receiving world state. Preserve topology-before-dependent-state ordering and existing send-failure behavior.

Reuse publication/scratch buffers where practical; clear revision tracking on topology/session changes. Do not split the whole gameplay protocol into new subsystems. This step ensures repeated energy spending does not force unchanged world rows into every publication, while preserving reliable final resource convergence.

### 9. Update compatibility and verify the complete slice

Bump `Compatibility` protocol 12 to the next available version (13 if unchanged). Recalculate new request/state/baseline bounds and add exact-limit tests; preserve current movement and shot-event layouts unless an actual dependency requires a change. Check every component mask and item-state codec consumer, including remote equipment snapshots.

| Verification layer | Required evidence |
| --- | --- |
| Runtime/unit | Exact and insufficient energy, invalid costs, revisions/power transitions, round bounds, reload no-ops/cancellation, duration conversion, capture isolation, fractional cadence and no catch-up. |
| Godot simulation | Full pipeline timing at 236/326; simultaneous mixed weapons, stable slot payment, same-tick power loss, no-group manual reload, and unchanged no-damage behavior. |
| Transactions/lifecycle | Failed/preview transfers leave reload intact; successful unequip, same-tick re-equip, fixture removal, component removal, control switch, and unowned/recovering ships follow the stated rules. |
| Network | Duplicate/expired requests, queue/rate limits, bad ownership/session/generation/revision, malformed batches, unchanged shot sequence with newer reload state, stale full snapshots, and send failure. |
| Baseline/restoration | Join/recovery during a reload and after its deadline; inventory state; complete vs partial gameplay snapshots; current state without historical effects or request replay. |
| Input/regression | Both binding slots, held-key suppression, targeting/focus/UI capture, existing settings upgrade and conflicts; no regression to group controls or firing input. |
| Multiplayer/performance | Five/ten peers, one/nine supported weapons, mixed supply and repeated reload cycles, impaired delivery, joining/recovery; capture timing, allocations, bytes, queue/backlog limits, and actual transport coverage. |

Extend existing `WeaponCadenceTests`, `WeaponSimulationTests`, `GameplaySimulationTests`, resource, item-restoration, network/baseline, and keybinding/input suites. Some current firing tests explicitly assert no supply consumption; update those expectations with deliberate charged fixtures while preserving geometry, sequence, and no-damage assertions. Do not add production supply bypasses to keep old tests passing.

Run focused tests during implementation, then `dotnet test sMPGWM.sln --no-restore --verbosity minimal` and the relevant multiplayer harness scenarios. Preserve test-support markers for any game-side helpers. Record new evidence and explicit GodotSteam coverage limitations; do not reuse story 5's counts as proof of this slice. No visualization work or visual acceptance gate is included.

## Open questions

None currently require a product decision. Visualization was explicitly excluded, and recovery/control-change behavior is resolved by the host-owned deadline policy above. Implementation-time findings may refine internal APIs or buffer limits without reopening the agreed gameplay rules.

## Acceptance and verification

- Exactly four stored energy units permit one laser shot; insufficient charge pays nothing and emits nothing. Test same-tick generation, initial zero energy, continuous demand, competing slots, both groups, and power changes after payment.
- With continuous valid input and power, Vulcan fires at ticks 0, 4, ..., 236; its final round starts reload at 236 and the next shot is possible at 326. No extra cooldown is added after reload.
- Cover manual partial reload, automatic empty reload, duplicate requests, independent simultaneous magazines, cancellation on deadline, power return, unequip/fixture/component removal, transfer, re-equip, and restoration without free refill or cooldown reset. Valid reloads continue through recovery/control loss while their ships remain simulated.
- Verify reload-only updates converge without a sequence change; stale/reordered state cannot undo completion or cancellation. Test malformed batches, ownership/session/generation/revision rejection, and delayed requests after replacement.
- Host and owner converge on energy, rounds, and reload state under packet loss, join, and recovery independently of cosmetic delivery. Include unequipped items and active reload restoration. Verify ready-peer changed rows and complete joining-peer snapshots together.
- Verify remapped reload bindings, blocked contexts, held-key suppression, preserved existing user settings, and unchanged group controls. Visualization is not an acceptance requirement.
- Preserve no-damage behavior: accepted shots and supply failures do not damage health or shields. Preserve existing cadence, input, baseline, restoration, and presentation regressions.
- Run focused tests and the existing regression suite. Exercise five/ten-player mixed-supply reload cycles; record recurring allocations, publication timing, bytes, and reliable backlog. State actual ENet and GodotSteam transport coverage separately; earlier story evidence does not validate these changes.

## Excluded

Ammo/reload/energy-starvation visualization, equipment supply displays, a weapon HUD, damage/death implementation, ammunition inventory items, finite reserves, additional weapon types, speculative client firing, new effects, and rebuilding existing grouping or shot replication. Death cancellation integration and full combat performance validation remain in story 7.

## Reference and verification record

The epic retains end-state contracts, particularly sections 6.2, 6.3, and 9.2. The original refinement preceded implementation; the verification record below describes the implemented slice. Follow repository test-support markers and record completed implementation evidence and remaining limitations here.


### Implementation record (2026-11-15)

Implemented item-owned `WeaponRuntimeState`, atomic ammunition admission and full-cost energy spending, 60 Hz reload deadlines, automatic/manual reload, successful-transaction cancellation, and explicit Weapons component availability. Reload maintenance runs after resource generation and before movement, independently of player readiness. Death integration is included with story 7.

Reload is configurable (default R), respects both binding slots and blocked-context release gates, and upgrades configurations missing only the new action while preserving existing bindings. Reliable commands use numeric ship references, equipment/item identities, a 32-command queue, 32-result per-peer history and six-tick rate limit. Session replay tombstones are capped at 64 peer identities; no history is restored with world state.

Protocol is now 13. Owner rows contain eight positional fields and are bounded to nine equipped weapons; complete item rows contain ten fields. Independent supply revisions publish reload-only changes; full item snapshots retain newer runtime state and reject conflicting equal revisions or cadence rollback. Ready peers receive changed gameplay rows; joining peers retain complete snapshots. No supply HUD was added.

Regression coverage includes exact/insufficient/invalid energy cost, 236/326 timing through the gameplay pipeline, reload idempotence/cancellation, capture isolation, stale item snapshots, transfer preview versus commit, restored reload deadlines, unready/unowned completion, component removal on the deadline, manual ungrouped reload, malformed/stale commands, and input/settings migration. Build/run and ENet measurements are recorded in [story 7](inc-07_weapons-damage.md#implementation-and-verification-record-2026-11-15).

Coverage limits: the local ENet firing harness checks admitted/delivered shots during mixed-supply reload cycles, not explicit per-item state equality during a mid-reload reconnect. The complete one/nine-weapon, join/recovery, adverse-delivery matrix and live GodotSteam transport session were not run. These remain validation work, not claims inferred from the earlier story's measurements.
