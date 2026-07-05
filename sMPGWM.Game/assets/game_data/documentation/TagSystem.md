# PRD: Gameplay Tag System

**Version:** 1.0.0  
**Target Engine:** Godot 4.x / C#  
**Primary Consumers:** RPG State Engine, Action System, Targeting System, Item System, Effect System, Cooldown System, Damage System, Faction/Relation Systems  
**Runtime Model:** Deterministic, low-memory, low-allocation, data-driven  
**Authoring Model:** Hierarchical tag names, domain-safe assignments, flat compiled runtime representation  
**Registry Lifetime:** Load/import → canonicalize → validate → compile → freeze  
**Runtime Rule:** No runtime tag-definition creation during deterministic simulation  
**Modding Note:** Future-compatible with content packs and mod-provided definitions before registry freeze. Full mod loading, workshop distribution, and live hot-reload are out of scope.

---

## Table of Contents

- [1 Goal](#1-goal)
- [2 Scope](#2-scope)
    - [2.1 In Scope](#21-in-scope)
    - [2.2 Out of Scope](#22-out-of-scope)
- [3 Design Summary](#3-design-summary)
- [4 Core Rules](#4-core-rules)
- [5 Core Concepts](#5-core-concepts)
    - [5.1 Gameplay Tag](#51-gameplay-tag)
    - [5.2 Gameplay Tag Domain](#52-gameplay-tag-domain)
    - [5.3 Root Prefix](#53-root-prefix)
    - [5.4 GameplayTagId](#54-gameplaytagid)
    - [5.5 DomainLocalIndex](#55-domainlocalindex)
    - [5.6 Tag Definition vs Tag Assignment](#56-tag-definition-vs-tag-assignment)
    - [5.7 Primary Registry IDs](#57-primary-registry-ids)
- [6 Domain Model](#6-domain-model)
    - [6.1 Registered Domains](#61-registered-domains)
    - [6.2 Initial Domains](#62-initial-domains)
    - [6.3 Domain-Specific Assignment Fields](#63-domain-specific-assignment-fields)
    - [6.4 Domain Safety](#64-domain-safety)
- [7 Authoring Model](#7-authoring-model)
    - [7.1 Canonical Names](#71-canonical-names)
    - [7.2 Strict Dot-Path Hierarchy](#72-strict-dot-path-hierarchy)
    - [7.3 Assignability](#73-assignability)
    - [7.4 Fully Qualified Names and Short Names](#74-fully-qualified-names-and-short-names)
    - [7.5 Aliases and Migration](#75-aliases-and-migration)
    - [7.6 Semantic Links](#76-semantic-links)
- [8 Registry Lifecycle](#8-registry-lifecycle)
    - [8.1 Content Source Loading](#81-content-source-loading)
    - [8.2 Canonicalization](#82-canonicalization)
    - [8.3 Validation](#83-validation)
    - [8.4 Compilation](#84-compilation)
    - [8.5 Domain Mask Size Assignment](#85-domain-mask-size-assignment)
    - [8.6 Startup Diagnostics](#86-startup-diagnostics)
    - [8.7 Freeze](#87-freeze)
    - [8.8 Registry Hash](#88-registry-hash)
- [9 Runtime Representation](#9-runtime-representation)
    - [9.1 Flat Runtime Structure](#91-flat-runtime-structure)
    - [9.2 GameplayTagSet](#92-gameplaytagset)
    - [9.3 DirectMask and ExpandedMask](#93-directmask-and-expandedmask)
    - [9.4 Dynamic Dense Domain Masks](#94-dynamic-dense-domain-masks)
    - [9.5 Runtime Immutability](#95-runtime-immutability)
- [10 Query System](#10-query-system)
    - [10.1 Query Operators](#101-query-operators)
    - [10.2 Query Validation](#102-query-validation)
    - [10.3 Query Match Modes](#103-query-match-modes)
    - [10.4 Parent Expansion](#104-parent-expansion)
    - [10.5 Precompiled Runtime Queries](#105-precompiled-runtime-queries)
    - [10.6 Dynamic Queries and Tools](#106-dynamic-queries-and-tools)
- [11 Public API](#11-public-api)
    - [11.1 API Principles](#111-api-principles)
    - [11.2 Startup Builder API](#112-startup-builder-api)
    - [11.3 Immutable Registry API](#113-immutable-registry-api)
    - [11.4 Runtime Tag Set API](#114-runtime-tag-set-api)
    - [11.5 Query API](#115-query-api)
    - [11.6 Import and Validation API](#116-import-and-validation-api)
    - [11.7 Error Handling Policy](#117-error-handling-policy)
- [12 Runtime Assignment Rules](#12-runtime-assignment-rules)
    - [12.1 Context-Free Assignment](#121-context-free-assignment)
    - [12.2 Entity Conditions](#122-entity-conditions)
    - [12.3 Effect Classifications](#123-effect-classifications)
    - [12.4 Action Classifications](#124-action-classifications)
    - [12.5 Item Classifications](#125-item-classifications)
    - [12.6 Target Classifications](#126-target-classifications)
    - [12.7 Cooldown Groups](#127-cooldown-groups)
    - [12.8 Damage Kinds](#128-damage-kinds)
    - [12.9 Faction Relations](#129-faction-relations)
- [13 Integration Rules](#13-integration-rules)
    - [13.1 State Engine Integration](#131-state-engine-integration)
    - [13.2 Action System Integration](#132-action-system-integration)
    - [13.3 Targeting System Integration](#133-targeting-system-integration)
    - [13.4 Cooldown System Integration](#134-cooldown-system-integration)
    - [13.5 Item System Integration](#135-item-system-integration)
- [14 Serialization and Networking](#14-serialization-and-networking)
- [15 Performance Requirements](#15-performance-requirements)
- [16 Validation Requirements](#16-validation-requirements)
- [17 Example Authoring Data](#17-example-authoring-data)
    - [17.1 Tag Registry Example](#171-tag-registry-example)
    - [17.2 Query Registry Example](#172-query-registry-example)
    - [17.3 EffectDefinition Usage Example](#173-effectdefinition-usage-example)
    - [17.4 ActionDefinition Usage Example](#174-actiondefinition-usage-example)
- [18 Suggested Runtime Types](#18-suggested-runtime-types)
- [19 Testing and Acceptance Criteria](#19-testing-and-acceptance-criteria)
- [20 Open Future Extensions](#20-open-future-extensions)

---

## 1 Goal

Build a shared **Gameplay Tag System** for deterministic RPG gameplay metadata.

The system provides reusable infrastructure for:

- tag identity,
- domain registration,
- canonical hierarchical names,
- domain-local runtime masks,
- direct and expanded tag assignment,
- tag queries,
- validation,
- deterministic runtime IDs,
- registry hashing,
- serialization compatibility.

The system must support use by multiple gameplay systems, including:

- entity conditions,
- effect classifications,
- action classifications,
- item classifications,
- target classifications,
- cooldown groups,
- damage kinds,
- faction or relation metadata,
- future gameplay systems.

The tag system must remain **context-free**. It does not know what an entity, effect, action, item, target, cooldown, damage formula, faction, or gameplay rule is. It only provides loaded tag definitions, mutable domain-bound tag sets, compiled queries, and fast match/compare operations.

---

## 2 Scope

### 2.1 In Scope

This PRD covers:

- gameplay tag identity,
- runtime-registered tag domains,
- globally unique root prefixes,
- hierarchical canonical tag names,
- strict dot-path hierarchy,
- flat runtime representation,
- registry-wide deterministic `GameplayTagId` assignment,
- domain-local dense bit indexes,
- dynamic per-domain mask sizing during compilation,
- direct and expanded masks,
- startup/import-only aliases,
- domain-safe assignment fields,
- tag-set mutation operations,
- simple `All` / `Any` / `None` tag queries,
- precompiled query registry,
- registry freeze,
- registry hash,
- deterministic serialization compatibility,
- public API shape,
- validation and testing requirements.

### 2.2 Out of Scope

The following are out of scope:

- full mod-loading pipeline,
- Steam Workshop integration,
- editor UI implementation,
- visual scripting implementation,
- ability execution logic,
- target acquisition implementation,
- inventory implementation,
- cooldown timer implementation,
- combat damage formula implementation,
- faction/reputation calculation implementation,
- condition contributor ownership,
- effect lifetime ownership,
- live runtime creation of new tag definitions during deterministic simulation,
- runtime hierarchy changes,
- runtime domain registration,

---

## 3 Design Summary

Recommended architecture:

```text
Startup / import:
  GameplayTagRegistryBuilder
  RegisterDomain
  RegisterTag
  RegisterAlias
  RegisterQuery
  CompileAndFreeze

Frozen runtime:
  GameplayTagRegistry
  GameplayTagId
  GameplayTagDomainId
  TagQueryId
  GameplayTagSet
  CompiledTagQuery
  GameplayTagRegistryHash
```

Tags are authored as readable hierarchical names:

```text
Condition.Poisoned
Effect.Damage.Poison
Effect.Damage.Poison.Dot
Action.Skill.Offensive
Cooldown.Weapon.Light
Damage.Thermal
Faction.Hostile
```

At runtime, tags are represented through stable numeric IDs and flat domain-local masks:

```text
Effect.Damage.Poison.Dot
  GameplayTagId:      registry-wide ushort
  Domain:             EffectClassification
  DomainLocalIndex:   dense bit index inside EffectClassification
```

`GameplayTagId` is a registry-wide tag identity. `DomainLocalIndex` is a bit index used only by compiled domain-local masks.

Runtime simulation must not compare strings, allocate tag collections, parse YAML, construct dynamic queries, or perform hierarchy traversal in the hot path.

---

## 4 Core Rules

The system must follow these rules:

1. A tag is identity metadata only.
2. A tag does not own gameplay behavior.
3. A tag definition is immutable after registry freeze.
4. A tag assignment may change only through the owning gameplay system.
5. The tag system does not know gameplay context.
6. Domains are registered definitions, not hardcoded enum-only concepts.
7. Each domain has one globally unique root prefix.
8. Every canonical tag belongs to exactly one domain.
9. Canonical names use strict dot-path hierarchy.
10. Parent expansion is compiled during startup, not evaluated dynamically at runtime.
11. Runtime tag sets store both `DirectMask` and `ExpandedMask`.
12. Queries match `ExpandedMask` by default.
13. Direct/exact matching is available through `MatchMode: Direct`.
14. Runtime masks are domain-local dense bitsets sized by the registry during compilation.
15. No public fixed-size type such as `TagMask64`, `TagMask128`, or `TagMask256` is required by consumers.
16. `GameplayTagId` is not a bit index.
17. Aliases are import/migration-only and do not exist at runtime.
18. Runtime systems must use cached IDs, tag sets, and compiled queries in hot paths.
19. Peers must use the same `GameplayTagRegistryHash` for deterministic simulation.
20. A content set cannot introduce unknown tags after registry freeze.

---

## 5 Core Concepts

### 5.1 Gameplay Tag

A `GameplayTag` is a named gameplay metadata label.

Examples:

```text
Condition.Dead
Condition.Poisoned
Effect.Damage.Poison
Effect.Buff.ShieldCapacity
Action.Skill.Offensive
Item.Weapon.Laser
Target.LivingEntity
Cooldown.Weapon.Light
Damage.Thermal
Faction.Hostile
```

A tag does not:

- mutate entities,
- apply damage,
- heal,
- start cooldowns,
- select targets,
- validate actions,
- own effect lifetime,
- own condition contributors,
- own cooldown remaining time,
- own inventory state,
- own faction state,
- play UI, audio, animation, or VFX.

A tag is identity metadata only. Gameplay meaning belongs to the consuming system.

### 5.2 Gameplay Tag Domain

A `GameplayTagDomain` defines the semantic area in which a tag may be used.

Examples:

```text
EntityCondition
EffectClassification
ActionClassification
ItemClassification
TargetClassification
CooldownGroup
DamageKind
FactionRelation
```

Domains prevent ambiguous interpretation.

Example:

```text
Condition.Poisoned
Effect.Damage.Poison
Action.PoisonCloud
```

These names may all relate to poison, but they are not interchangeable.

### 5.3 Root Prefix

A root prefix is the first canonical-name segment owned by a domain.

Examples:

| DomainKey | RootPrefix | Example Tag |
|---|---|---|
| `EntityCondition` | `Condition` | `Condition.Poisoned` |
| `EffectClassification` | `Effect` | `Effect.Damage.Poison` |
| `ActionClassification` | `Action` | `Action.Skill.Offensive` |
| `ItemClassification` | `Item` | `Item.Weapon.Laser` |
| `TargetClassification` | `Target` | `Target.LivingEntity` |
| `CooldownGroup` | `Cooldown` | `Cooldown.Weapon.Light` |
| `DamageKind` | `Damage` | `Damage.Thermal` |
| `FactionRelation` | `Faction` | `Faction.Hostile` |

Root prefixes must be globally unique.

Invalid:

```csharp
RegisterDomain("EffectClassification", rootPrefix: "Effect");
RegisterDomain("EffectRemovalCategory", rootPrefix: "Effect");
```

A canonical tag name must resolve to exactly one registered domain.

### 5.4 GameplayTagId

`GameplayTagId` is a registry-wide stable numeric identifier for one canonical tag definition inside one compiled `GameplayTagRegistry`.

Required type:

```text
GameplayTagId: ushort
Maximum tag IDs: 65,535
Reserved invalid ID: 0 or explicit Invalid value, depending on implementation policy
```

`GameplayTagId` scope:

```text
Scope: one compiled GameplayTagRegistry
Identifies: one canonical tag definition
Not a bit index
Not domain-local
Not a gameplay object ID
```

Example:

```text
Canonical tag:       Effect.Damage.Poison
Domain:              EffectClassification
GameplayTagId:        0351
DomainLocalIndex:     27
```

`GameplayTagId` is used for:

- registry lookup,
- serialization,
- debug name lookup,
- generated constants,
- editor references,
- save/replay/network metadata,
- resolving tag definitions.

`GameplayTagId` values are deterministic and stable only for the same resolved content set and the same `GameplayTagRegistryHash`, unless a future stable ID manifest is introduced.

### 5.5 DomainLocalIndex

`DomainLocalIndex` is the dense zero-based bit index assigned to a tag inside its domain during registry compilation.

Example:

```text
Effect.Damage.Poison
  GameplayTagId:      0351
  DomainLocalIndex:   27
```

`DomainLocalIndex` is used for:

- setting bits in `DirectMask`,
- setting bits in `ExpandedMask`,
- compiling query masks,
- fast `Has`, `HasAny`, `HasAll`, `HasNone`, and `Matches` operations.

Consumers must not store or depend on `DomainLocalIndex` directly. It is an internal compiled registry detail.

### 5.6 Tag Definition vs Tag Assignment

A **tag definition** is a vocabulary entry:

```text
Condition.Poisoned
Effect.Damage.Poison
Action.Skill.Offensive
```

A **tag assignment** means a domain-bound tag set currently has that tag:

```text
A condition tag set has Condition.Poisoned.
An effect classification tag set has Effect.Damage.Poison.
An action classification tag set has Action.Skill.Offensive.
```

Tag definitions are immutable after freeze.

Tag assignments are mutable only through the owning gameplay system.

The tag system does not know whether a tag set belongs to an entity, action, item, effect, target, cooldown, faction, or any other object.

### 5.7 Primary Registry IDs

Tags are not replacement IDs for other registries.

Each gameplay registry keeps its own primary ID:

```text
ActionDefinition       -> ActionId
EffectDefinition       -> EffectDefinitionId
ItemDefinition         -> ItemDefinitionId
TargetingDefinition    -> TargetingDefinitionId
CooldownDefinition     -> CooldownId
GameplayTagDefinition  -> GameplayTagId
```

Example:

```yaml
ActionDefinition:
  ActionId: action.skill.plasma_bolt
  ActionClassifications:
    - Action.Skill
    - Action.Offensive
    - Action.Projectile
  DamageKinds:
    - Damage.Thermal
```

`ActionId` identifies the action record. Tags describe and query it.

---

## 6 Domain Model

### 6.1 Registered Domains

Domains are registered during startup/import before compilation.

Required domain fields:

```text
DomainKey
RootPrefix
Description
```

Compiled domain fields:

```text
GameplayTagDomainId
DomainKey
RootPrefix
CanonicalTagCount
AssignableTagCount
DomainMaskWordCount
DomainMaskBitCapacity
DomainMaskByteSize
```

Example startup registration:

```csharp
builder.RegisterDomain(
    domainKey: "EffectClassification",
    rootPrefix: "Effect",
    description: "Metadata on effects used for blocking, removal, triggers, and filtering.");
```

`GameplayTagDomainId` is assigned by the registry during compilation and is immutable after freeze.

### 6.2 Initial Domains

Initial tag domains:

| DomainKey | RootPrefix | Purpose | Example |
|---|---|---|---|
| `EntityCondition` | `Condition` | Mutable state metadata on living entities | `Condition.Poisoned` |
| `EffectClassification` | `Effect` | Metadata on active effects and effect definitions | `Effect.Damage.Poison` |
| `ActionClassification` | `Action` | Metadata on usable actions | `Action.Skill.Offensive` |
| `ItemClassification` | `Item` | Metadata on items and equipment | `Item.Weapon.Laser` |
| `TargetClassification` | `Target` | Metadata used by targeting filters | `Target.LivingEntity` |
| `CooldownGroup` | `Cooldown` | Shared cooldown identity metadata | `Cooldown.Weapon.Light` |
| `DamageKind` | `Damage` | Damage taxonomy | `Damage.Thermal` |
| `FactionRelation` | `Faction` | Relationship or disposition metadata | `Faction.Hostile` |

Future systems may register additional domains before registry compilation.

### 6.3 Domain-Specific Assignment Fields

Authored gameplay data must use domain-specific assignment fields.

Valid:

```yaml
ActionDefinition:
  ActionId: action.skill.plasma_bolt
  ActionClassifications:
    - Action.Skill
    - Action.Offensive
    - Action.Projectile
  DamageKinds:
    - Damage.Thermal
```

Invalid canonical gameplay authoring:

```yaml
ActionDefinition:
  ActionId: action.skill.plasma_bolt
  Tags:
    - Action.Skill
    - Action.Offensive
    - Action.Projectile
    - Damage.Thermal
```

A generic debug/editor view may display a combined read-only tag list, but canonical gameplay data must keep assignments domain-specific.

### 6.4 Domain Safety

A `GameplayTagSet` belongs to exactly one domain.

The following must fail validation or throw/assert according to build policy:

```text
Add tag from different domain to tag set.
Remove tag from different domain from tag set.
Match query from different domain against tag set.
Compile query with tags from different domains.
Resolve short name that is ambiguous within a domain.
Register canonical tag whose root prefix does not match its domain.
```

Domain-safe queries prevent ambiguous behavior.

Valid:

```yaml
BlockedByConditions:
  Domain: EntityCondition
  Any:
    - Condition.Dead
    - Condition.Untargetable
```

Valid:

```yaml
BlockedByClassifications:
  Domain: EffectClassification
  Any:
    - Effect.Immunity.Poison
```

Invalid:

```yaml
BlockedBy:
  - Dead
  - Poison
  - Buff
```

---

## 7 Authoring Model

### 7.1 Canonical Names

All tags must have a canonical full name.

Canonical names use dot-separated hierarchy:

```text
RootPrefix.Category.Subcategory.Detail
```

Examples:

```text
Condition.Dead
Condition.Poisoned
Effect.Damage.Poison
Effect.Damage.Poison.Dot
Effect.Buff.ShieldCapacity
Action.Skill.Offensive
Action.Item.Consumable
Cooldown.Weapon.Light
Damage.Kinetic
Damage.Thermal
Faction.Hostile
```

Canonical names are case-sensitive.

Required style:

```text
PascalCase segments separated by dots.
```

Canonical names must:

- begin with a registered domain root prefix,
- contain only valid name segments,
- not contain empty segments,
- not contain leading or trailing dots,
- not contain whitespace,
- not use alias names after canonicalization.

### 7.2 Strict Dot-Path Hierarchy

Hierarchy is derived only from canonical dot-path names.

Example:

```text
Effect.Damage.Poison.Dot
```

Dot-path parents:

```text
Effect
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
```

No explicit extra parents are supported in v1.0.0.

Invalid v1.0.0 authoring:

```yaml
Condition.Poisoned:
  Domain: EntityCondition
  Parents:
    - Condition.DebuffState
```

If an object needs multiple classifications, assign multiple direct tags:

```yaml
EffectClassifications:
  - Effect.Damage.Poison
  - Effect.Debuff
```

Each direct tag expands its own dot-path parents independently.

### 7.3 Assignability

All canonical tags are queryable.

Domain root tags are structural and not assignable by default.

All non-root canonical tags are assignable by default.

A tag may opt out of direct assignment:

```yaml
Effect.InternalCompilerOnly:
  Domain: EffectClassification
  Assignable: false
  Description: Internal category tag used only for query grouping.
```

A parent/category tag may be directly assigned if it is non-root and assignable.

Example:

```yaml
EffectClassifications:
  - Effect.Damage
```

This is valid when the effect intentionally represents generic damage without a more specific subtype.

Child assignments still imply parent tags through `ExpandedMask`.

Example direct assignment:

```yaml
EffectClassifications:
  - Effect.Damage.Poison.Dot
```

Compiled expanded result:

```text
Effect
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
```

Exact/direct matching can distinguish whether `Effect.Damage` was directly assigned or only implied by a child.

### 7.4 Fully Qualified Names and Short Names

Canonical fully qualified names are required internally.

Domain-specific authoring fields may optionally allow short names, but import must canonicalize them.

Example authoring field:

```yaml
ConditionsAddRequest:
  - Poisoned
```

Canonicalized internally to:

```text
Condition.Poisoned
```

Example domain-specific field:

```yaml
EffectClassifications:
  - Damage.Poison.Dot
```

Canonicalized internally to:

```text
Effect.Damage.Poison.Dot
```

Short-name support is optional. If enabled, short names must be resolved only inside the known target domain of the field.

If a short name is ambiguous, validation must fail.

Runtime hot paths must not use short-name resolution.

### 7.5 Aliases and Migration

Aliases are supported only as startup/import migration inputs.

Example:

```yaml
Aliases:
  Effect.Dot.Poison: Effect.Damage.Poison.Dot
  Condition.Poison: Condition.Poisoned
  Effect.CoolDownReduction: Effect.CooldownReduction
```

Alias rules:

```text
Aliases are resolved before validation, compilation, registry hashing, and freeze.
Runtime data never stores aliases.
Runtime queries never use aliases.
Serialized/network/delta data never uses aliases.
Aliases must resolve to canonical tags in the same domain.
Alias chains are invalid.
Alias cycles are invalid.
Unknown alias targets are invalid.
Ambiguous aliases are invalid.
```

Using an alias during normal import should produce a warning unless the importer is running in an explicit migration mode.

Cross-domain aliases are invalid.

Invalid:

```yaml
Aliases:
  Condition.Poison: Effect.Damage.Poison
```

This is not a rename. It changes semantic domain and must be represented as explicit consuming-system logic or semantic metadata outside alias resolution.

### 7.6 Semantic Links

Cross-domain semantic relationships may exist as optional metadata, but they must not affect runtime mask expansion.

Example:

```yaml
SemanticLinks:
  Effect.Damage.Poison:
    RelatedConditions:
      - Condition.Poisoned
      - Condition.PoisonExposure
    RelatedDamageKinds:
      - Damage.Poison
```

Semantic links are metadata only.

They must not automatically:

- add conditions,
- remove conditions,
- classify effects,
- classify actions,
- mutate damage kinds,
- affect tag queries,
- affect parent expansion,
- affect hot-path matching.

---

## 8 Registry Lifecycle

### 8.1 Content Source Loading

The registry builder loads tag definitions from configured content sources.

Examples:

```text
base game tag files
expansion tag files
DLC tag files
tool-generated tag files
future mod tag files
```

All content sources are loaded before validation and freeze.

Content sources must have deterministic ordering.

Given the same base game version and same loaded content sources in the same resolved load order, the generated registry must produce the same canonical `GameplayTagId`s, domain-local indexes, compiled queries, and registry hash.

### 8.2 Canonicalization

Canonicalization resolves:

- domain root prefixes,
- optional short names,
- aliases,
- deprecated names,
- case rules,
- duplicate definitions,
- domain-specific assignment fields.

After canonicalization, all compiled runtime data uses canonical full names.

Aliases and short names must not survive into runtime data.

### 8.3 Validation

Validation checks all domains, tags, aliases, queries, and references.

Unknown tags must fail validation unless declared by a loaded content source before freeze.

Unknown domains must fail validation.

Domain mismatches must fail validation.

Canonical names must match the registered root prefix of their declared domain.

Dot-path parents must be derived from canonical names.

Each implied parent tag must exist as a canonical tag definition or be generated by import policy before validation completes.

Recommended policy:

```text
Explicitly declare parent/category tags in authored registry data.
Optional tooling may generate missing parent tags before validation.
Generated parent tags must appear in deterministic registry output.
```

### 8.4 Compilation

Compilation produces:

```text
GameplayTagDomainId table
GameplayTagId table
CanonicalName -> GameplayTagId lookup
GameplayTagId -> TagDefinition lookup
GameplayTagId -> Domain lookup
GameplayTagId -> DomainLocalIndex lookup
DomainLocalIndex tables
Parent expansion data
Domain mask word counts
CompiledTagQueries
Debug name tables
Registry hash
Generated runtime artifacts
```

Recommended deterministic assignment:

1. Load all configured content sources.
2. Canonicalize all names.
3. Resolve aliases.
4. Validate domains, root prefixes, tags, aliases, and queries.
5. Sort domains by stable `DomainKey` or explicit deterministic domain order.
6. Assign `GameplayTagDomainId` values.
7. Sort canonical tags by canonical full name.
8. Assign registry-wide `GameplayTagId` values.
9. Group canonical tags by domain.
10. Sort domain tags by canonical full name.
11. Assign dense `DomainLocalIndex` values.
12. Count tags per domain.
13. Assign domain mask word counts.
14. Compile direct-tag parent expansion data.
15. Compile query masks.
16. Emit registry hash.
17. Freeze registry.

### 8.5 Domain Mask Size Assignment

All domains use the same `GameplayTagMask` infrastructure.

There is no public `TagMask64`, `TagMask128`, `TagMask256`, or entity-condition-only storage type.

The registry determines the required mask size per domain during compilation.

Formula:

```text
DomainMaskWordCount = ceil(CompiledDomainTagCount / 64)
DomainMaskBitCapacity = DomainMaskWordCount * 64
DomainMaskByteSize = DomainMaskWordCount * sizeof(ulong)
```

`CompiledDomainTagCount` includes every canonical tag in that domain that can participate in matching, including parent/category tags.

After registry freeze, domain mask sizes are immutable for the whole simulation session.

Runtime systems may mutate assignments, but they may not change tag definitions, tag IDs, hierarchy, alias resolution, or compiled mask sizes.

### 8.6 Startup Diagnostics

The registry must log domain mask sizing information during startup compilation.

Logs must be deterministic and ordered by compiled `GameplayTagDomainId`.

Example:

```text
[GameplayTagRegistry] Domain compiled:
  DomainId: 2
  DomainKey: EntityCondition
  RootPrefix: Condition
  CanonicalTagCount: 187
  AssignableTagCount: 142
  NonAssignableTagCount: 45
  DomainMaskWordCount: 3
  DomainMaskBitCapacity: 192
  DomainMaskByteSize: 24
  ParentExpansion: DotPath
  Storage: DenseDomainLocalBitset
```

After all domains compile:

```text
[GameplayTagRegistry] Registry frozen:
  DomainCount: 8
  TotalCanonicalTagCount: 1264
  TotalCompiledQueryCount: 96
  RegistryHash: 0x7F4A8C10D20B9911
```

Startup diagnostics must not be required by hot-path gameplay logic.

### 8.7 Freeze

After compilation, the registry is frozen.

Frozen registry rules:

```text
No new domains.
No new tag definitions.
No deleted tag definitions.
No renamed tag definitions.
No changed root prefixes.
No changed parent hierarchy.
No changed GameplayTagIds.
No changed DomainLocalIndexes.
No changed mask word counts.
No changed alias resolution.
No changed compiled query masks.
```

Runtime systems may mutate only their own `GameplayTagSet` assignments.

### 8.8 Registry Hash

The compiled registry must provide a deterministic hash:

```text
GameplayTagRegistryHash
```

Uses:

- startup validation,
- save compatibility,
- replay compatibility,
- deterministic test fixtures,
- network peer compatibility,
- mod/content-set compatibility.

The hash must include:

```text
registered domains
root prefixes
domain IDs
canonical tag names
domain assignments
assignability flags
dot-path parent relationships
alias resolution policy and alias inputs
stable GameplayTagId assignments
DomainLocalIndex assignments
domain mask word counts
compiled query definitions
compiled query IDs
content source ordering metadata
registry version
```

Compatibility rule:

```text
GameplayTagId values are stable only inside the same compiled GameplayTagRegistryHash.
```

If the content set changes and generated IDs shift, the registry hash changes.

Network peers, saves, deterministic tests, and replays must reject or migrate mismatched registry hashes before simulation.

---

## 9 Runtime Representation

### 9.1 Flat Runtime Structure

Runtime storage must be flat.

The runtime must not perform string hierarchy traversal in gameplay simulation.

Preferred runtime representation:

```text
GameplayTagId: ushort
GameplayTagDomainId: byte or ushort
DomainLocalIndex: ushort or int internal index
GameplayTagMask: ulong[] / fixed pooled word span, domain-sized
TagQueryId: ushort or int
GameplayTagRegistryHash: ulong or wider deterministic hash
```

Hot-path gameplay should use cached IDs and compiled query handles.

### 9.2 GameplayTagSet

`GameplayTagSet` is the context-free runtime assignment container.

It represents a mutable set of assigned tags for exactly one domain.

A `GameplayTagSet` may be used by any gameplay system:

```text
StateEngine condition state
ActiveEffect classification metadata
ActionDefinition classification metadata
ItemDefinition classification metadata
Targeting filter metadata
Cooldown group metadata
Damage kind metadata
Faction relation metadata
future systems
```

The tag system does not know which gameplay object owns the set.

Required properties:

```text
Domain
DirectMask
ExpandedMask
Registry reference or registry version handle
```

The owning gameplay system decides where the set is stored and when it mutates.

### 9.3 DirectMask and ExpandedMask

Every runtime tag set stores two masks:

```text
DirectMask:
  Bits explicitly assigned by content or runtime state.

ExpandedMask:
  Direct bits plus all dot-path parent bits compiled from hierarchy.
```

Example direct assignment:

```text
Effect.Damage.Poison.Dot
```

Direct mask contains:

```text
Effect.Damage.Poison.Dot
```

Expanded mask contains:

```text
Effect
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
```

Normal gameplay queries should use `ExpandedMask`.

Exact matching, diagnostics, validation, and migration tools may use `DirectMask`.

### 9.4 Dynamic Dense Domain Masks

Runtime masks are domain-local dense bitsets.

Each domain has a compiled word count.

Example:

```text
EntityCondition has 300 compiled tags:
  DomainMaskWordCount = 5
  DomainMaskByteSize = 40

EffectClassification has 900 compiled tags:
  DomainMaskWordCount = 15
  DomainMaskByteSize = 120

DamageKind has 40 compiled tags:
  DomainMaskWordCount = 1
  DomainMaskByteSize = 8
```

This avoids forcing every tag set to use a full 65,536-bit mask.

A full 65,536-bit mask is not the default runtime representation.

Large domains must still use the same public `GameplayTagMask` and `GameplayTagSet` API. Internal storage may evolve to pooled dense words, sparse fixed chunks, or other deterministic allocation-free representations if a future domain becomes too large for dense masks.

### 9.5 Runtime Immutability

The tag registry is immutable after freeze.

Runtime must not:

- create tag definitions,
- register domains,
- rename tags,
- delete tags,
- re-parent tags,
- change tag IDs,
- change domain-local indexes,
- change hierarchy metadata,
- change alias resolution,
- change mask word counts,
- recompile queries.

Runtime systems may mutate tag assignments only on `GameplayTagSet` instances they own.

---

## 10 Query System

### 10.1 Query Operators

v1.0.0 supports simple query operators only:

```text
All
Any
None
```

Meaning:

```text
All  -> target tag set must contain every listed tag.
Any  -> target tag set must contain at least one listed tag.
None -> target tag set must contain none of the listed tags.
```

Example:

```yaml
TagQuery:
  Domain: EffectClassification
  All:
    - Effect.Damage.Poison
  None:
    - Effect.Immunity.Poison
```

Nested boolean expressions are out of scope for v1.0.0.

More complex logic can be represented by multiple named precompiled queries evaluated by the consuming system.

### 10.2 Query Validation

A query must contain at least one `All` or `Any` tag.

A query using only `None` is invalid by default.

Exception:

```yaml
TagQuery:
  Domain: EffectClassification
  MatchAll: true
  None:
    - Effect.Immunity.Bypass
```

`MatchAll: true` makes a broad domain-wide query intentional.

Validation rules:

```text
All tags must belong to the declared query domain.
Any tags must belong to the declared query domain.
None tags must belong to the declared query domain.
Unknown tags fail validation.
Aliases are resolved before query validation.
A query with no All and no Any requires MatchAll: true.
An empty Any clause is ignored.
An empty All clause is ignored.
An empty None clause is ignored.
```

### 10.3 Query Match Modes

Queries match against `ExpandedMask` by default.

Valid match modes:

```text
Expanded
Direct
```

Default authoring:

```yaml
TagQuery:
  Domain: EffectClassification
  All:
    - Effect.Damage
```

Equivalent canonical form:

```yaml
TagQuery:
  Domain: EffectClassification
  MatchMode: Expanded
  All:
    - Effect.Damage
```

This matches direct or child-implied damage classifications:

```text
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
Effect.Damage.Thermal
```

Exact/direct query:

```yaml
TagQuery:
  Domain: EffectClassification
  MatchMode: Direct
  All:
    - Effect.Damage
```

This matches only tag sets where `Effect.Damage` was directly assigned.

### 10.4 Parent Expansion

Parent expansion policy:

```text
Hierarchy is authored through canonical dot paths.
Parent tags are implied by child tags.
Runtime uses flat expanded masks.
Parent expansion is compiled during import/startup.
Runtime hot-path queries do not traverse hierarchy strings.
```

Example:

```text
Effect.Damage.Poison.Dot
```

May match expanded queries for:

```text
Effect
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
```

### 10.5 Precompiled Runtime Queries

Runtime gameplay should use precompiled queries.

Example authoring:

```yaml
TagQueries:
  query.effect.is_poison_damage:
    Domain: EffectClassification
    All:
      - Effect.Damage.Poison
```

Compiled:

```text
TagQueryId 44 -> CompiledTagQuery
```

Hot path:

```csharp
tagSet.Matches(queryId);
```

Runtime gameplay must not repeatedly parse query YAML or construct query masks during simulation.

### 10.6 Dynamic Queries and Tools

Dynamic interpreted queries may exist for:

- editor tools,
- debug console,
- tests,
- non-authoritative dynamic queries,
- content browsing,
- search UI.

Dynamic query caches must not be required by deterministic gameplay hot paths.

If a dynamic query cache is used in authoritative runtime, it must be:

```text
deterministic
capacity-bounded
allocation-free after initialization
safe under rollback/resimulation
not observable by gameplay logic
```

---

## 11 Public API

### 11.1 API Principles

The public API must hide mask internals.

Consumers should not care about:

- `DomainLocalIndex`,
- mask word count,
- parent expansion internals,
- bit offsets,
- generated parent masks,
- registry storage layout.

Consumers should be able to:

```text
Register domains.
Register tags.
Register aliases for migration.
Register queries.
Compile and freeze the registry.
Create a tag set for a domain.
Add a GameplayTagId to a tag set.
Remove a GameplayTagId from a tag set.
Clear a tag set.
Check whether a tag set has or does not have a GameplayTagId.
Check whether a tag set matches a compiled query.
Check whether a tag set satisfies All/Any/None requirements.
```

The tag system only matches and compares what was loaded into it before freeze.

The tag system must not expose context-specific methods such as:

```text
ApplyDamage
RemoveEffect
BlockAction
StartCooldown
FindTarget
ChangeFaction
AddConditionContributor
```

Those belong to consuming gameplay systems.

### 11.2 Startup Builder API

Suggested builder API:

```csharp
public sealed class GameplayTagRegistryBuilder
{
    public void RegisterDomain(
        string domainKey,
        string rootPrefix,
        string description = "");

    public void RegisterTag(
        string canonicalName,
        string domainKey,
        bool assignable = true,
        string description = "",
        string debugName = "");

    public void RegisterAlias(
        string aliasName,
        string canonicalTargetName);

    public void RegisterQuery(
        string queryKey,
        string domainKey,
        TagQueryDefinition definition);

    public GameplayTagRegistry CompileAndFreeze(
        IGameplayTagRegistryLogger logger);
}
```

The builder owns:

```text
canonicalization
alias resolution
domain validation
root prefix validation
dot-path parent validation
GameplayTagId assignment
DomainLocalIndex assignment
mask-size calculation
parent expansion compilation
query compilation
registry hash generation
startup diagnostics
freeze
```

### 11.3 Immutable Registry API

Suggested registry API:

```csharp
public sealed class GameplayTagRegistry
{
    public bool IsFrozen { get; }
    public ulong RegistryHash { get; }

    public bool TryGetDomainId(
        string domainKey,
        out GameplayTagDomainId domainId);

    public GameplayTagDomainId GetDomainId(
        string domainKey);

    public bool TryGetTagId(
        string canonicalName,
        out GameplayTagId tagId);

    public GameplayTagId GetTagId(
        string canonicalName);

    public bool TryGetQueryId(
        string queryKey,
        out TagQueryId queryId);

    public TagQueryId GetQueryId(
        string queryKey);

    public GameplayTagDomainId GetDomain(GameplayTagId tagId);

    public string GetDebugName(GameplayTagId tagId);

    public string GetCanonicalName(GameplayTagId tagId);

    public bool IsAssignable(GameplayTagId tagId);

    public GameplayTagSet CreateTagSet(GameplayTagDomainId domainId);

    public GameplayTagSet CreateTagSet(string domainKey);

    public CompiledTagQuery GetQuery(TagQueryId queryId);
}
```

String lookup is allowed at startup/import/debug boundaries.

Hot-path gameplay should cache `GameplayTagId`, `GameplayTagDomainId`, and `TagQueryId` values.

### 11.4 Runtime Tag Set API

`GameplayTagId` is immutable identity and cannot be mutated.

Tags are added to or removed from a `GameplayTagSet`.

Suggested runtime API:

```csharp
public sealed class GameplayTagSet
{
    public GameplayTagDomainId Domain { get; }

    public void Clear();

    public bool Add(GameplayTagId tagId);

    public bool Remove(GameplayTagId tagId);

    public bool HasDirect(GameplayTagId tagId);

    public bool HasExpanded(GameplayTagId tagId);

    public bool DoesNotHaveDirect(GameplayTagId tagId);

    public bool DoesNotHaveExpanded(GameplayTagId tagId);

    public bool HasAllDirect(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasAllExpanded(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasAnyDirect(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasAnyExpanded(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasNoneDirect(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasNoneExpanded(ReadOnlySpan<GameplayTagId> tagIds);

    public bool Matches(TagQueryId queryId);

    public bool Matches(in CompiledTagQuery query);

    public void CopyFrom(in GameplayTagSet other);
}
```

Implementation may use `struct`, pooled buffers, fixed arrays, or owner-managed storage depending on hot-path requirements. The public semantics remain the same.

Domain mismatch behavior:

```text
Add tag from wrong domain: fail.
Remove tag from wrong domain: fail.
Has tag from wrong domain: false or fail according to build policy.
Matches query from wrong domain: fail.
CopyFrom different domain: fail.
```

Recommended build policy:

```text
Debug/development builds: assert or throw with diagnostic.
Release deterministic runtime: return false or deterministic failure code where exceptions are not allowed.
Import/startup validation: fail hard.
```

### 11.5 Query API

Suggested query-definition API:

```csharp
public readonly struct TagQueryDefinition
{
    public GameplayTagQueryMatchMode MatchMode { get; }
    public bool MatchAll { get; }
    public IReadOnlyList<string> All { get; }
    public IReadOnlyList<string> Any { get; }
    public IReadOnlyList<string> None { get; }

    public static TagQueryDefinition Expanded(
        IReadOnlyList<string>? all = null,
        IReadOnlyList<string>? any = null,
        IReadOnlyList<string>? none = null,
        bool matchAll = false);

    public static TagQueryDefinition Direct(
        IReadOnlyList<string>? all = null,
        IReadOnlyList<string>? any = null,
        IReadOnlyList<string>? none = null,
        bool matchAll = false);
}
```

Suggested compiled query shape:

```csharp
public readonly struct CompiledTagQuery
{
    public readonly TagQueryId QueryId;
    public readonly GameplayTagDomainId Domain;
    public readonly GameplayTagQueryMatchMode MatchMode;
    public readonly bool MatchAll;

    // Internal domain-local masks, not exposed to normal consumers.
    internal readonly GameplayTagMask AllMask;
    internal readonly GameplayTagMask AnyMask;
    internal readonly GameplayTagMask NoneMask;
}
```

Query matching semantics:

```text
If MatchMode is Expanded, evaluate against ExpandedMask.
If MatchMode is Direct, evaluate against DirectMask.
AllMask must be fully contained.
AnyMask must overlap unless Any is empty.
NoneMask must not overlap.
If no All and no Any are present, MatchAll must be true.
```

### 11.6 Import and Validation API

Suggested import result shape:

```csharp
public readonly struct GameplayTagRegistryBuildResult
{
    public readonly bool Success;
    public readonly GameplayTagRegistry? Registry;
    public readonly IReadOnlyList<GameplayTagValidationMessage> Messages;
}
```

Validation message shape:

```csharp
public readonly struct GameplayTagValidationMessage
{
    public readonly GameplayTagValidationSeverity Severity;
    public readonly string Code;
    public readonly string Message;
    public readonly string SourceId;
    public readonly int Line;
    public readonly int Column;
}
```

Suggested severities:

```text
Trace
Info
Warning
Error
Fatal
```

### 11.7 Error Handling Policy

Startup/import errors must fail registry compilation.

Runtime deterministic hot paths should avoid throwing exceptions when configured for authoritative simulation.

Recommended policy:

```text
Startup/import:
  collect detailed diagnostics and fail compilation if invalid.

Development runtime:
  assert or throw on invalid API misuse.

Release deterministic runtime:
  use deterministic false/failure result for invalid match operations where possible.
```

Invalid registry state must never be accepted into simulation.

---

## 12 Runtime Assignment Rules

### 12.1 Context-Free Assignment

The tag system owns tag-set operations only.

It can answer:

```text
Does this tag set directly have this tag?
Does this tag set have this tag through expanded hierarchy?
Does this tag set not have this tag?
Does this tag set satisfy this compiled query?
Does this tag set contain all, any, or none of these tags?
```

It cannot answer:

```text
Should this effect be removed?
Should this action be blocked?
Should this target be selected?
Should this item be equipped?
Should this cooldown start?
Should this damage be reduced?
Should this condition contributor be removed?
```

Those decisions belong to consuming systems.

### 12.2 Entity Conditions

Entity condition tags describe mutable state on a living entity.

Examples:

```text
Condition.Dead
Condition.Poisoned
Condition.Invisible
Condition.ShieldCooldown
Condition.Untargetable
```

The tag system does not own condition contributors.

Condition contributor ownership remains a StateEngine concern.

The StateEngine may store a `GameplayTagSet` for entity conditions, but the tag system does not know the owning entity.

### 12.3 Effect Classifications

Effect classification tags describe effects.

Examples:

```text
Effect.Damage
Effect.Damage.Poison
Effect.Damage.Poison.Dot
Effect.Damage.Thermal
Effect.Debuff
Effect.Cleanse
Effect.Buff.ShieldCapacity
Effect.Immunity.Bypass
```

Effect classifications do not mutate entity state directly.

They are used by consuming systems for:

- blocking,
- removal selection,
- triggers,
- filtering,
- UI/debug grouping,
- deterministic inspection.

The tag system only answers whether an effect classification tag set matches requested tags or queries.

### 12.4 Action Classifications

Action classification tags describe usable actions.

Examples:

```text
Action.Skill
Action.Item
Action.Offensive
Action.Defensive
Action.Projectile
Action.AreaOfEffect
Action.Movement
```

Action tags may be used by consuming systems for:

- action filtering,
- loadout validation,
- input binding,
- UI grouping,
- cooldown rules,
- action-blocking rules,
- AI selection,
- tutorial/search categorization.

Action tags do not apply effects by themselves.

### 12.5 Item Classifications

Item classification tags describe items, equipment, inventory definitions, or item instances.

Examples:

```text
Item.Weapon
Item.Weapon.Laser
Item.Consumable
Item.Material
Item.Quest
Item.Equipment.ShieldGenerator
```

Item tags do not equip, unequip, consume, stack, or mutate inventory by themselves.

### 12.6 Target Classifications

Target classification tags describe target concepts and target-filter metadata.

Examples:

```text
Target.LivingEntity
Target.Hostile
Target.Friendly
Target.Self
Target.Area
Target.ProjectileHit
Target.LineOfSightRequired
```

The tag system does not perform world queries, physics queries, raycasts, faction checks, or line-of-sight checks.

### 12.7 Cooldown Groups

Cooldown group tags identify shared cooldown channels.

Examples:

```text
Cooldown.Weapon.Light
Cooldown.Weapon.Heavy
Cooldown.Skill.Defensive
Cooldown.Item.Consumable
```

Cooldown tags do not count time by themselves.

Cooldown state is owned by the ActionSystem or CooldownSystem.

### 12.8 Damage Kinds

Damage kind tags describe damage taxonomy.

Examples:

```text
Damage.Kinetic
Damage.Thermal
Damage.Explosive
Damage.Poison
Damage.Energy
```

Damage kind tags do not apply damage formulas.

Damage systems may use damage kind tag sets or IDs to choose resistance, bonus damage, mitigation, UI, VFX, or effect classification logic.

### 12.9 Faction Relations

Faction relation tags describe relationship or disposition metadata.

Examples:

```text
Faction.Friendly
Faction.Neutral
Faction.Hostile
Faction.Self
Faction.Ally
Faction.Enemy
```

The tag system does not calculate faction relations. It only stores and compares relation metadata assigned by a consuming faction or targeting system.

---

## 13 Integration Rules

### 13.1 State Engine Integration

The StateEngine may use the tag system for:

```text
EntityCondition tag sets
EffectClassification tag sets
blocking queries
removal queries
trigger queries
visibility/debug grouping
condition validation
effect definition validation
```

Important rule:

```text
Conditions and classifications use the same tag infrastructure,
but they remain separate semantic domains.
```

Do not unify conditions and classifications into one gameplay concept.

Reason:

```text
Condition.Poisoned on an entity:
  entity is poisoned.

Effect.Damage.Poison on an effect:
  effect is poison-type damage.
```

These are related but not the same state.

The tag system does not own:

- condition contributor records,
- effect lifetime,
- effect stacking,
- effect removal ordering,
- effect blocking policy,
- stat mutation,
- delta emission,
- rollback or resimulation.

### 13.2 Action System Integration

The tag system should support a future ActionSystem.

A clean architecture:

```text
ActionDefinition
  ActionId
  ActionClassifications
  TargetingDefinitionId
  CostDefinitionId
  CooldownDefinitionId
  DamageKinds
  EffectsOnUse / EffectsOnHit

TargetingDefinition
  target shape
  target filters
  blocked conditions
  required target tags

CostDefinition
  stat costs
  item costs
  ammo costs

CooldownDefinition
  cooldown duration
  cooldown group tags

EffectDefinition
  state changes processed by StateEngine
```

Activation flow remains owned by consuming systems:

```text
1. Player/AI requests action use.
2. ActionSystem validates actor state.
3. ActionSystem checks action tags, cooldown, charges, ammo, and resources.
4. TargetingSystem resolves and validates targets.
5. CostSystem reserves or spends resources.
6. ActionSystem creates deterministic ActionExecutionId.
7. ActionSystem emits normalized ActiveEffect requests.
8. StateEngine processes ActiveEffects.
9. Presentation layer plays feedback outside authoritative simulation.
```

The tag system only provides metadata matching for those systems.

### 13.3 Targeting System Integration

The TargetingSystem may use tag sets and queries for:

- required target classifications,
- forbidden target classifications,
- actor condition filters,
- effect classification filters,
- faction relation metadata.

The tag system does not perform candidate acquisition.

### 13.4 Cooldown System Integration

The CooldownSystem may use `CooldownGroup` tags to group cooldown channels.

The tag system does not:

- store cooldown remaining time,
- start cooldowns,
- expire cooldowns,
- reduce cooldowns,
- decide whether an action is available.

### 13.5 Item System Integration

The ItemSystem may use `ItemClassification` tags to describe item definitions or instances.

The tag system does not:

- own inventory slots,
- equip items,
- consume items,
- stack items,
- transfer items,
- generate loot.

---

## 14 Serialization and Networking

Runtime serialized data should use numeric IDs or compact masks.

Do not serialize canonical tag strings in hot-path network deltas.

Recommended serialization:

```text
GameplayTagRegistryHash
GameplayTagId
GameplayTagDomainId
TagQueryId
GameplayTagSet masks
```

Network compatibility rule:

```text
Peers must use the same compiled GameplayTagRegistryHash for deterministic gameplay simulation.
```

Save compatibility rule:

```text
A save file should store GameplayTagRegistryHash or compatible registry version metadata.
```

If a saved tag ID no longer exists in the current registry, migration must be handled before simulation.

If registry hashes differ, deterministic simulation must not proceed unless an explicit compatibility/migration layer validates equivalence.

Runtime mask serialization must include enough metadata to identify the registry hash and domain.

---

## 15 Performance Requirements

The tag system must be low-memory and low-allocation.

Hot-path requirements:

- no string comparisons,
- no string allocations,
- no YAML parsing,
- no dynamic query parsing,
- no dictionary lookups per tag comparison,
- no hierarchy traversal,
- no LINQ,
- no reflection,
- no enum boxing,
- no heap allocation for normal tag query execution.

Preferred hot-path operations:

```text
Add(tagId)
Remove(tagId)
HasDirect(tagId)
HasExpanded(tagId)
HasAll(mask)
HasAny(mask)
HasNone(mask)
Matches(compiledQuery)
```

Preferred storage:

```text
ushort GameplayTagId
small numeric GameplayTagDomainId
domain-local dense ulong masks
array-backed registries
precompiled query masks
cached runtime IDs
```

Mask operation costs scale with domain mask word count:

```text
Has(tagId):      one domain-local bit test
HasAny(mask):    scan DomainMaskWordCount words
HasAll(mask):    scan DomainMaskWordCount words
HasNone(mask):   scan DomainMaskWordCount words
Matches(query):  scan query masks for declared domain
```

The registry must expose startup diagnostics for domain mask sizes so large domains are visible before runtime.

---

## 16 Validation Requirements

Startup/import validation must reject:

- unknown domains,
- duplicate domain keys,
- duplicate root prefixes,
- invalid root prefixes,
- invalid canonical names,
- canonical names whose root prefix does not match domain,
- duplicate canonical tags,
- unknown tags,
- missing required parent/category tags, unless generated deterministically by import policy,
- explicit extra parents,
- aliases remaining in canonical runtime data,
- alias chains,
- alias cycles,
- alias targets in another domain,
- unknown alias targets,
- ambiguous aliases,
- ambiguous short names,
- domain mismatches,
- queries referencing tags outside their declared domain,
- queries with only `None` and no `MatchAll: true`,
- direct assignment of `Assignable: false` tags,
- direct assignment of domain root tags,
- invalid content source ordering,
- registry hash mismatch in deterministic contexts.

Validation should warn or fail depending on policy for:

- valid queries that match no current content,
- unused tags,
- tags with no description,
- broad parent/category tags assigned directly where a more specific tag is expected,
- alias usage outside migration mode,
- generated parent/category tags with no description.

Recommended policy:

```text
Unresolved tags: error
Domain mismatch: error
Duplicate root prefix: error
Explicit extra parent: error
Ambiguous short name: error
Alias usage during normal import: warning
Empty query result against current content: warning
Unused tag: warning
Missing description: warning
```

---

## 17 Example Authoring Data

### 17.1 Tag Registry Example

```yaml
GameplayTagRegistry:
  Version: 1.0.0

  Domains:
    - DomainKey: EntityCondition
      RootPrefix: Condition
      Description: Mutable condition metadata owned by gameplay state systems.

    - DomainKey: EffectClassification
      RootPrefix: Effect
      Description: Metadata on effects used for blocking, removal, triggers, and filtering.

    - DomainKey: ActionClassification
      RootPrefix: Action
      Description: Metadata on usable actions.

    - DomainKey: ItemClassification
      RootPrefix: Item
      Description: Metadata on items and equipment.

    - DomainKey: TargetClassification
      RootPrefix: Target
      Description: Metadata used by targeting filters.

    - DomainKey: CooldownGroup
      RootPrefix: Cooldown
      Description: Shared cooldown identity metadata.

    - DomainKey: DamageKind
      RootPrefix: Damage
      Description: Damage taxonomy metadata.

    - DomainKey: FactionRelation
      RootPrefix: Faction
      Description: Relationship and disposition metadata.

  Tags:
    Condition:
      Domain: EntityCondition
      Assignable: false
      Description: Structural root for entity condition tags.

    Condition.Dead:
      Domain: EntityCondition
      Description: Entity is dead and normally blocked from receiving non-revive effects.

    Condition.Untargetable:
      Domain: EntityCondition
      Description: Entity cannot be selected as a valid target by normal targeting rules.

    Condition.Poisoned:
      Domain: EntityCondition
      Description: Entity is currently poisoned.

    Condition.PoisonExposure:
      Domain: EntityCondition
      Description: Entity is exposed to poison but may not yet be taking poison damage.

    Effect:
      Domain: EffectClassification
      Assignable: false
      Description: Structural root for effect classification tags.

    Effect.Damage:
      Domain: EffectClassification
      Description: Effect is damage-related.

    Effect.Damage.Poison:
      Domain: EffectClassification
      Description: Effect deals or represents poison damage.

    Effect.Damage.Poison.Dot:
      Domain: EffectClassification
      Description: Effect deals poison damage over time.

    Effect.Damage.Thermal:
      Domain: EffectClassification
      Description: Effect deals or represents thermal damage.

    Effect.Debuff:
      Domain: EffectClassification
      Description: Effect is harmful to the target.

    Effect.Cleanse:
      Domain: EffectClassification
      Description: Effect removes one or more other effect classifications or conditions.

    Effect.Immunity:
      Domain: EffectClassification
      Description: Effect grants or represents immunity behavior.

    Effect.Immunity.Bypass:
      Domain: EffectClassification
      Description: Effect bypasses normal immunity classification checks.

    Action:
      Domain: ActionClassification
      Assignable: false
      Description: Structural root for action classification tags.

    Action.Skill:
      Domain: ActionClassification
      Description: Action is an active skill.

    Action.Offensive:
      Domain: ActionClassification
      Description: Action is used offensively.

    Action.Projectile:
      Domain: ActionClassification
      Description: Action creates or uses a projectile.

    Cooldown:
      Domain: CooldownGroup
      Assignable: false
      Description: Structural root for cooldown group tags.

    Cooldown.Weapon:
      Domain: CooldownGroup
      Description: Cooldown group for weapon actions.

    Cooldown.Weapon.Light:
      Domain: CooldownGroup
      Description: Shared cooldown group for light weapon actions.

    Damage:
      Domain: DamageKind
      Assignable: false
      Description: Structural root for damage kind tags.

    Damage.Kinetic:
      Domain: DamageKind
      Description: Kinetic damage kind.

    Damage.Thermal:
      Domain: DamageKind
      Description: Thermal damage kind.

    Damage.Poison:
      Domain: DamageKind
      Description: Poison damage kind.

  Aliases:
    Condition.Poison: Condition.Poisoned
    Effect.Dot.Poison: Effect.Damage.Poison.Dot
```

### 17.2 Query Registry Example

```yaml
GameplayTagQueries:
  Version: 1.0.0

  Queries:
    query.condition.blocks_normal_effects:
      Domain: EntityCondition
      MatchMode: Expanded
      Any:
        - Condition.Dead
        - Condition.Untargetable

    query.effect.is_damage:
      Domain: EffectClassification
      MatchMode: Expanded
      All:
        - Effect.Damage

    query.effect.is_poison_damage:
      Domain: EffectClassification
      MatchMode: Expanded
      All:
        - Effect.Damage.Poison

    query.effect.is_direct_generic_damage:
      Domain: EffectClassification
      MatchMode: Direct
      All:
        - Effect.Damage

    query.effect.can_be_removed_by_invulnerability:
      Domain: EffectClassification
      MatchMode: Expanded
      All:
        - Effect.Damage
      None:
        - Effect.Immunity.Bypass

    query.action.is_offensive_skill:
      Domain: ActionClassification
      MatchMode: Expanded
      All:
        - Action.Skill
        - Action.Offensive
```

### 17.3 EffectDefinition Usage Example

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.damage.poison.dot
    Name: Poison Damage Over Time
    Description: Applies poison damage over time and marks the entity as poisoned.

  EffectProcessing: Fast

  Trigger:
    TriggerType: OverTime
    TickIntervalMs: 1000

  Duration:
    DurationType: Temporary
    DurationMS: 5000

  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Flat
    Value: 5.0
    ApplicationMode: Instant

  EffectClassifications:
    - Effect.Damage.Poison.Dot
    - Effect.Debuff

  DamageKinds:
    - Damage.Poison

  ConditionRequests:
    - Timing: OnFirstApplication
      ConditionsAddRequest:
        - Condition.PoisonExposure

    - Timing: OnFirstExecution
      ConditionsAddRequest:
        - Condition.Poisoned

    - Timing: OnExpireOrRemove
      RemoveMode: RemoveOwnContribution
      ConditionsRemoveRequest:
        - Condition.PoisonExposure
        - Condition.Poisoned
```

### 17.4 ActionDefinition Usage Example

```yaml
ActionDefinition:
  ActionId: action.skill.plasma_bolt
  Name: Plasma Bolt
  Description: Fires a thermal projectile at a single hostile target.

  ActionClassifications:
    - Action.Skill
    - Action.Offensive
    - Action.Projectile

  DamageKinds:
    - Damage.Thermal

  TargetClassifications:
    - Target.LivingEntity
    - Target.Hostile

  CooldownGroups:
    - Cooldown.Weapon.Light

  TargetingDefinitionId: targeting.single_hostile_in_range
  CostDefinitionId: cost.energy.plasma_bolt
  CooldownDefinitionId: cooldown.weapon.light_attack

  EffectsOnHit:
    - effect.damage.thermal.plasma_bolt
```

---

## 18 Suggested Runtime Types

Suggested C# primitive wrappers:

```csharp
public readonly struct GameplayTagId
{
    public readonly ushort Value;
}

public readonly struct GameplayTagDomainId
{
    public readonly ushort Value;
}

public readonly struct TagQueryId
{
    public readonly ushort Value;
}
```

Suggested match mode:

```csharp
public enum GameplayTagQueryMatchMode : byte
{
    Expanded = 0,
    Direct = 1
}
```

Internal mask concept:

```csharp
internal struct GameplayTagMask
{
    // Domain-sized dense ulong words.
    // Storage may be fixed, pooled, array-backed, or owner-backed.

    public bool Has(int domainLocalIndex);
    public bool HasAny(in GameplayTagMask mask);
    public bool HasAll(in GameplayTagMask mask);
    public bool HasNone(in GameplayTagMask mask);
    public void Set(int domainLocalIndex);
    public void Clear(int domainLocalIndex);
    public void ClearAll();
}
```

Runtime tag set:

```csharp
public sealed class GameplayTagSet
{
    public GameplayTagDomainId Domain { get; }

    public bool Add(GameplayTagId tagId);
    public bool Remove(GameplayTagId tagId);
    public void Clear();

    public bool HasDirect(GameplayTagId tagId);
    public bool HasExpanded(GameplayTagId tagId);

    public bool HasAllExpanded(ReadOnlySpan<GameplayTagId> tagIds);
    public bool HasAnyExpanded(ReadOnlySpan<GameplayTagId> tagIds);
    public bool HasNoneExpanded(ReadOnlySpan<GameplayTagId> tagIds);

    public bool HasAllDirect(ReadOnlySpan<GameplayTagId> tagIds);
    public bool HasAnyDirect(ReadOnlySpan<GameplayTagId> tagIds);
    public bool HasNoneDirect(ReadOnlySpan<GameplayTagId> tagIds);

    public bool Matches(TagQueryId queryId);
    public bool Matches(in CompiledTagQuery query);
}
```

The concrete implementation may choose allocation-free owner-managed structs for hot-path systems. The semantic API remains the same.

Compiled query:

```csharp
public readonly struct CompiledTagQuery
{
    public readonly TagQueryId QueryId;
    public readonly GameplayTagDomainId Domain;
    public readonly GameplayTagQueryMatchMode MatchMode;
    public readonly bool MatchAll;

    internal readonly GameplayTagMask AllMask;
    internal readonly GameplayTagMask AnyMask;
    internal readonly GameplayTagMask NoneMask;
}
```

Registry:

```csharp
public sealed class GameplayTagRegistry
{
    public bool IsFrozen { get; }
    public ulong RegistryHash { get; }

    public GameplayTagDomainId GetDomainId(string domainKey);
    public GameplayTagId GetTagId(string canonicalName);
    public TagQueryId GetQueryId(string queryKey);

    public GameplayTagDomainId GetDomain(GameplayTagId tagId);
    public string GetCanonicalName(GameplayTagId tagId);
    public string GetDebugName(GameplayTagId tagId);
    public bool IsAssignable(GameplayTagId tagId);

    public GameplayTagSet CreateTagSet(GameplayTagDomainId domainId);
    public CompiledTagQuery GetQuery(TagQueryId queryId);
}
```

Runtime gameplay should prefer generated constants or startup-cached IDs over repeated string lookup.

Example cached constants:

```csharp
public static class GameTags
{
    public static GameplayTagId EffectDamagePoisonDot;
    public static GameplayTagId EffectDamage;
    public static GameplayTagId ConditionPoisoned;
    public static TagQueryId QueryEffectIsDamage;

    public static void Initialize(GameplayTagRegistry registry)
    {
        EffectDamagePoisonDot = registry.GetTagId("Effect.Damage.Poison.Dot");
        EffectDamage = registry.GetTagId("Effect.Damage");
        ConditionPoisoned = registry.GetTagId("Condition.Poisoned");
        QueryEffectIsDamage = registry.GetQueryId("query.effect.is_damage");
    }
}
```

---

## 19 Testing and Acceptance Criteria

### 19.1 Registry Tests

- Loads base tag file.
- Loads multiple content sources in deterministic order.
- Registers domains at startup.
- Rejects duplicate domain keys.
- Rejects duplicate root prefixes.
- Rejects unknown domains.
- Rejects duplicate canonical names.
- Rejects explicit extra parents.
- Resolves aliases before validation.
- Rejects alias chains.
- Rejects alias cycles.
- Rejects cross-domain aliases.
- Rejects ambiguous short names.
- Freezes registry after validation and compilation.
- Prevents mutation after freeze.

### 19.2 Determinism Tests

- Same content set produces same `GameplayTagId`s.
- Same content set produces same `DomainLocalIndex` values.
- Same content set produces same domain mask sizes.
- Same content set produces same compiled query masks.
- Same content set produces same registry hash.
- Different content set produces different registry hash when relevant data changes.
- Generated runtime table is stable across repeated runs.

### 19.3 Mask and Assignment Tests

- Registry counts tags per domain correctly.
- Registry assigns correct domain mask word count.
- Startup diagnostics log domain mask size information.
- `Add` sets direct and expanded bits.
- `Remove` clears direct tag and rebuilds or updates expanded bits correctly.
- Removing one child tag does not remove a parent bit still implied by another direct child tag.
- `Clear` clears direct and expanded masks.
- Domain-mismatched `Add` fails.
- Domain-mismatched `Remove` fails.
- Domain-mismatched query match fails.

### 19.4 Query Tests

- `All` query matches only when all required tags are present.
- `Any` query matches when at least one tag is present.
- `None` query fails when a forbidden tag is present.
- Query with only `None` fails validation unless `MatchAll: true`.
- Expanded query matches child tags through parent expansion.
- Direct query matches only direct assignments.
- Domain-mismatched query fails validation.
- Precompiled query returns same result as validation-time interpreted query.

### 19.5 Runtime Allocation Tests

- Hot-path `HasDirect`, `HasExpanded`, `HasAny`, `HasAll`, `HasNone`, and `Matches` allocate zero memory.
- No string comparison occurs in hot-path query execution.
- No hierarchy traversal occurs in hot-path query execution.
- Dynamic query cache is not required by runtime gameplay query path.

### 19.6 StateEngine Integration Tests

- `Condition.Poisoned` can be added and removed through StateEngine-owned condition assignment.
- Effect classification tags exist on active effects or effect metadata only.
- Effect removal by classification works through compiled tag masks.
- Blocking by condition works through compiled tag masks.
- Conditions and classifications remain separate domains.
- The tag system does not mutate condition contributors.

### 19.7 Content-Pack Extensibility Tests

- Additional content source can declare new domains before freeze.
- Additional content source can declare new tags before freeze.
- EffectDefinition using a new declared tag validates.
- ActionDefinition using a new declared tag validates.
- Definition using undeclared tag fails.
- Registry hash changes when content source set changes.

---

## 20 Open Future Extensions

Future PRDs may define:

- full mod-loading pipeline,
- stable append-only tag ID manifest,
- deprecated-but-reserved tag IDs,
- save-game migration tooling,
- network handshake using registry hash,
- editor UI for tag browsing,
- generated C# constants,
- editor-only hot reload,
- visual tag query editor,
- nested boolean query expressions,
- sparse fixed-chunk masks for very large domains,
- tag analytics,
- action system PRD integration,
- targeting system PRD integration,
- cooldown system PRD integration,
- item/equipment system PRD integration.
