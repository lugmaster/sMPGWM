# Gameplay increment 3 Equipment stats and resources

Status: implemented and verified on 2026-11-11. See implementation and validation record below. Gameplay decisions agreed on 2026-11-11.

Architecture follow-up: [gameplay architecture cleanup](gameplay-architecture-cleanup.md) covers player/ship ownership, effective stats, detachable systems, and their UI/network migration. Its implementation and verification status is recorded in that plan.

Equipment supplies each installed system's baseline values. Ships add flat modifiers to those values. The host resolves the resulting stats, runs energy and shield simulation, and supplies authoritative movement parameters and resource state to clients.

This increment builds on [the ability runtime and authoritative equipment integration](gameplay-increment-02-ability-architecture.md). Its rules supersede conflicting earlier stat proposals in [the design context](gameplay-design-context-and-decisions.md) and [Stats.md](../documentation/Stats.md), particularly modifier ordering, capacity increases, resource cadence, and equipment requirements.

## Agreed gameplay

### Stat calculation and equipment requirements

For a stat belonging to an installed system:

```text
subtotal = equipment baseline + ship flat modifier + other flat modifiers
final = subtotal * (1 + sum of percentage modifiers)
```

Percentages use the baseline plus all flat modifiers. Percentage modifiers add together rather than multiply sequentially. Preserve the existing supported ability operations, but update their calculation to this rule. A missing component suppresses that system's stats and its associated modifiers; a positive flat modifier cannot create a missing system.

| Component | Baseline values | Behavior when absent |
| --- | --- | --- |
| Engine | Speed, AfterBurnerSpeed, RotationSpeed, Acceleration, AfterBurnerAcceleration | No thrust, afterburner, rotation, or powered braking |
| Hull | Armor | No armor from this system or its associated ship modifier |
| Shield generator | ShieldCapacity, RechargeTime, DownTime | No operational shield or shield capacity |
| Reactor | EnergyProduction | No production; stored energy can still supply installed systems |
| Battery | EnergyStorage | No stored energy; direct production can still power systems |

Ship modifiers are flat contributions to the corresponding installed system. Omitted modifiers are zero. Equipment provides the timing, rate, and storage baselines as well as movement and shield baselines. This increment does not add arbitrary new ability mechanics just because a stat can be represented.

Two explicit aggregation rules remain separate:

- `Mass` is intrinsic ship mass plus the mass of every equipped item, including weapons. Inventory cargo does not contribute. Feed this total into the existing movement calculations: rotation inertia `1 + Mass * 1.05` and friction `0.15 / (1 + Mass * 0.95)`.
- `EnergyConsumption` is the sum of equipped items' configured passive demand, including weapons and the battery's own demand. Categories without consumption contribute zero. Demand does not disappear when a system fails, is idle, or is waiting for shield downtime. Afterburner adds 25% of the installed engine demand while active; the configured passive demand remains unchanged.

### Energy

Use **energy** for the runtime quantity and `EnergyStorage` for its capacity. A battery is an optional equipment component, not a second resource pool. Production and consumption are rates in energy units per second.

```text
net rate = EnergyProduction - EnergyConsumption
next current energy = clamp(current energy + net rate * elapsedSeconds, 0, EnergyStorage)
energyDepleted = EnergyProduction < EnergyConsumption AND current energy == 0
```

Surplus production fills storage; surplus beyond capacity is discarded. Stored energy supplies a deficit until empty. Equal production and consumption leaves energy unchanged and keeps systems powered, including when storage is empty or absent.

Keep configured demand constant during failure. With production 10 and demand 15, empty storage means the ship remains depleted. Shutting down systems must not change demand to zero and immediately restart them. This does not spend negative energy or permit charging from a deficit.

`energyDepleted` is derived by the host and removed when supply recovers. It disables thrust, afterburner, rotation, manual powered braking, automatic powered braking, shield protection, and shield regeneration. Retain existing velocity and collision handling. Passive behavior in the existing movement model remains distinct from powered braking. Weapon firing and energy-dependent ability costs are deferred; expose a reusable powered-system state for their later integration.

### Shield

`RechargeTime` and `DownTime` are authored in milliseconds. `RechargeTime` is the duration to regenerate from zero to the current maximum; it is not a fixed duration for any partially damaged shield. Require positive recharge time and nonnegative downtime.

```text
shield change magnitude = ShieldCapacity * elapsedMs / RechargeTime
```

