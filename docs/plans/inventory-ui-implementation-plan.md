# Inventory UI implementation plan

Status: planning refinement complete for INV-01 through INV-10; INV-01 and INV-02 implemented. INV-03 through INV-10 remain pending.

Epic: [inventory-ui.md](inventory-ui.md). This file records each completed story refinement and implementation progress. Proposed type names are implementation guidance; responsibilities and observable behavior are the contract.

## Reference games and design decisions

Use familiar interaction conventions, while retaining this project's explicit requirements. The following are verified reference behaviors, not claims that these games use the proposed internal architecture:

- Diablo's official manual separates worn/wielded equipment from backpack storage and explains equipment requirements and empty backpack capacity. Adopt recognizable equipment destinations and visible remaining capacity. Its spatial backpack is a comparison point; this project's compact one-cell presentation and automatic growth are explicit project choices. [Blizzard's Diablo manual, Inventory Screen](https://ftp.blizzard.com/pub/misc/Diablo.PDF).
- The Witcher 3 manual distinguishes inventory-selected quick-access items and equipped consumables. Adopt a clear distinction between owned items, equipped items, and quick-access presentation. Aggregated consumable links and weapon-slot links are project decisions, not a claim about Witcher internals. [The Witcher 3 PC manual, interface entries 17 and 21](https://cdn-l-thewitcher.cdprojektred.com/media/TW3/Pdf/Manuals/PC/The_Witcher_3_Wild_Hunt_Game_Manual_PC_EN.pdf).
- Baldur's Gate 3's official Hotfix 21 records fixes for incompatible equipment placement, repeated sorting, and escaping a trade while dragging to obtain free items. Use these as regression examples for compatibility, repeatable sorting, and cancellation safety. [Larian Hotfix 21](https://baldursgate3.game/news/hotfix-21-is-now-live_112).
- BG3 Patch 3 records inventory filters resetting during transfers. Preserve filter/search/sort choices during item operations. [Larian Patch 3](https://baldursgate3.game/news/patch-3-mac-support-magic-mirror-more_93).

The domain architecture below is an engineering recommendation based on the inspected project. Do not import grid puzzles, weight, rarity, crafting, trading, auto-equipping, controller navigation, or multiplayer from reference games into this epic.

## Implementation boundaries and common decisions

- `GamePlayManager` creates and releases one item catalog, inventory owner, equipment owner, transfer coordinator, and item hotbar state per game instance. Prefer ordinary C# services with explicit ownership over new Godot autoloads or static state. The empty `InventorySystem` singleton shell can become such a service after verifying references; its one-instance requirement is enforced by ownership, not global access.
- Dependencies flow toward `ItemSystem`. It does not depend on inventory, equipment, gameplay, or UI. The inventory system does not depend on equipment. Put cross-system transfer logic in `EquipmentSystem/InventoryEquipmentTransferService.cs`, referencing inventory through its domain API. `GamePlayManager` wires it; manager and views contain no placement rules.
- `ItemSystem` owns item hotbar binding values and state. A weapon binding stores an opaque stable equipment-slot key; the gameplay adapter resolves it through `EquipmentSystem`. This prevents an ItemSystem-to-EquipmentSystem dependency cycle. Equipment still owns slot definitions and compatibility.
- Domain state exposes read-only snapshots. Mutations enter through validated commands, never through writable collections handed to views. Expected validation failures return a typed result/reason. Invalid definitions fail initialization rather than creating partially usable items.
- Keep state changes synchronous on the game thread. Precompute and allocate affected next-state snapshots before committing, install all snapshots without callbacks, then notify observers. Reentrant mutations during notification are rejected or deferred until publication finishes. Observer exceptions cannot roll back committed state or prevent remaining observers from being notified; log them through the existing integration logging path.
- A successful state-changing command advances participating revisions once. Rejected requests and no-ops publish nothing. Requests carry expected source identity/location and relevant state revisions; empty targets must still be empty at commit. No transaction database, undo framework, or general command bus is needed.
- UI editing is authorized by a short-lived inventory-open session. Open/close/reopen creates a new session identity; focus loss invalidates pending interactions. User placement requests must match the current edit session and state revisions. Gameplay additions do not require inventory to be open.
- Preserve `UiCanvas`'s four public menu operations. Add internal binding/lifecycle hooks only. Existing window focus, centered rows, scrolling, Escape behavior, and input release tracking remain authoritative.
- Production startup remains an empty inventory with 128 cells. Seeded demo items are opt-in development/test fixtures, never unconditional startup content. Category YAML definitions and schema now load through `FileLoadingSystem` in `GamePlayManager`; item-definition YAML loading remains deferred.

## INV-01 — Item metadata and extensible categories

Refinement: complete. Dependency: none.

Implementation: complete (2026-10-26). Added immutable category/definition catalogs, typed category/definition/entity IDs, validated entity creation, centralized stack limits and compatibility, and structured tooltip data. The eight categories are ordered metadata, replacing the unused enum. Catalogs copy inputs and expose read-only collections. No runtime items are seeded. Move/merge identity conservation will be exercised with the transfer operations in INV-03.

Validation: `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --filter FullyQualifiedName~ItemCatalogTests --verbosity minimal` passed all 25 cases. Coverage includes invalid/default/duplicate IDs, missing categories, invalid limits, category extension, immutable snapshots, entity identity, count boundaries, compatibility, and tooltip metadata.

Regression validation: `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --verbosity quiet` passed all 152 cases; `git diff --check` passed. Existing nullable-context, member-hiding, and Godot test-source-generator warnings remain. Scene smoke checks were not run for this domain-only increment; UI integration starts in INV-04.

Category loading follow-up (2026-10-26): `assets/game_data/ItemCategoryDefinitions.yml` supplies the eight category definitions; `ItemCategorySchema.yml` validates their shape. `GamePlayManager.OnReady` assembles the existing `FileLoader.LoadYamlDefinition` call and converts the DTOs into its game-owned `ItemCategories` before activating gameplay. Defaults copy to `user://config/items/ItemCategoryDefinitions.yml` when missing or when `ReloadConfigOnStart` is enabled. Invalid schema/content or duplicate IDs use the existing abort-load path; no hardcoded fallback replaces invalid user data. Teardown also supports failed initialization. The domain remains independent of YAML and Godot.

Follow-up validation: all 16 `ItemCategoryLoadingTests` passed; the full NUnit suite passed all 168 cases. The headless `ui_architecture_smoke.gd` run confirmed successful default copying and startup loading across game lifetimes, but exited with window visibility, horizontal centering, and scrollbar assertion failures. These UI layout assertions remain unresolved; category loading produced no errors.

Category icon follow-up: YAML accepts optional `Icon: Alchemy.Arsenic` references to the public nested properties in `Icons`. All eight packaged categories now specify icons. Startup resolves these references into `GamePlayManager.CategoryIcons`, keyed by category ID, before publishing the loaded catalogs or activating gameplay. Unknown keys abort loading with category/source details. Domain categories retain only the string `IconKey`, keeping textures in the integration layer. Existing user YAML files are preserved: add the new `Icon` entries there or enable `config.reload_config_on_start` to copy updated defaults. Validation: all 30 category-loading cases and all 182 NUnit cases passed; `git diff --check` passed. No new scene smoke run was performed for this follow-up.

### Output and implementation steps

1. Replace the enum-only category vocabulary with a small immutable category catalog. Use stable string-backed category IDs, not enum ordinals or display labels. Initial IDs: `engine`, `hull`, `shield_generator`, `reactor`, `light_weapon`, `heavy_weapon`, `consumable`, `resource`.
2. Define category metadata: ID, display name, display order, optional icon reference, and `CanStack`. Consumable and Resource can stack; the other six cannot. Category tabs enumerate catalog metadata rather than an enum or UI switch.
3. Define immutable item definitions with definition ID, display name, category ID, optional icon asset path, description, and optional maximum stack size. Resolve referenced categories and reject duplicate/empty IDs, missing categories, blank names, and nonpositive explicit limits.
4. Compute effective stack limits centrally: one for nonstackable items; explicit limit or `int.MaxValue` for stackable ones. Reject contradictory explicit nonstackable limits above one rather than silently accepting incorrect content.
5. Define distinct definition IDs and runtime entity IDs. Generate runtime IDs once per new entity/stack, not per redraw. A full move/swap preserves IDs; a partial merge preserves both IDs; a full merge retains the destination ID and retires the source ID. Do not expose retired IDs as live entities.
6. Keep stack counts as positive `int` values bounded by the effective limit. Zero means an empty location, not a zero-count item entity. Use `long` for bulk additions and aggregate quantities, with checked arithmetic and bounds before allocation.
7. Provide structured tooltip data from the catalog: name, category name, description, effective stack information. UI formats it and resolves textures. No stats, rarity, effects, or category subclasses are introduced.

Likely files: `Scripts/ItemSystem/ItemCategories.cs` (replace obsolete enum representation), category/definition catalogs, identity values, item entity/stack model, and tooltip data. Keep file count proportionate; do not create a generic registry or inheritance framework.

### Acceptance examples and checks

- A Consumable with no limit has limit `2147483647`; an Engine with no limit has limit one.
- Two consumable definitions in the same category never merge with each other.
- Two stacks of one definition have different runtime IDs. Rendering changes neither ID.
- Registering another nonstackable category with metadata makes it discoverable without a UI edit; it is not automatically equippable or hotbar-eligible.
- NUnit covers invalid definitions, defaults, ID lifecycle, and stack compatibility. Category YAML schema/deserialization and catalog conversion are covered by `ItemCategoryLoadingTests`; item-definition loading remains deferred.

## INV-02 — Inventory state and growth

Refinement: complete. Dependency: INV-01.

Implementation: complete (2026-10-26). Replaced the unused singleton shell with a plain game-owned inventory service. Immutable snapshots expose stable indexed storage, occupied entries, genuine empty addresses, capacity, and revision. Bulk additions fill matching partial stacks and existing holes in ascending address order before growing in the minimum 64-cell chunks. Planning uses bounded `long` arithmetic and constructs the full next snapshot before installation; rejected requests and no-ops preserve the exact prior snapshot and publish nothing. Existing stack IDs survive additions. Storage never shrinks or compacts.

Internal prepare/validate/install/publish hooks support later transfer coordination, rejecting stale or repeated plans and duplicate live identities. Notifications run after installation, isolate observer/logging failures, and reject reentrant mutations. `GamePlayManager` creates an empty item catalog and inventory after category initialization, before gameplay activation, and disposes/releases them on teardown. Production remains empty; item-definition YAML loading, view binding, and inventory-open edit sessions remain with subsequent stories.

Validation: `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --filter FullyQualifiedName~InventorySystemTests --verbosity quiet` passed all 20 cases. `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --verbosity quiet` passed all 202 cases; `git diff --check` passed. Coverage includes 128/129/192/193/257 boundaries, partial stacks before holes, full stacks, ID conservation, quantities above `int.MaxValue`, huge atomic rejection, immutable snapshots, no shrinking, stale plans, observer failure/reentrancy, disposal, and independent game state. Existing nullable-context, member-hiding, and Godot test-source-generator warnings remain. Scene smoke checks were not rerun for this domain increment; actual menu/game-scene lifetime checks remain for UI integration.

### Output and implementation steps

1. Make `InventorySystem` the authoritative game-owned C# inventory service. Use indexed storage addresses stable across presentation filtering/sorting. Initialize 128 empty addresses; expose capacity, occupied snapshots, genuine empty addresses, revision, and change notifications.
2. Implement bulk add by definition and quantity. Fill matching partial stacks in ascending storage order, then existing empty addresses in ascending order, then append capacity in 64-cell chunks. New stacks receive new entity IDs; existing stacks retain theirs.
3. Plan the entire addition before commit. Determine required new stacks and capacity with `long` arithmetic; check supported collection/capacity bounds before allocating. Zero addition is a no-op; negative quantities and unknown definitions are rejected. Impossible capacity/quantity requests fail without partial insertion.
4. Never automatically shrink or compact storage. Compact presentation is separate. Removing/merging an entity can leave an underlying empty address; views pack remaining items without changing their addresses.
5. Add internal plan/commit access needed by transfer coordination; ordinary consumers receive no writable storage. Keep gameplay additions separate from inventory-open user edit permission.
6. `GamePlayManager.OnReady` creates and binds the service before gameplay activation. `OnExitTree` revokes edits, detaches consumers, and releases references. Closing a menu changes no inventory contents or capacity.

### Acceptance examples and checks

- 128 nonstackable items fill initial capacity; the 129th extends it to 192.
- Adding 257 nonstackable items to an empty inventory yields capacity 320, not 384; additions may span multiple chunks in one command.
- A partial matching stack consumes an addition before an empty slot is allocated. Multiple partial stacks fill deterministically.
- Adding to full matching stacks creates another stack as required; placing onto a full matching stack later is a no-op, not automatic growth.
- Unit checks cover exact boundaries, pre-existing holes, multiple chunks, huge rejected quantities, identity conservation, and no notifications on no-ops. Menu/new-game lifecycle is checked during integration.

## INV-03 — Equipment and atomic transfers

Refinement: complete. Dependencies: INV-01, INV-02.

### Output and implementation steps

1. Create game-owned `EquipmentSystem` state with immutable slot definitions and an ordered snapshot of eight locations. Stable keys: `engine`, `hull`, `shield_generator`, `reactor`, `light_weapon_1`, `light_weapon_2`, `heavy_weapon_1`, `heavy_weapon_2`. Display names follow the epic.
2. Each slot definition references its accepted category. Slots never infer compatibility by comparing translated labels. Both light slots accept Light Weapon; both heavy slots accept Heavy Weapon. Each occupies exactly one entity; no multi-slot weapons.
3. Add a transfer service in `EquipmentSystem` to handle inventory/inventory, inventory/equipment, and equipment/equipment placement using one validate/plan/commit path. Inventory remains ignorant of equipment rules.
4. Requests carry source location/entity, destination location, expected participating revisions, and current edit session. Check current identities and session, compute the operation, validate both resulting locations and limits, build all next states, then install and publish consistently.
5. Empty destination: move. Occupied matching stack: merge by `min(source count, remaining destination capacity)`. Full matching stack: no-op. Other occupied destination: swap only if both resulting placements are legal. Same source/destination: no-op. A rejected placement does not auto-route elsewhere.
6. Never grow inventory for an explicit placement: the target must be an existing address. Growth is driven by adding items, not failed equipment attempts. Supply typed failure reasons for feedback without embedding UI strings in domain rules.
7. `GamePlayManager` wires the coordinator and creates/disposes equipment state alongside inventory. No view can observe an item simultaneously owned by inventory and equipment or missing from both during a transfer.

### Acceptance examples and checks

- Inventory Engine onto occupied Engine swaps, returning the previous engine to the source inventory address.
- Inventory Hull onto Engine is rejected. Equipment Engine onto inventory Hull is rejected because the Hull cannot occupy the vacated Engine slot.
- Light Weapon Slot 1 and Slot 2 can swap. Light/heavy swaps are rejected.
- Consumable stacks 7 and 8 with limit 10 become source 5 and destination 10. Total quantity and live IDs remain correct.
- Unit checks cover all 64 category/slot combinations, reverse validation, same-location no-ops, merge overflow/full targets, stale revisions, closed edit sessions, and consistent snapshots observed in notifications.

## INV-04 — Prepare scenes and generate slots

Refinement: complete. Dependencies: INV-02, INV-03; epic editor checklist.

### Output and implementation steps

1. Use the epic's editor checklist for the inherited inventory scene, `inventory_container.tscn`, `item_grid.tscn`, `icon_with_label.tscn`, and `clickable_icon.tscn`. Keep the existing Button root and scene paths. Reuse the existing factory/provider pattern; add creation methods for labeled icons rather than constructing a competing UI hierarchy.
2. On view initialization, detach all preview children from `EquipmentContainer` and all cells from `SlotGrid` immediately, then queue them for deletion. Deferred deletion alone must not leave duplicates participating in layout. Retain `SlotGrid` and the scroll root. Cleanup is idempotent.
3. `EquipmentContainer` builds eight `IconWithLabel` instances from supplied equipment snapshots, including labels for empty slots. `InventoryContainer` coordinates presentation; `ItemGrid` renders cells and forwards pointer requests. Generic icon components accept display data and an opaque binding, without querying singleton services.
4. Introduce a small inventory presentation projection now, before filtering. Enumerate occupied addresses in storage order, followed by genuine empty addresses. This guarantees no visible gaps even before INV-09. Every cell carries its real domain address and entity expectation; display position is not an address.
5. Render 128 cells when empty; append capacity on growth. Under the unfiltered view, displayed cell count equals storage capacity. Update cells in place where practical; avoid rebuilding the whole grid every frame. Start without virtualization or pooling.
6. Add internal `UiCanvas` binding and window lifecycle integration. Bind new inventory views before interaction, supporting data supplied before `_Ready`. Open/close notifications explicitly identify InventoryMenu; `HasBlockingWindow` alone is insufficient because Map may remain open.
7. Maintain selection/search/sort presentation state at a UI session owner outside the disposable window so reopening preserves choices. Domain state stays in the game-owned services. Detach view subscriptions before freeing nodes and during game teardown.

Likely files: existing inventory menu scripts, `ItemGrid`, `IconWithLabel`, factory/provider helpers, internal `UiCanvas`/`UiWindowManager` integration, and the reusable scenes. Keep `UiCanvas` public API unchanged.

### Acceptance examples and checks

- First open yields exactly eight equipment cells and 128 empty inventory cells, regardless of preview child counts.
- Reopen yields the same state without duplicate cells or event handlers; a new game receives independent state.
- Removing an item from underlying address 2 packs visible occupied items together without relocating their domain entities.
- Inspect the real scene at logical 1280x720 and 1920x1080: labels fit, vertical scrolling works, and window row overflow still follows the existing manager.
- Focused projection checks verify address mapping and trailing empties; scene smoke checks verify actual nodes and lifecycle.

## INV-05 — Item visuals, tooltips, and quantities

Refinement: complete. Dependencies: INV-01, INV-04.

### Output and implementation steps

1. Extend `ClickableIcon` with generic display input for tooltip text and optional `long` quantity. It renders data, not category rules. The inventory presenter supplies a quantity only for stackable items; ordinary existing HUD icons keep no quantity badge by default.
2. Use the bottom-right `QuantityLabel` from the editor checklist. Show exact small values, including one. Abbreviate large values to fit the established cell size, for example `1.2K` or `2.1B`, without rounding above the actual value. Retain the exact full amount in the tooltip. Formatting must handle the supported `long` range without wrapping or expanding the grid.
3. Use normal delayed Godot tooltips initially, with name, category, description, and exact quantity/stack limit where applicable. Represent an omitted stack limit to players as no configured limit rather than requiring them to interpret `int.MaxValue`; the domain limit still applies. Avoid special stat comparison popups in this epic.
4. Resolve and cache textures in the UI adapter. Missing icons use a neutral placeholder while preserving item identity and tooltip text; empty slots have no item texture. Missing definitions are a domain error, not a texture fallback case.
5. Quantity labels use Mouse Filter Ignore and do not grab focus. Tooltip presentation does not become a drop target. Cancel visible tooltips when placement begins, a source disappears, the window closes, or focus is lost.
6. Reusing a cell clears every previous field before applying a new binding. An unavailable hotbar binding can still receive a tooltip explaining its state; it must not depend on an enabled activation Button.
7. Add an explicit development fixture containing every category, duplicate definitions in separate stacks, limited/unlimited stackable items, a missing-icon example, and large counts. Normal gameplay startup remains empty.

### Acceptance examples and checks

- A single consumable displays `1`; an engine displays no count; an emptied consumable cell retains neither count nor old tooltip.
- Multiple large stacks aggregate correctly beyond `int.MaxValue`. Compact labels and exact tooltip values agree.
- Tooltip hover and quantity overlays do not prevent clicking, dragging, scrolling, or cancellation.
- Unit checks cover formatting boundaries and reusable display-data reset. A real scene check covers pointer interception, tooltip dismissal, and large-count layout.

## INV-06 — Click pickup and placement

Refinement: complete. Dependencies: INV-02, INV-03, INV-05.

### Output and implementation steps

1. Add one UI interaction controller shared by inventory, equipment, and later hotbar surfaces. States are Idle, ClickCarrying, and Dragging; there is only one pending source at a time. This controller stores source identity/address, expected state revisions, edit-session identity, and presentation-mapping version, never item ownership.
2. In Idle, left-button release over an occupied cell starts ClickCarrying unless a drag began. Left-clicking an empty cell does nothing. While carrying, a left click on a target submits placement; clicking the source cancels.
3. Show a cursor-following icon preview and a distinct source highlight without removing the source icon. Use the same read-only domain validation for valid/invalid destination feedback; do not duplicate rules in the controller.
4. On success, end carrying and render committed state, including after partial merges. On a full matching stack no-op, retain carrying so the user can choose another target. An incompatible target retains carrying and shows a concise reason; stale state/session cancels and refreshes rather than silently retargeting.
5. A left click outside a slot while carrying cancels. Consume the placement/cancellation event so it cannot also activate a hotbar action or another control. Clicking a toolbar control to change the view cancels carrying first, then allows the control's normal operation.
6. Right-click cancels a pending source. Right-click on an idle inventory/equipment cell does nothing in this epic; it never consumes, equips, deletes, or world-drops an item. Hotbar right-click clearing is defined in INV-08 and cancels pending placement before clearing.
7. Preserve Escape close-all behavior in the current window manager while clearing item state; do not replace it with a first-Escape-only-cancels convention. Releases remain tracked. Revoke pending interactions on focus loss, inventory closure, inactive game state, source changes, or presentation-mapping changes.
8. Capture target expectations from the displayed binding before processing a click. If compaction refreshes the grid during the operation, never resolve the destination again by its new screen index.

### Acceptance examples and checks

- Click Engine in inventory, then Engine slot: equip. Click Engine then Hull slot: no mutation and clear invalid feedback; carrying remains available.
- Place seven consumables onto eight of the same definition with limit ten: source five, destination ten, then carrying ends.
- Pickup, close inventory, reopen, and click another slot: no old placement occurs. Escape still closes all windows.
- Unit checks cover controller transitions and command results; pointer smoke checks cover release behavior, outside cancellation, and no accidental double activation.

## INV-07 — Native drag-and-drop

Refinement: complete. Dependency: INV-06.

### Output and implementation steps

1. Adapt Godot `_GetDragData`, `_CanDropData`, and `_DropData` through a small item-slot-specific adapter around the existing icon controls. Keep reusable `ClickableIcon` free of inventory/equipment service dependencies; use a presentation adapter/subclass or forwarded delegates rather than another unrelated widget system.
2. Native drag payloads carry an opaque interaction token, not a mutable item object. Keep the typed source request in the interaction controller; accept tokens only from the current game/edit session. `_CanDropData` is read-only. `_DropData` revalidates current state and permissions.
3. Start dragging using Godot's configured threshold. Distinguish a completed click from a drag and suppress the corresponding Button release action. The drag preview ignores mouse input, and the authoritative source remains occupied until commit.
4. Valid destinations provide a clear border/indicator; invalid hovered destinations provide feedback without relying only on red/green colors. Do not play a success effect before the command succeeds.
5. Every drag end clears pending state, whether placement succeeds, fails, or lands outside. Closing inventory or losing focus invalidates the token immediately; late `_DropData` calls are rejected. A failed drag returns visually to the untouched source.
6. Support wheel scrolling while carrying/dragging using the existing scroll container. Keep window dragging and automatic edge-scroll enhancements outside this story. A source or mapping change cancels instead of placing an item onto a newly rebound cell.

### Acceptance examples and checks

- Repeat click-placement success/failure cases through actual native dragging; results and failure reasons match.
- Hovering several destinations changes no state or revisions. Dropping once commits at most once.
- Drag outside, press Escape, change filter, lose focus, or close/reopen inventory: no ownership or quantity changes.
- Domain tests remain those of INV-03; controller tests cover token validity. Scene smoke must generate real mouse movement across the drag threshold rather than calling the command method as a substitute.

## INV-08 — Hotbar item links and editing permissions

Refinement: complete. Dependencies: INV-05, INV-06, INV-07.

### Output and implementation steps

1. Retain ten hotbar positions, matching `PlayerMock.GetHotBarSkills()` and the current scene smoke expectations. `ItemSystem` defines consumable-definition links and weapon-equipment-slot links, plus validated item-binding operations. Category eligibility is domain metadata/policy, never a UI category switch; new categories default to no hotbar assignment.
2. Keep a small gameplay-owned composition adapter for existing mock skill entries and item links. Preserve original mock action identity separately from display position so rearrangement cannot invoke the wrong original skill index. ItemSystem must not depend on the mock or contain its actions.
3. Assigning an eligible inventory/equipment source to a hotbar position replaces that position's entry; it changes no source ownership. Do not auto-add items to the hotbar or create a second item copy. Multiple links to the same definition/weapon slot are permitted.
4. Consumable links store definition ID, sum all matching inventory stacks using `long`, and display quantity zero when none remain. Their definition/icon/tooltip remain available at zero. Same category with a different definition is excluded.
5. Weapon links store the stable equipment-slot key. Resolve current equipment each refresh: occupied means show the current weapon; empty means unavailable with the slot label/placeholder, retaining the link. Assignment from inventory weapons, empty equipment slots, and unsupported categories is rejected. Already assigned unavailable links can still be moved or cleared.
6. While inventory is open, both interaction modes move an existing hotbar entry to an empty position or swap with an occupied position. A new assignment from inventory/equipment replaces a destination entry; it does not move that entry back into inventory. Moving a hotbar link onto inventory/equipment is invalid; no item is transferred or unequipped.
7. Right-clicking a hotbar position while inventory is open cancels any current pointer interaction and clears that position only. Right-clicking an empty position is a no-op. Clearing leaves the position empty; it does not restore an overwritten mock action. Outside drops cancel rather than clear.
8. Separate `CanEditHotbar` from `CanActivateHotbar`. The current `HotbarControl.SetGameplayInputEnabled` disables Buttons, and `PlayerHud` also gates forwarded presses; those mechanisms cannot alone support editing. Keep pointer interactions/tooltips available during inventory editing while independently suppressing activation requests. Preserve `PlayerActionController` as the final activation gate.
9. Derive edit permission from InventoryMenu being open and the game session active, not from whether any blocking window exists. Map alone must not unlock editing. Close inventory while Map remains open: edits stop immediately and activation remains blocked by Map.
10. Item link activation validates current availability and produces a typed request/log through gameplay integration only. Do not consume quantities, fire weapons, execute effects, or call the legacy mock action at the item's display index. Legacy skill entries retain their current activation route.

Likely files: item hotbar binding model/state in `ItemSystem`, gameplay composition/availability integration, `GamePlayManager`, `UiCanvas`, `PlayerHud`, and `HotbarControl`. Reuse the slot interaction adapter and display inputs from INV-05 through INV-07.

### Acceptance examples and checks

- Matching consumable stacks with quantities 7 and 8 yield hotbar count 15. Merge, sort, or move either stack and the link remains correct; remove all and count becomes zero without losing the assignment.
- A link to `light_weapon_1` updates when its weapon is replaced, becomes unavailable when empty, and works again when re-equipped.
- Place an inventory consumable onto an occupied hotbar position: only its binding changes. Place an inventory weapon there: reject.
- Open inventory: click/drag/right-click edit entries without activating them. Close inventory: the same gestures cannot edit entries. Existing skill activation remains possible only through the existing policy.
- Unit checks cover eligibility, aggregates, duplicate links, stale edits, swap/replace/clear distinctions, and legacy action identity. Scene checks verify actual disabled/interactive behavior, not just a Boolean permission field.

## INV-09 — Category tabs, search, sorting, and compact results

Refinement: complete. Dependencies: INV-04, INV-05; INV-06/INV-07 integration required.

### Output and implementation steps

1. `InventoryContainer` owns UI orchestration through a plain presentation model; `CategoryTab` is a display descriptor, not an authoritative category definition. Generate All plus eight category tabs from the catalog, preserving stable category display order. Empty categories remain selectable; All is a presentation option, not a ninth category.
2. Search item display names case-insensitively using trimmed input. Combine the selected category and search with AND semantics. Empty search matches all names. No fuzzy search, regex, description search, or gameplay filter language is needed.
3. Populate Name ascending and Name descending options; default to ascending when this story lands. Compare names case-insensitively with an ordinal comparison for deterministic behavior, then definition ID, then entity ID for stable ties. Descending reverses the primary name order while retaining deterministic tie-breakers.
4. Produce a compact projection of matching occupied items, then genuinely empty storage addresses. If capacity is C, total occupied count N, and matching occupied count M, show M occupied cells followed by C-N empty cells. Hidden occupied entities never generate empty destinations. In All without search, this is exactly C cells.
5. Example: C=128, N=10, M=3 gives three matching item cells followed by 118 empty cells, not 125. If all capacity is occupied and nothing matches, show an empty-result message with no fake drop target. When genuine empties exist, the message can coexist with the trailing empty cells.
6. Filter/search/sort do not mutate inventory, merge stacks, or allocate capacity. Retain actual addresses on every projected entry. Dropping into a visible empty cell targets that genuine address; after a successful move, the item is rendered according to the active sort/filter, not the pointer's chosen screen position.
7. Manual spatial ordering is not guaranteed by this compact representation. With Name sort active, swapping two items may leave their visible name order unchanged while correctly exchanging underlying locations. Give normal success feedback; do not silently disable sorting to simulate a spatial backpack.
8. Keep category/search/sort selections through moves, merges, equipment transfers, and menu reopen. A changed view mapping cancels a pending placement rather than retargeting it. Clicking search/tab/sort controls remains usable. Typing in search should not trigger Inventory/Map toggle shortcuts; handle text entry before navigation while retaining Escape close-all and release tracking.
9. Refresh after committed changes and bound UI inputs, not each frame. With 128 initial cells, do not add debounce jobs, asynchronous filtering, or virtualization until measurement warrants it. Preserve a sensible scroll position and clamp it when results shrink.

### Acceptance examples and checks

- Every initial category appears without UI switches. Add another category fixture and the tab appears automatically.
- Search ` engine ` matches Engine case-insensitively; selecting Resource excludes Engine regardless of search text.
- Reapply the same sort repeatedly: ordering remains stable. Sorting never changes domain revisions or consumable aggregates.
- Full inventory plus a restrictive filter never exposes nonexistent free capacity. Actual empty addresses remain valid drop targets.
- Transfer a matching item out while filtered: remaining results pack together; selections remain set; no blank gaps appear.
- Projection unit checks cover the C-N+M formula, all/full/no-match states, deterministic ties, genuine address mapping, and state retention. Native scene checks verify filtered placement and text-input/navigation behavior.

## INV-10 — Integration, regression checks, and handoff

Refinement: complete. Dependencies: INV-01 through INV-09.

### Output and implementation steps

1. Add meaningful NUnit coverage alongside existing suites: item model/rules, inventory additions/growth, equipment and transfer coordination, hotbar bindings, and pure presentation/controller behavior. Place suites under the corresponding domain folders in `sMPGWM.Tests`; keep Godot pointer behavior in scene smoke tests.
2. Extend the existing `sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd` only where new semantics require it. Its current Button-disabled expectations must distinguish blocked activation from enabled editing: test that activation cannot execute while inventory is open and that editing remains possible. Do not weaken input-blocking checks to make the new feature pass.
3. Add `sMPGWM.Tests/UiSystem/inventory_smoke.gd` using opt-in fixtures, real scene instantiation, native mouse events and drag thresholds. Cover logical 1280x720 and 1920x1080, vertical bag scrolling, horizontal multi-window overflow, and two game-scene lifetimes.
4. Run focused domain/controller tests as each story is implemented; run scene checks once the relevant interaction exists. At epic completion, build and run the whole NUnit suite, existing architecture smoke, and new inventory smoke. Report actual failures and distinguish environment blockers from code regressions.
5. Inspect tooltip rendering, bottom-right count readability, focus loss, pointer preview cleanup, and empty results interactively where headless assertions cannot establish visual quality. Verify reference-game-inspired feedback without adding out-of-scope features.
6. Update `Scripts/UiSystem/README.md` to remove obsolete statements that item dragging is deferred and document the actual internal bindings/editing policy. Update the epic and this file with implemented story status and exact checks executed only when implementation occurs.

### Required regression matrix

| Area | Required cases |
| --- | --- |
| Item metadata | Eight categories; invalid/duplicate IDs; unknown category; nonpositive limits; nonstackable contradictions; omitted limit; definition versus entity identity. |
| Inventory | Empty 128; fill without growth; grow to 192; multiple chunks; fill partial stacks first; safe arithmetic; no shrinking; unchanged state on failed add. |
| Equipment | All 64 category/slot combinations; reverse-swap compatibility; light/light swap; light/heavy rejection; no multi-slot assumptions. |
| Atomicity | Rejected requests preserve both states; correct live IDs; conserved quantities; callbacks observe both committed states; stale source/target/session rejected; duplicate commit is harmless. |
| Pointer input | Click and native drag parity; full/partial merges; invalid feedback; no drag-release click; outside/source-click/right-click cancellation; Escape; focus loss; close/reopen. |
| Hotbar | Aggregate across all matching stacks; zero-count retention; no direct inventory weapon link; slot replacement/empty/refill; link swap/replace/clear; closed-inventory edit rejection; legacy action identity. |
| Presentation | No occupied gaps; correct empty-cell formula; filtered address mapping; deterministic repeated sorting; preserved view choices; exact tooltip counts; large count labels; missing icon fallback. |
| Lifecycle | GamePlayManager owns one instance of each system; no state reset on view reopen; independent next game; no stale subscriptions, previews, or edit tokens. |
| Existing behavior | Four public UiCanvas menu methods; window focus/placement/scrolling; held-input release guards; blocked activation; Map remaining open; unrelated HUD actions. |

### Implementation validation commands

Run from the repository root after implementation, starting with the relevant focused NUnit filter:

```powershell
dotnet build sMPGWM.sln
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
$godotExecutable = 'C:/Users/lober/godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe'
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/inventory_smoke.gd
```

Verify the local Godot executable path when implementing. The inventory smoke file above is planned, not present yet. Normal scene startup may load existing Steam autoloads; record actual environment limitations if encountered. No build or game tests are needed to validate this documentation-only refinement.

## Sequencing and completion ledger

Implement INV-01 through INV-10 in numeric order, completing the editor checklist before INV-04. Each story carries its own focused checks; INV-10 consolidates integration rather than postponing all verification until the end.

| Story | Refinement | Implementation |
| --- | --- | --- |
| INV-01 Item metadata | Complete | Complete |
| INV-02 Inventory state | Complete | Complete |
| INV-03 Equipment/transfers | Complete | Not started |
| INV-04 Scenes/empty slots | Complete | Not started |
| INV-05 Tooltips/counts | Complete | Not started |
| INV-06 Click placement | Complete | Not started |
| INV-07 Drag-and-drop | Complete | Not started |
| INV-08 Hotbar links | Complete | Not started |
| INV-09 Browsing/compact results | Complete | Not started |
| INV-10 Integration checks | Complete | Not started |

No unresolved product questions block these stories. Defaults inferred during refinement are recorded explicitly above; future item-definition YAML schema/loading and actual item effects remain outside the epic. Category YAML loading was added by explicit follow-up request.
