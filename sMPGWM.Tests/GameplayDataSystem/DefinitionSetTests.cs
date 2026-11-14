using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Tests.GameplayDataSystem;

public sealed class DefinitionSetTests
{
    [Test]
    public void OwnsInputArrayAndPreservesPublishedOrderAndLookup()
    {
        var first = new ConditionDefinition("first", "First", "First condition.");
        var second = new ConditionDefinition("second", "Second", "Second condition.");
        var input = new[] { first, second };
        var definitions = new DefinitionSet<ConditionDefinition>(input, "Conditions.yml");

        input[0] = new ConditionDefinition("replacement", "Replacement", "Replacement condition.");
        input[1] = first;

        Assert.That(definitions.All, Has.Count.EqualTo(2));
        Assert.That(definitions.All[0], Is.SameAs(first));
        Assert.That(definitions.All[1], Is.SameAs(second));
        Assert.That(definitions.GetById("first"), Is.SameAs(first));
        Assert.That(definitions.GetById("second"), Is.SameAs(second));
        Assert.That(definitions.TryGetById("replacement", out _), Is.False);
        Assert.Throws<NotSupportedException>(() => ((IList<ConditionDefinition>)definitions.All)[0] = input[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<ConditionDefinition>)definitions.All).Clear());
    }

    [Test]
    public void IdLookupUsesOrdinalCaseSensitiveIdentity()
    {
        var lower = new ConditionDefinition("shield", "Lower", "Lower case identity.");
        var upper = new ConditionDefinition("Shield", "Upper", "Upper case identity.");
        var definitions = new DefinitionSet<ConditionDefinition>(new[] { lower, upper }, "Conditions.yml");

        Assert.That(definitions.GetById("shield"), Is.SameAs(lower));
        Assert.That(definitions.TryGetById("Shield", out var found), Is.True);
        Assert.That(found, Is.SameAs(upper));
        Assert.That(definitions.TryGetById("SHIELD", out _), Is.False);
        Assert.Throws<KeyNotFoundException>(() => definitions.GetById("SHIELD"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n")]
    public void BlankIdsCannotEnterACompletedSet(string? id)
    {
        var input = new[] { new ConditionDefinition(id!, "Invalid", "Invalid identity.") };
        var error = Assert.Throws<ArgumentException>(() => new DefinitionSet<ConditionDefinition>(input, "Conditions.yml"));
        Assert.That(error!.Message, Does.Contain("Conditions.yml"));
    }

    [Test]
    public void DuplicateIdsCannotReplaceAnEarlierDefinition()
    {
        var input = new[]
        {
            new ConditionDefinition("same", "First", "First condition."),
            new ConditionDefinition("same", "Second", "Second condition.")
        };
        var error = Assert.Throws<ArgumentException>(() => new DefinitionSet<ConditionDefinition>(input, "Conditions.yml"));
        Assert.That(error!.Message, Does.Contain("Conditions.yml").And.Contain("same"));
    }

    [Test]
    public void EmptySetEnumeratesWithoutInventingDefinitions()
    {
        var definitions = new DefinitionSet<ConditionDefinition>(Array.Empty<ConditionDefinition>(), "Conditions.yml");
        Assert.That(definitions.All, Is.Empty);
        Assert.That(definitions.TryGetById("missing", out _), Is.False);
        Assert.Throws<KeyNotFoundException>(() => definitions.GetById("missing"));
    }

    [Test]
    public void SourceProvenanceSurvivesMutationOfTheInputMap()
    {
        var original = new CatalogueSource("Conditions.yml", "ConditionSchema.yml", "Conditions");
        var sources = new Dictionary<string, CatalogueSource>(StringComparer.Ordinal) { ["shield"] = original };
        var input = new[] { new ConditionDefinition("shield", "Shield", "Shield condition.") };
        var definitions = new DefinitionSet<ConditionDefinition>(input, "Conditions.yml", sources);

        sources["shield"] = new CatalogueSource("replacement.yml", "replacement.schema.yml", "Conditions");
        sources.Clear();

        Assert.That(definitions.SourceFor("shield"), Is.EqualTo(original));
        Assert.That(definitions.SourceFor("shield")!.UserPath, Is.EqualTo("user://config/gameplay/Conditions.yml"));
    }
}
