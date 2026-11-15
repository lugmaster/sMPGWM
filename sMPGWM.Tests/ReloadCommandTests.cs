using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ReloadCommandTests
{
    [Test]
    public void AmmunitionCapabilityAllowsReloadForOtherWeaponDefinitions()
    {
        var world = CreateWorld();
        try
        {
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            var equipment = ship.Equipment;
            var target = equipment.FindSlot("weapon_3");
            for (var i = 0; i < owner.Inventory.Snapshot.Slots.Count; i++)
            {
                var item = owner.Inventory.Snapshot.Slots[i];
                if (item?.DefinitionId != "mock_standard_medium_weapon") continue;
                Assert.That(world.Items.TransferItem(item.Id, new(owner.Inventory.Container.Id, i),
                    new(equipment.Container.Id, target), owner.Inventory.Revision, equipment.Revision,
                    equipment.Snapshot.Slots[target]!.Id), Is.EqualTo(sMPGWM.Scripts.ItemSystem.TransferResult.Success));
                item.WeaponState!.TryAccept(0, 1, true);
                item.WeaponState.CancelReload();
                var commands = new ReloadCommandProcessor(world);
                Assert.That(commands.TryEnqueue(new(1, 1, ship.EntityId, owner.ControlGeneration,
                    equipment.Revision, [item.Id.Value])), Is.True);
                commands.Step(1, _ => true);
                Assert.That(item.WeaponState.IsReloading, Is.True);
                return;
            }
            Assert.Fail("Missing ammunition weapon fixture.");
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void ManualReloadWithoutGroupsRevalidatesQueuedEquipment()
    {
        var world = CreateWorld();
        try
        {
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            ship.Equipment.ApplyWeaponGroupDefinition([], []);
            var item = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!;
            var state = item.WeaponState!;
            state.TryAccept(0, 15, true);
            var queue = new ReloadCommandProcessor(world);
            var result = ReloadResult.Pending;
            queue.Completed = (_, _, outcome) => result = outcome;
            var command = new PendingReload(1, 1, ship.EntityId, owner.ControlGeneration, ship.Equipment.Revision, [item.Id.Value]);
            Assert.That(queue.TryEnqueue(command), Is.True);
            queue.Step(1, _ => true);
            Assert.That(result, Is.EqualTo(ReloadResult.Applied));
            Assert.That(state.Rounds, Is.EqualTo(59));
            Assert.That(state.ReloadDeadlineTick, Is.EqualTo(91));
            state.CancelReload();
            Assert.That(queue.TryEnqueue(command with { Request = 2 }), Is.True);
            ship.Equipment.ApplyWeaponGroupDefinition(["weapon_1"], []);
            queue.Step(7, _ => true);
            Assert.That(result, Is.EqualTo(ReloadResult.InvalidBinding));
            Assert.That(state.IsReloading, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void MalformedBatchAndUnavailableControllerCannotStartReload()
    {
        var world = CreateWorld();
        try
        {
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            var item = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!;
            item.WeaponState!.TryAccept(0, 15, true);
            var queue = new ReloadCommandProcessor(world);
            var command = new PendingReload(1, 1, ship.EntityId, owner.ControlGeneration, ship.Equipment.Revision, [item.Id.Value, ulong.MaxValue]);
            Assert.That(queue.TryEnqueue(command), Is.False);
            Assert.That(item.WeaponState.IsReloading, Is.False);
            command = command with { Items = [item.Id.Value] };
            Assert.That(queue.TryEnqueue(command), Is.True);
            queue.Step(1, _ => false);
            Assert.That(item.WeaponState.IsReloading, Is.False);
            Assert.That(queue.TryEnqueue(command), Is.True);
            queue.RemovePeer(1);
            queue.Step(2, _ => true);
            Assert.That(item.WeaponState.IsReloading, Is.False);
            for (var i = 1; i <= 32; i++) Assert.That(queue.TryEnqueue(command with { Request = i }), Is.True);
            Assert.That(queue.TryEnqueue(command with { Request = 33 }), Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    private static GameWorld CreateWorld()
    {
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = DevelopmentLoadout.Equip };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        world.ApproveSpawn(1, PlayerStartupData.Create());
        return world;
    }
}
