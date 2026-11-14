using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ItemStoreTransactionTests
{
    [Test]
    public void DomainPreparationFailurePreservesContainersAndIndexes()
    {
        var store = new ItemStore();
        var source = store.CreateContainer(1);
        var destination = store.CreateContainer(1);
        Assert.That(store.CreateItems(source, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        var id = source.Slots[0]!.Value;
        var item = store.GetItem(id);
        var from = new ItemLocation(source.Id, 0);
        var to = new ItemLocation(destination.Id, 0);
        var sourceSlots = source.Slots;
        var destinationSlots = destination.Slots;
        var sourceRevision = source.Revision;
        var destinationRevision = destination.Revision;
        var installed = 0;
        var published = 0;
        source.PrepareChange = _ => () => installed++;
        destination.PrepareChange = static _ => throw new InvalidOperationException("Reject proposed domain state.");
        source.Changed += () => published++;
        destination.Changed += () => published++;
        Assert.That(store.TransferItem(id, from, to, sourceRevision, destinationRevision), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(store.GetItem(id), Is.SameAs(item));
        Assert.That(store.GetLocation(id), Is.EqualTo(from));
        Assert.That(source.Slots, Is.SameAs(sourceSlots));
        Assert.That(destination.Slots, Is.SameAs(destinationSlots));
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(destination.Revision, Is.EqualTo(destinationRevision));
        Assert.That(installed, Is.Zero);
        Assert.That(published, Is.Zero);
        destination.PrepareChange = null;
        Assert.That(store.TransferItem(id, from, to, sourceRevision, destinationRevision), Is.EqualTo(TransferResult.Success));
        Assert.That(store.GetLocation(id), Is.EqualTo(to));
        Assert.That(store.GetItem(id), Is.SameAs(item));
        Assert.That(source.Slots[0], Is.Null);
        Assert.That(destination.Slots[0], Is.EqualTo(id));
        Assert.That(installed, Is.EqualTo(1));
        Assert.That(published, Is.EqualTo(2));
    }

    [Test]
    public void ConfirmationRejectsDuplicateIdentitiesAcrossChangedAndRetainedContainers()
    {
        var store = new ItemStore();
        var source = store.CreateContainer(1);
        var destination = store.CreateContainer(1);
        var retained = store.CreateContainer(1);
        Assert.That(store.CreateItems(source, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(store.CreateItems(retained, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        var original = store.GetItem(source.Slots[0]!.Value);
        var other = store.GetItem(retained.Slots[0]!.Value);
        var sourceSlots = source.Slots;
        var destinationSlots = destination.Slots;
        var retainedSlots = retained.Slots;
        var sourceRevision = source.Revision;
        Assert.That(store.Confirm(new[]
        {
            new ItemContainerConfirmation(source, new ItemEntity?[] { other }, sourceRevision + 1),
            new ItemContainerConfirmation(destination, new ItemEntity?[1], destination.Revision + 1)
        }), Is.False);
        Assert.That(store.Confirm(new[]
        {
            new ItemContainerConfirmation(source, new ItemEntity?[] { original }, sourceRevision + 1),
            new ItemContainerConfirmation(destination, new ItemEntity?[] { original }, destination.Revision + 1)
        }), Is.False);
        Assert.That(source.Slots, Is.SameAs(sourceSlots));
        Assert.That(destination.Slots, Is.SameAs(destinationSlots));
        Assert.That(retained.Slots, Is.SameAs(retainedSlots));
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(destination.Revision, Is.Zero);
        Assert.That(store.GetItem(original.Id), Is.SameAs(original));
        Assert.That(store.GetItem(other.Id), Is.SameAs(other));
        Assert.That(store.GetLocation(original.Id), Is.EqualTo(new ItemLocation(source.Id, 0)));
        Assert.That(store.GetLocation(other.Id), Is.EqualTo(new ItemLocation(retained.Id, 0)));
    }

    [Test]
    public void TransferAllocationDoesNotGrowWithUnrelatedItems()
    {
        var store = new ItemStore();
        var source = store.CreateContainer(1);
        var destination = store.CreateContainer(1);
        Assert.That(store.CreateItems(source, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        var baseline = MeasureTransfers(store, source, destination);
        var unrelated = store.CreateContainer(2048);
        Assert.That(store.CreateItems(unrelated, "mock_standard_engine", 2048), Is.EqualTo(InventoryResult.Success));
        var populated = MeasureTransfers(store, source, destination);
        TestContext.Out.WriteLine($"512 transfers: baseline={baseline.Bytes} bytes/{baseline.Milliseconds:F3} ms/gen0={baseline.Collections}; 2048 unrelated items={populated.Bytes} bytes/{populated.Milliseconds:F3} ms/gen0={populated.Collections}");
        Assert.That(populated.Bytes, Is.EqualTo(baseline.Bytes));
        Assert.That(unrelated.Slots[0], Is.Not.Null);
        Assert.That(unrelated.Slots[2047], Is.Not.Null);
    }

    private static (long Bytes, double Milliseconds, int Collections) MeasureTransfers(ItemStore store, ItemContainer source, ItemContainer destination)
    {
        var id = source.Slots[0]!.Value;
        var from = new ItemLocation(source.Id, 0);
        var to = new ItemLocation(destination.Id, 0);
        for (var i = 0; i < 32; i++)
        {
            if (store.TransferItem(id, from, to, source.Revision, destination.Revision) != TransferResult.Success ||
                store.TransferItem(id, to, from, destination.Revision, source.Revision) != TransferResult.Success)
                Assert.Fail("Warm-up transfer failed.");
        }
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var gen0 = GC.CollectionCount(0);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++)
        {
            if (store.TransferItem(id, from, to, source.Revision, destination.Revision) != TransferResult.Success ||
                store.TransferItem(id, to, from, destination.Revision, source.Revision) != TransferResult.Success)
                Assert.Fail("Measured transfer failed.");
        }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        var milliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var collections = GC.CollectionCount(0) - gen0;
        Assert.That(store.GetLocation(id), Is.EqualTo(from));
        Assert.That(destination.Slots[0], Is.Null);
        return (bytes, milliseconds, collections);
    }
}

