using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.WorldSystem;

[Category("Godot")]
public sealed class WeaponGroupRestorationTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void WorldCapturePreservesWeaponConfigurationBeforePublishingRestoredShips(bool primary, bool secondary)
    {
        var source = new GameWorld { AuthoritativeItems = true, InitializeLoadout = DevelopmentLoadout.Equip };
        var target = new GameWorld
        {
            AuthoritativeItems = true,
            InitializeLoadout = (_, _) => Assert.Fail("Restoration reapplied startup groups.")
        };
        try
        {
            var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
            root.AddChild(source);
            root.AddChild(target);
            source.ApproveSpawn(1, PlayerStartupData.Create());
            var equipment = source.Players[1].ControlledShip!.Equipment;
            var bindings = equipment.Snapshot.WeaponGroups.Bindings;
            Assert.That(bindings.Count, Is.EqualTo(3));
            foreach (var binding in bindings)
                Assert.That(equipment.SetWeaponGroups(binding.Slot, binding.Item, equipment.Revision, primary, secondary),
                    Is.AnyOf(TransferResult.Success, TransferResult.NoChange));
            var saved = source.CaptureState(0, 0);
            var savedShip = saved.Ships[0];
            Assert.That(savedShip.WeaponGroups.Bindings, Is.EqualTo(equipment.Snapshot.WeaponGroups.Bindings));
            var published = 0;
            target.EntitySpawned += ship =>
            {
                Assert.That(ship.Equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(savedShip.WeaponGroups.Bindings));
                Assert.That(ship.Equipment.Revision, Is.EqualTo(savedShip.Equipment.Revision));
                published++;
            };
            target.RestoreState(saved);
            target.InitializePlayerLoadout(1);
            Assert.That(published, Is.EqualTo(1));
            var restored = target.Players[1].ControlledShip!.Equipment;
            Assert.That(restored.Snapshot.WeaponGroups.Bindings, Is.EqualTo(savedShip.WeaponGroups.Bindings));
            var recaptured = target.CaptureState(0, 0);
            Assert.That(recaptured.Ships[0].WeaponGroups.Bindings, Is.EqualTo(savedShip.WeaponGroups.Bindings));
            Assert.That(recaptured.Ships[0].Equipment.Revision, Is.EqualTo(savedShip.Equipment.Revision));
        }
        finally
        {
            target.Clear();
            target.Free();
            source.Clear();
            source.Free();
        }
    }
}
