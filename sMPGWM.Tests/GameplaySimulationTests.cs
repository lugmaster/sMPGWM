using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class GameplaySimulationTests
{
    [Test]
    public void StepUsesCurrentBindingAndStopsRemovedPowerBeforeMovement()
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        using var abilities = new AbilityRuntime(GameplayTestData.Load());
        using var simulation = new GameplaySimulation(world, abilities);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            ship.Runtime.ApplyEquipment(new ResolvedShipStats(100, 200, 1, 1, 2, 1, 0, 0, 0, 0, 10, 1, 10, true, false, false, true, true), 1);
            ship.Runtime.InitializeResources();
            ship.Restore(new Vector2(5000, 5000), 0, new Vector2(60, 0), new(Vector2.Right, 0, false, false, false, false));
            ship.Runtime.StageComponentChange(ShipComponentKind.Power, false);
            var transfer = new TransferRequest(0, ItemCellAddress.Inventory(0), default, ItemCellAddress.Inventory(1), null, 0, 0);
            var result = TransferResult.Pending;
            Assert.That(simulation.QueueTransfer(1, ship.EntityId, owner.ControlGeneration, transfer, value => result = value), Is.True);
            owner.AssignShip(null);
            owner.AssignShip(ship);
            simulation.Step(1, _ => true, (_, _, _) => { });
            Assert.That(result, Is.EqualTo(TransferResult.InvalidSession));
            Assert.That(ship.Runtime.Energy.Current, Is.Zero);
            Assert.That(ship.MovementConfiguration.Enabled, Is.False);
            Assert.That(ship.Velocity, Is.EqualTo(new Vector2(60, 0)));
            Assert.That(ship.GlobalPosition.X, Is.EqualTo(5001).Within(.001));
            Assert.Throws<InvalidOperationException>(() => simulation.Step(1, _ => true, (_, _, _) => { }));
            var position = ship.GlobalPosition;
            owner.AssignShip(null);
            simulation.Step(2, _ => true, (_, _, _) => { });
            Assert.That(ship.GlobalPosition, Is.EqualTo(position));
            Assert.That(ship.Runtime.AuthoritativeTick, Is.EqualTo(2));
        }
        finally { world.Free(); }
    }

    [Test]
    public void QueueIsBoundedAndPeerRemovalDiscardsOnlyItsCommands()
    {
        var world = new GameWorld();
        using var abilities = new AbilityRuntime(GameplayTestData.Load());
        using var simulation = new GameplaySimulation(world, abilities);
        try
        {
            var request = new TransferRequest(0, ItemCellAddress.Inventory(0), default, ItemCellAddress.Inventory(1), null, 0, 0);
            var replies = 0;
            for (var i = 0; i < 32; i++)
                Assert.That(simulation.QueueTransfer(i % 2, "", 0, request, _ => replies++), Is.True);
            Assert.That(simulation.QueueTransfer(0, "", 0, request, _ => replies++), Is.False);
            simulation.RemovePeer(0);
            simulation.Step(1, _ => true, (_, _, _) => { });
            Assert.That(replies, Is.EqualTo(16));
            simulation.Dispose();
            Assert.That(simulation.QueueTransfer(0, "", 0, request, _ => replies++), Is.False);
        }
        finally { world.Free(); }
    }
    [Test]
    public void CommandsQueuedByCompletionWaitUntilNextAbilityPhase()
    {
        var world = new GameWorld();
        using var abilities = new AbilityRuntime(GameplayTestData.Load());
        var commands = new AbilityCommandProcessor();
        var command = (1, 1L, "missing", 0L, "missing", "shieldCapacityBoost", AbilitySource.AllShips);
        commands.Enqueue(command);
        var completed = 0;
        try
        {
            commands.Step(1, world, abilities, _ => true, (_, _, _) => { completed++; commands.Enqueue(command); });
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(commands.Count, Is.EqualTo(1));
        }
        finally { world.Free(); }
    }

}
