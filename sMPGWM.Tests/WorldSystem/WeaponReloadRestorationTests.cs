using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.WorldSystem;

[Category("Godot")]
public sealed class WeaponReloadRestorationTests
{
    [Test]
    public void RestoredReloadKeepsOriginalDeadlineAndCompletesWithoutReadyOwner()
    {
        var source = CreateWorld(true);
        var target = CreateWorld(false);
        try
        {
            var original = source.Players[1].ControlledShip!;
            var state = Magazine(original);
            Assert.That(original.Runtime.WeaponsAvailable, Is.True);
            Assert.That(state.TryAccept(0, 15, true), Is.True);
            Assert.That(state.TryStartReload(10), Is.True);
            var saved = source.CaptureState(50, 0);
            target.RestoreState(saved);
            var ship = target.Players[1].ControlledShip!;
            var restored = Magazine(ship);
            Assert.That(restored.Rounds, Is.EqualTo(59));
            Assert.That(restored.ReloadStartTick, Is.EqualTo(10));
            Assert.That(restored.ReloadDeadlineTick, Is.EqualTo(100));
            Assert.That(restored.Revision, Is.EqualTo(state.Revision));
            using var simulation = new WeaponSimulation(target);
            simulation.MaintainReloads(99, _ => false);
            Assert.That(restored.IsReloading, Is.True);
            target.Players[1].AssignShip(null);
            simulation.MaintainReloads(100, _ => false);
            Assert.That(restored.Rounds, Is.EqualTo(60));
            Assert.That(restored.IsReloading, Is.False);
            Assert.That(restored.Sequence, Is.EqualTo(1));
            Assert.That(state.IsReloading, Is.True);
        }
        finally { target.Clear(); target.Free(); source.Clear(); source.Free(); }
    }

    [TestCase(ShipComponentKind.Power)]
    [TestCase(ShipComponentKind.Weapons)]
    public void RemovingRequiredComponentOnDeadlineCancelsWithoutFilling(ShipComponentKind component)
    {
        var world = CreateWorld(true);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            var state = Magazine(ship);
            state.TryAccept(0, 15, true);
            state.TryStartReload(1);
            Assert.That(state.ReloadDeadlineTick, Is.EqualTo(91));
            using var simulation = new WeaponSimulation(world);
            ship.Runtime.StageComponentChange(component, false);
            ship.Runtime.CommitConfiguration(91);
            simulation.MaintainReloads(91, _ => false);
            Assert.That(state.IsReloading, Is.False);
            Assert.That(state.Rounds, Is.EqualTo(59));
            ship.Runtime.StageComponentChange(component, true);
            ship.Runtime.CommitConfiguration(92);
            simulation.MaintainReloads(92, _ => true);
            Assert.That(state.Rounds, Is.EqualTo(59));
            Assert.That(state.IsReloading, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void InventoryReloadIsRejectedBeforeInstallingWorld()
    {
        var source = CreateWorld(true);
        var target = CreateWorld(false);
        try
        {
            var player = source.Players[1];
            Assert.That(source.Items.CreateItems(player.Inventory.Container, "vulcan_mk1", 1), Is.EqualTo(InventoryResult.Success));
            var saved = source.CaptureState(50, 0);
            WeaponRuntimeState? invalid = null;
            foreach (var item in saved.Players[0].Inventory.Slots)
                if (item?.DefinitionId == "vulcan_mk1") { invalid = item.WeaponState; break; }
            Assert.That(invalid, Is.Not.Null);
            invalid!.TryAccept(0, 15, true);
            invalid.TryStartReload(10);
            Assert.Throws<ArgumentException>(() => target.RestoreState(saved));
            Assert.That(target.Entities, Is.Empty);
        }
        finally { target.Clear(); target.Free(); source.Clear(); source.Free(); }
    }

    private static WeaponRuntimeState Magazine(ShipController ship) =>
        ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!.WeaponState!;

    private static GameWorld CreateWorld(bool spawn)
    {
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = DevelopmentLoadout.Equip };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        if (spawn) world.ApproveSpawn(1, PlayerStartupData.Create());
        return world;
    }
}
