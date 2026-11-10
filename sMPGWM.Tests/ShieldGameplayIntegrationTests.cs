using Godot;
using System.Reflection;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-INTEGRATION-001 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession

[Category("Godot")] // TEST-SUPPORT: ABILITY-INTEGRATION-002 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
public sealed class ShieldGameplayIntegrationTests // TEST-SUPPORT: ABILITY-INTEGRATION-003 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
{
    [Test] // TEST-SUPPORT: ABILITY-INTEGRATION-004 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
    public void HostAdmissionRefreshDuplicateAndExpiryUseNormalSession() // TEST-SUPPORT: ABILITY-INTEGRATION-005 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
    {
        var development = typeof(GameConfig).GetProperty(nameof(GameConfig.Development))!; // TEST-SUPPORT: ABILITY-INTEGRATION-006 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var previous = GameConfig.Development; // TEST-SUPPORT: ABILITY-INTEGRATION-007 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        development.SetValue(null, true); // TEST-SUPPORT: ABILITY-INTEGRATION-008 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: ABILITY-INTEGRATION-009 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var parent = new Node(); var world = new Node2D(); // TEST-SUPPORT: ABILITY-INTEGRATION-010 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>(); // TEST-SUPPORT: ABILITY-INTEGRATION-011 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        root.AddChild(parent); parent.AddChild(world); parent.AddChild(canvas); // TEST-SUPPORT: ABILITY-INTEGRATION-012 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var errors = new List<string>(); // TEST-SUPPORT: ABILITY-INTEGRATION-013 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        var coordinator = new SessionCoordinator(() => { }, errors.Add, e => errors.Add(e.Message)); // TEST-SUPPORT: ABILITY-INTEGRATION-014 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        GameSession? session = null; // TEST-SUPPORT: ABILITY-INTEGRATION-015 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        int port; // TEST-SUPPORT: ABILITY-INTEGRATION-016 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        using (var socket = new System.Net.Sockets.UdpClient(0)) port = ((System.Net.IPEndPoint)socket.Client.LocalEndPoint!).Port; // TEST-SUPPORT: ABILITY-INTEGRATION-017 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        try // TEST-SUPPORT: ABILITY-INTEGRATION-018 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        {
            coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world, // TEST-SUPPORT: ABILITY-INTEGRATION-019 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
                new(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port), coordinator, new EmptyGameFixture())); // TEST-SUPPORT: ABILITY-INTEGRATION-020 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            session!.Network.CreateNetwork(); // TEST-SUPPORT: ABILITY-INTEGRATION-021 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(errors, Is.Empty); // TEST-SUPPORT: ABILITY-INTEGRATION-022 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var network = session.Network; // TEST-SUPPORT: ABILITY-INTEGRATION-023 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var sync = network.Synchronization!; // TEST-SUPPORT: ABILITY-INTEGRATION-024 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var ship = session.Player.Participant!.ControlledShip!; // TEST-SUPPORT: ABILITY-INTEGRATION-025 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-INTEGRATION-026 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-027 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: ABILITY-INTEGRATION-028 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: ABILITY-INTEGRATION-029 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var deadline = ship.Gameplay.ActiveBehaviours[0].Deadline; // TEST-SUPPORT: ABILITY-INTEGRATION-030 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var instance = ship.Gameplay.ActiveBehaviours[0].RuntimeId; // TEST-SUPPORT: ABILITY-INTEGRATION-031 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var receive = typeof(NetworkManager).GetMethod("QueueAbility", BindingFlags.NonPublic | BindingFlags.Instance)!; // TEST-SUPPORT: ABILITY-INTEGRATION-032 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            receive.Invoke(network, new object[] { 1, sync.Session.ToString("N"), 1L, ship.EntityId, // TEST-SUPPORT: ABILITY-INTEGRATION-033 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
                session.Player.Participant.ControlGeneration, ship.EntityId, "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-INTEGRATION-034 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-035 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.ActiveBehaviours[0].Deadline, Is.EqualTo(deadline)); // TEST-SUPPORT: ABILITY-INTEGRATION-036 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-037 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-INTEGRATION-038 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-039 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.ActiveBehaviours[0].RuntimeId, Is.EqualTo(instance)); // TEST-SUPPORT: ABILITY-INTEGRATION-040 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.ActiveBehaviours[0].Deadline, Is.GreaterThan(deadline)); // TEST-SUPPORT: ABILITY-INTEGRATION-041 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var ally = network.World.ApproveSpawn(2, session.Network.Startup.Player); // TEST-SUPPORT: ABILITY-INTEGRATION-042 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-043 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.SelectedTarget = ally.EntityId; // TEST-SUPPORT: ABILITY-INTEGRATION-044 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-INTEGRATION-045 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-046 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var allyShip = network.World.Entities[ally.EntityId]; // TEST-SUPPORT: ABILITY-INTEGRATION-047 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(allyShip.Gameplay.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: ABILITY-INTEGRATION-048 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var allyDeadline = allyShip.Gameplay.ActiveBehaviours[0].Deadline; // TEST-SUPPORT: ABILITY-INTEGRATION-049 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            allyShip.Teleport(ship.Position + new Vector2(1000, 0), 0, Vector2.Zero); // TEST-SUPPORT: ABILITY-INTEGRATION-050 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-051 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-INTEGRATION-052 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-053 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(allyShip.Gameplay.ActiveBehaviours[0].Deadline, Is.EqualTo(allyDeadline)); // TEST-SUPPORT: ABILITY-INTEGRATION-054 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 1801; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-055 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-INTEGRATION-056 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(allyShip.Gameplay.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-INTEGRATION-057 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var feedback = ""; // TEST-SUPPORT: ABILITY-INTEGRATION-058 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.AbilityFeedback += message => feedback = message; // TEST-SUPPORT: ABILITY-INTEGRATION-059 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.SelectedTarget = ""; // TEST-SUPPORT: ABILITY-INTEGRATION-060 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("unsupportedAbility"); // TEST-SUPPORT: ABILITY-INTEGRATION-061 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-062 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(feedback, Is.EqualTo("Rejected: ability not granted")); // TEST-SUPPORT: ABILITY-INTEGRATION-063 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-064 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.SelectedTarget = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: ABILITY-INTEGRATION-065 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            network.ActivateAbility("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-INTEGRATION-066 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-067 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(feedback, Is.EqualTo("Rejected: target is not a living ally")); // TEST-SUPPORT: ABILITY-INTEGRATION-068 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-069 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            receive.Invoke(network, new object[] { 1, sync.Session.ToString("N"), 100L, ally.EntityId, // TEST-SUPPORT: ABILITY-INTEGRATION-070 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
                session.Player.Participant.ControlGeneration, ship.EntityId, "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-INTEGRATION-071 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-072 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(feedback, Is.EqualTo("Rejected: caster changed")); // TEST-SUPPORT: ABILITY-INTEGRATION-073 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-INTEGRATION-074 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var i = 0; i < 6; i++) sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-075 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            receive.Invoke(network, new object[] { 1, Guid.NewGuid().ToString("N"), 101L, ship.EntityId, // TEST-SUPPORT: ABILITY-INTEGRATION-076 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
                session.Player.Participant.ControlGeneration, ship.EntityId, "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-INTEGRATION-077 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            sync._PhysicsProcess(1d / 60); // TEST-SUPPORT: ABILITY-INTEGRATION-078 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(ship.Gameplay.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-INTEGRATION-079 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            for (var peer = 3; peer <= 10; peer++) network.World.ApproveSpawn(peer, network.Startup.Player); // TEST-SUPPORT: ABILITY-INTEGRATION-080 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            foreach (var entity in network.World.Entities.Values) // TEST-SUPPORT: ABILITY-INTEGRATION-081 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
                network.Abilities.Activate("shieldCapacityBoost", new(entity.Gameplay, entity.Gameplay, true, 0, Array.Empty<string>()), sync.CompletedTick); // TEST-SUPPORT: ABILITY-INTEGRATION-082 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var snapshot = typeof(NetworkManager).GetMethod("GameplaySnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!; // TEST-SUPPORT: ABILITY-INTEGRATION-083 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            var rows = (Godot.Collections.Array)snapshot.Invoke(network, null)!; // TEST-SUPPORT: ABILITY-INTEGRATION-084 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(GD.VarToBytes(rows).Length, Is.LessThan(4096)); // TEST-SUPPORT: ABILITY-INTEGRATION-085 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            TestContext.Out.WriteLine($"Gameplay snapshot: {rows.Count} ships, {GD.VarToBytes(rows).Length} bytes"); // TEST-SUPPORT: ABILITY-INTEGRATION-086 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
            Assert.That(errors, Is.Empty); // TEST-SUPPORT: ABILITY-INTEGRATION-087 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        }
        finally // TEST-SUPPORT: ABILITY-INTEGRATION-088 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        {
            coordinator.Stop(); parent.Free(); development.SetValue(null, previous); // TEST-SUPPORT: ABILITY-INTEGRATION-089 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession
        }
    }
}