| State or transition | Result |
| --- | --- |
| Powered, downtime finished, shield below capacity | Regenerate at the calculated rate |
| Partially damaged | Continue regeneration; ordinary hits do not introduce another delay |
| Current shield changes from positive to zero | Start downtime once |
| Downtime active | No regeneration or shield protection |
| Power lost | Immediately disable protection and regeneration; drain remaining shield at the same rate |
| Power restored before shield reaches zero | Restore protection for remaining shield and resume regeneration |
| Drain reaches zero | Start normal downtime |
| Downtime expires without power | Remain at zero until power returns |

Downtime continues during power loss. Staying at zero does not restart its deadline. A zero-capacity or missing generator must not create a repeated downtime loop. Recalculate the regeneration/drain rate when capacity or recharge time changes, but preserve an already-running downtime deadline.

Shield charge and shield protection are separate state: an unpowered ship can display a positive, draining shield amount that provides no protection. Future damage handling must consult the protection state rather than only checking current shield. This increment exposes and verifies that contract without implementing weapon damage.

### Spawn and equipment changes

- Resolve the initial loadout before initializing resource amounts. Spawn with shield at the resolved maximum and energy at zero. A ship without a generator starts with zero shield.
- Increasing capacity never increases the current amount. Shield regenerates toward its new maximum; energy fills only from surplus production.
- Decreasing capacity clamps current amounts to the new maximum.
- Removing the battery discards stored energy. Implementation default: replacing its item identity also counts as removal; the new battery starts empty. Increasing the capacity of the same installed battery preserves current energy.
- Atomic shield-generator replacement preserves current shield up to the new capacity. A separate unequip sets capacity and current shield to zero; re-equipping cannot refill it for free.
- Equipment swaps do not reset or shorten an existing shield downtime. Initialization is a one-time spawn operation, never a side effect of binding a player, receiving a snapshot, or recalculating stats.
- Existing timed ability effects retain their established lifetime after their granting item is removed. Their stat contribution is ineffective while the required system is absent, and resumes if it is reinstalled before expiry.

### Scope

Implement movement stats, armor as a calculated value, shield capacity/timing/runtime behavior, energy production/consumption/storage/runtime behavior, and total equipped mass. Include weapon mass and passive energy demand only.

Deferred: weapon attacks, damage/armor formulas, energy per shot, health regeneration, attack speed, oxygen, food, water, affix generation, general ability costs, additional behaviour types, and new manual subsystem-toggle controls. The power-loss contract must remain usable by future controls without adding them now.

## Existing integration points

| Area | Current behavior and required change |
| --- | --- |
| `Ships.yml`, `ShipsSchema.yml`, `ShipDefinitionsDocument`, `ShipRegistry` | Ship data supplies movement and both shield capacity/current. Replace system bases with flat modifiers; separate intrinsic mass; remove authored ShieldCurrent. |
| `ItemCategoryDefinitions.yml`, `ItemDefinitions.yml`, `ItemRegistry` | Item values are validated generic dictionaries. Rename generator Shield to ShieldCapacity, remove reactor Energy, add missing masses, and project supported values into typed runtime inputs. |
| `EquipmentSlotDefinition`, `PlayerItemState` | Equipment changes already update ability grants. Add a battery slot and host-owned stat composition on committed equipment changes and ship binding. |
| `ResourcePool`, `ShipGameplayState` | Shield has immutable base capacity and owned timed modifiers. Support equipment baseline changes, the agreed modifier order, energy, and shield downtime without losing current resources or active behaviours. |
| `ShipController`, `MovementPhysics` | Movement reads ship definitions and caches mass-derived terms only when Data is assigned. Consume resolved stats and power state; update cached terms when total mass changes. |
| `SynchronizationCoordinator`, `NetworkManager.Gameplay` | Movement currently runs before gameplay expiry/ability processing. Establish one explicit order for stat/resource changes, movement, and publication. |
| `GameplaySnapshotCodec`, `ItemSnapshotCodec`, `MovementWire` | Extend bounded replication for new resource state, the battery slot, and movement configuration changes. Item contents remain owner-only. |
| `PlayerStatsPanel` | Currently displays only confirmed shield. Add current energy/storage and clear power/shield availability feedback. |

Paths in this table are under `sMPGWM.Game/assets/game_data` or `sMPGWM.Game/Scripts` as appropriate. The existing user change in `Attributes.yml` enables the attack_speed definition; preserve it while keeping attack-speed implementation out of scope.

## Data and runtime design

### Authoring contract

