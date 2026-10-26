# Epic: Inventory, Character Equipment, and Item Hotbar Links

Status: implementation started. INV-01 item metadata/startup category YAML loading, INV-02 inventory state/growth, and INV-03 equipment/atomic transfers are implemented; all 281 NUnit tests pass. INV-04 through INV-10 remain pending. The headless UI architecture smoke previously failed layout assertions; it was not rerun for these domain increments. See the implementation plan for validation details.

All ten stories have completed planning refinement. See [inventory-ui-implementation-plan.md](inventory-ui-implementation-plan.md) for story outputs, technical decisions, examples, implementation progress, and checks. Refined does not mean implemented. The implementation plan resolves presentation and interaction details within the requirements below.

## Goal and verified baseline

Refine the existing inventory and character screen, with domain-owned state and rules, familiar click placement and drag-and-drop, tooltips, stack counts, and hotbar links. Implement metadata and the minimum state operations needed by these interactions; defer actual item gameplay.

INV-01 added the extensible item metadata and domain model. INV-02 added game-owned inventory, immutable storage snapshots, atomic bulk additions, and automatic growth. INV-03 added game-owned equipment and atomic transfer coordination with revision/entity/session validation. INV-04 binds these services to disposable inventory views, generates eight labeled equipment cells and initially 128 empty bag cells, and maintains compact address-preserving projections as state changes. `ItemGrid/SlotGrid` retains six columns and replaces its twelve editor preview cells at runtime. Equipment previews now contain all eight labels. Empty cells have no mock texture, and the shared icon scene has a hidden quantity label prepared for INV-05. Item visuals, pointer interactions, and category/search/sort behavior remain pending.

The existing HUD routes activation through `PlayerActionController` and the player-input policy. Preserve that integration and the existing overlay window system.

This epic supersedes the previous document's claims of an implemented prototype and its conflicting three-weapon-slot layout.

## Domain ownership

Paths are relative to `sMPGWM.Game/Scripts`.

| Owner | Responsibility |
| --- | --- |
| `ItemSystem` | Extensible categories, item definitions, definition and entity IDs, stack eligibility and limits, tooltip content, shared item metadata, and item-related hotbar binding data. |
| `InventorySystem` | One instance per running game; inventory state, capacity, additions, inventory moves, stack merging, and validation. Uses `ItemSystem`. |
| `EquipmentSystem` | One instance per running game; equipment state, slot definitions, compatibility, equipping and unequipping. Uses `ItemSystem`. |
| `GamePlayManager` and domain coordination | `GamePlayManager` initializes and tears down game-instance systems and wires integration. A domain coordinator handles atomic inventory/equipment transfers and resolves hotbar links through the state owners. |
| Existing UI system | Renders supplied data, forwards requests, and owns selection, hover, previews, filtering, search, and sort presentation. |
| `Util/UiComponents` | Reusable visual controls accepting icons, labels, tooltip content, stackability, and quantities. No item compatibility rules. |

Inventory and equipment remain independent systems, outside `ItemSystem`. They are not owned by inventory views. Opening or closing a view never resets their state. `GamePlayManager` is the initialization/teardown owner per running game instance; avoid duplicate scene-owned and autoload instances. Integrate with its existing `OnReady` and `OnExitTree` lifecycle, initializing dependencies before binding views and detaching consumers before releasing state.

Domain data must not depend on Godot controls or textures. Views resolve visual assets. Cross-system transfers must succeed or fail together; a view must not perform separate remove/add mutations. Do not move unrelated existing hotbar actions into the item domain.

Use a domain coordinator with a validate/plan/commit boundary for cross-system transfers. Validate identities, current source/destination state, compatibility in both directions, and quantities before committing. Serialize commits on the game thread, reject stale requests, and publish change notifications only after all affected state is consistent. Keep observers outside the commit itself. No database or distributed transaction framework is required for local game state.

## Confirmed requirements

### Categories and character slots

| Category | Stackable | Equipment destination |
| --- | --- | --- |
| Engine | No | Engine |
| Hull | No | Hull |
| Shield Generator | No | Shield Generator |
| Reactor | No | Reactor |
| Light Weapon | No | Light Weapon Slot 1 or 2 |
| Heavy Weapon | No | Heavy Weapon Slot 1 or 2 |
| Consumable | Yes | None; inventory hotbar link permitted |
| Resource | Yes | None |

