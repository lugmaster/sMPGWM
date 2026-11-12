# Session initialization and gameplay readiness implementation plan

Status: planned. Implementation has not started.

Establish a clear boundary between StartMenu, SessionInitialization behind the loading screen, and active gameplay. Completing loading must mean the player can immediately take off and use the supported abilities supplied by their loadout. All initial world, inventory, equipment, resource, and synchronization work must already be complete.

This plan consolidates the review of `797d6bc` and `4e18890` with the agreed lifecycle boundaries. It follows [gameplay architecture cleanup](gameplay-architecture-cleanup.md), preserving its ownership, gameplay, transaction, and resource contracts. It supersedes earlier proposals to prioritize allocation reduction inside definition loading.

## Decisions and scope

| Phase | Required boundary | Priority |
| --- | --- | --- |
| StartMenu | UiCanvas and clickable menu elements; no constructed gameplay session or gameplay registry loading | Clear UI and application responsibilities |
| Loading | SessionInitialization owns preparation of the world, definitions, player state, bindings, and initial synchronization | Clean architecture, complete validation, useful diagnostics |
| Active gameplay | Consume prepared state; simulation and publication run at their defined cadence | Predictable runtime cost and correctness |
| Late join on an active host | Prepare and synchronize the joining participant while existing players continue playing | Runtime budgets on the host; loading remains visible on the joining client |

Menu configuration and application services required to display the menu or select Steam/ENet remain available outside a session. Gameplay initialization begins only after Host or Join starts the loading screen. Ensure the screen actually renders before expensive work starts; setting a loading flag alone is insufficient.

Preserve 60 FPS, 60 physics steps per second, host authority, and network publication every second physics step at 30 Hz. The expected session is 5 players, with a maximum of 10.

Persistence receives an architectural seam in this work. Save-file formats, storage, migration, AI, enemy combat, new ability mechanics, a general dependency-injection framework, and a universal event bus are outside scope.

## Existing foundations and findings

Source paths below are relative to `sMPGWM.Game/Scripts`.

| Existing code | Finding and intended treatment |
| --- | --- |
| `GameFlowSystem/GameFlowManager.cs` | Enters Loading before switching scenes. Preserve navigation ownership and verify presentation timing. |
| `GamePlaySystem/SessionCoordinator.cs` and `SessionScope.cs` | Already own session lifetime and cleanup. Add explicit initialization completion without creating another lifetime owner. |
| `GamePlaySystem/GameSessionFactory.cs` | Connects `Network.SessionReady` directly to loading completion. Route network milestones through the complete initialization contract. |
| `GameConfigSystem/GameConfig.cs` | `EnsureCompatibilityData` forces registry loading. Make this preparation explicit during loading rather than relying on incidental first access. |
| `ItemSystem/ItemRegistry.cs` and `GameplayDataSystem/GameplayCatalogues.cs` | Already precompile item stats and validate/prepare gameplay definitions. Preserve these safeguards. |
| `GameplayDataSystem/GameplayCatalogueLoader.cs` | Typed descriptors remove positional coupling, but item parsing, ship deserialization, and ability/effect parsing share one class. Unknown categories and duplicate item IDs do not receive consistent contextual diagnostics. |
| `NetworkSystem/NetworkManager.cs` | Initial client topology, local binding, movement, and gameplay baselines are checked; JoinComplete also checks the item baseline. Consolidate these existing checks. Spawn acknowledgement currently precedes the separate item-baseline completion check. |
| `WorldSystem/GameWorld.cs` and `GamePlaySystem/StarterLoadout.cs` | Initial inventory and equipment are installed synchronously during spawn. Per-item creation and per-slot transfers also run for late joins on an active host. |
| `ItemSystem/ItemStore.cs` and `ShipSystem/ShipLoadout.cs` | Incremental indexes and prepared changes reduce runtime work. Installation safety remains a contract to audit, not a demonstrated regression. |

Retain the fixed-capacity command queue, cached join membership snapshots, direct equipment-stat calculation from slots, incremental store indexes, category-ID placement checks, message presentation binding, and separate starter-loadout orchestration.

## Ownership and readiness contract

```text
StartMenu
  Host or Join using Steam or ENet
    Loading screen
      SessionInitialization
        Prepare definitions and resources
        Obtain initial world state
        Construct and install world and player state
        Apply required authoritative baselines
        Complete local bindings and verify readiness
      Active gameplay

Initialization failure or cancellation
  Stop the session and release acquired resources
  Present the result and return to StartMenu
```

`GameFlowManager` owns navigation and presentation. `SessionCoordinator` owns lifetime and the single transition to Active. `SessionInitialization` coordinates the stages and their completion. `GameSessionFactory` constructs and connects components. Networking reports transport and replication milestones; it does not independently decide that the whole application is ready.

