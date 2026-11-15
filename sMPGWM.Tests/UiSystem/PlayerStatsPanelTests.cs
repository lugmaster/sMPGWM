using System.Reflection;
using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;
using PlayerController = sMPGWM.Scripts.PlayerControllerSystem.PlayerController;
using StatBar = sMPGWM.Scripts.UtilitySystem.UiComponents.StatBar;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class PlayerStatsPanelTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ShieldTracksBindingMutationReplicationReplacementAndDisposal(bool bindBeforeReady)
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var parent = new Node2D();
        root.AddChild(parent);
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        parent.AddChild(canvas);
        using var items = new ShipItemTestContext();
        using var transfers = new InventoryEquipmentTransferService(items.Inventory, items.Equipment);
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        var player = new PlayerController();
        player.Initialize(canvas, transfers, _ => { }, parent);
        parent.AddChild(player);
        var world = new GameWorld { LocalPlayer = player };
        parent.AddChild(world);
        var network = new NetworkManager { World = world, Abilities = runtime };
        var panel = GD.Load<PackedScene>("res://scenes/ui/player_hud/player_stats_panel.tscn").Instantiate<PlayerStatsPanel>();
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            ship.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 3000, 0, 0, 0, false, false, true, false, false), 0);
            ship.Runtime.InitializeResources();
            ship.Runtime.SetShieldCurrent(28);
            if (bindBeforeReady) panel.Bind(network);
            parent.AddChild(panel);
            if (!bindBeforeReady) panel.Bind(network);
            var bar = panel.GetNode<StatBar>("%BarContainer/ShieldBar");
            var value = bar.GetNode<Label>("%ValueLabel");
            var progress = bar.GetNode<ProgressBar>("%ProgressBar");
            var healthBar = panel.GetNode<StatBar>("%BarContainer/HealthBar");
            var healthValue = healthBar.GetNode<Label>("%ValueLabel");
            var healthProgress = healthBar.GetNode<ProgressBar>("%ProgressBar");
            Assert.That(panel.GetNode<Container>("%BarContainer").GetChildCount(), Is.EqualTo(3));
            Assert.That(panel.GetNode<Container>("%GridContainer").GetChildCount(), Is.EqualTo(4));
            Assert.That(healthBar.Title, Is.EqualTo("Health"));
            Assert.That(healthValue.Text, Is.EqualTo("150/150"));
            Assert.That(healthProgress.Value, Is.EqualTo(150));
            Assert.That(healthProgress.MaxValue, Is.EqualTo(150));
            Assert.That(bar.Title, Is.EqualTo("Shield"));
            Assert.That(value.Text, Is.EqualTo("28/100"));
            Assert.That(progress.Value, Is.EqualTo(28));
            Assert.That(progress.MaxValue, Is.EqualTo(100));

            ship.Runtime.SetShieldCurrent(18);
            ship.Runtime.SetHealthCurrent(82);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("18/100"));
            Assert.That(healthValue.Text, Is.EqualTo("82/150"));
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" });
            Assert.That(runtime.Activate("shieldCapacityBoost", new(ship.Runtime, ship.Runtime, 0), 1),
                Is.EqualTo(AbilityActivationResult.Accepted));
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("18/150"));
            Assert.That(healthValue.Text, Is.EqualTo("82/150"));
            Assert.That(progress.MaxValue, Is.EqualTo(150));

            var authoritative = new ShipRuntime(100, 40);
            authoritative.Confirm(20, 160, 125, Array.Empty<BehaviourInstance>(), 0, Array.Empty<string>(), authoritative.Stats with { ShieldCapacity = 160 }, 0, false, true, 0, 0, 1, false, 150, 100);
            var entityNames = (Dictionary<int, string>)typeof(NetworkManager).GetField("_weaponEntityNames", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(network)!;
            entityNames.Add(1, ship.EntityId);
            var stateRow = GameplaySnapshotCodec.Encode(ship.EntityId, authoritative);
            stateRow[0] = 1;
            var snapshot = new Godot.Collections.Array { stateRow };
            var apply = typeof(NetworkManager).GetMethod("ApplyGameplay", BindingFlags.Instance | BindingFlags.NonPublic)!;
            using var missing = new Godot.Collections.Array();
            Assert.That(apply.Invoke(network, new object[] { missing, true }), Is.False);
            using var unknown = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), authoritative) };
            Assert.That(apply.Invoke(network, new object[] { unknown, true }), Is.False);
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(18));
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(82));
            Assert.That(apply.Invoke(network, new object[] { snapshot, true }), Is.True);
            Assert.That(apply.Invoke(network, new object[] { snapshot, false }), Is.True);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("125/160"));
            Assert.That(healthValue.Text, Is.EqualTo("100/150"));
            Assert.That(healthProgress.Value, Is.EqualTo(100));
            Assert.That(healthProgress.MaxValue, Is.EqualTo(150));
            Assert.That(progress.Value, Is.EqualTo(125));
            Assert.That(progress.MaxValue, Is.EqualTo(160));
            var displayedText = value.Text;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 120; i++) panel._Process(1d / 60);
            var idleBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Assert.That(value.Text, Is.EqualTo(displayedText));
            TestContext.Out.WriteLine($"Unchanged stats HUD: 120 frames, {idleBytes} managed bytes");
            Assert.That(idleBytes, Is.Zero);

            var replacement = world.SpawnEntity(new(Guid.NewGuid().ToString("N"), "fighter", Vector2.Zero, 0, Vector2.Zero));
            replacement.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 3000, 0, 0, 0, false, false, true, false, false), 0);
            replacement.Runtime.SetShieldCurrent(7);
            replacement.Runtime.SetHealthCurrent(47);
            owner.AssignShip(replacement);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("7/100"));
            Assert.That(healthValue.Text, Is.EqualTo("47/150"));
            ship.Runtime.SetShieldCurrent(3);
            ship.Runtime.SetHealthCurrent(12);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("7/100"));
            Assert.That(healthValue.Text, Is.EqualTo("47/150"));

            owner.AssignShip(null);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("0/0"));
            Assert.That(progress.Value, Is.Zero);
            Assert.That(progress.MaxValue, Is.Zero);
            Assert.That(healthValue.Text, Is.EqualTo("0/0"));
            Assert.That(healthProgress.Value, Is.Zero);
            Assert.That(healthProgress.MaxValue, Is.Zero);
            owner.AssignShip(replacement);
            panel._Process(1d / 60);
            panel.Bind(null);
            Assert.That(healthValue.Text, Is.EqualTo("0/0"));
            Assert.That(value.Text, Is.EqualTo("0/0"));
            replacement.Runtime.SetShieldCurrent(9);
            replacement.Runtime.SetHealthCurrent(35);
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("0/0"));
            panel.Bind(network);
            Assert.That(value.Text, Is.EqualTo("9/100"));
            Assert.That(healthValue.Text, Is.EqualTo("35/150"));
            parent.RemoveChild(panel);
            Assert.That(healthValue.Text, Is.EqualTo("0/0"));
            Assert.That(value.Text, Is.EqualTo("0/0"));
            parent.AddChild(panel);
            Assert.That(healthValue.Text, Is.EqualTo("0/0"));
            panel._Process(1d / 60);
            Assert.That(value.Text, Is.EqualTo("0/0"));
            panel.Bind(network);
            panel.Free();
            replacement.Runtime.SetShieldCurrent(11);
            owner.AssignShip(ship);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(panel)) panel.Free();
            player.Shutdown();
            network.Free();
            parent.Free();
        }
    }
}

