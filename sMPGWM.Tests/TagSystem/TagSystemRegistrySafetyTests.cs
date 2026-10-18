using sMPGWM.Scripts.TagSystem;
using sMPGWM.Scripts.TagSystem.Exceptions;

namespace sMPGWM.Tests.TagSystem;

[TestFixture]
public class TagSystemRegistrySafetyTests
{
    [TestCase("All", 0)]
    [TestCase("All", 65)]
    [TestCase("Any", 0)]
    [TestCase("Any", 65)]
    [TestCase("None", 0)]
    [TestCase("None", 65)]
    public void RegistryHash_ChangesWhenQueryTagChanges(string clause, int tagIndex)
    {
        TagRegistry Build(int selectedIndex)
        {
            var builder = CreateBuilder(70);
            string[] tags = [$"Condition.Tag{selectedIndex:D3}"];
            builder.RegisterQuery("query.test", "Conditions",
                all: clause == "All" ? tags : null,
                any: clause == "Any" ? tags : null,
                none: clause == "None" ? tags : null,
                matchAll: clause == "None");
            return builder.CompileAndFreeze();
        }

        var first = Build(tagIndex);
        var second = Build(tagIndex + 1);
        Assert.That(second.RegistryHash, Is.Not.EqualTo(first.RegistryHash));
    }

    [Test]
    public void RegistryHash_AndIdsIgnoreRegistrationOrder()
    {
        TagRegistry Build(bool reverse)
        {
            var builder = new TagRegistryBuilder();
            string[] domains = reverse ? ["Effects", "Conditions"] : ["Conditions", "Effects"];
            foreach (var domain in domains)
            {
                var root = domain == "Conditions" ? "Condition" : "Effect";
                builder.RegisterDomain(domain, root, "Test domain.");
                string[] leaves = reverse ? ["B", "A"] : ["A", "B"];
                foreach (var leaf in leaves)
                {
                    builder.RegisterTag(domain, $"{root}.{leaf}");
                    builder.RegisterQuery($"query.{root}.{leaf}", domain, all: [$"{root}.{leaf}"]);
                }
            }

            return builder.CompileAndFreeze();
        }

        var first = Build(false);
        var second = Build(true);
        Assert.That(second.RegistryHash, Is.EqualTo(first.RegistryHash));
        Assert.That(second.Domains.Select(d => (d.Id, d.DomainKey)),
            Is.EqualTo(first.Domains.Select(d => (d.Id, d.DomainKey))));
        Assert.That(second.Tags.Select(t => (t.Id, t.CanonicalName, t.DomainId, t.DomainLocalIndex)),
            Is.EqualTo(first.Tags.Select(t => (t.Id, t.CanonicalName, t.DomainId, t.DomainLocalIndex))));
        Assert.That(second.Queries.Select(q => (q.Id, q.QueryKey)),
            Is.EqualTo(first.Queries.Select(q => (q.Id, q.QueryKey))));
    }

    [TestCase(2, 2, false)]
    [TestCase(2, 2, true)]
    [TestCase(70, 2, false)]
    [TestCase(2, 70, false)]
    public void Query_RejectsForeignRegistryBeforeReadingMasks(int localCount, int foreignCount, bool shiftBits)
    {
        var local = CreateBuilder(localCount)
            .RegisterQuery("query.test", "Conditions", all: ["Condition.Tag000"])
            .CompileAndFreeze();
        var foreignBuilder = CreateBuilder(foreignCount);
        if (shiftBits)
        {
            foreignBuilder.RegisterTag("Conditions", "Condition.Aardvark");
        }

        var foreign = foreignBuilder
            .RegisterQuery("query.test", "Conditions", all: ["Condition.Tag000"])
            .CompileAndFreeze();
        var set = local.CreateTagSet("Conditions");
        set.Add(local.GetTagId("Condition.Tag000"));
        var query = foreign.GetQuery(foreign.GetQueryId("query.test"));
        Assert.That(query.DomainId, Is.EqualTo(set.DomainId));
        if (localCount == foreignCount && !shiftBits)
        {
            Assert.That(foreign.RegistryHash, Is.EqualTo(local.RegistryHash));
        }

        Assert.Throws<TagException>(() => set.Matches(query));
        Assert.That(set.TryMatches(query, out var matches), Is.False);
        Assert.That(matches, Is.False);
        var localQueryId = local.GetQueryId("query.test");
        Assert.That(set.Matches(localQueryId), Is.True);
        Assert.That(set.Matches(local.GetQuery(localQueryId)), Is.True);
        Assert.That(set.TryMatches(local.GetQuery(localQueryId), out matches), Is.True);
        Assert.That(matches, Is.True);
    }

    [Test]
    public void Registry_CollectionsCannotMutateCompiledTables()
    {
        var registry = CreateBuilder(2)
            .RegisterQuery("query.test", "Conditions", all: ["Condition.Tag000"])
            .CompileAndFreeze();
        var originalHash = registry.RegistryHash;
        var domains = registry.Domains;
        var tags = registry.Tags;
        var queries = registry.Queries;
        AssertReadOnly(registry.Domains);
        AssertReadOnly(registry.Tags);
        AssertReadOnly(registry.Queries);
        Assert.That(registry.GetDomain(registry.Domains[0].Id), Is.SameAs(registry.Domains[0]));
        Assert.That(registry.GetTag(registry.Tags[0].Id), Is.SameAs(registry.Tags[0]));
        Assert.That(registry.GetQuery(registry.Queries[0].Id), Is.SameAs(registry.Queries[0]));
        Assert.That(registry.RegistryHash, Is.EqualTo(originalHash));
        Assert.That(registry.Domains, Is.SameAs(domains));
        Assert.That(registry.Tags, Is.SameAs(tags));
        Assert.That(registry.Queries, Is.SameAs(queries));
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> items)
    {
        var original = items.ToArray();
        Assert.That(items, Is.Not.InstanceOf<T[]>());
        var list = (IList<T>)items;
        Assert.That(list.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => list[0] = default!);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.That(items, Is.EqualTo(original));
    }

    private static TagRegistryBuilder CreateBuilder(int tagCount)
    {
        var builder = new TagRegistryBuilder()
            .RegisterDomain("Conditions", "Condition", "Test conditions.");
        for (var i = 0; i < tagCount; i++)
        {
            builder.RegisterTag("Conditions", $"Condition.Tag{i:D3}");
        }

        return builder;
    }
}
