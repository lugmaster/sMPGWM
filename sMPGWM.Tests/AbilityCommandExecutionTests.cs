using Godot; // TEST-SUPPORT: ABILITY-EXECUTION-001 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: ABILITY-EXECUTION-002 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
using sMPGWM.Scripts.Items; // TEST-SUPPORT: ABILITY-EXECUTION-003 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: ABILITY-EXECUTION-004 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
using sMPGWM.Scripts.Items.Inventory; // TEST-SUPPORT: ABILITY-EXECUTION-005 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
using sMPGWM.Scripts.World; // TEST-SUPPORT: ABILITY-EXECUTION-006 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-EXECUTION-007 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands

[Category("Godot")] // TEST-SUPPORT: ABILITY-EXECUTION-008 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
public sealed class AbilityCommandExecutionTests // TEST-SUPPORT: ABILITY-EXECUTION-009 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
{
    [TestCase("accepted", AbilityActivationResult.Accepted)] // TEST-SUPPORT: ABILITY-EXECUTION-010 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("grant revoked", AbilityActivationResult.AbilitySourceChanged)] // TEST-SUPPORT: ABILITY-EXECUTION-011 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("unequipped", AbilityActivationResult.AbilitySourceChanged)] // TEST-SUPPORT: ABILITY-EXECUTION-012 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("generation changed", AbilityActivationResult.CasterChanged)] // TEST-SUPPORT: ABILITY-EXECUTION-013 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("not ready", AbilityActivationResult.CasterChanged)] // TEST-SUPPORT: ABILITY-EXECUTION-014 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("condition changed", AbilityActivationResult.CasterIsDead)] // TEST-SUPPORT: ABILITY-EXECUTION-015 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    [TestCase("target removed", AbilityActivationResult.TargetIsNotALivingAlly)] // TEST-SUPPORT: ABILITY-EXECUTION-016 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    public void ExecutionRevalidatesQueuedCommands(string change, AbilityActivationResult expected) // TEST-SUPPORT: ABILITY-EXECUTION-017 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
    {
        var world = new GameWorld(); // TEST-SUPPORT: ABILITY-EXECUTION-018 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: ABILITY-EXECUTION-019 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: ABILITY-EXECUTION-020 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        using var items = new PlayerItemState(); // TEST-SUPPORT: ABILITY-EXECUTION-021 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        try // TEST-SUPPORT: ABILITY-EXECUTION-022 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        {
            var record = world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: ABILITY-EXECUTION-023 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var owner = world.Players[1]; // TEST-SUPPORT: ABILITY-EXECUTION-024 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var ship = owner.ControlledShip!; // TEST-SUPPORT: ABILITY-EXECUTION-025 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var targetRecord = world.ApproveSpawn(2, new("fighter")); // TEST-SUPPORT: ABILITY-EXECUTION-ALLY-001 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var target = world.Entities[targetRecord.EntityId]; // TEST-SUPPORT: ABILITY-EXECUTION-ALLY-002 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            items.Bind(owner); // TEST-SUPPORT: ABILITY-EXECUTION-026 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            ship.Gameplay.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-EXECUTION-027 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var source = AbilitySource.Grant("skill:pilot"); // TEST-SUPPORT: ABILITY-EXECUTION-028 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            Assert.That(items.Store.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 1), // TEST-SUPPORT: ABILITY-EXECUTION-029 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: ABILITY-EXECUTION-030 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            var generator = items.Inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: ABILITY-EXECUTION-031 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            if (change == "unequipped") // TEST-SUPPORT: ABILITY-EXECUTION-032 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            {
                Assert.That(items.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id, // TEST-SUPPORT: ABILITY-EXECUTION-033 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    ItemCellAddress.Equipment("shield_generator"), null, items.Inventory.Revision, items.Equipment.Revision)), // TEST-SUPPORT: ABILITY-EXECUTION-034 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: ABILITY-EXECUTION-035 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                source = AbilitySource.Equipment("shield_generator", (long)generator.Id.Value); // TEST-SUPPORT: ABILITY-EXECUTION-036 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            }
            var processor = new AbilityCommandProcessor(); // TEST-SUPPORT: ABILITY-EXECUTION-037 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            processor.Enqueue((1, 42, record.EntityId, owner.ControlGeneration, targetRecord.EntityId, "shieldCapacityBoost", source)); // TEST-SUPPORT: ABILITY-EXECUTION-038 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            switch (change) // TEST-SUPPORT: ABILITY-EXECUTION-039 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            {
                case "grant revoked": // TEST-SUPPORT: ABILITY-EXECUTION-040 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    ship.Gameplay.Grants.RemoveSource("skill:pilot"); // TEST-SUPPORT: ABILITY-EXECUTION-041 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    ship.Gameplay.Grants.SetSource("another grant", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-EXECUTION-042 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    break; // TEST-SUPPORT: ABILITY-EXECUTION-043 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                case "unequipped": // TEST-SUPPORT: ABILITY-EXECUTION-044 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    Assert.That(items.Transfer(new(0, ItemCellAddress.Equipment("shield_generator"), generator.Id, // TEST-SUPPORT: ABILITY-EXECUTION-045 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                        ItemCellAddress.Inventory(0), null, items.Inventory.Revision, items.Equipment.Revision)), // TEST-SUPPORT: ABILITY-EXECUTION-046 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                        Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: ABILITY-EXECUTION-047 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    break; // TEST-SUPPORT: ABILITY-EXECUTION-048 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                case "generation changed": // TEST-SUPPORT: ABILITY-EXECUTION-049 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    owner.AssignShip(null); // TEST-SUPPORT: ABILITY-EXECUTION-050 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    owner.AssignShip(ship); // TEST-SUPPORT: ABILITY-EXECUTION-051 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    break; // TEST-SUPPORT: ABILITY-EXECUTION-052 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                case "condition changed": // TEST-SUPPORT: ABILITY-EXECUTION-053 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    ship.Gameplay.SetCondition(runtime.Definitions.Conditions.GetById("dead"), true); // TEST-SUPPORT: ABILITY-EXECUTION-054 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    break; // TEST-SUPPORT: ABILITY-EXECUTION-055 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                case "target removed": // TEST-SUPPORT: ABILITY-EXECUTION-056 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    world.DestroyEntity(targetRecord.EntityId); // TEST-SUPPORT: ABILITY-EXECUTION-057 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                    break; // TEST-SUPPORT: ABILITY-EXECUTION-058 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            }
            var completions = new List<(int Peer, long Request, AbilityActivationResult Result)>(); // TEST-SUPPORT: ABILITY-EXECUTION-059 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            processor.Step(20, world, runtime, new Dictionary<int, PlayerItemState> { [1] = items }, // TEST-SUPPORT: ABILITY-EXECUTION-060 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
                _ => change != "not ready", (peer, request, result) => completions.Add((peer, request, result))); // TEST-SUPPORT: ABILITY-EXECUTION-061 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            Assert.That(completions, Is.EqualTo(new[] { (1, 42L, expected) })); // TEST-SUPPORT: ABILITY-EXECUTION-064 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            Assert.That(processor.Count, Is.Zero); // TEST-SUPPORT: ABILITY-EXECUTION-065 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            Assert.That(target.Gameplay.ActiveBehaviours.Count, Is.EqualTo(expected == AbilityActivationResult.Accepted ? 1 : 0)); // TEST-SUPPORT: ABILITY-EXECUTION-066 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        }
        finally // TEST-SUPPORT: ABILITY-EXECUTION-067 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        {
            items.Dispose(); // TEST-SUPPORT: ABILITY-EXECUTION-068 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
            world.Free(); // TEST-SUPPORT: ABILITY-EXECUTION-069 | Tests: sMPGWM.Tests.AbilityCommandExecutionTests.ExecutionRevalidatesQueuedCommands
        }
    }
}

