using sMPGWM.Scripts.EquipmentSystem; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-001 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-002 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-003 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-004 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations

namespace sMPGWM.Tests; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-005 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations

public sealed class EquipmentStatAllocationTests // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-006 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
{ // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-007 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
    [Test] // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-008 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
    public void ArrayInputCalculatesWithoutPerCallAllocations() // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-009 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
    { // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-010 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var engine = new ItemDefinition("engine", "Engine", "engine", "", false, 1, null!) // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-011 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        { // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-012 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
            Values = new Dictionary<string, object> { ["Speed"] = 200d, ["Mass"] = 2d } // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-013 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        }; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-014 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var items = new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), engine, 1) }; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-015 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var slots = new[] { new EquipmentSlotDefinition("engine", "Engine", "engine") }; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-016 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var modifiers = new ShipStatModifiersDefinition { Speed = 50 }; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-017 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        for (var i = 0; i < 100; i++) EquipmentStatCalculator.Calculate(modifiers, 10, items, slots); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-018 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var before = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-019 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var stats = default(ResolvedShipStats); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-020 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        for (var i = 0; i < 1000; i++) stats = EquipmentStatCalculator.Calculate(modifiers, 10, items, slots); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-021 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before; // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-022 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        Assert.That(allocated, Is.Zero); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-023 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        Assert.That(stats.Speed, Is.EqualTo(250)); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-024 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        Assert.That(stats.Mass, Is.EqualTo(12)); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-025 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
        Assert.That(items[0]!.Definition, Is.SameAs(engine)); // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-026 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
    } // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-027 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations
} // TEST-SUPPORT: EQUIP-ARRAY-ALLOC-028 | Tests: sMPGWM.Tests.EquipmentStatAllocationTests.ArrayInputCalculatesWithoutPerCallAllocations