Replace `ship_stats` with an explicit `stat_modifiers` object and a separate intrinsic `mass` property. Preserve geometry, textures, names, and weapon-slot metadata. Use known stat keys and finite flat values; allow signed flat adjustments where supported and apply final stat bounds centrally. Missing modifiers default to zero.

Migrate all schemas, typed loaders, bundled data, fixtures, and validators together. Reject legacy `ShieldCurrent`, reactor `Energy`, and generator `Shield` fields with actionable diagnostics rather than silently assigning them new meanings. Increase affected data versions and document the change for user/mod overrides.

Existing mock engine values are mostly 1 while the fighter carries movement values such as Speed 350. Redistribute those values into realistic equipment baselines and deliberate ship bonuses. Treat resulting numbers as provisional tuning, not a change to the agreed formulas. Set meaningful generator timings in milliseconds and reactor output that can power the intended starter loadout. Account for the newly aggregated mass when tuning that loadout.

Add `Mass` to reactors and both weapon categories and populate every affected item. Preserve zero demand for categories that do not declare EnergyConsumption. Add the battery slot using its existing battery category and item definition; keep established slot keys stable and update slot-count assumptions.

Keep `Attributes.yml` as stat metadata, not a source of ship or item values. Add missing scoped descriptors such as rotation speed and consumption. Make the relationship between the existing `shield` and `energy_storage` resource IDs and the `ShieldCapacity`/`EnergyStorage` value fields explicit. Existing effects already reference those resource IDs; avoid creating parallel energy or shield pools from the display-only `energy`/`battery` entries. Leave out-of-scope catalog entries available for future use.

Validate numeric values at loading and resolved-state boundaries: finite numbers, nonnegative mass/demand/production/capacities, positive recharge time for a functioning generator, and nonnegative downtime. Bound resolved movement values to finite float representations before passing them into Godot. Reject arithmetic overflow. Clamp ordinary negative resolved outputs to zero; do not silently divide by zero when modifiers produce an invalid recharge duration.

### Ownership and recomputation

Keep one authoritative stat state per ship. Use a compact typed result for the scoped stats and installed-system flags, with a pure calculator that combines ship modifiers, equipped item inputs, and supported owned modifiers. Compile numeric item values once when definitions load, or cache the projection by definition; avoid dictionary/string conversion in each simulation step.

Recompute when equipment, ship definition, or an applicable modifier changes. Resource ticking changes current amounts and conditions, not the equipment baseline. Apply a committed equipment transaction as one change, so replacing a generator does not transiently remove capacity, drain shield, and then reinstall capacity. Preserve the special battery identity rule above.

Connect composition through the existing PlayerItemState equipment/control-binding lifecycle with explicit authority. Client inventory confirmations may refresh UI and grants, but must not overwrite confirmed gameplay resources. Clean up subscriptions on control changes, despawn, and session teardown. Rebinding an existing ship must not rerun spawn initialization.

Adapt ResourcePool to accept changing baseline and resolved capacity through production APIs that preserve/clamp current values. Use the same modifier calculation for existing Shield Capacity Boost effects and equipment composition. Retain source ownership and expiry; do not create a second independent modifier implementation that can diverge from the ability path.

Maintain separate revisions for calculated stats/movement configuration and changing runtime resources. Current-energy or shield changes must not invalidate movement history unless movement parameters or availability actually change. Coalesce observer notifications and gameplay revision changes for each committed update.

### Simulation order and timing

Use the existing fixed 60 Hz simulation clock. Author shield durations in milliseconds; convert downtime to a simulation deadline using ceiling-to-tick conversion, matching existing ability duration handling. Calculate continuous rates using the fixed elapsed duration without rounding each physics step to 16 or 17 ms. Publish state at most every second physics step, preserving 30 Hz network cadence.

Adopt this deterministic host order:

1. Incorporate committed equipment changes and expire behaviours due at this tick; refresh stats and immediate capability changes.
2. Execute admitted abilities, retaining expiry-before-activation semantics; resolve resulting modifier changes.
3. Advance energy using the net-rate equation, then derive energyDepleted from the updated amount.
4. Advance shield downtime and regeneration/drain using the resolved supply state.
5. Consume movement input and simulate using the resulting movement parameters and capability flags.
6. Publish committed gameplay, item, and movement state on the network tick.

The fixed-step rule switches systems off on the step whose energy update reaches zero. This deliberately quantizes exhaustion to at most one physics step; it does not permit another full movement step using a stale powered flag. Immediate supply changes from equipment must be reflected before validating subsequent energy-dependent actions. Abilities with resource costs remain deferred.

