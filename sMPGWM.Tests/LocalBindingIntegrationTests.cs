using Godot;
using System.Reflection;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using PlayerMock = sMPGWM.Scripts.UtilitySystem.Mocks.PlayerMock;
namespace sMPGWM.Tests;
[Category("Godot")]
public sealed class LocalBindingIntegrationTests
{
    [Test]
    public void SessionShipSwitchCancelsTargetingRevokesEditsAndPreservesAssignments()
    {
        var parent = new Node();
        var world = new Node2D();
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(parent);
        parent.AddChild(world);
        parent.AddChild(canvas);
        var errors = new List<string>();
        var coordinator = new SessionCoordinator(() => { }, errors.Add, e => errors.Add(e.Message));
        var mock = new PlayerMock();
        using var socket = new System.Net.Sockets.UdpClient(0);
        var port = ((System.Net.IPEndPoint)socket.Client.LocalEndPoint!).Port;
        socket.Close();
        GameSession? session = null;
        try
        {
            coordinator.Initialization.PrepareDefinitions(sMPGWM.Scripts.GameConfigSystem.GameConfig.EnsureCompatibilityData);
            coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                new(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port), coordinator, new DevelopmentGameFixture(mock, true)));
            session!.Network.CreateNetwork();
            Assert.That(errors, Is.Empty);
            session.Player.SetSessionActive(true);
            typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, true);
            var owner = session.Player.Participant!;
            var ship = owner.ControlledShip!;
            var hotbar = (ItemHotbarController)typeof(UiCanvas).GetField("_hotbar", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            var transfers = (InventoryEquipmentTransferService)typeof(UiCanvas).GetField("_transfers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas)!;
            var targeting = world.GetNode<AbilityTargetingController>("AbilityTargeting");
            var slot = ItemCellAddress.Equipment("shield_generator");
            var generator = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("shield_generator")]!;
            var edit = transfers.OpenEditSession();
            hotbar.SetEditSession(edit);
            Assert.That(hotbar.Assign(new(edit, hotbar.Revision, 0, slot, generator.Id, owner.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(HotbarResult.NoChange));
            var binding = (EquipmentAbilityHotbarBinding)hotbar.Entries[0]!;
            targeting.Begin(binding);
            Assert.That(targeting.IsTargeting, Is.True);
            var spawned = session.Network.World.ApproveSpawn(2, new("fighter"));
            var replacement = session.Network.World.Entities[spawned.EntityId];
            Assert.That(replacement.Equipment.Revision, Is.EqualTo(ship.Equipment.Revision));
            owner.AssignShip(replacement);
            Assert.That(targeting.IsTargeting, Is.False);
            Assert.That(hotbar.Entries[0], Is.EqualTo(binding));
            Assert.That(hotbar.Resolve(0).Available, Is.True);
            Assert.That(typeof(UiCanvas).GetField("_hotbar", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas), Is.SameAs(hotbar));
            Assert.That(typeof(UiCanvas).GetField("_equipment", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas), Is.SameAs(replacement.Equipment));
            var current = replacement.Equipment.Snapshot.Slots[replacement.Equipment.FindSlot("shield_generator")]!;
            Assert.That(transfers.Transfer(new(edit, slot, current.Id, ItemCellAddress.Inventory(0), null, owner.Inventory.Revision, replacement.Equipment.Revision)), Is.EqualTo(TransferResult.InvalidSession));
            targeting.Begin(binding);
            Assert.That(targeting.IsTargeting, Is.True);
            ship.Loadout.Dispose();
            Assert.That(targeting.IsTargeting, Is.True, "Previous ship equipment no longer owns targeting subscriptions.");
            owner.AssignShip(null);
            Assert.That(targeting.IsTargeting, Is.False);
            Assert.That(hotbar.Entries[0], Is.EqualTo(binding));
            Assert.That(hotbar.Resolve(0).Available, Is.False);
            Assert.That(typeof(UiCanvas).GetField("_inventory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(canvas), Is.SameAs(owner.Inventory));
            owner.AssignShip(replacement);
            Assert.That(hotbar.Resolve(0).Available, Is.True);
            Assert.That(errors, Is.Empty);
        }
        finally
        {
            coordinator.Stop();
            parent.Free();
            mock.Free();
        }
    }
}
