using sMPGWM.Scripts.Util.Mocks;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
[Category("Godot")]
public class InventoryProjectionTests
{
    [Test]
    public void Tabs_FollowLoadedCategoryOrderAndIncludeEmptyCategories()
    {
        var id = "mock_standard_engine";
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, id, 1);
        var state = new InventoryViewState { SelectedTab = 1 };
        Assert.That(InventoryProjection.Create(inventory.Snapshot, state)[0].Entity?.DefinitionId, Is.EqualTo(id));
        state.SelectedTab = 2;
        Assert.That(InventoryProjection.Create(inventory.Snapshot, state).Count, Is.EqualTo(127));
    }

    [TestCase(10, " engine ", 121)]
    [TestCase(128, "missing", 0)]
    [TestCase(0, "missing", 128)]
    [TestCase(10, "", 128)]
    public void Browsing_PreservesGenuineCapacityAndSourceBindings(int occupied, string search, int expected)
    {
        var resource = "mock_standard_resource";
        using var inventory = new InventoryController();
        for (var index = 0; index < occupied; index++)
            ItemFixture.Place(inventory.Items, inventory.Container, index, index < 3 ? Engine : resource);
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SearchText = search };
        var cells = InventoryProjection.Create(before, state);
        Assert.That(cells.Count, Is.EqualTo(expected));
        Assert.That(cells.Where(cell => cell.Entity == null).Select(cell => cell.Address), Is.EqualTo(before.EmptyAddresses));
        Assert.That(cells.All(cell => cell.Entity == before.Slots[cell.Address] && cell.Revision == before.Revision), Is.True);
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        state.SelectedTab = 8;
        state.SearchText = "engine";
        Assert.That(InventoryProjection.Create(before, state).All(cell => cell.Entity == null), Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Sorting_ReversesOnlyNamesAndRetainsDeterministicTies(int sort)
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, "mock_standard_engine", 1);
        inventory.Items.CreateItems(inventory.Container, "mock_advanced_engine", 2);
        inventory.Items.CreateItems(inventory.Container, "mock_standard_hull", 2);
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SelectedSort = sort };
        var ordered = sort == 0
            ? before.Occupied.OrderBy(entry => entry.Entity.Definition.DisplayName, StringComparer.OrdinalIgnoreCase)
            : before.Occupied.OrderByDescending(entry => entry.Entity.Definition.DisplayName, StringComparer.OrdinalIgnoreCase);
        var expected = ordered.ThenBy(entry => entry.Entity.DefinitionId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Entity.Id.Value).Select(entry => entry.Entity.Id).ToArray();
        for (var repeat = 0; repeat < 3; repeat++)
            Assert.That(InventoryProjection.Create(before, state).Where(cell => cell.Entity != null)
                .Select(cell => cell.Entity!.Id), Is.EqualTo(expected));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    private static readonly string Engine = "mock_standard_engine";

    [Test]
    public void EmptyInventory_Projects128GenuineAddresses()
    {
        using var inventory = new InventoryController();
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
        using var inventory = new InventoryController();
        ItemFixture.Place(inventory.Items, inventory.Container, 0, Engine);
        ItemFixture.Place(inventory.Items, inventory.Container, 3, Engine);
        ItemFixture.Place(inventory.Items, inventory.Container, 127, Engine);
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
        using var inventory = new InventoryController();
        var before = InventoryProjection.Create(inventory.Snapshot);
        inventory.Items.CreateItems(inventory.Container, Engine, count);
        var cells = InventoryProjection.Create(inventory.Snapshot);

        Assert.That(cells.Count, Is.EqualTo(capacity));
        Assert.That(cells.Count(cell => cell.Entity != null), Is.EqualTo(count));
        Assert.That(cells.Count(cell => cell.Entity == null), Is.EqualTo(capacity - count));
        Assert.That(before.Count, Is.EqualTo(128));
        Assert.That(before.All(cell => cell.Entity == null && cell.Revision == 0), Is.True);
        for (var index = 0; index < count; index++)
            Assert.That(cells[index].Entity, Is.SameAs(inventory.Snapshot.Slots[index]));
        Assert.Throws<NotSupportedException>(() => ((IList<ItemCellBinding>)cells).Clear());
    }
}