Separate gameplay advancement from publication in NetworkManager.Gameplay so reordering does not accidentally send pre-movement snapshots. Resource advancement must occur once per authoritative tick, never during client movement replay.

## Implementation sequence

Each step includes its focused checks. Complete the integrated validation after all dependent changes land.

### 1 Migrate definitions and equipment slots

Update ship/item schemas, data, typed definitions, registry validation, Attributes metadata, and offline validation together. Add the battery slot and update inventory projections, slot serialization, startup data, and existing test fixtures that assume eight equipment slots. Review and retune bundled example values for the new baseline semantics.

Acceptance: production content loads; obsolete fields and malformed values are rejected; all equipment can be placed in the intended slot; out-of-scope metadata and the user's existing Attributes edit are preserved.

### 2 Add equipment stat composition

Implement typed inputs/results and the shared calculation in StatSystem. Integrate authoritative PlayerItemState changes with ShipGameplayState; make system presence explicit. Update ResourcePool and supported ability modifier handling. Recompute mass-derived movement terms whenever total mass changes, while preserving the current formulas.

Acceptance: equipment plus flat plus percentage examples resolve correctly; missing systems receive no bonus; weapon values contribute only mass/demand; swapping equipment does not add free resources; existing timed boost ownership/expiry remains correct.

### 3 Add energy and shield simulation

Add current energy, shield downtime deadline, derived energyDepleted, and shield-protection availability to the ship's runtime state. Centralize current-shield mutation so positive-to-zero transitions consistently start downtime. Implement initialization, capacity clamps, battery removal/replacement, net flow, recharge, and reverse drain. Expose the shield-protection contract for later damage handling.

Acceptance: exact/surplus/deficit supply behaves correctly; no-battery and no-reactor scenarios work; depletion is stable rather than oscillating; shield downtime triggers once and survives equipment changes; power loss immediately removes protection and drains charge.

### 4 Integrate movement and authoritative tick order

Reorder the host step as specified. Make ShipController consume resolved movement inputs and capability flags. Disable powered controls without erasing velocity or skipping collision handling. Review MovementPhysics branches for accidental powered braking when disabled, especially automatic braking and speed-limit correction; do not simulate a power failure merely by setting all movement parameters to zero.

Update spawn composition so an intended starter loadout is applied before resource initialization. The current initial-item snapshot contains inventory, not a guaranteed equipped loadout. Supply an explicit usable starter loadout through the existing composition path and initialize remote participants consistently. Bare-ship cases remain supported and cannot receive a free shield when equipment is added later.

Acceptance: control loss and recovery happen at the defined tick; total mass affects the existing inertia/friction calculations; powered braking stops during failure while motion/collisions remain consistent; loadout initialization happens exactly once.

### 5 Extend replication and prediction

Extend gameplay snapshots with resolved stats/system presence, current shield and energy, capacities, power/protection state, relevant revisions, and the shield downtime deadline/authoritative tick. Continue replicating behaviour instances and conditions. Clients consume confirmed results without applying equipment or percentage modifiers a second time. Full baselines, join-in-progress, and recovery carry the same complete resource state as live updates.

Movement packets need the configuration that produced their authoritative state. Recommended implementation: include the compact movement parameters, movement-enabled flag, and a separate configuration revision in each movement sample. This avoids relying on arrival order between reliable gameplay packets and movement packets. Keep the existing 1,400-byte movement limit and prove the ten-player payload fits before finalizing the layout; do not casually raise the limit.

On a confirmed movement-configuration change, install its matching parameters and reset/rebase local prediction from that authoritative sample. Replay only history compatible with the configuration; do not replay old steps using newly equipped values. Parameter changes are exceptional boundaries, while ordinary same-configuration reconciliation remains unchanged. Power transitions use this boundary; continuously changing shield/energy amounts do not. Use authoritative ticks/revisions to prevent delayed gameplay messages from rolling movement configuration backward.

Keep client movement prediction separate from authoritative resource simulation. Before confirmation, prediction can temporarily use the last confirmed powered configuration; authoritative samples correct it. Do not spend energy again during replay. Preserve remote interpolation and control-generation/despawn handling.

Bump protocol compatibility for changed wire layouts and equipment slot counts. Validate finite numeric values, bounds, current/capacity relationships, flag/condition consistency, unique identities, and stale revisions before applying a batch. Preserve owner-only inventory details; other players receive the resolved ship state they need.

