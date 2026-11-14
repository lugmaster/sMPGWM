using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class EquipmentAbilityTests
{
    [Test]
    public void OwnerSnapshotsValidateAtomicallyAndPreserveIdentity()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        host.Store.CreateItems(host.Inventory.Container, "mock_standard_shield_generator", 2);
        var generator = host.Inventory.Snapshot.Slots[0]!;
        var initial = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(initial, client.Player), Is.True);
        Assert.That(client.Inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(generator.Id));
        var transfer = new TransferRequest(0, ItemCellAddress.Inventory(0), generator.Id,
            ItemCellAddress.Equipment("shield_generator"), null, host.Inventory.Revision, host.Equipment.Revision);
        Assert.That(host.Transfer(transfer), Is.EqualTo(TransferResult.Success));
        Assert.That(host.Transfer(transfer), Is.EqualTo(TransferResult.StaleRevision));
        var confirmed = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(confirmed, client.Player), Is.True);
        Assert.That(client.Provides("shield_generator", (long)generator.Id.Value, "shieldCapacityBoost"), Is.True);
        Assert.That(client.Provides("shield_generator", (long)generator.Id.Value + 100000, "shieldCapacityBoost"), Is.False);
        Assert.That(ItemSnapshotCodec.Apply(initial, client.Player), Is.True);
        Assert.That(client.Provides("shield_generator", (long)generator.Id.Value, "shieldCapacityBoost"), Is.True);
        var malformed = confirmed.Duplicate(true);
        var inventory = malformed[7].AsGodotArray();
        inventory[0] = new Godot.Collections.Array { (long)generator.Id.Value, generator.DefinitionId, 1 };
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
        Assert.That(client.Inventory.Snapshot.Slots[0], Is.Null);
        malformed = confirmed.Duplicate(true);
        malformed[8].AsGodotArray()[host.Equipment.FindSlot("engine")] =
            new Godot.Collections.Array { 999999L, generator.DefinitionId, 1 };
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
        malformed = confirmed.Duplicate(true);
        malformed[7].AsGodotArray()[0] = new Godot.Collections.Array { 999998L, "unknown", 1 };
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
    }

    [Test]
    public void CursorVariantsAreCachedTintedAndKeepAlpha()
    {
        var green = Cursors.GreenTargetCursor;
        Assert.That(Cursors.GreenTargetCursor, Is.SameAs(green));
        using var source = GD.Load<Texture2D>("res://assets/ui/cursors/crosshair.png").GetImage();
        if (source.IsCompressed()) source.Decompress();
        var textures = new[] { Cursors.RedTargetCursor, Cursors.GreenTargetCursor, Cursors.BlueTargetCursor };
        for (var variant = 0; variant < textures.Length; variant++)
        {
            using var tinted = textures[variant].GetImage();
            Assert.That(tinted.GetSize(), Is.EqualTo(source.GetSize()));
            var visible = false;
            for (var y = 0; y < source.GetHeight(); y++)
                for (var x = 0; x < source.GetWidth(); x++)
                {
                    var original = source.GetPixel(x, y);
                    var pixel = tinted.GetPixel(x, y);
                    Assert.That(pixel.A, Is.EqualTo(original.A).Within(.005));
                    if (original.A < .5f || original.R < .5f) continue;
                    visible = true;
                    Assert.That(variant == 0 ? pixel.R : variant == 1 ? pixel.G : pixel.B, Is.GreaterThan(0));
                    Assert.That(variant == 0 ? pixel.G + pixel.B : variant == 1 ? pixel.R + pixel.B : pixel.R + pixel.G, Is.Zero);
                }
            Assert.That(visible, Is.True);
        }
    }
}
