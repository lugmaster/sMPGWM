using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class EquipmentStatAllocationTests
{
    [Test]
    public void ArrayInputCalculatesWithoutPerCallAllocations()
    {
        var engine = new ItemDefinition("engine", "Engine", "engine", "", false, 1, null!)
        {
            Values = new Dictionary<string, object> { ["Speed"] = 200d, ["Mass"] = 2d }
        };
        var items = new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), engine, 1) };
        var slots = new[] { new EquipmentSlotDefinition("engine", "Engine", "engine") };
        var modifiers = new ShipStatModifiersDefinition { Speed = 50 };
        for (var i = 0; i < 100; i++) EquipmentStatCalculator.Calculate(modifiers, 10, items, slots);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stats = default(ResolvedShipStats);
        for (var i = 0; i < 1000; i++) stats = EquipmentStatCalculator.Calculate(modifiers, 10, items, slots);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
        Assert.That(stats.Speed, Is.EqualTo(250));
        Assert.That(stats.Mass, Is.EqualTo(12));
        Assert.That(items[0]!.Definition, Is.SameAs(engine));
    }
}

