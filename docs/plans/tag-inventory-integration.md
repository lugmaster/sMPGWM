# Item tags and inventory filtering

Status: planned. Implementation has not started.

## Goal

Load multiple tags on item definitions, automatically include their category, and use the existing TagSystem to filter inventory entries with All / Any / None queries. Deliver this small foundation before extending classification into affixes, modifiers, skills, or runtime state.

Related references: [TagSystem](../documentation/TagSystem.md) and [inventory UI plan](inventory-ui-implementation-plan.md).

## Classification contract

Keep one required `CategoryId` and add an optional `Tags` list:

```yaml
Items:
  - Id: mock_advanced_hull
    DisplayName: Advanced Hull
    CategoryId: hull
    Tags:
      - light
      - titan
```

The effective public classification is `hull`, `light`, `titan`: the union of CategoryId and explicit Tags. Missing Tags or an empty list means category-only classification. Deduplicate repeated tags, including an explicit repeat of the category. Keys are case-sensitive. Reject null, blank, non-string, and unknown entries with file/item/field context.

Author each fact once. CategoryId is authoritative for category membership; its tag is derived automatically. Explicit tags provide additional classification. Future systems may derive tags from damage configuration or affixes, but those derivations are outside this implementation. Numeric values remain stats, and current conditions such as recharging remain instance state.

Category metadata retains its existing stacking and presentation responsibilities. Tags do not alter IDs, stack compatibility, limits, icons, tooltip labels, or item behavior.

## Implementation steps

### 1. Load Tags and automatically include CategoryId

Extend the item loading schema and loader to accept the optional list. Declare additional tag keys centrally through the existing FileLoadingSystem conventions so misspellings fail validation. Category IDs automatically contribute vocabulary entries and need no duplicate declarations. Follow existing packaged/user configuration loading rules.

Keep short public keys such as `hull`, `light`, `titan`, `weapon`, and `plasma`. Map them deterministically into canonical names in the existing ItemClassification domain, for example `hull` to `Item.hull`. Register required ancestors and validate mapped names and conflicting declarations. Canonical paths stay inside the adapter; public item content and selectors use short keys.

Do not infer semantics from category spelling. `light_weapon` automatically contributes `light_weapon`, not `weapon` or `light`. Where broad weapon searches are needed, explicitly assign those additional classifications.

Acceptance:

- The hull example exposes exactly the three public tags above; internal ancestor expansion does not add authored public keys.
- Existing item YAML without Tags remains valid.
- Invalid content fails with actionable diagnostics; typos never create tags implicitly.
- Existing category defaults and user-authored item data are preserved.

### 2. Resolve immutable definition classification using the existing TagSystem

Resolve tags once while constructing the item catalog. Expose read-only matching on ItemDefinition; keep mutable TagSet storage private or use an immutable representation. Instances of a definition share its classification.

Use one coherent registry context for the catalog and its queries, and freeze the vocabulary before publishing definitions. Integrate with the current ItemRegistry loading lifetime; a general conversion of the static registry to per-game services is outside scope. Tests with independent catalogs must use matching registry contexts and must not leak state between them.

Acceptance:

- Consumers cannot mutate definition classifications.
- Duplicate assignments are harmless.
- No per-item-instance copy of definition tags is required.
- Loading failures do not publish partially resolved definitions.
- No hierarchy traversal or string resolution occurs during repeated matching.

### 3. Expose All / Any / None matching

Provide a small structured-query adapter over the existing compiled-query machinery:

- All: every listed tag must be present.
- Any: at least one listed tag must be present when this list is nonempty.
- None: every listed tag must be absent.

Combine the three conditions with AND. Empty lists impose no restriction. Preserve the existing explicit `MatchAll: true` requirement whenever both All and Any are empty, including negative-only queries. Reject unknown selector keys, including those in None.

Examples below are separate query documents:

```yaml
# Light hulls
All: [hull, light]
---
# Hulls that are neither light nor titan
All: [hull]
None: [light, titan]
---
# Hulls that are light or titan, or both
All: [hull]
Any: [light, titan]
---
# Plasma weapons
All: [weapon, plasma]
```

The agreed exclusion means `hull AND NOT (light OR titan)`.

Compile the initial configured/programmatic selectors during catalog initialization using the existing builder, then pass compiled selectors to consumers. Expose matching without introducing a second query engine. Supporting arbitrary new queries after registry freeze, external named-query YAML catalogs, text parsing, and nested expressions is deferred.

Acceptance: query semantics match the examples; queries and classifications use the same registry context; repeated candidate matching performs no query compilation or string lookup.

### 4. Integrate one real consumer: inventory filtering

Add an optional compiled tag selector to the inventory projection/filtering path. Combine it with existing category selection, name search, and deterministic sorting. A caller can set or clear the selector; no selector preserves existing behavior. Use the existing All category view when searching across categories.

Keep current category tabs. A new filter editor, search syntax, configurable tabs, and a combined Weapons tab are not required. This step delivers a working programmatic inventory filter, exercised through the real projection path.

Responsibilities remain explicit:

- ItemRegistry loads definitions and their resolved classification.
- Inventory projection selects matching entries from the inventory snapshot.
- InventoryController continues owning the inventory container/view lifecycle.
- ItemController continues owning item identity, movement, and transactions.

Filtering preserves entity identity and real storage addresses. Hidden occupied cells never become empty placement targets. Multiple instances of the same definition remain separate entries. Clearing a filter restores the normal projection. Filtering does not change item artwork or tooltip labels.

Acceptance: apply and clear the example selectors through the inventory projection; verify composition with name/category filters and sorting, including refresh after inventory changes. Existing movement and selection behavior remains valid.

### 5. Test the hull and plasma-weapon examples

Use focused loader, matching, and inventory projection tests.

| Effective public tags | Hull AND light | Hull AND neither light nor titan | Hull AND (light OR titan) |
| --- | --- | --- | --- |
| hull | false | true | false |
| hull, light | true | false | true |
| hull, titan | false | false | true |
| hull, light, titan | true | false | true |
| armor, light | false | false | false |

For plasma searches, include a weapon tagged plasma, a weapon without plasma, and a non-weapon tagged plasma. Only the first matches All: [weapon, plasma]. Include weapons from both existing size categories to prove that explicit weapon classification works across categories.

For these fixtures, plasma denotes plasma damage classification; it does not implement combat. When damage configuration becomes authoritative later, derive this classification from that configuration instead of maintaining the fact twice.

Cover missing/empty Tags, duplicate/category-repeat tags, invalid entries, unknown selectors, empty and negative-only selectors, and immutable access. Test inventory filtering against actual entries, including repeated definitions and hidden occupied slots.

Preserve the authored light/titan hull example. Keep production seed quantities unchanged; plasma examples can remain test fixtures. Run the solution build, relevant NUnit suites, and applicable inventory smoke checks during implementation. Record actual results then; this planning update does not claim implementation validation.

## Deferred work

Equipment and hotbar eligibility migration, equipped-item search, catalog search services, universal item/skill search, runtime-state tags, affixes, modifiers, numeric stats, damage-derived tags, dynamic query compilation, player-facing query controls, tab redesign, category removal, stacking redesign, hot reload, and registry lifetime redesign are separate follow-up work.

## Completion criteria

Item tag lists load and validate, categories automatically contribute tags, definitions expose immutable classification, All / Any / None use the existing TagSystem, and inventory filtering demonstrates the hull and plasma-weapon cases without changing inventory state or existing item behavior.
