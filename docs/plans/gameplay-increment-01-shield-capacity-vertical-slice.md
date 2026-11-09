# Gameplay increment 1 — Runtime stats and multiplayer Shield Capacity Boost

Status: planned; implementation has not started.

Date: 2026-11-10.

Companion: [Gameplay design context and decisions](gameplay-design-context-and-decisions.md).

Network foundation: [Synchronization lifecycle and verification](closed/multiplayer-synchronization-story-05-lifecycle-and-verification.md).

## Player outcome

As the host or a client, I can activate Shield Capacity Boost through the hotbar on myself or a nearby allied ship. The host grants 50 additional maximum Shield for 30 seconds. Applying it does not restore current Shield. Reapplication refreshes the duration without stacking the bonus. All participants see consistent authoritative state, including after late join and expiration.

This is a vertical slice through existing UI, authority, runtime stats, modifiers, Behaviour lifetime, replication, and HUD presentation. It is not an implementation of the entire authored catalogue.

## Scope and boundaries

Implement the smallest reusable foundation that supports:

- Immutable, validated definitions for the slice and its dependencies.
- Runtime resource state with current value, base capacity, effective capacity, and source-owned modifier contributions.
- Flat and additive baseline-percentage modifiers with explicit arithmetic and removal ownership.
- Instant ability activation, self-or-ally targeting, host-side admission, request deduplication, and rejection feedback.
- ApplyBehaviour, timed lifetime, RefreshDuration, and +50 Shield maximum modification.
- Authoritative replication of resource state and active Behaviour identity/deadline, including join and teardown.
- A development loadout granting this ability without relying on synchronized inventory.
- HUD binding to confirmed state and an inspectable selected-ally state for verification.

Do not implement combat damage, shield regeneration, death/revival, condition-driven interruption, affixes, quality generation, passives, item grants, inventory synchronization, projectiles, arbitrary effect sequences, or all stacking policies in this increment. Keep their contracts visible in the companion document. Unsupported authored kinds must not be silently treated as supported or exposed as usable abilities.

## Starting points verified in the repository

| Existing code/data | Integration intent |
| --- | --- |
| `Scripts/Items/Hotbar/HotbarBinding.cs` / `ItemHotbarController.Activate` | Reuse ActionHotbarBinding and the action callback for the development ability. Preserve item/weapon hotbar paths. |
| `Scripts/ShipSystem/ShipDefinitionsDocument.cs` and ship loader/schema | Existing stats cover movement. Add explicit initial Shield pool data without rewriting movement configuration. |
| `Scripts/ShipSystem/ShipController.cs` | Entity identity and controlling-player binding already exist. ShipState currently contains movement state, not gameplay pools. |
| `Scripts/NetworkSystem/SynchronizationCoordinator.cs`, NetworkTicker, NetworkClock | Reuse host scheduling and session timing; do not create independent per-ship clocks. |
| JoinProtocol, SessionScope/GameSessionFactory, NetworkManager cleanup | Bind gameplay lifetime and baseline readiness to the existing session/spawn lifecycle. Verify exact owners before edits. |
| `Scripts/UiSystem/HudSystem/PlayerStatsPanel.cs`, `StatBar.cs` | Stats panel is largely a placeholder; bind it to confirmed runtime state. |
| `assets/game_data/AbilityDefinitions.yml`, EffectDefinitions, BehaviourDefinitions, TargetingDefinitions | Resolve the shieldCapacityBoost chain from authored IDs, not hard-coded ability-name branches. |
| `Scripts/GameConfigSystem/GameConfig.cs` | Existing compatibility inventory references absent EffectSchemas.yml and omits the new catalogues. Reconcile the actual loaded content inventory. |

Paths above are relative to `sMPGWM.Game`. These are entry points, not a requirement to put all new responsibilities in those classes.

## Implementation sequence

### 1. Confirm contracts and load the slice

1. Trace current action routing, controlled-ship changes, spawn/baseline readiness, simulation ordering, and content/schema validation. Reuse established patterns and disposal ownership.
2. Define supported execution capabilities for this increment. Parse/validate the chosen dependency closure and make only Shield Capacity Boost activatable. If loading the complete authoring catalogue, retain unsupported definitions as data and reject their execution explicitly; do not implement them merely to satisfy loading.
3. Add typed definitions/registry lookups for the required ability, ApplyBehaviour effect, timed Behaviour, modifier, and targeting profile. Resolve IDs at load time; report duplicate IDs, missing references, bad stat kinds, invalid numeric values, and unsupported combinations clearly.
4. Add initial Shield capacity/current value to ship authoring and its schema/loader. Use explicit development fixture values, for example 100 capacity and 40 current Shield, and label them as test tuning. Do not introduce duplicate runtime resources under Energy/energy_storage/battery aliases; their broader reconciliation is deferred.
5. Fix the loaded-content compatibility manifest and obsolete effect-schema reference. Include all content and schemas that can affect supported gameplay, including user overrides actually loaded. Document treatment of inactive legacy passive files. Address the existing ItemCategory Values/schema mismatch as an integration prerequisite if it blocks startup; do not bypass validation wholesale.

