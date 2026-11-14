# Unified catalogue loading refactor

Status: planned; implementation requires a separate request.  
Scope: gameplay definitions, item categories, items, and ships.  
Primary reference: [Catalogue.cs](../../sMPGWM.Game/Scripts/GameplayDataSystem/Catalogue.cs).

## Outcome and agreed architecture

Use the existing gameplay catalogue pipeline for all production definition loading. Keep `Catalogue<T>` as a loading specification containing source metadata and a type-specific row parser. It must not become a container of loaded definitions, a registry, or a mutable initialization object.

```text
Catalogue<TDefinition>
    | supplies source information and a row parser
    v
GameplayCatalogueLoader
    | reads YAML through FileLoader, which validates its schema
    v
CatalogueParser.Parse<TDefinition>()
    | constructs typed definitions and checks duplicate identities
    v
TDefinition[]
    | passed to the specific registry constructor
    v
SpecificRegistry
    | stores definitions and provides lookup by ID
    v
GameplayCatalogues
    | validates cross-catalogue references and prepares plans
    v
Publish the completed dataset for gameplay
```

The shared pipeline owns loading mechanics. Type-specific parsers own construction of their runtime types. Registries receive typed definitions, not YAML, JSON tokens, file paths, or schema readers.

Schema and conversion validity are established before registry construction. Complete domain validity, cross-catalogue consistency, and preparation are established before publication. Constructing a registry internally does not make it available to gameplay. This entire process is definition initialization, including item initialization.

## Current implementation

| Component | Current responsibility | Required direction |
| --- | --- | --- |
| `Catalogue<T>` | A `CatalogueSource` and `Func<JToken, T>` | Retain this role. |
| `CatalogueSource` | Filename, schema name, root collection key | Describe identity selection and resolve the correct user/default/schema paths. |
| `FileLoader` | Prepare user files, read files, validate YAML, capture checksum inputs | Remain the common production file boundary. |
| `CatalogueParser` | Parse a root array, require `Id`, check duplicates, report row errors | Support source-specific identity fields and common typed parsing for every catalogue. |
| `GameplayCatalogueLoader` | Assemble nine gameplay registries | Also assemble item categories, items, and ships through the same pipeline. |
| `ItemDefinitionParser` | Parse categories and items, resolve defaults/icons, choose typed subtypes | Retain domain conversion; move document iteration into the common parser. |
| `ItemDefinitionDocuments` | Merge eleven item documents as JSON and check versions | Replace the production JSON merge with typed assembly retaining source provenance. |
| `ItemRegistry` | Independently lazy-load categories/items and precompile item values | Retain lookup APIs, backed by the shared prepared dataset. |
| `ShipDefinitionLoader` / `ShipRegistry.Build` | Deserialize a document, validate assets/geometry, construct `ShipData` | Adapt row conversion to `Catalogue<ShipData>`; remove the separate production loading route. |
| `GameplayCatalogues` | Own gameplay registries, validate their relationships, prepare abilities | Own the complete collection of registries and its final preparation. |
| `GameplayDefinitions` / `GameConfig.EnsureCompatibilityData` | Coordinate multiple lazy caches, validation, checksums, and retry resets | Publish one completed definition dataset and preserve compatibility behavior. |

Relevant sources are under `sMPGWM.Game/Scripts/GameplayDataSystem`, `FileLoadingSystem`, `ItemSystem`, `ShipSystem`, and `GameConfigSystem`.

## Contracts to preserve

- Preserve existing YAML structure, filenames, IDs, values, versions, item order, and lookup behavior. This is a loading refactor, not a schema-format migration.
- Preserve all recent schema and loading fixes: required nonblank descriptions for items, categories, and ships; identifier and numeric bounds; icon syntax; integer bounds; lifetime constraints; and strict identifier/version patterns.
- Preserve typed item subclasses and `WeaponSupplyDefinition` variants. Keep category defaults, stack semantics, icon inheritance, and explicit nullable fields where the schema permits them.
- Preserve ship texture checks, polygon validation, float conversion rules, descriptions, health, mass, and stat modifiers.
- Preserve cross-references, dependency-cycle checks, modifier-sum checks, equipment-combination validation, and starter-loadout validation.
- Keep supported ability plan preparation and explicit unsupported-content diagnostics. Do not implement deferred abilities or reject all intentionally unsupported bundled content.
- Keep source input capture and checksum behavior. A refactor alone must not change the compatibility hash for identical input content and protocol metadata.
- Preserve user-file selection and `ReloadConfigOnStart` behavior. Invalid user data must fail, not silently fall back to defaults.
- Preserve deterministic order and ordinal identity comparison. Check item identity uniqueness across all eleven files.
- Keep runtime checks for network input, restored state, dynamic stat changes, and custom equipment layouts. These are separate from validation of static definition fields.

The working tree already contains schema, description, validator, and test changes from preceding work. Treat those changes as the baseline; do not revert or recreate them from an older commit. The last full run before this plan passed 525 tests, including the Godot integration runner. Recheck the current baseline before implementation.

## Loading specifications and source metadata

