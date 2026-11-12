using System.Reflection; // TEST-SUPPORT: STATS-HUD-001 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using Godot; // TEST-SUPPORT: STATS-HUD-002 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: STATS-HUD-003 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: STATS-HUD-004 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.Items; // TEST-SUPPORT: STATS-HUD-005 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: STATS-HUD-006 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: STATS-HUD-007 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.UiSystem; // TEST-SUPPORT: STATS-HUD-008 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.UiSystem.HudSystem; // TEST-SUPPORT: STATS-HUD-009 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.Util.UiComponents; // TEST-SUPPORT: STATS-HUD-010 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
using sMPGWM.Scripts.World; // TEST-SUPPORT: STATS-HUD-011 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: STATS-HUD-012 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

[Category("Godot")] // TEST-SUPPORT: STATS-HUD-013 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
public sealed class PlayerStatsPanelTests // TEST-SUPPORT: STATS-HUD-014 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
{
    [TestCase(false)] // TEST-SUPPORT: STATS-HUD-015 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
    [TestCase(true)] // TEST-SUPPORT: STATS-HUD-016 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
    public void ShieldTracksBindingMutationReplicationReplacementAndDisposal(bool bindBeforeReady) // TEST-SUPPORT: STATS-HUD-017 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: STATS-HUD-018 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var parent = new Node2D(); // TEST-SUPPORT: STATS-HUD-019 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        root.AddChild(parent); // TEST-SUPPORT: STATS-HUD-020 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>(); // TEST-SUPPORT: STATS-HUD-021 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        parent.AddChild(canvas); // TEST-SUPPORT: STATS-HUD-022 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        using var items = new ShipItemTestContext(); // TEST-SUPPORT: STATS-HUD-023 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        using var transfers = new InventoryEquipmentTransferService(items.Inventory, items.Equipment); // TEST-SUPPORT: STATS-HUD-024 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: STATS-HUD-025 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var player = new PlayerController(); // TEST-SUPPORT: STATS-HUD-026 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        player.Initialize(canvas, transfers, _ => { }, parent); // TEST-SUPPORT: STATS-HUD-027 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        parent.AddChild(player); // TEST-SUPPORT: STATS-HUD-028 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var world = new GameWorld { LocalPlayer = player }; // TEST-SUPPORT: STATS-HUD-029 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        parent.AddChild(world); // TEST-SUPPORT: STATS-HUD-030 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var network = new NetworkManager { World = world, Abilities = runtime }; // TEST-SUPPORT: STATS-HUD-031 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        var panel = GD.Load<PackedScene>("res://scenes/ui/player_hud/player_stats_panel.tscn").Instantiate<PlayerStatsPanel>(); // TEST-SUPPORT: STATS-HUD-032 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        try // TEST-SUPPORT: STATS-HUD-033 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: STATS-HUD-034 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var owner = world.Players[1]; // TEST-SUPPORT: STATS-HUD-035 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var ship = owner.ControlledShip!; // TEST-SUPPORT: STATS-HUD-036 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            ship.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 3000, 0, 0, 0, false, false, true, false, false), 0); // TEST-SUPPORT: STATS-HUD-EQUIP-001 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            ship.Runtime.SetShieldCurrent(28); // TEST-SUPPORT: STATS-HUD-037 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            if (bindBeforeReady) panel.Bind(network); // TEST-SUPPORT: STATS-HUD-038 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            parent.AddChild(panel); // TEST-SUPPORT: STATS-HUD-039 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            if (!bindBeforeReady) panel.Bind(network); // TEST-SUPPORT: STATS-HUD-040 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var bar = panel.GetNode<StatBar>("%BarContainer/StatBar"); // TEST-SUPPORT: STATS-HUD-041 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var value = bar.GetNode<Label>("%ValueLabel"); // TEST-SUPPORT: STATS-HUD-042 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var progress = bar.GetNode<ProgressBar>("%ProgressBar"); // TEST-SUPPORT: STATS-HUD-043 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(bar.Title, Is.EqualTo("Shield")); // TEST-SUPPORT: STATS-HUD-044 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("28/100")); // TEST-SUPPORT: STATS-HUD-045 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.Value, Is.EqualTo(28)); // TEST-SUPPORT: STATS-HUD-046 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.MaxValue, Is.EqualTo(100)); // TEST-SUPPORT: STATS-HUD-047 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