Deliverable: a valid resolved definition chain and initialized per-ship Shield state on a normal host/client session.

### 2. Build runtime stats and owned modifiers

1. Keep definitions immutable. Store current Shield on the ship's runtime gameplay state; calculate effective maximum from authored base data and contributions.
2. Represent each contribution with stable owner identity and a stat/value-kind/operation/value tuple. Removing one owner removes only its own contributions. Runtime Behaviour IDs must not be confused with definition IDs, source IDs, or request IDs.
3. Implement flat addition and additive baseline fractions. For this increment, define a nonrecursive baseline B and aggregate as `effective = B + sum(flat contributions) + B * sum(fraction contributions)`, then apply stat limits. This preserves existing AddBaselineFraction semantics. Document that flat contributions do not enlarge the percentage basis in this first model. More elaborate equipment base layers and separate multipliers remain deferred.
4. Recompute only affected stats on application, removal, or changed inputs. Do not repeatedly mutate a base value or reverse percentages by division. Preserve existing movement behavior; no movement-stat migration is required for this Shield slice.
5. Capacity increases leave current Shield unchanged; decreases clamp it to the new capacity. Ensure values are finite and within valid bounds. Releasing a modifier must not heal or lose unrelated contributions.
6. Add pure calculation tests before integrating network state. Use controlled additional owners to prove contribution isolation without implementing a real equipment/passive provider.

Deliverable: deterministic pool and modifier behavior independently testable from Godot presentation.

### 3. Execute the ability and timed Behaviour on the host

1. Grant shieldCapacityBoost through a host-owned development loadout. A hotbar binding is a request route, not proof of possession. The host verifies the grant for both local-host and remote-client activations.
2. Provide minimal usable self/ally selection through the existing input/UI flow. Show the selected target and allow self selection explicitly. Treat ship range as authored tuning, evaluated from authoritative positions using existing world topology rules where relevant.
3. Route host input through the same authoritative activation service as client requests. Validate controlled caster, live entity identity, grant, target relation/life state, range, and currently available blocking state. Costs and cooldown are zero for this example; do not build the full resource-spending system yet.
4. Resolve EffectId to ApplyBehaviour, then apply the Behaviour and its modifier. Refresh an existing per-target definition instance instead of adding another modifier or replaying application work. Refresh from the newly accepted application tick.
5. Schedule expiration on the host's fixed simulation timeline. Choose and document millisecond-to-tick rounding, expiry boundary, and ordering of expiry versus same-tick reapplication. Keep this independent of packet publication frequency; the 30 Hz network cadence alone is not the simulation clock.
6. Publish a result for accepted/rejected activation and state changes for the affected target. Destroy Behaviour state and contributions on entity/session teardown without firing unsupported gameplay hooks.

Deliverable: the same authored ability works through host and client requests, with one modifier and a deterministic deadline.

### 4. Synchronize gameplay state and lifecycle

1. Define a bounded activation message using existing authenticated session/entity identities, request identity, ability ID, caster, and target. Clients cannot supply modifier values, deadlines, effective capacities, or authoritative timestamps. No source item is needed for this slice.
2. Deduplicate within the authenticated session and sender scope. Duplicate requests replay the result without refreshing duration again. Different requests may legitimately refresh. Bound retained results and define how old requests are rejected after cache eviction; never re-execute an old request simply because its cached result expired.
3. Reuse transport-independent ENet/Steam routing. Prefer reliable, revisioned low-frequency gameplay updates for this slice, separate from movement interpolation. Define session/entity-generation and state-revision checks, payload limits, rate limits, and malformed-input handling.
4. Replicate confirmed current/effective Shield and active boost identity/deadline. Clients apply confirmed state rather than independently applying modifiers on top of replicated totals. A client countdown is presentation only; the host owns expiration.
5. Extend late-join readiness with a coherent gameplay baseline from a completed host tick. Include active boost state and remaining lifetime via authoritative time. Handle baseline/update races and stale revisions without resetting existing players or extending the boost for the newcomer.
6. Reuse bounded buffering and existing baseline recovery patterns for unknown entities or gaps. Unknown gameplay messages cannot create ships; stale updates cannot resurrect despawned ships. Handle a lost/late activation result independently from the authoritative state update.
7. On disconnect, host loss, controlled-ship change, teardown, partial startup failure, and restart, invalidate pending requests and detach state/UI subscriptions. A new session cannot accept old packets or callbacks.

Deliverable: confirmed state converges on host and clients, including late join and session restart, without changing movement prediction behavior.

### 5. Bind UI and complete playable verification

