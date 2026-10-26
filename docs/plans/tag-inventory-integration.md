# Epic: Tag-driven item classification and inventory views

Status: proposed (2026-10-26). Implementation has not started.

Predecessor: [Inventory UI implementation plan](inventory-ui-implementation-plan.md). Tag contract: [Gameplay Tag System](../documentation/TagSystem.md), especially sections 12.5 and 13.5.

## Goal

Integrate the existing TagSystem with ItemSystem, inventory presentation, equipment eligibility, and item hotbar eligibility. Replace the single-category classification constraint with independent tag dimensions. Present light and heavy weapons together in one Weapons tab while preserving their distinct equipment destinations.

This is an integration of classification and queries. InventorySystem and EquipmentSystem retain ownership of state and operations; TagSystem remains a shared classification/query service.

## Agreed classification convention

Whenever classification dimensions vary independently, represent them as separate branches. Use ancestry for genuine subtype relationships. This convention applies to future classification problems across domains.

```text
Light laser:
  Item.Weapon.Size.Light
  Item.Weapon.Technology.Laser

Heavy laser:
  Item.Weapon.Size.Heavy
  Item.Weapon.Technology.Laser
```

Both assignments expand to `Item.Weapon`. A broad weapon query selects both items. A laser query selects both; a light-size query selects only the first. An All query can require heavy size and laser technology together. Do not encode technology separately beneath each size.

Resource subtypes can use `Item.Resource.Ore.Iron`, `Item.Resource.Ore.Copper`, and `Item.Resource.Gas.Helium`. Additional independent properties belong in separate branches. These are classification examples, not a request to implement resource gameplay or laser combat.

Strict dot-path ancestry remains unchanged: each tag has one parent; an item can have multiple direct assignments. Required and exclusive dimensions are validated by consumers, not inferred from the hierarchy.

## Boundaries and target behavior

- Preserve definition IDs, entity IDs, quantities, real storage addresses, 128-cell initial capacity, 64-cell growth, and atomic transfer semantics.
- Item definitions own authored canonical tag references and explicit `CanStack` / optional `MaximumStackSize`. Resolve references once during initialization. Nonstackable definitions have limit one and reject explicit limits above one; stackable definitions default to `int.MaxValue`. Merges still require identical definition IDs.
- Remove mandatory `CategoryId` from the final item model. Inventory tabs own stable IDs, labels, order, optional icons, and query references; they do not own item behavior. Keep an All tab independent of configured tab matches. Overlapping tabs are permitted; an item appears once per active projection.
- Default tabs are Engine, Hull, Shield Generator, Reactor, Weapons, Consumable, and Resource. Weapons matches `Item.Weapon`. Keep eight equipment slots, including two light and two heavy weapon slots.
- Keep artwork and tooltip results deterministic. Item artwork overrides a separately configured item fallback icon reference, then the placeholder. Migrate existing category artwork to item fallback references rather than choosing artwork from the selected tab or whichever tag happens to match first. Preserve a separately authored item type label for tooltips where needed; tab membership must not determine that label.
- Equipment slot definitions reference compiled acceptance queries. Default light/heavy slot queries select their corresponding size tags; other slots select their equipment classifications. Preserve the existing one-entity/count-one constraint and validate swaps in both directions.
- Hotbar eligibility queries replace hardcoded category comparisons. Consumable links still aggregate by definition ID; weapon links still require an equipped source and resolve by stable equipment-slot key. Tags do not execute consumption or attacks.
- Build and own one registry per game instance through the existing startup lifecycle. Resolve all item/tab/slot/hotbar references against that same registry. Freeze before runtime consumers are published; no live registry reload.
- Keep definition classifications immutable to consumers. The existing mutable `TagSet` must remain privately owned behind read-only matching access, or gain an immutable representation. No mutable tag collection may escape through definitions or snapshots.
- Author tags and named queries in YAML using the existing FileLoadingSystem conventions. Register all declarations and queries before freeze; validate all consumer references before constructing state or binding views. Unknown tags, wrong domains, invalid queries, and invalid dimensions abort startup with source details.
- Use `ItemClassification` for these queries. Runtime matches use cached IDs/compiled queries, without per-cell string resolution, hierarchy traversal, or query compilation.

## Stories and acceptance criteria

### TINV-01 — Content contract and taxonomy

Dependency: none.

Define the initial item branches, named tab/slot/hotbar queries, and YAML contracts for declarations, item assignments, tab presentation, and eligibility policies. Specify required metadata and stable keys. Initial equipment branches may use `Item.Equipment.Engine`, `Item.Equipment.Hull`, `Item.Equipment.ShieldGenerator`, and `Item.Equipment.Reactor`; retain `Item.Consumable` and `Item.Resource` for broad classification.

Define validation for weapon size: every weapon requires exactly one supported size assignment, light or heavy. Technology is optional for current mock definitions; laser examples explicitly assign it. Reject contradictory light/heavy assignments. Do not require every item to match a tab or globally prohibit overlapping families without a concrete consumer constraint.

Acceptance: document valid light/heavy laser and resource examples, invalid size examples, and expected tab/slot/hotbar matches. Every query references explicitly declared tags, including required ancestors. Existing mock items can be represented without inventing technology.

### TINV-02 — Registry loading and game lifetime

Dependency: TINV-01.

