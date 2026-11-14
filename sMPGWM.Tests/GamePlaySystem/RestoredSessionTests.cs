using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.GamePlaySystem;

[Category("Godot")]
public class RestoredSessionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void HostRestartsFromSuppliedStateWithoutFreshSpawnOrClockReset(bool emptyHotbar)
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        RestoredWorldState? saved = null;
        Guid oldSession = default;
        var errors = new List<string>();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var parent = new Node();
            var world = new Node2D();
            var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
            host.AddChild(parent);
            parent.AddChild(world);
            parent.AddChild(canvas);
            var coordinator = new SessionCoordinator(() => { }, errors.Add, error => errors.Add(error.Message));
            GameSession? session = null;
            int port;
            using (var reservation = new System.Net.Sockets.UdpClient(0))
                port = ((System.Net.IPEndPoint)reservation.Client.LocalEndPoint!).Port;
            try
            {
                coordinator.Initialization.PrepareDefinitions(GameConfig.EnsureCompatibilityData);
                coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                    new NetworkStartupData(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port) { RestoredWorld = saved },
                    coordinator));
                session!.Network.CreateNetwork();
                Assert.That(errors, Is.Empty);
                Assert.That(coordinator.State, Is.EqualTo(SessionState.Active));
                var synchronization = session.Network.Synchronization!;
                var owner = session.Player.Participant!;
                var ship = owner.ControlledShip!;
                if (attempt == 0)
                {
                    for (var tick = 0; tick < 60; tick++) synchronization._PhysicsProcess(1.0 / 60);
                    ship.Runtime.SetShieldCurrent(17);
                    saved = session.CaptureState();
                    if (emptyHotbar)
                        saved = new RestoredWorldState(saved.CompletedTick, saved.AbilitySequence, saved.NextSpawnIndex,
                            saved.Ships, saved.Players, new ItemHotbarSnapshot(0, new HotbarBinding?[10]));
                    oldSession = synchronization.Session;
                }
                else
                {
                    Assert.That(synchronization.Session, Is.Not.EqualTo(oldSession));
                    Assert.That(synchronization.CompletedTick, Is.EqualTo(60));
                    Assert.That(session.Network.Simulation.CompletedTick, Is.EqualTo(60));
                    Assert.That(ship.EntityId, Is.EqualTo(saved!.Ships[0].Entity.EntityId));
                    Assert.That(owner.ControlGeneration, Is.EqualTo(saved.Players[0].Binding.Generation));
                    Assert.That(owner.Inventory.Container.Id, Is.EqualTo(saved.Players[0].Inventory.Id));
                    Assert.That(ship.Equipment.Container.Id, Is.EqualTo(saved.Ships[0].Equipment.Id));
                    Assert.That(ship.Equipment.Revision, Is.EqualTo(saved.Ships[0].Equipment.Revision));
                    Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(17));
                    Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(saved.Ships[0].Runtime.EnergyCurrent));
                    Assert.That(session.Hotbar.Revision, Is.EqualTo(saved.LocalHotbar!.Revision));
                    Assert.That(session.Hotbar.Entries, Is.EqualTo(saved.LocalHotbar.Entries));
                    session.Hotbar.InitializeEquipmentAbilities();
                    Assert.That(session.Hotbar.Entries, Is.EqualTo(saved.LocalHotbar.Entries));
                    synchronization._PhysicsProcess(1.0 / 60);
                    Assert.That(synchronization.CompletedTick, Is.EqualTo(61));
                    Assert.That(ship.Runtime.AuthoritativeTick, Is.EqualTo(61));
                    Assert.That(session.Network.World.Entities.Count, Is.EqualTo(saved.Ships.Count));
                }
            }
            finally
            {
                coordinator.Stop();
                parent.Free();
            }
        }
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void SavedRemotePeerBindingsCannotClaimNewTransportPeers()
    {
        var failures = new List<string>();
        var coordinator = new SessionCoordinator(() => Assert.Fail("Invalid restore activated"), failures.Add, _ => Assert.Fail());
        var saved = new RestoredWorldState(0, 0, 0, Array.Empty<RestoredShipState>(),
            new[] { new RestoredPlayerState(new ParticipantBinding(2, 0, ""), null!) });
        coordinator.Initialization.PrepareDefinitions(() => { });
        coordinator.Start(scope => GameSessionFactory.Create(scope, null!, null!, null!,
            new NetworkStartupData(NetworkRole.Host, NetworkType.Lan, 10, 1000) { RestoredWorld = saved },
            coordinator));
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped));
        Assert.That(failures, Has.Count.EqualTo(1));
        Assert.That(failures[0], Does.Contain("authenticated reconnect mapping"));
    }

    [TestCase("item")]
    [TestCase("slot")]
    [TestCase("ability")]
    public void InvalidHotbarReferenceDoesNotInstallAndCanRetry(string invalid)
    {
        var store = new sMPGWM.Scripts.ItemSystem.ItemStore();
        using var inventory = new sMPGWM.Scripts.InventorySystem.InventoryController(store);
        using var equipment = new sMPGWM.Scripts.EquipmentSystem.EquipmentController(store);
        using var transfers = new sMPGWM.Scripts.EquipmentSystem.InventoryEquipmentTransferService(inventory, equipment);
        using var hotbar = new ItemHotbarController(inventory, equipment, transfers);
        var entries = new HotbarBinding?[10];
        entries[0] = invalid switch
        {
            "item" => new ConsumableHotbarBinding("missing_item"),
            "slot" => new WeaponHotbarBinding("missing_slot"),
            _ => new EquipmentAbilityHotbarBinding("shield_generator", "missing_ability")
        };
        Assert.Throws<ArgumentException>(() => hotbar.RestoreState(new ItemHotbarSnapshot(4, entries)));
        Assert.That(hotbar.Revision, Is.Zero);
        Assert.That(hotbar.Entries[0], Is.Null);
        hotbar.RestoreState(new ItemHotbarSnapshot(0, new HotbarBinding?[10]));
        hotbar.InitializeEquipmentAbilities();
        Assert.That(hotbar.Revision, Is.Zero);
    }
}