Use explicit stages and a small completion contract. Keep one owner of each prerequisite and one activation point. Avoid another generic workflow engine or multiple competing readiness state machines. Initialization progress drives loading-screen text; it is separate from runtime simulation state.

Successfully prepared immutable registry data may remain application-owned and be reused by later sessions, but preparation occurs only during loading. Each session uses one complete, validated definition set without runtime replacement. Session objects and explicitly retained world resource references belong to the session scope. Failed preparation must not leave a permanently failed lazy cache or partially published registry set that prevents another initialization attempt.

All of the following must hold before activation:

- Definitions and registries are valid, required cross-references resolve, and runtime definition preparation is complete.
- World resources and required entities exist with valid ownership and local ship binding.
- Initial personal inventory and ship equipment are installed, with consistent item identities, locations, revisions, and container ownership.
- Derived stats, resources, equipment grants, movement configuration, supported abilities, and hotbar bindings reflect the installed state.
- The host has completed authoritative initialization. A joining client has installed the required topology, movement, gameplay, and private item baselines and completed the handshake for that same session and binding.
- Relevant remote-player state is available. Private remote inventories are not required unless the existing visibility contract permits and requires them.
- HUD, camera, targeting, and input bindings are ready to consume that state. No mandatory preparation remains for the first movement, supported ability use, or inventory interaction.

Network processing must continue during loading so baselines can arrive. Gameplay input and participation become active only after readiness. A host starting a new session need not wait for hypothetical future players. An active host continues running while another participant initializes.

No automatic deferred inventory population, starter equipping, registry parsing, stat compilation, or initial-state repair may run after loading completes. Later player menu interactions and existing gameplay-driven state changes remain normal runtime operations. Menu presentation can be created when opened if it only reads already initialized state. Normal resource and effect simulation continues during gameplay.

Preserve existing resource semantics: resolve a new ship's initial loadout, then initialize shield full and energy empty exactly once. Fight-ready describes completed initialization; it does not change energy, shield, cooldown, or ability rules. Rebinding, replication, and restoration must not repeat fresh-spawn initialization.

## Ordered implementation tasks

### 1 Establish the lifecycle and regression baseline

- [ ] Map startup and cleanup for Host/Join across Steam/ENet, including first registry access, scene construction, local bindings, baseline application, activation, and late joins.
- [ ] Record current build/test results and existing failures. Use current results rather than historical test counts from prior plans.
- [ ] Identify the existing tests that protect ownership, transactions, equipment grants, resource initialization, joins, session disposal, and movement.
- [ ] Record a reproducible runtime baseline at 5 and 10 players, with normal gameplay, equipment changes, and late joins. Separate diagnostic logging overhead from ordinary runtime measurements.

Acceptance: the implementation has a concrete map of the current gates and measurable comparisons for changed runtime paths. The existing gameplay contract remains the reference.

### 2 Separate definition loading responsibilities

- [ ] Keep typed catalogue descriptors and extract `Catalogue<T>` to its own file with a matching namespace.
- [ ] Separate item/category parsing and validation from ability/effect parsing. Give ship deserialization an explicit home rather than an unrelated forwarding method in the gameplay catalogue loader.
- [ ] Keep file access and schema validation in the loading layer, parsing in focused parsers, and lookup/storage in registries. Retain a small shared parser helper only for genuinely shared behaviour.
- [ ] Standardize expected loading failures using the existing `LoadResult<T>` and `DataLoadError` conventions at loading boundaries. Include source/schema paths, definition ID, and offending field or reference where applicable.
- [ ] Cover unknown categories, duplicate IDs, invalid stacking, invalid numeric values, and unresolved gameplay references. Keep duplicate detection within contextual error handling and avoid blanket catches that disguise programming errors as bad content.
- [ ] Preserve data formats, defaults, authored IDs, compatibility input capture, and existing precompilation. Temporary arrays, dictionaries, and additional validation passes are acceptable when they improve clarity.

Acceptance: existing valid data produces equivalent definitions and gameplay values. Invalid data produces actionable diagnostics. Loader extraction does not change compatibility checksums for unchanged inputs.

### 3 Establish SessionInitialization and its activation gate