There are eight initial categories and eight equipment slots. Slot display order follows the table, with the two light slots followed by the two heavy slots. Stable slot and category identities are separate from display labels.

Category definitions supply display metadata and stacking eligibility. Item definitions supply identity, name, category, icon reference, tooltip content, and an optional stack limit. Item entities/stacks have IDs distinct from definition IDs; multiple stacks can share a definition.

Category definitions are authored in `assets/game_data/ItemCategoryDefinitions.yml` and validated against `ItemCategorySchema.yml`. `GamePlayManager` loads them before gameplay activation using the existing file loader, which copies packaged defaults to `user://config/items/ItemCategoryDefinitions.yml` when absent or forced by `ReloadConfigOnStart`. Invalid data aborts startup through the existing load-error path. The loaded catalog belongs to the game instance and is released on teardown. `ItemCategories.CreateDefault()` remains available for explicit programmatic fixtures; gameplay uses the loaded catalog.

Item definitions will later be authored in `.yml` files and loaded at startup. Their YAML schema, paths, and production loading integration remain deferred to a later story. Keep the domain model independent of serialization and use development item definitions for this epic; reuse the existing file-loading system when implementing item-definition loading.

For stackable categories, an omitted limit means `int.MaxValue`; explicit limits must be positive. Nonstackable categories always hold one item per entity/cell. Merge compatibility requires the same item definition, not just the same category. Adding a category with existing behavior must not require UI category switches. New gameplay behavior or equipment destinations may still require domain changes.

### Inventory and interaction

- Start with 128 empty storage slots. Use existing matching stack space and empty slots before growing capacity in 64-slot chunks: 128, 192, 256, etc. Large additions can require multiple chunks.
- Expanded capacity remains available for the game instance; automatic shrinking is deferred.
- Support both click-to-pick-up/click-to-place and conventional drag-and-drop through the same validated domain operations.
- Empty compatible destinations receive items. Other compatible occupied destinations swap, validating both resulting placements.
- Matching stackable items merge up to the destination limit, retaining overflow in the source. A full matching stack causes no change.
- Starting placement never removes an item from authoritative state. Invalid, cancelled, or stale requests leave state unchanged.
- Escape, outside drops, inventory closure, and focus loss cancel pending interactions. Preserve existing Escape/menu behavior while clearing transient item state.
- Item movement and dragging are allowed only while inventory is open, including equipment and hotbar editing. Reject commits after inventory closes.
- Inventory presentation is compact: visible occupied cells are consecutive, with no empty gaps between them. Genuine empty capacity cells follow occupied results. Filtering removes nonmatching results from the presentation instead of leaving holes or turning hidden occupied cells into empty cells. Every visible item retains its underlying identity/address; visual indices are never storage identities.

### Hotbar links

- Consumable links reference the item definition, aggregating all matching consumables in inventory. Moving, merging, or reordering stacks does not break the link. Zero quantity retains the assignment but makes it unavailable.
- Weapon links reference equipment slots. Replacement updates the link; emptying disables it; re-equipping restores availability. Inventory weapons cannot be assigned directly.
- Assigning a link never transfers or duplicates an item. Other item categories cannot be assigned as item hotbar links.
- Assignment, movement, rearrangement, and clearing are allowed only while inventory is open, even though it blocks gameplay input. Right-clicking a hotbar slot clears its link without removing the underlying item. Moving a link to an occupied hotbar slot swaps links; moving to an empty slot relocates it. Existing activation restrictions remain intact.
- Actual consumption, firing, effects, and combat behavior are deferred.

### Tooltips and corner quantities

- Occupied item icons show tooltips using content provided by `ItemSystem`: initially name, category, description, and applicable stack-limit information. Do not invent gameplay stats.
- Equipment labels remain visible when empty. Empty cells have no item tooltip; empty equipment cells may explain the slot and accepted category.
- Stackable item icons show the current amount as a small number in an icon corner, including quantity one. Nonstackable items show no quantity badge.
- Consumable hotbar icons show aggregate inventory quantity, including zero for retained unavailable links.
- Quantity labels and tooltip presentation must not intercept clicks or drops. Tooltips close when the source disappears or placement starts.
- Count arithmetic must respect stack limits and avoid overflow, including aggregate quantities across multiple large stacks.

