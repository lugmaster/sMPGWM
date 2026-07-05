using sMPGWM.Scripts.Gameplay.Tags;

namespace sMPGWM.Tests.TagSystem;

[TestFixture]
public class TagSystemRunTests
{
    [Test]
    public void Determinism_SameContentProducesSameCompiledOutput()
    {
        var first = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var second = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.That(second.RegistryHash, Is.EqualTo(first.RegistryHash));
        Assert.That(second.Tags, Has.Count.EqualTo(first.Tags.Count));
        Assert.That(second.Queries, Has.Count.EqualTo(first.Queries.Count));

        for (var i = 0; i < first.Tags.Count; i++)
        {
            Assert.That(second.Tags[i].Id, Is.EqualTo(first.Tags[i].Id));
            Assert.That(second.Tags[i].CanonicalName, Is.EqualTo(first.Tags[i].CanonicalName));
            Assert.That(second.Tags[i].DomainLocalIndex, Is.EqualTo(first.Tags[i].DomainLocalIndex));
        }
    }

    [Test]
    public void Determinism_DifferentContentChangesRegistryHash()
    {
        var first = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var second = TagSystemFixtureFactory.CreateSmallGameplayBuilder()
            .RegisterTag(TagSystemFixtureFactory.EntityCondition, "Condition.Stunned")
            .CompileAndFreeze();

        Assert.That(second.RegistryHash, Is.Not.EqualTo(first.RegistryHash));
    }