- [ ] Keep StartMenu as UiCanvas/menu presentation with application services only. Audit eager scene/static initialization for gameplay work occurring before the loading screen.
- [ ] Ensure every Host/Join path shows and renders loading before starting session preparation.
- [ ] Introduce the explicit initialization orchestrator under the existing coordinator and scope. Define stage progress, prerequisites, failure, cancellation, and one successful completion transition.
- [ ] Invoke registry loading, cross-reference validation, compatibility preparation, and required resource preparation explicitly within initialization. Preserve reuse of already valid immutable data without making lazy first access the readiness contract.
- [ ] Define publication of the complete prepared definition set and reset failed preparation state, including affected captured compatibility inputs. Audit the existing static `Lazy` holders so failure followed by retry does not reuse a cached exception or mixed data. Keep successful data stable for every active session.
- [ ] Replace direct network-to-loading-completion wiring with reports to initialization. Enable simulation participation/input and dismiss loading only after the full contract is satisfied, without an observable partially active frame.
- [ ] Preserve cleanup after partial construction and reject callbacks from stopped or superseded sessions. Loading completion and cancellation must remain harmless when reported more than once.

Acceptance: transport connection or scene readiness alone cannot activate gameplay. Failure at any initialization stage releases the resources acquired so far and permits a clean new Host/Join attempt.

### 4 Install complete initial world and player state

- [ ] Separate obtaining initial authoritative world state from constructing and installing it. Supply the current new-world path now, with a narrow seam for a future restored-world source.
- [ ] Make the dependency order explicit: definitions/resources, world/entities, player ownership, inventory/equipment, derived stats/grants, resource state, and local bindings. Prepare prerequisites before publishing a participant as ready.
- [ ] Complete all initial inventory creation, equipment installation, hotbar assignments, and required presentation state during initialization. Remove delayed initialization or first-interaction fallbacks for these responsibilities.
- [ ] Distinguish new-player/new-ship defaults from supplied restored state. A future restore must preserve entity/item identities, ownership, equipment, current resources, and continuity metadata, and must not reapply starter equipment or fresh-resource initialization.
- [ ] Ensure the production startup policy supplies the intended playable loadout independently of optional development fixtures. Preserve supported mechanics and explicit empty-loadout scenarios used elsewhere.
- [ ] Keep the persistence seam concrete and small. Do not add save storage, a speculative save schema, or a general provider framework.

Acceptance: the first active frame can move and execute an available supported ability without completing inventory/equipment setup. Initializing a supplied state through focused tests does not duplicate items, grants, or resource initialization. Runtime control switches retain the existing ownership contract.

### 5 Complete synchronization before reporting readiness

- [ ] Define the initial synchronization contract for topology, local control, movement/gameplay state, private item state, revisions, and binding/session identity.
- [ ] Consolidate the existing snapshot and item-baseline safeguards. Acknowledge application readiness only when all required initial state and bindings are installed, rather than acknowledging spawn as though it represents the complete state.
- [ ] Specify a coherent snapshot-to-live-update handoff using the existing session/tick/revision mechanisms. Handle updates received during loading without rolling state back or requiring the live world to become static.
- [ ] Preserve authority checks, stale-message rejection, timeouts, duplicate handling, and private inventory visibility. Protocol changes must update compatibility/version checks and corresponding tests together.
- [ ] Keep late-join initialization scoped to the joining participant. Do not rerun session initialization or pause existing players on the host.

Acceptance: delayed or missing required state keeps the client loading or leads to a controlled failure. Duplicate/stale messages cannot activate the wrong session or overwrite newer state. Every supported transport follows the same application-readiness contract.

### 6 Verify runtime costs and transaction guarantees

- [ ] Retain the runtime improvements from the reviewed commits; avoid replacing bounded arrays or incremental indexes with allocation-heavy abstraction layers.
- [ ] Audit `ItemStore` preparation and installation callbacks. Preparation must not mutate published domain state; reserve required capacity before commit; installation must complete without expected failure; publish only after all indexes and domain state agree.
- [ ] Profile late-join entity construction, per-item inventory creation, per-slot equipment transfers, topology capture, and snapshot delivery on an already-active host.
- [ ] Verify resource retention/cache behaviour around ship spawning before treating a resource lookup as disk I/O. Prepare and retain resources during initialization where this removes measured runtime work.
- [ ] If measurements show spawn/loadout spikes, prepare initial state together and install it through an atomic validated path, or stage bounded work before admitting the new participant. Preserve ongoing host simulation and snapshot consistency.
- [ ] Confirm idle gameplay does not repeatedly reconstruct inventory/equipment views or redo loadout preparation. Preserve lazy presentation work where it is appropriate and does not conceal domain initialization.

Acceptance: transfers do not regain work proportional to unrelated store contents. Failed preparation preserves state. Late joins and gameplay interactions satisfy the runtime validation below without weakening authority or transaction safety.

### 7 Validate the complete phase boundary