Use one `Catalogue<T>` specification per source document. The eleven item documents share the runtime base type `ItemDefinition` and feed one item registry. A shared multi-source assembly operation may collect their typed arrays, but there must be only one file-validation and row-conversion pipeline.

Extend source metadata with an identity field, defaulting to `Id`; ships use `name`. Keep root keys explicit: gameplay keys vary, item categories use `Categories`, items use `Items`, and ships use `ships`.

Resolve full source paths centrally from explicit metadata rather than applying the gameplay directory to every source:

| Definitions | User location | Packaged location |
| --- | --- | --- |
| Gameplay | `user://config/gameplay/<file>` | `res://assets/game_data/<file>` |
| Item categories and items | `user://config/items/<file>` | `res://assets/game_data/items/<file>` |
| Ships | `user://config/ships/Ships.yml` | `res://assets/game_data/Ships.yml` |

Schemas remain packaged resources at their existing paths. Preserve the existing capture key convention as well as the captured text. Keep the actual item file and schema attached to conversion errors instead of reporting the nonexistent aggregate `ItemDefinitions.yml` as the source.

Prefer existing delegates and explicit dependencies. Category lookup and icon/texture resolution can be supplied when creating a parser delegate. Do not introduce a general service locator, reflection-based type discovery, or an additional public `class` parameter; the generic type and parser already specify construction.

## Initialization and publication order

1. Create a private preparation attempt with its source-input capture and temporary definitions. None of its partially built registries are publicly accessible.
2. Load and parse item categories through `Catalogue<ItemCategoryDefinition>`, then construct their registry. Pass this local registry or an equivalent read-only lookup into item row conversion.
3. Load each item document through `Catalogue<ItemDefinition>`. Validate each against its own category schema, preserve the existing file order, require matching item-document versions, and combine typed results. Check duplicates across source files before constructing the item registry.
4. Load ships through `Catalogue<ShipData>`. A ship row parser may use an internal DTO as an implementation detail, but the common pipeline must produce `ShipData[]` before constructing the ship registry. Copy owned data so later DTO changes cannot alter registered definitions.
5. Load the existing nine gameplay catalogues using the same mechanics and construct their specific registries. Keep cross-catalogue references as typed definition fields during conversion; do not require global registry access from row parsers.
6. Assemble `GameplayCatalogues` with all local registries. Validate gameplay references and cycles, item references, ship/equipment combinations, and starter loadout requirements. Precompile item values, prepare ability plans, and produce support diagnostics before publication. Preserve meaningful diagnostic order where tests or callers depend on it.
7. Finalize compatibility inputs and publish only the successfully prepared dataset. Read-only lookup facades then expose that dataset. On failure, discard the attempt and its partial capture; a repaired input must support a clean retry under the existing preparation/reset contract.

Category-dependent construction is an explicit dependency: item categories must be available locally before items are converted. This does not require publishing categories early or changing the agreed pipeline.

### Avoid recursive loading

If the existing static `ItemRegistry` and `ShipRegistry` APIs delegate to `GameplayDefinitions.Current`, initialization must never call those facades while constructing `Current`. Otherwise `Lazy<T>` will reenter its own factory.

Pass staged registry instances or typed collections explicitly into item conversion, `ValidateItems`, equipment validation, starter validation, and support diagnostics. Change `StarterLoadout.ValidateDefinitions` to consume prepared data rather than initiating static lookups during preparation. Keep runtime starter-equipping behavior unchanged.

Use a single owner for prepared definitions and reset/retry. Remove independent item/ship loading caches once their facades delegate to that owner. Ensure startup callers do not expose a completed checksum for a failed definition attempt.

### What sealing means

Sealing is the completion of initialization and publication of a stable dataset. It does not require a public mutable `Seal()` method on every registry.

The generic registry owns identity indexing and read-only enumeration. Specialized registries own their prepared indexes or plans. Prevent registration after publication and prevent callers from mutating backing arrays or dictionaries. Inspect mutable nested definition data, especially `ShipData.StatModifiers`, and use immutable published values or owned snapshots. Runtime ship state must remain separate from authored definitions.

Only remove redundant required-field checks from consumers when all their definition inputs come from this validated boundary. Retain appropriate construction validation where public/manual construction remains supported. Tests that intentionally bypass file loading must not create an unchecked production loading route.

## Implementation sequence

### 1 Establish the baseline

- Read `AGENTS.md` and applicable refactoring/testing skills before semantic edits.
- Inspect current working changes and run the existing test command.
- Record shipped definition IDs, ordering, representative values, current input-capture keys, and registry lookup behavior for comparison.
- Inventory every production entry point and direct parser/build caller. Include compatibility preparation, startup equipment, tests, and UI consumers.

### 2 Generalize the common parser and loader

- Add source-specific identity selection and correct path resolution without changing existing gameplay behavior.
- Keep file reading/schema validation in the production reader; make the validated-input contract explicit for internal test readers.
- Centralize typed row iteration, source-aware conversion errors, and duplicate checks. Preserve source provenance when combining documents.
- Handle expected conversion failures consistently, including format, cast, overflow, asset, and semantic errors. Avoid extracting filenames by searching exception message text.
- Keep structural validation at the loading boundary; do not add recurring validation to gameplay loops.