1. Bind the existing action hotbar route to activation. Preserve menu focus/input blocking and item interactions. Provide concise pending/accepted/rejected feedback without predicting a successful modifier.
2. Display current and maximum Shield from confirmed state. Show the active boost and remaining lifetime; expose selected ally values through a small reusable view or existing inspection UI for the test session.
3. Rebind when the controlled/selected ship changes and unsubscribe on disposal. Avoid polling/rebuilding all HUD nodes each frame for unchanged stats.
4. Verify the complete loop in ordinary game startup, not only a test scene. Keep setup for low current Shield and extra modifier owners in controlled fixtures/development tools rather than new shipping gameplay abilities.

## Verification and acceptance

### Traceability of test-support code

Every line of executable code or declaration introduced solely to enable, drive, inspect, or verify tests must be marked with a unique traceability ID, followed by the fully qualified test method name(s) that use it. This includes test-only hooks, accessors, instrumentation, injected controls, and development helpers added to application code for automated or manual verification. Ordinary production code does not require a marker merely because tests exercise it.

Use this comment format on each affected line, or immediately above it when an inline comment is impractical:

```csharp
internal void SetShieldForTest(float value) // TEST-SUPPORT: <unique-id> | Tests: Namespace.Class.Method
{
    _shield = value; // TEST-SUPPORT: <different-unique-id> | Tests: Namespace.Class.Method
}
```

IDs must be unique repository-wide, stable across edits, and never reused for different code. Each affected line has its own ID; a single marker on a method or block does not cover its other executable lines or declarations. For shared support code, list all consuming test methods. For manual verification, replace the method name with the exact plan/evidence path and scenario identifier. Update references when tests are renamed or removed; remove orphaned support code unless a documented verification scenario still needs it. Do not mark punctuation-only lines or ordinary test implementations; the rule identifies additional support code introduced for testing, not the tests themselves. Follow the repository's global ID allocation convention once defined; do not invent a conflicting local convention.

### Test execution

Use the existing NUnit/.NET and Godot integration infrastructure. Add focused tests near the responsible subsystem; do not duplicate the same arithmetic in every network/UI test.

| Scenario | Required result |
| --- | --- |
| Base 100 maximum / 40 current, first activation | 150 maximum / 40 current; one active boost |
| Reapply before expiration | Still 150 maximum; one instance/contribution; deadline refreshed |
| Original deadline after refresh | No premature removal |
| Final expiration | 100 maximum / 40 current |
| Current Shield 140 under boost, then expire | Current Shield clamps to 100; no negative or over-cap state |
| Separate +20 owner plus boost | 170 maximum, then 120 after boost expiry; other owner survives |
| Mixed flat/fraction modifiers | Order-independent result according to the documented baseline formula |
| Host→self, host→client, client→self, client→host, client→other client | Correct target receives the same authoritative outcome |
| Out-of-range, nonexistent, nonallied, wrong-owner caster, ungranted ability | Rejected; no target mutation or hidden grant |
| Duplicate request / duplicate update | No second execution, refresh, or contribution |
| Two valid users apply to the same target | Shared non-stacking boost refreshes deterministically |
| Late join during active boost | Current capacity and original deadline received; no full-duration restart |
| Baseline/update reordering and old session packets | Newer state preserved; no entity resurrection |
| Disconnect, host loss, restart, partial join failure | Complete cleanup; no stale callbacks, timers, or modifiers |
| Existing movement, menus, inventory, and hotbar | Remain functional; no new movement correction regressions |

Run the content validator (`python tools/validate_gameplay_data.py`, with its requirements installed), focused tests during development, and the repository's normal `dotnet test sMPGWM.Tests` gate after integration. Use GODOT_BIN as documented in the game README. Record actual commands/results and pre-existing failures separately. Do not claim runtime acceptance from YAML validation alone.

Manual verification uses host H and clients A/B over ENet, then Steam where available. Record player count, transport, join order, activation/expiry timestamps, selected targets, and a concrete latency/loss/reordering profile. At least one late join occurs while the boost is active. Missing Steam or adverse-network coverage is an explicit remaining check, not an assumed pass. Preserve the completed movement slice's lifecycle guarantees.

Measure allocation/timing impact and message sizes at the README's expected 5–10-player scale using bounded test traffic. Avoid recurring per-frame allocations where practical; do not impose an unmeasured zero-GC requirement or force garbage collection.

## Completion evidence and handoff

Record implementation files, supported execution kinds, stat formula, clock/deadline choices, protocol bounds/revision policy, test commands, and host/client observations under `docs/plans/evidence`. Link that evidence here when marking the increment implemented. Record tuning separately from correctness.

Complete means the normal hotbar-to-host-to-HUD path is playable and verified, including expiry and late join. A standalone stat calculator or host-only ability is not completion. Do not mark all other YAML abilities implemented.

Subsequent increments: instant resource effects; direct combat damage and outgoing bonuses; typed ability parameters and delayed execution; authoritative inventory/equipment sources and affixes; then remaining Behaviour/lifecycle features. Adjust this order based on evidence from the slice, preserving the companion's gameplay decisions.