            ship.Runtime.SetShieldCurrent(18); // TEST-SUPPORT: STATS-HUD-048 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-049 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("18/100")); // TEST-SUPPORT: STATS-HUD-050 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: STATS-HUD-051 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(runtime.Activate("shieldCapacityBoost", new(ship.Runtime, ship.Runtime, 0), 1), // TEST-SUPPORT: STATS-HUD-052 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
                Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: STATS-HUD-053 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-054 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("18/150")); // TEST-SUPPORT: STATS-HUD-055 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.MaxValue, Is.EqualTo(150)); // TEST-SUPPORT: STATS-HUD-056 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

            var authoritative = new ShipRuntime(100, 40); // TEST-SUPPORT: STATS-HUD-057 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            authoritative.Confirm(20, 160, 125, Array.Empty<BehaviourInstance>(), 0, Array.Empty<string>(), authoritative.Stats with { ShieldCapacity = 160 }, 0, false, true, 0, 0, 1, false); // TEST-SUPPORT: STATS-HUD-058 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var snapshot = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(ship.EntityId, authoritative) }; // TEST-SUPPORT: STATS-HUD-059 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var apply = typeof(NetworkManager).GetMethod("ApplyGameplay", BindingFlags.Instance | BindingFlags.NonPublic)!; // TEST-SUPPORT: STATS-HUD-060 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(apply.Invoke(network, new object[] { snapshot }), Is.True); // TEST-SUPPORT: STATS-HUD-061 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-062 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("125/160")); // TEST-SUPPORT: STATS-HUD-063 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.Value, Is.EqualTo(125)); // TEST-SUPPORT: STATS-HUD-064 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.MaxValue, Is.EqualTo(160)); // TEST-SUPPORT: STATS-HUD-065 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var displayedText = value.Text; // TEST-SUPPORT: STATS-HUD-066 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var allocated = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: STATS-HUD-067 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            for (var i = 0; i < 120; i++) panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-068 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            var idleBytes = GC.GetAllocatedBytesForCurrentThread() - allocated; // TEST-SUPPORT: STATS-HUD-069 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo(displayedText)); // TEST-SUPPORT: STATS-HUD-070 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            TestContext.Out.WriteLine($"Unchanged stats HUD: 120 frames, {idleBytes} managed bytes"); // TEST-SUPPORT: STATS-HUD-071 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(idleBytes, Is.Zero); // TEST-SUPPORT: STATS-HUD-072 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

            var replacement = world.SpawnEntity(new(Guid.NewGuid().ToString("N"), "fighter", Vector2.Zero, 0, Vector2.Zero)); // TEST-SUPPORT: STATS-HUD-073 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            replacement.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 3000, 0, 0, 0, false, false, true, false, false), 0); // TEST-SUPPORT: STATS-HUD-EQUIP-002 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            replacement.Runtime.SetShieldCurrent(7); // TEST-SUPPORT: STATS-HUD-074 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            owner.AssignShip(replacement); // TEST-SUPPORT: STATS-HUD-075 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-076 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("7/100")); // TEST-SUPPORT: STATS-HUD-077 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            ship.Runtime.SetShieldCurrent(3); // TEST-SUPPORT: STATS-HUD-078 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-079 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("7/100")); // TEST-SUPPORT: STATS-HUD-080 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal

            owner.AssignShip(null); // TEST-SUPPORT: STATS-HUD-081 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-082 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: STATS-HUD-083 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.Value, Is.Zero); // TEST-SUPPORT: STATS-HUD-084 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(progress.MaxValue, Is.Zero); // TEST-SUPPORT: STATS-HUD-085 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            owner.AssignShip(replacement); // TEST-SUPPORT: STATS-HUD-086 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-087 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel.Bind(null); // TEST-SUPPORT: STATS-HUD-088 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: STATS-HUD-089 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            replacement.Runtime.SetShieldCurrent(9); // TEST-SUPPORT: STATS-HUD-090 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-091 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: STATS-HUD-092 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel.Bind(network); // TEST-SUPPORT: STATS-HUD-093 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("9/100")); // TEST-SUPPORT: STATS-HUD-094 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            parent.RemoveChild(panel); // TEST-SUPPORT: STATS-HUD-095 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: STATS-HUD-096 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            parent.AddChild(panel); // TEST-SUPPORT: STATS-HUD-097 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel._Process(1d / 60); // TEST-SUPPORT: STATS-HUD-098 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            Assert.That(value.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: STATS-HUD-099 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel.Bind(network); // TEST-SUPPORT: STATS-HUD-100 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            panel.Free(); // TEST-SUPPORT: STATS-HUD-101 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            replacement.Runtime.SetShieldCurrent(11); // TEST-SUPPORT: STATS-HUD-102 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            owner.AssignShip(ship); // TEST-SUPPORT: STATS-HUD-103 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        }
        finally // TEST-SUPPORT: STATS-HUD-104 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        {
            if (GodotObject.IsInstanceValid(panel)) panel.Free(); // TEST-SUPPORT: STATS-HUD-105 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            player.Shutdown(); // TEST-SUPPORT: STATS-HUD-106 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            network.Free(); // TEST-SUPPORT: STATS-HUD-107 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
            parent.Free(); // TEST-SUPPORT: STATS-HUD-108 | Tests: sMPGWM.Tests.UiSystem.PlayerStatsPanelTests.ShieldTracksBindingMutationReplicationReplacementAndDisposal
        }
    }
}

