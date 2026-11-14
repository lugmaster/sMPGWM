using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class AbilityCommandExecutionTests
{
    [TestCase("accepted", AbilityActivationResult.Accepted)]
    [TestCase("grant revoked", AbilityActivationResult.AbilitySourceChanged)]
    [TestCase("unequipped", AbilityActivationResult.AbilitySourceChanged)]
    [TestCase("generation changed", AbilityActivationResult.CasterChanged)]
    [TestCase("not ready", AbilityActivationResult.CasterChanged)]
    [TestCase("condition changed", AbilityActivationResult.CasterIsDead)]
    [TestCase("target removed", AbilityActivationResult.TargetIsNotALivingAlly)]
    public void ExecutionRevalidatesQueuedCommands(string change, AbilityActivationResult expected)
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        try
        {
            var record = world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            var targetRecord = world.ApproveSpawn(2, new("fighter"));
            var target = world.Entities[targetRecord.EntityId];
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" });
            var source = AbilitySource.Grant("skill:pilot");
            Assert.That(owner.Inventory.Items.CreateItems(owner.Inventory.Container, "mock_standard_shield_generator", 1),
                Is.EqualTo(InventoryResult.Success));
            var generator = owner.Inventory.Snapshot.Slots[0]!;
            if (change == "unequipped")
            {
                Assert.That(owner.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id,
                    ItemCellAddress.Equipment("shield_generator"), null, owner.Inventory.Revision, ship.Equipment.Revision)),
                    Is.EqualTo(TransferResult.Success));
                source = AbilitySource.Equipment("shield_generator", (long)generator.Id.Value);
            }
            var processor = new AbilityCommandProcessor();
            processor.Enqueue((1, 42, record.EntityId, owner.ControlGeneration, targetRecord.EntityId, "shieldCapacityBoost", source));
            switch (change)
            {
                case "grant revoked":
                    ship.Runtime.Grants.RemoveSource("skill:pilot");
                    ship.Runtime.Grants.SetSource("another grant", new[] { "shieldCapacityBoost" });
                    break;
                case "unequipped":
                    Assert.That(owner.Transfer(new(0, ItemCellAddress.Equipment("shield_generator"), generator.Id,
                        ItemCellAddress.Inventory(0), null, owner.Inventory.Revision, ship.Equipment.Revision)),
                        Is.EqualTo(TransferResult.Success));
                    break;
                case "generation changed":
                    owner.AssignShip(null);
                    owner.AssignShip(ship);
                    break;
                case "condition changed":
                    ship.Runtime.SetCondition(runtime.Definitions.Conditions.GetById("dead"), true);
                    break;
                case "target removed":
                    world.DestroyEntity(targetRecord.EntityId);
                    break;
            }
            var completions = new List<(int Peer, long Request, AbilityActivationResult Result)>();
            processor.Step(20, world, runtime,
                _ => change != "not ready", (peer, request, result) => completions.Add((peer, request, result)));
            Assert.That(completions, Is.EqualTo(new[] { (1, 42L, expected) }));
            Assert.That(processor.Count, Is.Zero);
            Assert.That(target.Runtime.ActiveBehaviours.Count, Is.EqualTo(expected == AbilityActivationResult.Accepted ? 1 : 0));
        }
        finally
        {
            world.Free();
        }
    }
}

