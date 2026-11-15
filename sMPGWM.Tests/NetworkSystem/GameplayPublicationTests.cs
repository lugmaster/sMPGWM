using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class GameplayPublicationTests
{
    [Test]
    public void ResourceRowsRemainLiveInReusablePublicationWithoutManagedAllocation()
    {
        using var state = new ShipRuntime(100);
        state.InitializeResources();
        using var publisher = new GameplayDeltaState();
        using var batch = new Array();
        using var consumer = new Array();
        using Godot.Variant batchValue = batch;
        consumer.Resize(1);
        for (var i = 0; i < 100; i++) Publish(i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) Publish(i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
        using var received = consumer[0].AsGodotArray();
        Assert.That(received.Count, Is.EqualTo(1));
        using var row = received[0].AsGodotArray();
        using var resources = row[5].AsGodotArray();
        Assert.That(resources[9].AsDouble(), Is.EqualTo(91));

        void Publish(int i)
        {
            batch.Clear();
            state.SetHealthCurrent(90 + i % 2);
            if (publisher.Encode(1, state) != null) batch.Add(publisher.RowVariant);
            consumer[0] = batchValue;
        }
    }
}