### 3 Migrate item categories and items

- Extract row construction from `ItemDefinitionParser` while preserving its category resolution, subtype selection, supply variants, stacking, and icon behavior.
- Declare the category source and the eleven item sources as catalogue specifications.
- Replace `ItemDefinitionDocuments.Load` in production with typed multi-source assembly and version/identity checks.
- Construct item registries from typed arrays. Move item precompilation into the shared initialization lifecycle.
- Preserve static lookup APIs through delegation where practical; add instance registry types only as needed by `GameplayCatalogues`.
- Retain the aggregate `items/ItemSchema.yml` as an authoring/test schema if still useful; it is not a required second production validation pass.

### 4 Migrate ships

- Introduce a row parser producing `ShipData` from validated ship rows with explicit asset dependencies.
- Reuse geometry and resource validation without preserving a separate file-loading pipeline.
- Construct the ship registry from `ShipData[]`, with `name` mapped to its runtime identity.
- Remove obsolete document wrappers/loaders only after checking all callers. Keep useful DTOs local to conversion rather than in registry APIs.

### 5 Unify final preparation

- Extend `GameplayCatalogues` to own category, item, and ship registries alongside gameplay registries.
- Move the distributed cross-catalogue validation and preparation into one private build operation in the agreed order.
- Adapt `GameplayDefinitions`, lookup facades, `GameConfig.EnsureCompatibilityData`, and starter validation to use the single prepared dataset without lazy recursion.
- Preserve retry and checksum semantics; eliminate partial-cache visibility.

### 6 Remove superseded paths and verify

- Remove unused independent lazy factories, JSON merge code, obsolete path constants, and adapters that would permit an alternate production loading path.
- Update direct-loader tests to exercise the common pipeline or focused domain factories. Preserve tests for supported manual construction where needed.
- Keep useful context-specific errors and inspect the final diff for unrelated changes.
- Record the completed verification and any remaining limitations in this plan when implementation is performed.

## Verification and acceptance

The refactor is complete when all the following hold:

- Every active definition document is described by a `Catalogue<T>` and loaded through the same schema-validation and row-parsing pipeline.
- Gameplay, item categories, items, and ships become typed arrays before entering their specific registries. Registries perform no file I/O or YAML/JSON deserialization.
- Missing, null, empty, whitespace-only, wrong-type, out-of-range, and unknown-field inputs fail at the appropriate loading boundary. Preserve the existing description and numeric regressions.
- Duplicate IDs within one document and across item documents fail with the relevant source and identity. Duplicate ship names use the same common mechanism.
- Item version mismatches, invalid category references, missing assets, invalid polygons, invalid gameplay references, dependency cycles, and invalid static numeric combinations fail before public access.
- Errors identify the actual file, schema, definition, and field where available; item errors do not point to an invented merged source.
- Registry lookups return the correct typed definitions with unchanged IDs, ordering, values, defaults, and descriptions. Item instances, equipment, tooltips, and ship creation retain behavior.
- Unsupported abilities remain unavailable with diagnostics, while malformed definition data fails loading. Prepared plans remain owned by the prepared dataset and reusable by independent sessions.
- A failure after several registries have been constructed exposes none of those partial results. Fixing the input allows a clean retry. Repeated successful access does not reload files or rebuild plans.
- Preparation works when first reached through any retained static lookup facade, without recursion. All supported startup paths pass the same preparation boundary.
- Identical file content and protocol metadata produce the same compatibility checksum. Failed attempts leave no partial input capture or success marker.
- Published collections and nested definition data cannot be mutated to invalidate registry guarantees. Dynamic runtime and network validation remains intact.
- No YAML parsing, schema validation, initialization allocations, or plan preparation enter the 60 FPS frame loop or 30 Hz network tick.

Run from the repository root:

```powershell
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --no-restore --verbosity minimal
git diff --check
```

The test project runs its Godot-tagged cases through `GodotIntegrationTests`. Preserve that integration coverage, especially preparation failure/retry, item creation and equipment, and ship creation. Relevant existing tests include `DefinitionPreparationTests`, `CatalogueDiagnosticsTests`, `DefinitionVersionTests`, `ItemRegistryTests`, `ItemDefinitionSchemaTests`, `ItemDescriptionTests`, `ShipDefinitionTests`, `ShipRegistryTests`, `EquipmentDefinitionValidationTests`, and ability preparation/architecture tests.

The JSON Schema library has a validation quota. Reuse already-validated unchanged test fixtures and avoid repeated tests of identical schema branches. Keep all mutated data validation intact and do not treat quota errors as expected schema rejection.

Follow the repository's test-support markers for any new test-only production hooks. Prefer testing real production loading components with injected file/asset access over introducing test-specific alternate loaders.

## Excluded

New gameplay features, unsupported ability implementation, weapon execution, new networking payloads, schema-format redesign, YAML field renaming, mod discovery, hot reload, a general dependency-injection framework, and UI redesign are outside this refactor. The passive design documents are not active catalogues and must not be wired into runtime loading as part of this work.
