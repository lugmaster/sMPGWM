using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class ItemCatalogTests
{
    private static readonly ItemDefinitionId EngineId = new("starter_engine");
    private static readonly ItemDefinitionId ConsumableId = new("repair_kit");

    [Test]
    public void DefaultCategories_HaveStableIdsOrderAndStackingPolicy()
    {
        var categories = ItemCategories.CreateDefault();

        Assert.That(categories.All.Select(category => category.Id.Value), Is.EqualTo(new[]
        {
            "engine", "hull", "shield_generator", "reactor", "light_weapon", "heavy_weapon",
            "consumable", "resource"
        }));
        Assert.That(categories.All.Where(category => category.CanStack).Select(category => category.Id),
            Is.EqualTo(new[] { ItemCategories.Consumable, ItemCategories.Resource }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Ids_RejectBlankValues(string? value)
    {
        Assert.Throws<ArgumentException>(() => new ItemCategoryId(value!));
        Assert.Throws<ArgumentException>(() => new ItemDefinitionId(value!));
    }

    [Test]
    public void DefinitionsAndCategories_RejectDefaultIdsAndBlankNames()
    {
        Assert.Throws<ArgumentException>(() => new ItemCategory(default, "Engine", 0, false));
        Assert.Throws<ArgumentException>(() => new ItemCategory(ItemCategories.Engine, " ", 0, false));
        Assert.Throws<ArgumentException>(() => new ItemDefinition(default, "Engine", ItemCategories.Engine));
        Assert.Throws<ArgumentException>(() => new ItemDefinition(EngineId, "Engine", default));
        Assert.Throws<ArgumentException>(() => new ItemDefinition(EngineId, " ", ItemCategories.Engine));
    }

    [Test]
    public void Catalogs_RejectDuplicateIdsAndMissingCategories()
    {
        var category = new ItemCategory(ItemCategories.Engine, "Engine", 0, false);
        Assert.Throws<ArgumentException>(() => new ItemCategories(new[] { category, category }));

        var definition = new ItemDefinition(EngineId, "Engine", ItemCategories.Engine);
        Assert.Throws<ArgumentException>(() => new ItemCatalog(ItemCategories.CreateDefault(),
            new[] { definition, definition }));
        Assert.Throws<ArgumentException>(() => new ItemCatalog(new ItemCategories(Array.Empty<ItemCategory>()),
            new[] { definition }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Definition_RejectsNonpositiveExplicitLimits(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ItemDefinition(ConsumableId, "Repair Kit", ItemCategories.Consumable, maximumStackSize: limit));
    }

    [Test]
    public void Catalog_RejectsContradictoryNonstackableLimit()
    {
        var definition = new ItemDefinition(EngineId, "Engine", ItemCategories.Engine, maximumStackSize: 2);

        Assert.Throws<ArgumentException>(() => new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition }));
    }

    [TestCase("engine", null, 1)]
    [TestCase("engine", 1, 1)]
    [TestCase("consumable", null, int.MaxValue)]
    [TestCase("consumable", 10, 10)]
    [TestCase("resource", null, int.MaxValue)]
    [TestCase("resource", 1, 1)]
    public void StackLimit_UsesCategoryAndOptionalDefinitionLimit(string categoryId, int? configured, int expected)
    {
        var definition = new ItemDefinition(ConsumableId, "Item", new ItemCategoryId(categoryId),
            maximumStackSize: configured);
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition });

        Assert.That(catalog.GetStackLimit(ConsumableId), Is.EqualTo(expected));
    }

    [Test]
    public void StackCompatibility_RequiresSameStackableDefinition()
    {
        var otherConsumableId = new ItemDefinitionId("fuel_cell");
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(EngineId, "Engine", ItemCategories.Engine),
            new ItemDefinition(ConsumableId, "Repair Kit", ItemCategories.Consumable),
            new ItemDefinition(otherConsumableId, "Fuel Cell", ItemCategories.Consumable)
        });

        Assert.That(catalog.CanStackTogether(ConsumableId, ConsumableId), Is.True);
        Assert.That(catalog.CanStackTogether(ConsumableId, otherConsumableId), Is.False);
        Assert.That(catalog.CanStackTogether(EngineId, EngineId), Is.False);
    }

    [Test]
    public void Entities_HaveDistinctStableIdsAndSharedDefinition()
    {
        var catalog = CreateConsumableCatalog();
        var first = catalog.CreateEntity(ConsumableId, 7);
        var second = catalog.CreateEntity(ConsumableId, 8);
        var originalId = first.Id;

        for (var refresh = 0; refresh < 3; refresh++)
            catalog.GetTooltip(first.DefinitionId);

        Assert.That(first.Id, Is.EqualTo(originalId));
        Assert.That(first.Id.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(second.Id, Is.Not.EqualTo(first.Id));
        Assert.That(second.DefinitionId, Is.EqualTo(first.DefinitionId));
        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(8));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(11)]
    public void Entities_RejectCountsOutsideEffectiveLimit(int count)
    {
        var catalog = CreateConsumableCatalog(10);

        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.CreateEntity(ConsumableId, count));
    }

    [Test]
    public void Entities_RespectNonstackableAndUnlimitedBoundaries()
    {
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(EngineId, "Engine", ItemCategories.Engine),
            new ItemDefinition(ConsumableId, "Repair Kit", ItemCategories.Consumable)
        });

        Assert.That(catalog.CreateEntity(EngineId).Count, Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.CreateEntity(EngineId, 2));
        Assert.That(catalog.CreateEntity(ConsumableId, int.MaxValue).Count, Is.EqualTo(int.MaxValue));
        Assert.Throws<ArgumentException>(() => catalog.CreateEntity(new ItemDefinitionId("missing")));
    }

    [TestCase(null, int.MaxValue)]
    [TestCase(10, 10)]
    public void Tooltip_PreservesDescriptionAndConfiguredVersusEffectiveLimit(int? configured, int effective)
    {
        var catalog = CreateConsumableCatalog(configured);

        Assert.That(catalog.GetTooltip(ConsumableId), Is.EqualTo(new ItemTooltipData(
            "Repair Kit", "Consumable", "Repairs the ship.", true, effective, configured)));
    }

    [Test]
    public void Catalogs_CopyInputsAndExposeReadOnlyCollections()
    {
        var categoryInput = ItemCategories.CreateDefault().All.ToList();
        var categories = new ItemCategories(categoryInput);
        var definitionInput = new List<ItemDefinition>
        {
            new(EngineId, "Engine", ItemCategories.Engine)
        };
        var catalog = new ItemCatalog(categories, definitionInput);
        categoryInput.Clear();
        definitionInput.Clear();

        Assert.That(categories.All, Has.Count.EqualTo(8));
        Assert.That(catalog.Get(EngineId).DisplayName, Is.EqualTo("Engine"));
        Assert.Throws<NotSupportedException>(() => ((IList<ItemCategory>)categories.All).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ItemDefinition>)catalog.Definitions).Clear());
    }

    [Test]
    public void AddedCategory_IsDiscoverableAndUsesExistingStackRules()
    {
        var categoryId = new ItemCategoryId("artifact");
        var categories = new ItemCategories(ItemCategories.CreateDefault().All.Append(
            new ItemCategory(categoryId, "Artifact", 8, false, "res://artifact.png")));
        var artifactId = new ItemDefinitionId("ancient_artifact");
        var catalog = new ItemCatalog(categories, new[]
        {
            new ItemDefinition(artifactId, "Ancient Artifact", categoryId)
        });

        Assert.That(categories.All.Last().Id, Is.EqualTo(categoryId));
        Assert.That(catalog.GetStackLimit(artifactId), Is.EqualTo(1));
        Assert.That(catalog.GetTooltip(artifactId).CategoryName, Is.EqualTo("Artifact"));
    }

    private static ItemCatalog CreateConsumableCatalog(int? limit = null) => new(
        ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(ConsumableId, "Repair Kit", ItemCategories.Consumable,
                "Repairs the ship.", maximumStackSize: limit)
        });
}
