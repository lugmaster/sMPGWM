using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Tests.Network;

[Category("Godot")]
public class GameSessionFactoryTests
{
    [TestCase("populate")]
    [TestCase("actions")]
    [TestCase("network")]
    [TestCase("endpoint")]
    public void FailedStartupReleasesBindingsAndAllowsFreshConstruction(string failure)
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var parent = new Node();
        var world = new Node2D();
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        host.AddChild(parent);
        parent.AddChild(world);
        parent.AddChild(canvas);
        var failures = new List<string>();
        var cleanupErrors = new List<Exception>();
        var fixture = new FailingFixture(failure);
        var coordinator = new SessionCoordinator(() => Assert.Fail("Failed session activated"), failures.Add, cleanupErrors.Add);
        GameSession? session = null;
        try
        {
            coordinator.Start(scope =>
            {
                session = GameSessionFactory.Create(scope, parent, canvas, world,
                    new NetworkStartupData(failure == "network" ? (NetworkRole)999 : NetworkRole.Host,
                        NetworkType.Lan, 10, 100, Port: 0), coordinator, fixture);
                session.Network.CreateNetwork();
            });
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(cleanupErrors, Is.Empty);
            Assert.That(fixture.Inventory!.Availability, Is.EqualTo(InventoryResult.Disposed));
            Assert.That(canvas.GetNode<Control>("Root/MenuLayer/ModalBlocker").Visible, Is.False);
            session?.Network.FailSession("duplicate failure");
            coordinator.Stop();
            Assert.That(failures, Has.Count.EqualTo(1));

            // QueueFree is deferred by production code to permit cleanup during RPC dispatch.
            // Drain those owned nodes synchronously here before constructing another session.
            FreeQueuedChildren(parent);
            Assert.That(MpManager.Instance, Is.Null);
            Assert.That(NetworkManager.Instance, Is.Null);
            Assert.That(world.GetChildCount(), Is.Zero);
            var fresh = new SessionCoordinator(() => { }, failures.Add, cleanupErrors.Add);
            fresh.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                new NetworkStartupData(NetworkRole.Host, NetworkType.Lan, 10, 100), fresh, new EmptyGameFixture()));
            Assert.That(fresh.State, Is.EqualTo(SessionState.Starting));
            Assert.That(session!.Player.Participant, Is.Null);
            fresh.Stop();
            FreeQueuedChildren(parent);
            Assert.That(cleanupErrors, Is.Empty);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(world.GetChildCount(), Is.Zero);
        }
        finally
        {
            coordinator.Stop();
            parent.Free();
        }
    }

    [Test]
    public void HostCanStartStopAndStartAgainWithAuthenticationSettingsRestored()
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var api = (SceneMultiplayer)host.Multiplayer;
        var auth = api.AuthCallback;
        var timeout = api.AuthTimeout;
        var decoding = api.AllowObjectDecoding;
        var previousPeer = api.MultiplayerPeer;
        var failures = new List<string>();
        var cleanupErrors = new List<Exception>();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var parent = new Node();
            var world = new Node2D();
            var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
            host.AddChild(parent);
            parent.AddChild(world);
            parent.AddChild(canvas);
            var activations = 0;
            var coordinator = new SessionCoordinator(() => activations++, failures.Add, cleanupErrors.Add);
            GameSession? session = null;
            int port;
            using (var reservation = new System.Net.Sockets.UdpClient(0))
                port = ((System.Net.IPEndPoint)reservation.Client.LocalEndPoint!).Port;
            try
            {
                coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                    new NetworkStartupData(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port),
                    coordinator, new EmptyGameFixture()));
                session!.Network.CreateNetwork();
                Assert.That(failures, Is.Empty);
                Assert.That(coordinator.State, Is.EqualTo(SessionState.Active));
                Assert.That(activations, Is.EqualTo(1));
                Assert.That(session.Player.Participant?.ControlledShip, Is.Not.Null);
                Assert.That(session.Network.IsConnected(), Is.True);
                coordinator.Stop();
                Assert.That(session.Player.Participant, Is.Null);
                Assert.That(session.Network.IsConnected(), Is.False);
                Assert.That(api.AuthCallback, Is.EqualTo(auth));
                Assert.That(api.AuthTimeout, Is.EqualTo(timeout));
                Assert.That(api.AllowObjectDecoding, Is.EqualTo(decoding));
                Assert.That(api.MultiplayerPeer, Is.SameAs(previousPeer));
                FreeQueuedChildren(parent);
                Assert.That(MpManager.Instance, Is.Null);
                Assert.That(NetworkManager.Instance, Is.Null);
                Assert.That(HeartbeatMonitor.Instance, Is.Null);
                Assert.That(cleanupErrors, Is.Empty);
            }
            finally
            {
                coordinator.Stop();
                parent.Free();
            }
        }
    }

    private static void FreeQueuedChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child.IsQueuedForDeletion()) child.Free();
            else FreeQueuedChildren(child);
        }
    }

    private sealed class FailingFixture(string failure) : IGameFixture
    {
        public InventoryController? Inventory { get; private set; }
        public IReadOnlyList<IconDefinition> Actions => failure == "actions"
            ? throw new InvalidOperationException("Action initialization failed") : Array.Empty<IconDefinition>();
        public void Populate(InventoryController inventory, MessageController messages)
        {
            Inventory = inventory;
            if (failure == "populate") throw new InvalidOperationException("Fixture initialization failed");
        }
        public void ActivateAction(string key) { }
        public void Tick(double delta, MessageController messages) { }
    }
}
