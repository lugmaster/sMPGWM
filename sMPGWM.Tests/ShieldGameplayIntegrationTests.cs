using Godot;
using System.Reflection;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ShieldGameplayIntegrationTests
{
    [Test]
    public void HostAdmissionRefreshDuplicateAndExpiryUseNormalSession()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var parent = new Node();
        var world = new Node2D();
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        root.AddChild(parent);
        parent.AddChild(world);
        parent.AddChild(canvas);
        var errors = new List<string>();
        var coordinator = new SessionCoordinator(() => { }, errors.Add, e => errors.Add(e.Message));
        GameSession? session = null;
        using var socket = new System.Net.Sockets.UdpClient(0);
        var port = ((System.Net.IPEndPoint)socket.Client.LocalEndPoint!).Port;
        socket.Close();
        try
        {
            coordinator.Initialization.PrepareDefinitions(sMPGWM.Scripts.GameConfigSystem.GameConfig.EnsureCompatibilityData);
            coordinator.Start(scope => session = GameSessionFactory.Create(scope, parent, canvas, world,
                new(NetworkRole.Host, NetworkType.Lan, 10, 1000, Port: port), coordinator));
            session!.Network.CreateNetwork();
            Assert.That(errors, Is.Empty);
            session.Player.SetSessionActive(true);
            typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, true);
            var network = session.Network;
            var sync = network.Synchronization!;
            var ship = session.Player.Participant!.ControlledShip!;
            var items = session.Player.Participant!;
            items.Inventory.Items.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 1);
            var generatorIndex = -1;
            for (var i = 0; i < items.Inventory.Capacity; i++)
                if (items.Inventory.Snapshot.Slots[i]?.DefinitionId == "mock_standard_shield_generator") { generatorIndex = i; break; }
            Assert.That(generatorIndex, Is.GreaterThanOrEqualTo(0));
            var generator = items.Inventory.Snapshot.Slots[generatorIndex]!;
            var starterIndex = ship.Equipment.FindSlot("shield_generator");
            var starter = ship.Equipment.Snapshot.Slots[starterIndex]!;
            var emptyIndex = 0;
            while (items.Inventory.Snapshot.Slots[emptyIndex] != null) emptyIndex++;
            Assert.That(items.Inventory.Items.TransferItem(starter.Id, new(ship.Equipment.Container.Id, starterIndex),
                new(items.Inventory.Container.Id, emptyIndex), ship.Equipment.Revision, items.Inventory.Revision), Is.EqualTo(TransferResult.Success));
            Assert.That(ship.Runtime.Grants.Contains("shieldCapacityBoost"), Is.False);
            using var transfers = new InventoryEquipmentTransferService(items.Inventory, ship.Equipment);
            transfers.SetAuthority(network.RequestItemTransfer);
            var edit = transfers.OpenEditSession();
            var slot = ItemCellAddress.Equipment("shield_generator");
            Assert.That(transfers.Transfer(new(edit, ItemCellAddress.Inventory(generatorIndex), generator.Id, slot, null,
                items.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Pending));
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.Grants.Contains("shieldCapacityBoost"), Is.True);
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(100d / 300).Within(.000001), "Reinstallation only permits one tick of regeneration.");
            ship.Runtime.SetShieldCurrent(40);
            using var hotbar = new ItemHotbarController(items.Inventory, ship.Equipment, transfers);
            hotbar.SetEditSession(edit);
            Assert.That(hotbar.Assign(new(edit, hotbar.Revision, 0, slot, generator.Id, items.Inventory.Revision,
                ship.Equipment.Revision)), Is.EqualTo(HotbarResult.Success));
            canvas.BindHotbar(hotbar, System.Array.Empty<IconDefinition>());
            var view = canvas.GetNode<HotbarControl>("%PlayerHud/HotbarAnchor/HotbarControl");
            Assert.That(view.Icons[0].Icon, Is.SameAs(GameplayDefinitions.Current.Abilities.GetById("shieldCapacityBoost").Icon!.IconTexture));
            var targeting = world.GetNode<AbilityTargetingController>("AbilityTargeting");
            hotbar.Activate(0, _ => Assert.Fail("Unexpected action"), request => targeting.Begin((EquipmentAbilityHotbarBinding)request.Binding));
            Assert.That(targeting.IsTargeting, Is.True);
            Assert.That(ShipTargetPicker.Pick(network.World, ship.DisplayedPosition), Is.SameAs(ship));
            targeting._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true,
                Position = ship.GetCanvasTransform() * ship.DisplayedPosition });
            Assert.That(targeting.IsTargeting, Is.False);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.Shield.Capacity, Is.EqualTo(150));
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(40.5).Within(.000001));
            var deadline = ship.Runtime.ActiveBehaviours[0].Deadline;
            var instance = ship.Runtime.ActiveBehaviours[0].RuntimeId;
            var receive = typeof(NetworkManager).GetMethod("QueueAbility", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var shipIndex = network.GetWeaponEntityIndex(ship.EntityId);
            receive.Invoke(network, new object[] { 1, sync.SessionWireIdentity, 1L, shipIndex,
                session.Player.Participant.ControlGeneration, shipIndex, "shieldCapacityBoost", "shield_generator", (long)generator.Id.Value, (int)AbilitySourceKind.Equipment });
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.EqualTo(deadline));
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ship.EntityId, "shield_generator", (long)generator.Id.Value);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].RuntimeId, Is.EqualTo(instance));
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.GreaterThan(deadline));
            var allySpawn = network.World.ApproveSpawn(2, network.Startup.Player);
            var ally = network.World.Entities[allySpawn.EntityId];
            var allyIndex = network.GetWeaponEntityIndex(ally.EntityId);
            var generation = session.Player.Participant.ControlGeneration;
            foreach (var invalid in new[]
                     {
                         (Caster: 0, Target: shipIndex, Generation: generation, Session: sync.SessionWireIdentity),
                         (Caster: -1, Target: shipIndex, Generation: generation, Session: sync.SessionWireIdentity),
                         (Caster: int.MaxValue, Target: shipIndex, Generation: generation, Session: sync.SessionWireIdentity),
                         (Caster: shipIndex, Target: 0, Generation: generation, Session: sync.SessionWireIdentity),
                         (Caster: allyIndex, Target: shipIndex, Generation: generation, Session: sync.SessionWireIdentity),
                         (Caster: shipIndex, Target: shipIndex, Generation: generation + 1, Session: sync.SessionWireIdentity),
                         (Caster: shipIndex, Target: shipIndex, Generation: generation, Session: Guid.NewGuid().ToString("N"))
                     })
            {
                receive.Invoke(network, new object[] { 1, invalid.Session, 1000L, invalid.Caster,
                    invalid.Generation, invalid.Target, "shieldCapacityBoost", "shield_generator", (long)generator.Id.Value, (int)AbilitySourceKind.Equipment });
                Assert.That(network.Simulation.AbilityCommands.Count, Is.Zero, "Invalid numeric references must be rejected before command admission.");
            }
            ally.Teleport(ship.Position + new Vector2(100, 0), 0, Vector2.Zero);
            ally.Presentation.Snap();
            var binding = (EquipmentAbilityHotbarBinding)hotbar.Entries[0]!;
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            targeting.Begin(binding);
            targeting._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true,
                Position = ship.GetCanvasTransform() * ally.DisplayedPosition });
            sync._PhysicsProcess(1d / 60);
            Assert.That(ally.Runtime.Shield.Capacity, Is.EqualTo(150));
            var allyDeadline = ally.Runtime.ActiveBehaviours[0].Deadline;
            ally.Teleport(ship.Position + new Vector2(1000, 0), 0, Vector2.Zero);
            ally.Presentation.Snap();
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            targeting.Begin(binding);
            targeting._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true,
                Position = ship.GetCanvasTransform() * ally.DisplayedPosition });
            Assert.That(targeting.IsTargeting, Is.True);
            targeting._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            Assert.That(targeting.IsTargeting, Is.False);
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ally.EntityId, "shield_generator", (long)generator.Id.Value);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ally.Runtime.ActiveBehaviours[0].Deadline, Is.EqualTo(allyDeadline));
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            targeting.Begin(binding);
            session.Player.InputPolicy.SetHasBlockingWindow(true);
            Assert.That(targeting.IsTargeting, Is.False);
            session.Player.InputPolicy.SetHasBlockingWindow(false);
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            targeting.Begin(binding);
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ship.EntityId, "shield_generator", (long)generator.Id.Value);
            Assert.That(transfers.Transfer(new(edit, slot, generator.Id, ItemCellAddress.Inventory(generatorIndex), null,
                items.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Pending));
            sync._PhysicsProcess(1d / 60);
            Assert.That(targeting.IsTargeting, Is.False);
            Assert.That(hotbar.Resolve(0).Available, Is.False);
            var before = ship.Runtime.ActiveBehaviours[0].Deadline;
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.EqualTo(before));
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" });
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ship.EntityId, AbilitySource.Grant("skill:pilot"));
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.GreaterThan(before));
            before = ship.Runtime.ActiveBehaviours[0].Deadline;
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ship.EntityId, AbilitySource.Grant("skill:pilot"));
            ship.Runtime.Grants.RemoveSource("skill:pilot");
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.EqualTo(before));
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            network.ActivateAbility("shieldCapacityBoost", ship.EntityId, AbilitySource.AllShips);
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.ActiveBehaviours[0].Deadline, Is.EqualTo(before));
            Assert.That(ship.Runtime.Grants.Contains("shieldCapacityBoost"), Is.False);
            Assert.That(ship.Runtime.Shield.Capacity, Is.Zero);
            Assert.That(transfers.Transfer(new(edit, ItemCellAddress.Inventory(generatorIndex), generator.Id, slot, null,
                items.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Pending));
            sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.Shield.Capacity, Is.EqualTo(150), "Live boost resumes when the generator is reinstalled.");
            for (var i = 0; i < 1801; i++) sync._PhysicsProcess(1d / 60);
            Assert.That(ship.Runtime.Shield.Capacity, Is.EqualTo(100));
            Assert.That(ally.Runtime.Shield.Capacity, Is.EqualTo(100));
            var removedSpawn = network.World.ApproveSpawn(3, network.Startup.Player);
            var removedIndex = network.GetWeaponEntityIndex(removedSpawn.EntityId);
            network.World.Despawn(3);
            var feedback = new List<AbilityActivationResult>();
            network.AbilityFeedback += feedback.Add;
            network.ActivateAbility("shieldCapacityBoost", removedSpawn.EntityId, "shield_generator", (long)generator.Id.Value);
            Assert.That(feedback, Is.EqualTo(new[] { AbilityActivationResult.Pending, AbilityActivationResult.TargetIsNotALivingAlly }),
                "A target removed before host admission must complete the pending ability.");
            Assert.That(network.Simulation.AbilityCommands.Count, Is.Zero);
            var requestField = typeof(NetworkManager).GetField("_nextRequest", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var rejectedRequest = (long)requestField.GetValue(network)!;
            receive.Invoke(network, new object[] { 1, sync.SessionWireIdentity, rejectedRequest, shipIndex,
                generation, removedIndex, "shieldCapacityBoost", "shield_generator", (long)generator.Id.Value, (int)AbilitySourceKind.Equipment });
            Assert.That(feedback[^1], Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly), "Duplicate requests retain their terminal result.");
            Assert.That(feedback.Count, Is.EqualTo(3));
            for (var step = 0; step < 6; step++) sync._PhysicsProcess(1d / 60);
            feedback.Clear();
            var unknownRequest = rejectedRequest + 1;
            requestField.SetValue(network, unknownRequest);
            receive.Invoke(network, new object[] { 1, sync.SessionWireIdentity, unknownRequest, shipIndex,
                generation, int.MaxValue, "shieldCapacityBoost", "shield_generator", (long)generator.Id.Value, (int)AbilitySourceKind.Equipment });
            Assert.That(feedback, Is.EqualTo(new[] { AbilityActivationResult.TargetIsNotALivingAlly }),
                "An index removed from the host map must also receive terminal feedback.");
            Assert.That(network.Simulation.AbilityCommands.Count, Is.Zero);
            network.AbilityFeedback -= feedback.Add;
            Assert.That(errors, Is.Empty);
            canvas.UnbindInventory();
        }
        finally
        {
            coordinator.Stop();
            parent.Free();
        }
    }
}