    [Test]
    public void TagSet_CreateForDomainStartsEmpty()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);

        Assert.That(set.DirectMask.ToArray(), Is.All.Zero);
        Assert.That(set.ExpandedMask.ToArray(), Is.All.Zero);
    }

    [Test]
    public void TagSet_AddAssignableTagSetsDirectAndExpandedBits()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var poisonDot = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");

        Assert.That(set.Add(poisonDot), Is.True);

        Assert.That(set.HasDirect(poisonDot), Is.True);
        Assert.That(set.HasDirect(TagSystemFixtureFactory.Tag(registry, "Effect.Damage")), Is.False);
        Assert.That(set.HasExpanded(TagSystemFixtureFactory.Tag(registry, "Effect.Damage")), Is.True);
        Assert.That(set.HasExpanded(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison")), Is.True);
    }

    [Test]
    public void TagSet_AddLeafCategoryExpandsParentCategory()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var buff = TagSystemFixtureFactory.Tag(registry, "Effect.Buff");
        var shieldCapacity = TagSystemFixtureFactory.Tag(registry, "Effect.Buff.ShieldCapacity");

        Assert.That(set.Add(shieldCapacity), Is.True);
        Assert.That(set.HasDirect(shieldCapacity), Is.True);
        Assert.That(set.HasDirect(buff), Is.False);
        Assert.That(set.HasExpanded(buff), Is.True);
    }

    [Test]
    public void TagSet_AddNonAssignableTagFails()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var damage = TagSystemFixtureFactory.Tag(registry, "Effect.Damage");

        Assert.Throws<GameplayTagException>(() => set.Add(damage));
        Assert.That(set.TryAdd(damage), Is.False);
    }

    [Test]
    public void TagSet_AddDomainRootFails()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var root = TagSystemFixtureFactory.Tag(registry, "Effect");

        Assert.Throws<GameplayTagException>(() => set.Add(root));
        Assert.That(set.TryAdd(root), Is.False);
    }

    [Test]
    public void TagSet_AddDuplicateIsIdempotent()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var poisonDot = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");

        Assert.That(set.Add(poisonDot), Is.True);
        Assert.That(set.Add(poisonDot), Is.False);
        Assert.That(set.HasDirect(poisonDot), Is.True);
    }

    [Test]
    public void TagSet_RemoveChildRebuildsExpandedParents()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var poisonDot = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");
        var thermal = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Thermal");
        var damage = TagSystemFixtureFactory.Tag(registry, "Effect.Damage");
        var poison = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison");

        set.Add(poisonDot);
        set.Add(thermal);

        Assert.That(set.Remove(poisonDot), Is.True);

        Assert.That(set.HasExpanded(damage), Is.True);
        Assert.That(set.HasExpanded(poison), Is.False);
        Assert.That(set.HasDirect(poisonDot), Is.False);
    }

    [Test]
    public void TagSet_RemoveMissingTagIsDeterministicNoOp()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var dead = TagSystemFixtureFactory.Tag(registry, "Condition.Dead");

        Assert.That(set.Remove(dead), Is.False);
        Assert.That(set.TryRemove(dead), Is.False);
    }

    [Test]
    public void TagSet_ClearClearsDirectAndExpandedMasks()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var dead = TagSystemFixtureFactory.Tag(registry, "Condition.Dead");

        set.Add(dead);
        set.Clear();

        Assert.That(set.HasDirect(dead), Is.False);
        Assert.That(set.HasExpanded(dead), Is.False);
    }

    [Test]
    public void TagSet_DomainMismatchedOperationsFailStrictAndTry()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var poisonEffect = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");

        Assert.Throws<GameplayTagException>(() => conditionSet.Add(poisonEffect));
        Assert.Throws<GameplayTagException>(() => conditionSet.HasExpanded(poisonEffect));
        Assert.That(conditionSet.TryAdd(poisonEffect), Is.False);
        Assert.That(conditionSet.TryHasExpanded(poisonEffect, out var hasTag), Is.False);
        Assert.That(hasTag, Is.False);
    }

    [Test]
    public void TagSet_TryHasDirectChecksCachedIdWithoutThrowing()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var effectSet = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var poisonDot = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");
        var dead = TagSystemFixtureFactory.Tag(registry, "Condition.Dead");

        Assert.That(effectSet.TryHasDirect(poisonDot, out var hasBeforeAdd), Is.True);
        Assert.That(hasBeforeAdd, Is.False);

        effectSet.Add(poisonDot);

        Assert.That(effectSet.TryHasDirect(poisonDot, out var hasAfterAdd), Is.True);
        Assert.That(hasAfterAdd, Is.True);
        Assert.That(effectSet.TryHasDirect(dead, out var mismatchedDomainResult), Is.False);
        Assert.That(mismatchedDomainResult, Is.False);
    }

    [Test]
    public void TagSet_HasAllExpandedUsesCachedIdsForBatchContainment()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        var requiredPresentTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot")
        };
        var requiredMissingTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Immunity.Poison")
        };

        Assert.That(set.HasAllExpanded(requiredPresentTags), Is.True);
        Assert.That(set.HasAllExpanded(requiredMissingTags), Is.False);
        Assert.That(set.HasAllExpanded([]), Is.True);
    }

    [Test]
    public void TagSet_HasNoneExpandedUsesCachedIdsForBatchRejection()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        var forbiddenMissingTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Effect.Immunity.Poison"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Fire")
        };
        var forbiddenPresentTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Immunity.Poison")
        };

        Assert.That(set.HasNoneExpanded(forbiddenMissingTags), Is.True);
        Assert.That(set.HasNoneExpanded(forbiddenPresentTags), Is.False);
        Assert.That(set.HasNoneExpanded([]), Is.True);
    }

    [Test]
    public void Query_ExpandedModeMatchesParentFromChild()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.That(set.Matches(TagSystemFixtureFactory.Query(registry, "query.effect.is_damage")), Is.True);
    }

    [Test]
    public void Query_DirectModeRequiresDirectAssignment()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.That(set.Matches(TagSystemFixtureFactory.Query(registry, "query.effect.exact_damage")), Is.False);
    }

    [Test]
    public void Query_AllAnyNoneSemantics()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var query = TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal");

        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));
        Assert.That(set.Matches(query), Is.True);

        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Immunity.Poison"));
        Assert.That(set.Matches(query), Is.False);
    }

    [Test]
    public void Query_NoneOnlyRequiresMatchAllTrue()
    {
        var builder = TagSystemFixtureFactory.CreateSmallGameplayBuilder()
            .RegisterQuery(
                "query.effect.invalid_none_only",
                TagSystemFixtureFactory.EffectClassification,
                none: ["Effect.Immunity.Poison"]);

        var exception = Assert.Throws<GameplayTagValidationException>(() => builder.CompileAndFreeze());
        Assert.That(exception!.Diagnostics.Select(d => d.Code), Does.Contain("EmptyQueryRequiresMatchAll"));
    }

    [Test]
    public void Query_DomainMismatchedRuntimeMatchFailsStrictAndTry()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var effectQuery = registry.GetQuery(TagSystemFixtureFactory.Query(registry, "query.effect.is_damage"));

        Assert.Throws<GameplayTagException>(() => conditionSet.Matches(effectQuery));
        Assert.That(conditionSet.TryMatches(effectQuery, out var matches), Is.False);
        Assert.That(matches, Is.False);
    }

    [Test]
    public void Query_MatchesByIdAndByCompiledStructAreEquivalent()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var queryId = TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal");

        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.That(set.Matches(queryId), Is.EqualTo(set.Matches(registry.GetQuery(queryId))));
    }

    [Test]
    public void Query_InvalidIdIsSentinelForMissingPrecompiledQuery()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);

        Assert.That(TagQueryId.Invalid.IsValid, Is.False);
        Assert.Throws<GameplayTagException>(() => set.Matches(TagQueryId.Invalid));
        Assert.That(set.TryMatches(TagQueryId.Invalid, out var matches), Is.False);
        Assert.That(matches, Is.False);
        Assert.That(registry.TryGetQuery(TagQueryId.Invalid, out _), Is.False);
    }

    [TestCase(nameof(MeasureHasDirect))]
    [TestCase(nameof(MeasureHasExpanded))]
    [TestCase(nameof(MeasureMatchesCompiledQuery))]
    [TestCase(nameof(MeasureAddRemoveClear))]
    public void Allocation_HotPathOperationsAllocateZeroBytes(string scenario)
    {
        var allocatedBytes = scenario switch
        {
            nameof(MeasureHasDirect) => MeasureHasDirect(),
            nameof(MeasureHasExpanded) => MeasureHasExpanded(),
            nameof(MeasureMatchesCompiledQuery) => MeasureMatchesCompiledQuery(),
            nameof(MeasureAddRemoveClear) => MeasureAddRemoveClear(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown allocation scenario.")
        };

        Assert.That(allocatedBytes, Is.EqualTo(0));
    }

    private static long MeasureHasDirect()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var dead = TagSystemFixtureFactory.Tag(registry, "Condition.Dead");
        set.Add(dead);

        void Work()
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = set.HasDirect(dead);
            }
        }

        return MeasureAllocatedBytes(Work);
    }

    private static long MeasureHasExpanded()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var poisonDot = TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot");
        var damage = TagSystemFixtureFactory.Tag(registry, "Effect.Damage");
        set.Add(poisonDot);

        void Work()
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = set.HasExpanded(damage);
            }
        }

        return MeasureAllocatedBytes(Work);
    }

    private static long MeasureMatchesCompiledQuery()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        set.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));
        var query = registry.GetQuery(TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal"));

        void Work()
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = set.Matches(query);
            }
        }

        return MeasureAllocatedBytes(Work);
    }

    private static long MeasureAddRemoveClear()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var dead = TagSystemFixtureFactory.Tag(registry, "Condition.Dead");

        void Work()
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = set.Add(dead);
                _ = set.Remove(dead);
                set.Clear();
            }
        }

        return MeasureAllocatedBytes(Work);
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        action();
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
