using Godot;
using System.Reflection;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ShieldGameplayIntegrationTests
{
    [Test]
    public void HostAdmissionRefreshDuplicateAndExpiryUseNormalSession()
    {
        var development = typeof(GameConfig).GetProperty(nameof(GameConfig.Development))!;
        var previous = GameConfig.Development;
        development.SetValue(null, true);
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var parent = new Node(); var world = new Node2D();
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        root.AddChild(parent); parent.AddChild(world); parent.AddChild(canvas);
        var errors = new List<string>();
        var coordinator = new SessionCoordinator(() => { }, errors.Add, e => errors.Add(e.Message));
        GameSession? session = null;
        int port;
        using (var socket = new System.Net.Sockets.UdpClient(0)) port = ((System.Net.IPEndPoint)socket.Client.LocalEndPoint!).Port;
        try
        {
            coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                new(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port), coordinator, new EmptyGameFixture()));
            session!.Network.CreateNetwork();
            Assert.That(errors, Is.Empty);
            var network = session.Network;
            var sync = network.Synchronization!;
            var ship = session.Player.Participant!.ControlledShip!;
            network.ActivateAbility(GameplayDefinitions.Boost.AbilityId);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(150));
            Assert.That(ship.Gameplay.Shield.Current, Is.EqualTo(40));
            var deadline = ship.Gameplay.Deadline;
            var instance = ship.Gameplay.BehaviourInstance;
            var receive = typeof(NetworkManager).GetMethod("QueueAbility", BindingFlags.NonPublic | BindingFlags.Instance)!;
            receive.Invoke(network, new object[] { 1, sync.Session.ToString("N"), 1L, ship.EntityId,
                session.Player.Participant.ControlGeneration, ship.EntityId, GameplayDefinitions.Boost.AbilityId });
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Gameplay.Deadline, Is.EqualTo(deadline));
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility(GameplayDefinitions.Boost.AbilityId);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Gameplay.BehaviourInstance, Is.EqualTo(instance));
            Assert.That(ship.Gameplay.Deadline, Is.GreaterThan(deadline));
            var ally = network.World.ApproveSpawn(2, session.Network.Startup.Player);
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            network.SelectedTarget = ally.EntityId;
            network.ActivateAbility(GameplayDefinitions.Boost.AbilityId);
            sync._PhysicsProcess(1d / 60);
            var allyShip = network.World.Entities[ally.EntityId];
            Assert.That(allyShip.Gameplay.Shield.Capacity, Is.EqualTo(150));
            var allyDeadline = allyShip.Gameplay.Deadline;
            allyShip.Teleport(ship.Position + new Vector2(1000, 0), 0, Vector2.Zero);
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility(GameplayDefinitions.Boost.AbilityId);
            sync._PhysicsProcess(1d / 60);
            Assert.That(allyShip.Gameplay.Deadline, Is.EqualTo(allyDeadline));
            for (var i = 0; i < 1801; i++) sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100));
            Assert.That(allyShip.Gameplay.Shield.Capacity, Is.EqualTo(100));
            var feedback = "";
            network.AbilityFeedback += message => feedback = message;
            network.SelectedTarget = "";
            network.ActivateAbility("unsupportedAbility");
            sync._PhysicsProcess(1d / 60);
            Assert.That(feedback, Is.EqualTo("Rejected: ability not granted"));
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            network.SelectedTarget = Guid.NewGuid().ToString("N");
            network.ActivateAbility(GameplayDefinitions.Boost.AbilityId);
            sync._PhysicsProcess(1d / 60);
            Assert.That(feedback, Is.EqualTo("Rejected: target is not a living ally"));
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            receive.Invoke(network, new object[] { 1, sync.Session.ToString("N"), 100L, ally.EntityId,
                session.Player.Participant.ControlGeneration, ship.EntityId, GameplayDefinitions.Boost.AbilityId });
            sync._PhysicsProcess(1d / 60);
            Assert.That(feedback, Is.EqualTo("Rejected: caster changed"));
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100));
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60);
            receive.Invoke(network, new object[] { 1, Guid.NewGuid().ToString("N"), 101L, ship.EntityId,
                session.Player.Participant.ControlGeneration, ship.EntityId, GameplayDefinitions.Boost.AbilityId });
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100));
            for (var peer = 3; peer <= 10; peer++) network.World.ApproveSpawn(peer, network.Startup.Player);
            foreach (var entity in network.World.Entities.Values)
                entity.Gameplay.Apply(GameplayDefinitions.Boost, sync.CompletedTick, 1000);
            var snapshot = typeof(NetworkManager).GetMethod("GameplaySnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var rows = (Godot.Collections.Array)snapshot.Invoke(network, null)!;
            Assert.That(GD.VarToBytes(rows).Length, Is.LessThan(4096));
            TestContext.Out.WriteLine($"Gameplay snapshot: {rows.Count} ships, {GD.VarToBytes(rows).Length} bytes");
            Assert.That(errors, Is.Empty);
        }
        finally
        {
            coordinator.Stop(); parent.Free(); development.SetValue(null, previous);
        }
    }
}
