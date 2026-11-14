using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-001 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
using sMPGWM.Scripts.InventorySystem; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-002 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-003 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
using sMPGWM.Scripts.WeaponSystem; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-004 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
using sMPGWM.Scripts.WorldSystem; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-005 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults

namespace sMPGWM.Tests; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-007 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults

public sealed class WeaponCommandTests // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-009 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
{ // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-010 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
    [Test, Category("Godot")] // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-011 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
    public void CommandsValidateAtExecutionAndPublishAfterSimulation() // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-012 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
    { // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-013 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        using var inventory = new InventoryController(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-014 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        var world = new GameWorld { AuthoritativeItems = true, InitialInventory = inventory.Snapshot, InitializeLoadout = StarterLoadout.Equip }; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-015 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        try // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-016 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        { // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-017 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            world.ApproveSpawn(1, PlayerStartupData.Create()); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-018 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var owner = world.Players[1]; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-019 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var ship = owner.ControlledShip!; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-020 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var slot = ship.Equipment.FindSlot("weapon_1"); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-021 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var item = ship.Equipment.Container.Slots[slot]!.Value; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-022 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var revision = ship.Loadout.Weapons.ConfigurationRevision; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-023 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var processor = new WeaponCommandProcessor(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-024 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            var results = new List<WeaponCommandResult>(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-025 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(processor.Enqueue(1, ship.EntityId, owner.ControlGeneration, revision, WeaponCommandKind.AssignGroup, // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-026 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
                "weapon_1", item, 2, results.Add), Is.True); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-027 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(processor.Enqueue(1, ship.EntityId, owner.ControlGeneration, revision, WeaponCommandKind.Reload, // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-028 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
                "", default, 0, results.Add), Is.True); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-029 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            processor.Step(0, world, _ => true); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-030 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(ship.Loadout.Weapons.GetGroup(slot), Is.EqualTo(2)); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-031 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(ship.Loadout.Weapons.ConfigurationRevision, Is.EqualTo(revision + 1)); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-032 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(results, Is.Empty); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-033 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            processor.PublishResults(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-034 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(results, Is.EqualTo(new[] { WeaponCommandResult.Accepted, WeaponCommandResult.Rejected })); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-035 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            processor.PublishResults(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-036 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
            Assert.That(results.Count, Is.EqualTo(2)); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-037 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        } // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-038 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
        finally { world.Clear(); world.Free(); } // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-039 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation
    } // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-040 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation

    [Test] // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-042 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
    public void QueueCapacityAndPeerRemovalPreservePendingCommands() // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-043 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
    { // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-044 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        var processor = new WeaponCommandProcessor(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-045 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        var completed = 0; // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-046 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        for (var i = 0; i < 32; i++) // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-047 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
            Assert.That(processor.Enqueue(i % 2 + 1, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++), Is.True); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-048 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        Assert.That(processor.Enqueue(3, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++), Is.False); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-049 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        processor.RemovePeer(1); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-050 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        for (var i = 0; i < 16; i++) // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-051 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
            Assert.That(processor.Enqueue(3, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++), Is.True); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-052 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        Assert.That(processor.Enqueue(3, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++), Is.False); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-053 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        processor.Clear(); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-054 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        Assert.That(completed, Is.Zero); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-055 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
        Assert.That(processor.Enqueue(3, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++), Is.True); // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-056 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
    } // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-057 | Tests: sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands
    [Test, Category("Godot")] // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-002 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
    public void ClearDuringCompletionCancelsRemainingResults() // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-003 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
    { // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-004 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
        var world = new GameWorld(); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-005 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
        try // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-006 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
        { // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-007 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            var processor = new WeaponCommandProcessor(); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-008 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            var completed = 0; // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-009 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            processor.Enqueue(1, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => { completed++; processor.Clear(); }); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-010 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            processor.Enqueue(2, "", 0, 0, WeaponCommandKind.Reload, "", default, 0, _ => completed++); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-011 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            processor.Step(0, world, _ => false); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-012 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            processor.PublishResults(); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-013 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            Assert.That(completed, Is.EqualTo(1)); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-014 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            processor.PublishResults(); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-015 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
            Assert.That(completed, Is.EqualTo(1)); // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-016 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
        } // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-017 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
        finally { world.Clear(); world.Free(); } // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-018 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
    } // TEST-SUPPORT: WEAPON-COMMAND-CLEAR-019 | Tests: sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults

} // TEST-SUPPORT: WEAPON-COMMAND-REFACTOR-058 | Tests: sMPGWM.Tests.WeaponCommandTests.CommandsValidateAtExecutionAndPublishAfterSimulation, sMPGWM.Tests.WeaponCommandTests.QueueCapacityAndPeerRemovalPreservePendingCommands, sMPGWM.Tests.WeaponCommandTests.ClearDuringCompletionCancelsRemainingResults