Acceptance: joins during charge/drain/downtime match the host; equipment swaps and power failures under latency converge without repeated prediction resets; malformed/stale packets cannot partially apply; ten-player packets remain bounded.

### 6 Update presentation and complete verification

Extend the existing HUD to show confirmed shield and energy/current capacity, power failure, and shield unavailability/downtime. A draining unpowered shield must not appear protective. Display zero storage sensibly when no battery is installed. Update item tooltips for renamed values and millisecond units. Preserve the established UI layout and avoid adding a separate stat-management interface.

Update relevant documentation and regression fixtures to the agreed rules. Record integration and performance evidence with the completed increment rather than treating this plan as verification.

## Verification and completion criteria

| Area | Required cases |
| --- | --- |
| Definitions | Legacy fields rejected; missing required item values; NaN/infinity/overflow; zero recharge time; negative downtime; battery slot round trip; user/mod overrides receive useful diagnostics |
| Calculation | 200 engine + 50 ship, then +20% = 300; multiple flat/percentage sources; missing components; modifier expiry after equipment replacement; total mass/demand includes weapons but excludes inventory cargo |
| Energy | Surplus fills and caps; equality keeps systems running; deficit drains; exhausted storage fails; no battery; battery's own demand; removed reactor uses stored energy; recovery; no recurring on/off loop |
| Capacity and lifetime | Increased maximum preserves current; decreased maximum clamps; removed/replaced battery discards energy; generator swap cannot refill or reset downtime; repeated binds/snapshots do not initialize resources |
| Shield | Partial regeneration; positive-to-zero transition; exact downtime boundary; no repeated downtime at zero; drain equals reverse recharge; protection disabled immediately on power loss; restoration before and after zero; missing generator and zero capacity |
| Movement | Same input with changed engine/mass; rotation/thrust/afterburner/manual and automatic braking gating; velocity/collisions during failure; exhaustion/recovery tick order |
| Multiplayer | Owner-only item state; host authority; late join during each resource phase; reordered gameplay/movement arrival; equipment configuration changes during prediction; stale/malformed batches; disconnect/rebind/recovery |
| Performance | Sustained charging, draining, regeneration, swaps, and boosts at 5 and 10 players; 30 Hz publication; bounded payloads and histories; timing and allocation/GC measurements |

Use the existing NUnit/Godot harness, content validator, and multiplayer orchestration where applicable. Starting commands are `python tools/validate_gameplay_data.py`, `dotnet build sMPGWM.Game/sMPGWM.csproj`, and `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj`. The current content validator does not by itself establish the new ship runtime contract; extend validation or cover it through loader tests. Run normal and impaired ENet integration and a GodotSteam smoke check for the transport-neutral gameplay changes.

Exercise at least a host plus two clients for functional integration and ten participants for scale. Record host step times, publication intervals, serialized bytes, allocations, and GC pauses during sustained resource changes; reliable gameplay snapshots that previously changed only on ability events will now change continuously. Reuse buffers and cached results where measurements justify it, and never force garbage collection to satisfy a check.

Follow AGENTS.md traceability requirements for every executable declaration/line introduced solely for automated or manual verification, including fixture helpers: unique `TEST-SUPPORT` IDs with fully qualified consuming test method names. Use ordinary production mutation APIs in tests when available. New source files follow namespace directories, one type per file, straightforward loops, and allocation-conscious storage.

Completion requires the agreed behavior, passing relevant regressions, working HUD and equipment interactions, correct joins/recovery, and recorded performance evidence at the required scale. Broader combat, ability, or stat features are not prerequisites for this increment.

## Implementation and validation record

The host composes installed equipment through `PlayerItemState`, initializes the explicit starter loadout once, and advances resources before movement. `EquipmentStatCalculator` caches item projections. `ResourcePool` applies all flats before summed percentages and retains owned contributions while a system is absent. Signed ship shield modifiers remain separate until that shared calculation, avoiding an early clamp. Battery identity changes discard stored energy; generator replacement preserves charge and downtime.

Gameplay snapshots carry complete confirmed resources, stats, system presence, conditions, downtime and revisions. Movement samples independently carry their matching parameters and configuration revision, so delayed gameplay messages cannot change movement parameters. Configuration transitions rebase prediction without spending resources during replay. Ten samples occupy 1,135 bytes including the header, within the unchanged 1,400-byte limit. Wire compatibility is protocol 7 / gameplay protocol 5.

The existing HUD displays shield and energy, with explicit power/protection feedback. Item tooltips display values and millisecond shield timing units. No weapon attacks, general ability energy costs, or subsystem toggle controls were added.