Add YAML DTOs, schemas, configured packaged/user paths, and loading through FileLoadingSystem. Construct, validate, compile, and freeze the game-owned registry; then resolve consumer references before publishing catalogs or constructing inventory/equipment. Support teardown after partial initialization.

Acceptance: invalid declarations, unknown references, wrong domains, and malformed queries abort through the existing load-error path with file/schema context. Menu reopen reuses the game registry; a new game receives independent registry and consumer instances. No static tag cache or silent fallback bypasses invalid content.

### TINV-03 — Immutable item classification and stack policy

Dependency: TINV-02.

Replace mandatory category membership with resolved definition-level classification. Move stackability to definitions; retain stack-limit defaults and contradiction checks. Provide read-only matching access and independent item type label/fallback artwork metadata. Keep ItemSystem independent of equipment, inventory, UI, and Godot resources.

Acceptance: light/heavy lasers both match weapon and laser queries, but only their own size queries. Iron matches resource and ore queries. Several definitions may share all tags and still never merge. Repeated entities of a definition share immutable classification; callers cannot mutate it. Existing identity/count/limit invariants remain covered.

### TINV-04 — Query-based inventory tabs

Dependency: TINV-03.

Replace category-derived tabs with loaded tab definitions and compiled selectors. Update InventoryProjection and view binding. Track selection by stable tab key, adapting native UI indices at the boundary. Combine the active selector with existing name search and deterministic sort. Keep All available for items that match no configured tab.

Acceptance: one Weapons tab displays both light and heavy examples. Configuring an additional Laser or Ore tab requires content changes only. Overlapping matches never duplicate entries within a tab. Filtering preserves real storage addresses and genuine empty cells; hidden occupied cells never become placement targets. Selection/search/sort survive operations and menu reopen as before. Item artwork and tooltip labels do not change across tabs.

### TINV-05 — Equipment and hotbar query policies

Dependency: TINV-03; may be developed independently of TINV-04.

Resolve slot and hotbar selectors at initialization and route compatibility checks through the shared item matching API. Remove category-ID comparisons. Keep eligibility within authoritative validation paths used by hover, click, drag, commit, and hotbar assignment.

Acceptance: light lasers fit both light slots and no heavy slots; heavy lasers fit both heavy slots and no light slots. Technology does not affect size compatibility unless explicitly required by a future policy. Invalid reverse swaps remain atomic rejections. Weapon links still require equipped items; consumable links still aggregate matching definition stacks. Sessions, revisions, identity checks, and activation behavior remain intact. Cached hover matching preserves the existing allocation expectations.

### TINV-06 — Packaged content and existing user configuration migration

Dependencies: TINV-04, TINV-05.

Convert all 16 packaged mock definitions, their artwork defaults, tabs, and eligibility policies. Preserve item IDs and explicit PlayerMock seed quantities. Retire category catalogs and obsolete schema/model references once all consumers are converted.

Provide a one-time migration for the known legacy category/item YAML format. Read legacy item/category pairs, resolve each category's stacking/artwork/type-label defaults into explicit item metadata, map known categories to the new tags, and generate tab definitions with a combined Weapons tab. Preserve custom category display groups using deterministic declared classification tags and matching tab queries; detect name collisions. Validate the complete generated content before installation, retain backups, and report migration details. Reject unsupported or ambiguous conversions with source details and actionable guidance; do not guess or overwrite user changes. Treat interrupted/partial migrations as recoverable and ensure reruns do not duplicate or reinterpret converted content.

Acceptance: default migration preserves 16 definitions, 34 occupied seeded cells, eight empty equipment slots, and ten empty hotbar positions. Existing user content is preserved or explicitly diagnosed; reload-default behavior remains an explicit existing setting. Test custom categories, artwork, omitted/explicit limits, collisions, and interrupted migration. Fixtures include light and heavy lasers and several resource subtypes; new production example items are optional and must not silently change existing seed totals.

### TINV-07 — Integration regression and documentation

Dependencies: TINV-06.

Update affected unit/loading fixtures and scene smoke checks. Cover selectors, hierarchy expansion, independent dimensions, immutability, invalid content, migration, artwork, tab persistence, and independent game lifetimes. Retain inventory growth, merge/identity conservation, stale/cancelled interactions, atomic swap, and hotbar regressions. Run the solution build, full NUnit suite, relevant UI architecture/icon/inventory smoke checks, and rendered inventory checks at the existing resolutions. Inspect the combined Weapons tab and equipment destinations.

Update the predecessor epic/implementation plan and TagSystem documentation to describe implemented behavior and exact validation results. Keep historical INV completion records identifiable as the pre-integration implementation.

Acceptance: required checks pass, or unresolved failures are explicitly recorded without claiming completion. No category-ID gameplay comparisons remain. No mutable classification escapes or runtime query compilation is introduced.

## Out of scope

Actual laser attacks, consuming items, crafting, loot generation, rarity/stat systems, resource processing, instance-specific mutable tags, nested boolean query expressions, mod distribution, save-game/network migration, hot reload, and a new secondary-filter UI are outside this epic. Query-defined additional tabs demonstrate extensibility; a dedicated faceted filtering interface can follow separately.

## Completion criteria

Items support independent classifications, one default Weapons tab contains both sizes, equipment and hotbar eligibility use cached tag queries, and stack policy is independent of tabs. All content loads and validates before gameplay, legacy configuration is handled explicitly, and existing inventory/interaction guarantees remain verified. This epic does not merge state ownership into TagSystem.