## Godot editor checklist before implementation

Keep current scripts attached. Runtime behavior follows in the stories below.

1. In `scenes/ui/menu/game_overlay_menu/inventory_menu.tscn`, retain the inherited overlay, `MainLayout`, `CharacterSection`, `EquipmentContainer`, and inventory section.
2. Preview eight `icon_with_label.tscn` instances under `EquipmentContainer` using the agreed labels. Choose columns and spacing visually. These children are editor previews; runtime clears and regenerates them from domain slot definitions.
3. In `scenes/ui/base/item_grid.tscn`, preserve the `ItemGrid` scroll root and its structural child `SlotGrid`. Keep six columns initially. Runtime clears cells inside `SlotGrid`, not the grid itself. Do not manually create 128 preview cells.
4. In `scenes/ui/base/clickable_icon.tscn`, retain the Button root and script. Prepare an empty-slot visual and remove the default mock compass from item presentation. Existing consumers must still display explicitly supplied icons.
5. Add a child Label named `QuantityLabel`, anchored bottom-right, with a small readable font and Mouse Filter set to Ignore. Hide it initially. Allow large counts without increasing cell size; runtime controls its text and visibility.
6. In `scenes/ui/base/icon_with_label.tscn`, retain `Label` and `ClickableIcon`. Check that long equipment labels fit without covering neighboring cells.
7. In `scenes/ui/base/inventory_container.tscn`, retain `CategoryTabs`, `FilterToolbar`, and `ItemGrid`. In `filter_toolbar.tscn`, retain `LineEdit` and `SortSelector`. Tabs and sorting options will be populated by their story.
8. Mark script-referenced child nodes as scene-unique in their owning reusable scenes where appropriate. Preserve node names and scene ownership; implementation binds within those scene boundaries.
9. Preview supported viewport sizes: verify labels, slot spacing, vertical inventory scrolling, quantity readability, and existing multi-window placement.

Start with Godot's existing tooltip presentation; a custom tooltip scene is not required before implementation. Domain values such as allowed categories and inventory capacity are not authored as UI rules in the editor.

## Stories

### INV-01: Define item metadata and extensible categories

Dependency: none.

Acceptance criteria:
- `ItemSystem` owns the eight categories, item definitions, entity/stack identities, tooltip data, and stacking rules.
- Limits follow the agreed category eligibility, item-level configuration, and defaults.
- Separate stacks sharing a definition have distinct entity IDs.
- Adding another category with existing behavior needs no UI category branch.
- No category-specific gameplay effects are implemented.
- Definition construction supports future startup YAML loading without coupling domain types to the file format; production YAML loading is deferred.

### INV-02: Establish inventory state and automatic capacity growth

Dependency: INV-01.

Implementation: complete. See the implementation plan for details and validation.

Acceptance criteria:
- One inventory system per running game owns 128 initially empty slots.
- Additions use existing stacks and empty slots before growing in sufficient 64-slot chunks.
- Arithmetic respects limits without overflow, item loss, or duplication.
- State and expanded capacity survive menu reopening; a new game instance gets independent state.
- `GamePlayManager` initializes and tears down the inventory owner, independently of window lifetime.

### INV-03: Establish equipment state and atomic transfers

Dependencies: INV-01, INV-02.

Acceptance criteria:
- One equipment system per running game owns eight slots with the agreed labels and compatibility.
- Inventory/equipment moves and swaps validate every resulting location before committing.
- Rejected operations change neither system.
- UI controls contain no compatibility decisions.
- `GamePlayManager` owns equipment lifecycle. Cross-system transfers use validate/plan/commit coordination and notify observers only after consistent state is committed.

### INV-04: Prepare scenes and generate empty slots

Dependencies: INV-02, INV-03, editor checklist.

Implementation: complete. See the implementation plan for details, passing slot/lifecycle checks, and the existing architecture-smoke layout failures.

Acceptance criteria:
- Startup clears every preview child of `EquipmentContainer` and every preview cell inside `SlotGrid`, retaining structural nodes.
- Render eight labeled equipment slots and initially 128 empty inventory slots.
- Growth adds cells without resetting state or duplicating existing cells.
- Empty cells show no mock item texture. Views opened after initialization immediately render current state.
- Preserve the existing overlay/window system.