### Data override migration

Ship, item and item-category documents are version 2. `Ships.yml` replaces `ship_stats` with `stat_modifiers` and a separate nonnegative intrinsic `mass`. Optional modifiers are finite signed flats and default to zero. Remove `ShieldCurrent`; resource amounts are runtime state. Item generators rename `Shield` to `ShieldCapacity`; reactors remove `Energy`; reactors and both weapon categories require `Mass`. `RechargeTime` is positive milliseconds and `DownTime` is nonnegative milliseconds. Legacy fields fail validation with diagnostics. The battery is appended to the equipment slots, preserving existing slot keys and indices. Attribute metadata retains the existing `attack_speed` entry without implementing attack speed.

For user/mod overrides, migrate these fields before loading with `ReloadConfigOnStart` disabled. A version bump alone does not replace an existing user file. The existing development reload option copies bundled defaults; keep override backups when using that option.

### Verification

- Content validator: 133 definitions across 12 catalogues; 17 invalid-content checks rejected.
- `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj`: 263 passing outer tests, including the Godot harness with 181 passing in-engine cases. The game builds as part of this run. Existing `UiIcon.Size` and NUnit analyzer warnings remain.
- Focused resource tests exercise 36,000 steps at five and ten ships with supply changes. Observed total times were 28.446 ms and 61.730 ms respectively; allocations were 280 B and 560 B, with no collections during those isolated loops. These numbers exclude networking, rendering, and startup.
- Normal three-player ENet: all processes exited successfully and exercised charge, depletion, draining unprotected shields, restored protection, equipment swaps and late clients. [Recorded observations](evidence/equipment-resources-variants-enet-3.txt).
- Impaired three-player ENet: 20–60 ms delay each way, 1% packet loss and reordering; 617 observations across 229 entity revisions converged. [Recorded observations](evidence/equipment-resources-variants-enet-3-impaired.txt).
- Five-player ENet: 1,675 observations across 571 revisions converged. Host step mean 0.1871 ms, maximum 22.0093 ms; publication mean 33.2937 ms; movement payload maximum 580 bytes. Whole-process allocation was 80.95 MB over 89 seconds, including the verification fixture, UI and startup; GC counts 4/4/2 with 67.795 ms cumulative pause. [Recorded observations](evidence/equipment-resources-variants-enet-5.txt).
- Ten-player impaired ENet: all ten processes exited successfully; 6,608 observations across 2,307 revisions converged. Host step mean 0.3198 ms, maximum 27.5743 ms; publication mean 33.2876 ms; movement maximum 1,135 bytes and sampled gameplay maximum 4,008 bytes. Whole-process allocation was 135.25 MB over 89 seconds including verification/startup; GC counts 8/8/2 with 123.152 ms cumulative pause. Occasional maximum step times exceed the 16.67 ms physics budget on this workstation, while measured mean cadence remains 30 Hz. [Recorded observations](evidence/equipment-resources-variants-enet-10-impaired.txt).
- Ten-player normal ENet: all ten processes exited successfully; 6,657 observations across 2,087 revisions converged. Host step mean 0.2720 ms, maximum 23.9998 ms; publication mean 33.2891 ms; maximum movement/gameplay payloads 1,135/4,008 bytes. Whole-process allocation 136.63 MB over 89 seconds; GC counts 8/8/2 with 113.480 ms cumulative pause. [Recorded observations](evidence/equipment-resources-variants-enet-10.txt). Scale runs exercise resource/configuration transitions on stationary ships; prediction under nonzero input is covered separately.
- Three-player impaired ENet with held forward input through power loss and recovery: all three processes exited successfully; 618 resource observations across 245 revisions converged, with no runtime errors. Host step mean 0.1385 ms, maximum 27.3071 ms; publication mean 33.2907 ms. [Recorded observations](evidence/equipment-resources-variants-enet-3-impaired-moving.txt). This complements the prediction regression that checks history replacement and confirms replay does not advance resources.
- GodotSteam smoke: Steam initialized, a real Steam transport hosted a lobby and entered the game, simulated approximately 14 seconds, showed energy 100/100 and protective shield 100/100, then stopped the network and returned to the start menu cleanly (exit 0). The one-player movement sample was 136 bytes. This covers single-account host lifecycle, not a multi-account Steam remote-peer session.

The process fixtures run in isolated copied projects and mark verification instrumentation with test traceability IDs. No forced garbage collection is used.