- [ ] Run focused managed and Godot/in-engine tests for changed responsibilities, then the required integration checks. Include failure, cancellation, retry, and delayed/stale callback cases.
- [ ] Verify all four Host/Join and Steam/ENet paths show loading before initialization and satisfy the same readiness conditions.
- [ ] Verify the first playable frame and first supported ability use. Check that initial inventory/equipment preparation does not continue in subsequent idle frames, and opening the menu exposes correct initialized state.
- [ ] Exercise delayed/missing initial state, active-world changes during joining, repeated joins, and disconnects during initialization.
- [ ] Measure 5- and 10-player sessions under healthy and impaired network conditions, including movement/combat using existing supported mechanics, equipment-transfer bursts, and late joins.
- [ ] Record environment, workload, build configuration, frame/physics duration percentiles and worst spikes, publication intervals, allocation rate, and GC pauses. Compare with the baseline from task 1 and investigate regressions.
- [ ] Use ENet for repeatable local scenarios and actual GodotSteam peers for transport-specific validation. Record remote multi-account Steam verification separately if the environment cannot provide it.

Acceptance: 60 FPS/physics and 30 Hz publication remain stable at the supported scale on the documented test environment. The approximately 16.67 ms frame budget includes network-publication work on every second physics step; 33.33 ms is the publication interval, not an exclusive CPU budget. Record overruns and their causes rather than relying only on average FPS. Automatic GC is allowed; do not force collection.

### 8 Afterwork review for simplifications enabled by phase separation

Perform this task after the boundaries work and task 7 establishes their behaviour. It is required work, not an optional future cleanup.

- [ ] Revisit `GameFlowManager`, `SessionCoordinator`, `GamePlayManager`, `GameSessionFactory`, network join state, `GameWorld`, local bindings, and registry access with the new ownership map.
- [ ] Identify duplicate readiness flags, overlapping state transitions, repeated null/readiness checks, lazy initialization fallbacks, deferred setup callbacks, and repeated binding logic whose purpose disappears once initialization guarantees complete state.
- [ ] Review loader/registry forwarding methods, temporary adapters introduced during migration, source descriptors, caches, and startup-only objects for responsibilities that can now be combined or removed. Retain caches that serve measured runtime needs.
- [ ] Separate connection/protocol state, application lifecycle, and entity lifetime. Remove only duplicated state; these distinct concerns still need their own valid checks.
- [ ] Implement justified simplifications in a separate reviewable change. Replace hidden setup side effects with explicit construction/initialization dependencies where that reduces branching and coupling.
- [ ] Preserve checks at untrusted network boundaries, authority/revision/ownership validation, transaction safeguards, legitimate optional components, and protection against stale callbacks after cancellation. Initialization completion does not make these redundant.
- [ ] Record each candidate, the invariant that makes it removable, the change or reason for retaining it, and the relevant validation. If no simplification is justified, record the reviewed candidates and reasons rather than forcing deletions.
- [ ] Rerun the checks affected by simplification and repeat performance measurements when runtime paths change. Update the architecture documentation with the final lifecycle and ownership map.

Acceptance: the final architecture has fewer unnecessary branches, flags, wrappers, or callbacks where the new boundaries support their removal. No second readiness mechanism or migration-only fallback remains without a documented purpose. The readiness and runtime contracts still hold after simplification.

## Coding and validation rules

Follow [AGENTS.md](../../AGENTS.md): prefer `var`, avoid LINQ, keep comments useful and minimal, and align files with namespaces. Use one file per class/enum/record subject to the documented embedded-class exception. Apply these rules to new and modified responsibilities, including extracted lifecycle enums and catalogue records; unrelated repository-wide cleanup is separate.

Every new declaration or executable line introduced solely for automated or manual verification must carry a unique `TEST-SUPPORT` ID and the fully qualified test methods that use it. This includes initialization probes, performance instrumentation, injected failures, and helpers in application code. Verify marker uniqueness and referenced test names. Production initialization controls used by actual startup are ordinary production code.

Use meaningful boundary and regression tests rather than tests that merely mirror implementation details. Keep cancellation and failure coverage alongside the increment that changes those paths. Record actual verification results; do not mark a task complete from a plan or a successful earlier commit.

## Delivery and completion

Implement the numbered tasks in order, keeping each change reviewable and its relevant checks passing. Task 2 covers loading structure; tasks 3 through 5 establish the application boundary and synchronization; task 6 addresses measured runtime concerns; task 7 validates the integrated result; task 8 removes complexity made redundant by that result.

The work is complete when StartMenu remains outside the session, every startup enters visible loading, SessionInitialization produces a fully playable state, failure cleanup permits retry, active hosts tolerate late joins within runtime budgets, and the afterwork simplification review is completed and documented. Preserve the historical results in the preceding cleanup plan and place new implementation and validation evidence alongside this successor plan.