### INV-05: Render items, tooltips, and stack counts

Dependencies: INV-01, INV-04.

Acceptance criteria:
- Existing icon controls render supplied item visuals and tooltip content.
- Stackable items show a small bottom-right quantity, including one; other items show no badge.
- Emptying a cell clears stale item texture, tooltip, and quantity.
- Domain changes refresh presentation; tooltip/count overlays do not consume placement input.
- Development fixtures demonstrate all categories without introducing real gameplay effects.

### INV-06: Support click-to-pick-up and click-to-place

Dependencies: INV-02, INV-03, INV-05.

Acceptance criteria:
- Clicking an occupied cell starts placement; clicking a destination requests move, swap, or merge.
- Merge overflow stays in the source; swaps validate both resulting placements.
- Clicking the source again cancels. Invalid destinations give feedback without changing ownership.
- Escape, menu closure, and focus loss clear pending placement.
- Item moves are rejected while inventory is closed. Compact display positions resolve to actual source/destination addresses rather than visual indices.

### INV-07: Support native drag-and-drop

Dependency: INV-06.

Acceptance criteria:
- Inventory and equipment use native Godot dragging with the same domain commands as click placement.
- Hover feedback is read-only; commit revalidates current state.
- Outside drops, invalid destinations, cancellation, and source changes cannot lose or duplicate items.
- Completing a drag does not also trigger click placement.

### INV-08: Assign item hotbar links

Dependencies: INV-05, INV-06, INV-07.

Acceptance criteria:
- Both interaction modes assign consumables from inventory and weapons from occupied equipment slots.
- Reject inventory weapons and unsupported categories without changing assignments.
- Consumable links aggregate all matching stacks and retain unavailable zero-count assignments.
- Weapon links follow equipment-slot contents and availability.
- Hotbar item icons display tooltips and consumable aggregate counts through shared presentation.
- Right-click clears a link only while inventory is open. Links can move to empty slots or swap with occupied slots through both placement modes, only while inventory is open.
- Closing inventory cancels editing and invalidates pending placement; closed-inventory edit requests leave links and item state unchanged.
- Preserve existing HUD activation/input policy; no item effects execute in this story.

### INV-09: Add category tabs, search, and sorting

Dependencies: INV-04, INV-05; integrate placement before completion.

Acceptance criteria:
- Generate All plus one tab for each registered category, including Resource.
- Search item names case-insensitively. Initially support Name ascending/descending with a stable tie-breaker.
- Filtering, searching, and sorting compose as presentation operations without changing storage contents, identities, or quantities.
- Pack visible occupied results consecutively, with genuine empty capacity cells trailing them. There are no empty gaps between results, including after moves, merges, removal, filtering, and sorting.
- Every visible cell retains its real storage address. Hidden occupied slots never appear as empty drop targets; filtered items are omitted rather than replaced with blank cells.
- Presentation compaction does not mutate underlying inventory storage. Placement remains correct after every view change.

### INV-10: Verify lifecycle and integration

Dependencies: INV-01 through INV-09.

Acceptance criteria:
- Domain checks cover all category/slot combinations, reverse-swap validation, stack limits, merge overflow, and multi-chunk capacity growth.
- Hotbar checks cover aggregate quantities, zero-count retention, equipment replacement, invalid assignments, right-click clearing, link swaps, and rejection of edits while inventory is closed.
- UI checks cover both placement modes, cancellation, tooltip/count refresh, growth, filtering with placement, and reopening without duplicate cells.
- Lifecycle checks confirm one owner per game instance and detached view subscriptions on teardown.
- Existing window placement, focus, input blocking, and unrelated hotbar actions remain functional.
- Record only validation actually executed during implementation.

## Deferred scope and presentation defaults

Defer item gameplay effects, equipment bonuses, combat, cooldowns, persistence, networking, world drops, trading, manual stack splitting, production YAML definition loading/schema, and a general item scripting framework.

Defaults for otherwise unspecified presentation details: retain six inventory columns and existing hotbar capacity; place quantity labels bottom-right; use standard Godot tooltips and presentation-only name sorting. Capacity remains expanded during the game instance. These defaults do not add category-specific UI rules.
