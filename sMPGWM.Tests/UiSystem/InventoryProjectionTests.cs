using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class InventoryProjectionTests
{
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
