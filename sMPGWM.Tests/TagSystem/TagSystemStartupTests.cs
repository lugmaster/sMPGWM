using sMPGWM.Scripts.TagSystem;
using sMPGWM.Scripts.TagSystem.Exceptions;
using sMPGWM.Scripts.TagSystem.Ids;
using sMPGWM.Scripts.TagSystem.Queries;

namespace sMPGWM.Tests.TagSystem;

[TestFixture]
public class TagSystemStartupTests
{
    [Test]
    public void Registry_CompilesSmallGameplayFixture()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.That(registry.Domains, Has.Count.EqualTo(8));
        Assert.That(registry.Tags, Has.Count.GreaterThan(40));
        Assert.That(registry.RegistryHash, Is.Not.EqualTo(0UL));
        Assert.That(registry.TryGetTagId("Effect.Damage.Poison.Dot", out var poisonDot), Is.True);
        Assert.That(poisonDot.IsValid, Is.True);
    }

    [Test]
    public void Registry_RegistersDomainsAtStartup()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.ActionClassification).Value, Is.EqualTo(1));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.CooldownGroup).Value, Is.EqualTo(2));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.DamageKind).Value, Is.EqualTo(3));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.EffectClassification).Value, Is.EqualTo(4));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.EntityCondition).Value, Is.EqualTo(5));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.FactionRelation).Value, Is.EqualTo(6));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.ItemClassification).Value, Is.EqualTo(7));
        Assert.That(registry.GetDomainId(TagSystemFixtureFactory.TargetClassification).Value, Is.EqualTo(8));
    }

    [Test]
    public void Registry_ResolvesDomainIdByRootPrefix()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.That(
            registry.GetDomainIdByRootPrefix("Effect"),
            Is.EqualTo(registry.GetDomainId(TagSystemFixtureFactory.EffectClassification)));
        Assert.That(
            registry.GetDomainIdByRootPrefix("Condition"),
            Is.EqualTo(registry.GetDomainId(TagSystemFixtureFactory.EntityCondition)));
        Assert.Throws<TagException>(() => registry.GetDomainIdByRootPrefix("Unknown"));
    }

    [Test]
    public void Registry_RejectsDuplicateDomainKey()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterDomain(TagSystemFixtureFactory.EntityCondition, "Condition2", "Duplicate key.");

        AssertValidationCode(builder, "DuplicateDomainKey");
    }

    [Test]
    public void Registry_ValidationExceptionMessageSummarizesDiagnostics()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterDomain(TagSystemFixtureFactory.EntityCondition, "Condition2", "Duplicate key.");

        var exception = Assert.Throws<TagValidationException>(() => builder.CompileAndFreeze());

        Assert.That(exception!.Message, Does.Contain("Gameplay tag validation failed"));
        Assert.That(exception.Message, Does.Contain("DuplicateDomainKey"));
        Assert.That(exception.Message, Does.Contain("Domains[8].DomainKey"));
    }

    [Test]
    public void Registry_RejectsDuplicateRootPrefix()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterDomain("ConditionMirror", "Condition", "Duplicate root.");

        AssertValidationCode(builder, "DuplicateRootPrefix");
    }

    [Test]
    public void Registry_RejectsUnknownDomain()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterTag("MissingDomain", "Missing.Tag");

        AssertValidationCode(builder, "UnknownDomain");
    }

    [Test]
    public void Registry_RejectsDuplicateCanonicalName()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterTag(TagSystemFixtureFactory.EntityCondition, "Condition.Dead")
            .RegisterTag(TagSystemFixtureFactory.EntityCondition, "Condition.Dead");

        AssertValidationCode(builder, "DuplicateCanonicalName");
    }

    [Test]
    public void Registry_RejectsMissingDotPathParents()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterTag(TagSystemFixtureFactory.EffectClassification, "Effect.Damage.Poison.Dot");

        var exception = Assert.Throws<TagValidationException>(() => builder.CompileAndFreeze());

        Assert.That(exception!.Diagnostics.Select(d => d.Code), Does.Contain("MissingParentTag"));
        Assert.That(exception.Diagnostics.Select(d => d.InvalidValue), Does.Contain("Effect.Damage"));
        Assert.That(exception.Diagnostics.Select(d => d.InvalidValue), Does.Contain("Effect.Damage.Poison"));
    }

    [Test]
    public void Registry_RejectsShortNames()
    {
        var builder = TagSystemFixtureFactory.CreateDomainOnlyBuilder()
            .RegisterTag(TagSystemFixtureFactory.EntityCondition, "Dead");

        AssertValidationCode(builder, "ShortNamesDisabled");
    }

    [TestCase("All")]
    [TestCase("Any")]
    [TestCase("None")]
    public void Query_RejectsUnknownCanonicalSelectors(string clause)
    {
        var builder = TagSystemFixtureFactory.CreateSmallGameplayBuilder();
        string[] selectors = ["Condition.Unknown"];
        builder.RegisterQuery("query.unknown", TagSystemFixtureFactory.EntityCondition,
            all: clause == "All" ? selectors : null,
            any: clause == "Any" ? selectors : null,
            none: clause == "None" ? selectors : null,
            matchAll: true);

        var exception = Assert.Throws<TagValidationException>(() => builder.CompileAndFreeze());

        Assert.That(exception!.Diagnostics, Has.Some.Matches<TagValidationDiagnostic>(diagnostic =>
            diagnostic.Code == "UnknownTag" &&
            diagnostic.FieldPath.EndsWith($".{clause}[0]", StringComparison.Ordinal) &&
            diagnostic.InvalidValue == "Condition.Unknown"));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void Query_RejectsInvalidMatchMode(int matchMode)
    {
        var builder = new TagRegistryBuilder()
            .RegisterDomain("Conditions", "Condition", "Test conditions.")
            .RegisterQuery("query.invalid", "Conditions", matchMode: (TagQueryMatchMode)matchMode, matchAll: true);

        var exception = Assert.Throws<TagValidationException>(() => builder.CompileAndFreeze());

        Assert.That(exception!.Diagnostics, Has.Some.Matches<TagValidationDiagnostic>(diagnostic =>
            diagnostic.Code == "InvalidMatchMode" &&
            diagnostic.FieldPath == "Queries[0].MatchMode"));
    }

    [Test]
    public void Registry_MissingNamesFailStrictAndTryLookups()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.Throws<TagException>(() => registry.GetDomainId("Unknown"));
        Assert.That(registry.TryGetDomainId("Unknown", out var domainId), Is.False);
        Assert.That(domainId, Is.EqualTo(TagDomainId.Invalid));
        Assert.Throws<TagException>(() => registry.GetTagId("Condition.Unknown"));
        Assert.That(registry.TryGetTagId("Condition.Unknown", out var tagId), Is.False);
        Assert.That(tagId, Is.EqualTo(TagId.Invalid));
        Assert.Throws<TagException>(() => registry.GetQueryId("query.unknown"));
        Assert.That(registry.TryGetQueryId("query.unknown", out var queryId), Is.False);
        Assert.That(queryId, Is.EqualTo(TagQueryId.Invalid));
    }

    [TestCase(0)]
    [TestCase(65535)]
    public void Registry_InvalidIdsFailThroughPublicOperations(int value)
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var set = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var domainId = new TagDomainId((ushort)value);
        var tagId = new TagId((ushort)value);
        var queryId = new TagQueryId((ushort)value);

        Assert.Throws<TagException>(() => registry.GetDomain(domainId));
        Assert.Throws<TagException>(() => registry.CreateTagSet(domainId));
        Assert.Throws<TagException>(() => registry.GetTag(tagId));
        Assert.Throws<TagException>(() => registry.GetQuery(queryId));
        Assert.Throws<TagException>(() => set.Add(tagId));
        Assert.Throws<TagException>(() => set.Remove(tagId));
        Assert.Throws<TagException>(() => set.HasDirect(tagId));
        Assert.Throws<TagException>(() => set.HasExpanded(tagId));
        Assert.That(set.TryAdd(tagId), Is.False);
        Assert.That(set.TryRemove(tagId), Is.False);
        Assert.That(set.TryHasDirect(tagId, out var hasDirect), Is.False);
        Assert.That(hasDirect, Is.False);
        Assert.That(set.TryHasExpanded(tagId, out var hasExpanded), Is.False);
        Assert.That(hasExpanded, Is.False);
        Assert.Throws<TagException>(() => set.Matches(queryId));
        Assert.That(set.TryMatches(queryId, out var matches), Is.False);
        Assert.That(matches, Is.False);
    }

    [Test]
    public void Registry_FreezesAfterCompilation()
    {
        var builder = TagSystemFixtureFactory.CreateSmallGameplayBuilder();
        var registry = builder.CompileAndFreeze();

        Assert.That(registry.RegistryHash, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Registry_PreventsMutationAfterFreeze()
    {
        var builder = TagSystemFixtureFactory.CreateSmallGameplayBuilder();
        builder.CompileAndFreeze();

        Assert.Throws<TagException>(() =>
            builder.RegisterTag(TagSystemFixtureFactory.EntityCondition, "Condition.New"));
    }

    [Test]
    public void Registry_ReportsDomainMaskSizes()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var effectDomain = registry.GetDomain(registry.GetDomainId(TagSystemFixtureFactory.EffectClassification));

        Assert.That(effectDomain.CanonicalTagCount, Is.GreaterThan(0));
        Assert.That(effectDomain.AssignableTagCount, Is.GreaterThan(0));
        Assert.That(effectDomain.DomainMaskWordCount, Is.EqualTo((effectDomain.CanonicalTagCount + 63) / 64));
        Assert.That(effectDomain.DomainMaskBitCapacity, Is.EqualTo(effectDomain.DomainMaskWordCount * 64));
        Assert.That(effectDomain.DomainMaskByteSize, Is.EqualTo(effectDomain.DomainMaskWordCount * sizeof(ulong)));
    }

    [Test]
    public void Registry_ReportsAssignableAndNonAssignableTagCounts()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var effectDomain = registry.GetDomain(registry.GetDomainId(TagSystemFixtureFactory.EffectClassification));

        Assert.That(effectDomain.CanonicalTagCount, Is.EqualTo(23));
        Assert.That(effectDomain.AssignableTagCount, Is.EqualTo(18));
        Assert.That(effectDomain.NonAssignableTagCount, Is.EqualTo(5));
        Assert.That(effectDomain.CanonicalTagCount, Is.EqualTo(effectDomain.AssignableTagCount + effectDomain.NonAssignableTagCount));
    }

    [Test]
    public void Registry_ParentCategoriesDefaultNonAssignable()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        Assert.That(registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect.Damage")).IsAssignable, Is.False);
        Assert.That(registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect.Buff")).IsAssignable, Is.False);
        Assert.That(registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect")).IsAssignable, Is.False);
    }

    [Test]
    public void Registry_MarksOnlyDomainRootTagsAsRoot()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();

        var effectRoot = registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect"));
        var effectParent = registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect.Damage"));
        var effectLeaf = registry.GetTag(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.That(effectRoot.IsRoot, Is.True);
        Assert.That(effectRoot.ParentId, Is.EqualTo(TagId.Invalid));
        Assert.That(effectParent.IsRoot, Is.False);
        Assert.That(effectLeaf.IsRoot, Is.False);
    }

    private static void AssertValidationCode(TagRegistryBuilder builder, string expectedCode)
    {
        var exception = Assert.Throws<TagValidationException>(() => builder.CompileAndFreeze());
        Assert.That(exception!.Diagnostics.Select(d => d.Code), Does.Contain(expectedCode));
    }
}
