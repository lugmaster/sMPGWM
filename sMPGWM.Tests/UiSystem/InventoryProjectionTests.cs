using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class InventoryProjectionTests
{
    [Test]
    public void Tabs_IncludeEmptyAndExtendedCategoriesInCatalogOrder()
    {
        var categories = new ItemCategories(ItemCategories.CreateDefault().All.Append(
            new ItemCategory(new ItemCategoryId("extra"), "Extra", -1, false)));
        var tabs = CategoryTab.Create(categories);
        Assert.That(tabs.Select(tab => tab.CategoryId), Is.EqualTo(
            new ItemCategoryId?[] { null }.Concat(categories.All.Select(category => (ItemCategoryId?)category.Id))));
        Assert.That(tabs[1].Title, Is.EqualTo("Extra"));
        Assert.That(tabs.Count, Is.EqualTo(10));
    }

    [TestCase(10, " engine ", 121)]
    [TestCase(128, "missing", 0)]
    [TestCase(0, "missing", 128)]
    [TestCase(10, "", 128)]
    public void Browsing_PreservesGenuineCapacityAndSourceBindings(int occupied, string search, int expected)
    {
        var resource = new ItemDefinitionId("ore");
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(Engine, "ENGINE", ItemCategories.Engine),
            new ItemDefinition(resource, "Ore", ItemCategories.Resource)
        });
        using var inventory = new InventoryImpl(catalog);
        var slots = new ItemEntity?[128];
        for (var index = 0; index < occupied; index++)
            slots[index] = catalog.CreateEntity(index < 3 ? Engine : resource);
        inventory.Install(inventory.PrepareChange(slots));
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SearchText = search };
        var cells = InventoryProjection.Create(before, catalog, state);
        Assert.That(cells.Count, Is.EqualTo(expected));
        Assert.That(cells.Where(cell => cell.Entity == null).Select(cell => cell.Address), Is.EqualTo(before.EmptyAddresses));
        Assert.That(cells.All(cell => cell.Entity == before.Slots[cell.Address] && cell.Revision == before.Revision), Is.True);
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        state.SelectedTab = 8;
        state.SearchText = "engine";
        Assert.That(InventoryProjection.Create(before, catalog, state).All(cell => cell.Entity == null), Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Sorting_ReversesOnlyNamesAndRetainsDeterministicTies(int sort)
    {
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(new("alpha_b"), "ALPHA", ItemCategories.Engine),
            new ItemDefinition(new("alpha_a"), "alpha", ItemCategories.Engine),
            new ItemDefinition(new("zulu"), "Zulu", ItemCategories.Engine)
        });
        using var inventory = new InventoryImpl(catalog);
        inventory.Add(new("zulu"), 1);
        inventory.Add(new("alpha_b"), 2);
        inventory.Add(new("alpha_a"), 2);
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SelectedSort = sort };
        var expected = before.Occupied.OrderBy(entry => entry.Entity.DefinitionId.Value, StringComparer.Ordinal)
            .ThenBy(entry => entry.Entity.Id.Value).Select(entry => entry.Entity.Id).ToArray();
        if (sort == 1) expected = expected.TakeLast(1).Concat(expected.Take(4)).ToArray();
        for (var repeat = 0; repeat < 3; repeat++)
            Assert.That(InventoryProjection.Create(before, catalog, state).Where(cell => cell.Entity != null)
                .Select(cell => cell.Entity!.Id), Is.EqualTo(expected));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    private static readonly ItemDefinitionId Engine = new("engine");
    private static readonly ItemCatalog Catalog = new(ItemCategories.CreateDefault(), new[]
    {
        new ItemDefinition(Engine, "Engine", ItemCategories.Engine)
    });

    [Test]
    public void EmptyInventory_Projects128GenuineAddresses()
    {
        using var inventory = new InventoryImpl(Catalog);
        var cells = InventoryProjection.Create(inventory.Snapshot);
        Assert.That(cells.Count, Is.EqualTo(128));
        for (var index = 0; index < cells.Count; index++)
        {
            Assert.That(cells[index].Address, Is.EqualTo(index));
            Assert.That(cells[index].Entity, Is.Null);
            Assert.That(cells[index].Revision, Is.Zero);
        }
    }

    [Test]
    public void Holes_PackOccupiedCellsThenTrueEmptiesWithoutMutatingStorage()
    {
        using var inventory = new InventoryImpl(Catalog);
        var slots = new ItemEntity?[128];
        slots[0] = Catalog.CreateEntity(Engine);
        slots[3] = Catalog.CreateEntity(Engine);
        slots[127] = Catalog.CreateEntity(Engine);
        inventory.Install(inventory.PrepareChange(slots));
        var snapshot = inventory.Snapshot;
        var cells = InventoryProjection.Create(snapshot);

        Assert.That(cells.Take(3).Select(cell => cell.Address), Is.EqualTo(new[] { 0, 3, 127 }));
        Assert.That(cells.Skip(3).Select(cell => cell.Address), Is.EqualTo(snapshot.EmptyAddresses));
        Assert.That(cells.Select(cell => cell.Address).Distinct().Count(), Is.EqualTo(128));
        foreach (var cell in cells)
        {
            Assert.That(cell.Entity, Is.SameAs(snapshot.Slots[cell.Address]));
            Assert.That(cell.Revision, Is.EqualTo(snapshot.Revision));
        }
        Assert.That(inventory.Snapshot, Is.SameAs(snapshot));
        Assert.That(snapshot.Slots[2], Is.Null);
    }

    [TestCase(128, 128)]
    [TestCase(129, 192)]
    [TestCase(257, 320)]
    public void GrowthAndFullInventories_KeepExactCapacityAndIdentity(int count, int capacity)
    {
        using var inventory = new InventoryImpl(Catalog);
        var before = InventoryProjection.Create(inventory.Snapshot);
        inventory.Add(Engine, count);
        var cells = InventoryProjection.Create(inventory.Snapshot);

        Assert.That(cells.Count, Is.EqualTo(capacity));
        Assert.That(cells.Count(cell => cell.Entity != null), Is.EqualTo(count));
        Assert.That(cells.Count(cell => cell.Entity == null), Is.EqualTo(capacity - count));
        Assert.That(before.Count, Is.EqualTo(128));
        Assert.That(before.All(cell => cell.Entity == null && cell.Revision == 0), Is.True);
        for (var index = 0; index < count; index++)
            Assert.That(cells[index].Entity, Is.SameAs(inventory.Snapshot.Slots[index]));
        Assert.Throws<NotSupportedException>(() => ((IList<InventoryCellBinding>)cells).Clear());
    }
}
