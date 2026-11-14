using System.IO; // TEST-SUPPORT: WEAPON-ENCODE-001 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using Godot; // TEST-SUPPORT: WEAPON-ENCODE-002 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: WEAPON-ENCODE-003 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: WEAPON-ENCODE-004 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: WEAPON-ENCODE-005 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: WEAPON-ENCODE-006 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
using sMPGWM.Scripts.WorldSystem; // TEST-SUPPORT: WEAPON-ENCODE-007 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership

namespace sMPGWM.Tests; // TEST-SUPPORT: WEAPON-ENCODE-008 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership

public sealed class WeaponEncodingTests // TEST-SUPPORT: WEAPON-ENCODE-009 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
{ // TEST-SUPPORT: WEAPON-ENCODE-010 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
    [TestCase(false), TestCase(true), Category("Godot")] // TEST-SUPPORT: WEAPON-ENCODE-011 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
    public void EncodingPreservesBinaryContractAndPayloadOwnership(bool withShip) // TEST-SUPPORT: WEAPON-ENCODE-012 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
    { // TEST-SUPPORT: WEAPON-ENCODE-013 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        var items = new ItemStore(); // TEST-SUPPORT: WEAPON-ENCODE-014 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        using var owner = new PlayerSession(items); // TEST-SUPPORT: WEAPON-ENCODE-015 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        var ship = withShip ? ShipFactory.Create(ShipRegistry.GetById("fighter"), "00112233445566778899aabbccddeeff", Vector2.Zero, 0, Vector2.Zero, items, true) : null; // TEST-SUPPORT: WEAPON-ENCODE-016 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        try // TEST-SUPPORT: WEAPON-ENCODE-017 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        { // TEST-SUPPORT: WEAPON-ENCODE-018 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            if (ship != null) // TEST-SUPPORT: WEAPON-ENCODE-019 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            { // TEST-SUPPORT: WEAPON-ENCODE-020 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                owner.AssignShip(ship); // TEST-SUPPORT: WEAPON-ENCODE-021 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                StarterLoadout.Equip(ship.Loadout, owner.Inventory); // TEST-SUPPORT: WEAPON-ENCODE-022 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            } // TEST-SUPPORT: WEAPON-ENCODE-023 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(items.CreateItems(owner.Inventory.Container, "vulcan_mk1", 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: WEAPON-ENCODE-024 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(items.CreateItems(owner.Inventory.Container, "laser_mk1", 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: WEAPON-ENCODE-025 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            var id = owner.Inventory.Container.Slots[0]!.Value; // TEST-SUPPORT: WEAPON-ENCODE-026 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(items.Weapons.TryGet(id, out var first), Is.True); // TEST-SUPPORT: WEAPON-ENCODE-027 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            var updated = first.CaptureState() with { Revision = 2, Rounds = 33, NextFireTick = 123.25, ReloadStartTick = 120, ReloadDeadline = 120 + items.GetItem(id).Definition.Weapon!.Stats.ReloadTicks, ShotSequence = 27 }; // TEST-SUPPORT: WEAPON-ENCODE-028 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(items.Weapons.Confirm(new[] { updated }, items), Is.True); // TEST-SUPPORT: WEAPON-ENCODE-029 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            using var stream = new MemoryStream(); // TEST-SUPPORT: WEAPON-ENCODE-030 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            using var writer = new BinaryWriter(stream); // TEST-SUPPORT: WEAPON-ENCODE-031 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            writer.Write(owner.ControlGeneration); // TEST-SUPPORT: WEAPON-ENCODE-032 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            writer.Write(withShip); // TEST-SUPPORT: WEAPON-ENCODE-033 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            if (ship != null) // TEST-SUPPORT: WEAPON-ENCODE-034 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            { // TEST-SUPPORT: WEAPON-ENCODE-035 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                writer.Write(Guid.ParseExact(ship.EntityId, "N").ToByteArray()); // TEST-SUPPORT: WEAPON-ENCODE-036 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                var configuration = ship.Loadout.Weapons.CaptureConfiguration(); // TEST-SUPPORT: WEAPON-ENCODE-037 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                writer.Write(configuration.Revision); // TEST-SUPPORT: WEAPON-ENCODE-038 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                writer.Write(configuration.Groups.Length); // TEST-SUPPORT: WEAPON-ENCODE-039 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                for (var i = 0; i < configuration.Groups.Length; i++) // TEST-SUPPORT: WEAPON-ENCODE-040 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                { // TEST-SUPPORT: WEAPON-ENCODE-041 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(configuration.Groups[i]); // TEST-SUPPORT: WEAPON-ENCODE-042 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(configuration.Items[i].Value); // TEST-SUPPORT: WEAPON-ENCODE-043 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                } // TEST-SUPPORT: WEAPON-ENCODE-044 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            } // TEST-SUPPORT: WEAPON-ENCODE-045 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            writer.Write(items.Weapons.CaptureState().Length); // TEST-SUPPORT: WEAPON-ENCODE-046 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            foreach (var container in new[] { owner.Inventory.Container, ship?.Equipment.Container }) // TEST-SUPPORT: WEAPON-ENCODE-047 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            { // TEST-SUPPORT: WEAPON-ENCODE-048 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                if (container == null) continue; // TEST-SUPPORT: WEAPON-ENCODE-049 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                foreach (var item in container.Slots) // TEST-SUPPORT: WEAPON-ENCODE-050 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                { // TEST-SUPPORT: WEAPON-ENCODE-051 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    if (item is not { } itemId || !items.Weapons.TryGet(itemId, out var state)) continue; // TEST-SUPPORT: WEAPON-ENCODE-052 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    var snapshot = state.CaptureState(); // TEST-SUPPORT: WEAPON-ENCODE-053 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.ItemId.Value); // TEST-SUPPORT: WEAPON-ENCODE-054 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.Revision); // TEST-SUPPORT: WEAPON-ENCODE-055 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.Rounds); // TEST-SUPPORT: WEAPON-ENCODE-056 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.NextFireTick); // TEST-SUPPORT: WEAPON-ENCODE-057 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.ReloadStartTick); // TEST-SUPPORT: WEAPON-ENCODE-058 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.ReloadDeadline); // TEST-SUPPORT: WEAPON-ENCODE-059 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                    writer.Write(snapshot.ShotSequence); // TEST-SUPPORT: WEAPON-ENCODE-060 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                } // TEST-SUPPORT: WEAPON-ENCODE-061 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            } // TEST-SUPPORT: WEAPON-ENCODE-062 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            var encoded = WeaponStateCodec.Encode(owner, items); // TEST-SUPPORT: WEAPON-ENCODE-063 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(encoded, Is.EqualTo(stream.ToArray())); // TEST-SUPPORT: WEAPON-ENCODE-064 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(WeaponStateCodec.Apply(encoded, owner, items, out var applied), Is.True); // TEST-SUPPORT: WEAPON-ENCODE-065 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(applied, Is.True); // TEST-SUPPORT: WEAPON-ENCODE-066 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(items.Weapons.Confirm(new[] { updated with { Revision = 3, Rounds = 32 } }, items), Is.True); // TEST-SUPPORT: WEAPON-ENCODE-067 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            var next = WeaponStateCodec.Encode(owner, items); // TEST-SUPPORT: WEAPON-ENCODE-068 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(next, Is.Not.EqualTo(encoded)); // TEST-SUPPORT: WEAPON-ENCODE-069 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            Assert.That(encoded, Is.EqualTo(stream.ToArray()), "Subsequent encoding must not overwrite a payload already handed to transport."); // TEST-SUPPORT: WEAPON-ENCODE-070 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        } // TEST-SUPPORT: WEAPON-ENCODE-071 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        finally // TEST-SUPPORT: WEAPON-ENCODE-072 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        { // TEST-SUPPORT: WEAPON-ENCODE-073 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            owner.AssignShip(null); // TEST-SUPPORT: WEAPON-ENCODE-074 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            if (ship != null) // TEST-SUPPORT: WEAPON-ENCODE-075 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            { // TEST-SUPPORT: WEAPON-ENCODE-076 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                ship.Loadout.Dispose(); // TEST-SUPPORT: WEAPON-ENCODE-077 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                ship.Runtime.Dispose(); // TEST-SUPPORT: WEAPON-ENCODE-078 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
                ship.Free(); // TEST-SUPPORT: WEAPON-ENCODE-079 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
            } // TEST-SUPPORT: WEAPON-ENCODE-080 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
        } // TEST-SUPPORT: WEAPON-ENCODE-081 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
    } // TEST-SUPPORT: WEAPON-ENCODE-082 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
} // TEST-SUPPORT: WEAPON-ENCODE-083 | Tests: sMPGWM.Tests.WeaponEncodingTests.EncodingPreservesBinaryContractAndPayloadOwnership
