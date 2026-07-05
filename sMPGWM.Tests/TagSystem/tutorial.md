# Gameplay Tag System Tutorial

This tutorial shows the basic flow:

1. Register a domain.
2. Register tags for that domain.
3. Register a query.
4. Compile and freeze the registry.
5. Assign a `GameplayTagSet` to another object.
6. Query that object with precompiled queries.

The tag system uses canonical, fully qualified names. Short names like `Poison` or `Dead` are intentionally not supported in runtime fixtures.

## 1. Register A Domain

A domain defines where tags are valid. For effect metadata, use an `EffectClassification` domain with the root prefix `Effect`.

```csharp
using sMPGWM.Scripts.Gameplay.Tags;

var builder = new GameplayTagRegistryBuilder();

builder.RegisterDomain(
    domainKey: "EffectClassification",
    rootPrefix: "Effect",
    description: "Metadata on effects used for blocking, removal, triggers, and filtering.");
```

The root tag `Effect` is created by the registry, but it is not assignable.

## 2. Register Tags

Parent/category tags must be registered explicitly. This catches typos early.

```csharp
builder
    .RegisterTag("EffectClassification", "Effect.Damage")
    .RegisterTag("EffectClassification", "Effect.Damage.Poison")
    .RegisterTag("EffectClassification", "Effect.Damage.Poison.Dot")
    .RegisterTag("EffectClassification", "Effect.Immunity")
    .RegisterTag("EffectClassification", "Effect.Immunity.Poison");
```

`Effect.Damage` and `Effect.Damage.Poison` default to non-assignable because they have children. `Effect.Damage.Poison.Dot` is a leaf tag, so it defaults to assignable.

## 3. Register A Query

Queries are compiled during registry freeze. Runtime systems should use cached query IDs or cached `CompiledTagQuery` instances.

```csharp
builder.RegisterQuery(
    queryKey: "query.effect.cleanse_poison_removal",
    domainKey: "EffectClassification",
    all: ["Effect.Damage.Poison"],
    none: ["Effect.Immunity.Poison"]);
```

This query means:

- Match effects classified as poison damage.
- Reject effects that also have poison immunity.
- Use expanded matching by default, so `Effect.Damage.Poison.Dot` satisfies `Effect.Damage.Poison`.

## 4. Compile And Freeze

After compilation, domains, tags, indexes, masks, queries, and the registry hash are immutable.

```csharp
var registry = builder.CompileAndFreeze();

var poisonDotTag = registry.GetTagId("Effect.Damage.Poison.Dot");
var poisonImmunityTag = registry.GetTagId("Effect.Immunity.Poison");
var cleansePoisonQueryId = registry.GetQueryId("query.effect.cleanse_poison_removal");
var cleansePoisonQuery = registry.GetQuery(cleansePoisonQueryId);
```

String lookup is fine at startup. Hot paths should keep the resolved IDs and compiled query references.

## 5. Assign A Tag Set To Another Object

The tag system is context-free. The owner can be an effect definition, an active effect, an action, an item, or another gameplay object.

```csharp
public sealed class EffectMetadata
{
    public EffectMetadata(GameplayTagSet classifications)
    {
        Classifications = classifications;
    }

    public GameplayTagSet Classifications { get; }
}

var effectClassificationDomain = registry.GetDomainId("EffectClassification");
var effect = new EffectMetadata(registry.CreateTagSet(effectClassificationDomain));

effect.Classifications.Add(poisonDotTag);
```

Adding `Effect.Damage.Poison.Dot` sets:

- Direct bit: `Effect.Damage.Poison.Dot`
- Expanded bits: `Effect`, `Effect.Damage`, `Effect.Damage.Poison`, `Effect.Damage.Poison.Dot`

## 6. Query With A Precompiled Query

Use the cached `CompiledTagQuery` in runtime code.

```csharp
var canBeRemovedByPoisonCleanse = effect.Classifications.Matches(cleansePoisonQuery);
// true
```

If the effect also has poison immunity, the `None` clause rejects the match.

```csharp
effect.Classifications.Add(poisonImmunityTag);

var stillCanBeRemovedByPoisonCleanse = effect.Classifications.Matches(cleansePoisonQuery);
// false
```

## 7. Strict API Versus Try API

Strict methods throw on programmer misuse, such as adding a tag from another domain.

```csharp
effect.Classifications.Add(poisonDotTag);
var hasPoisonDamage = effect.Classifications.HasExpanded(
    registry.GetTagId("Effect.Damage.Poison"));
```

No-throw `Try...` variants are available for hot-path callers that want deterministic failure instead of exceptions.

```csharp
if (effect.Classifications.TryMatches(cleansePoisonQuery, out var matches) && matches)
{
    // The effect matches the poison cleanse classification query.
}
```

Use strict APIs in setup, tests, and development integration. Use `Try...` APIs where simulation code needs deterministic no-throw behavior.
