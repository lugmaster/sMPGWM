using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class WorldTopologyCodecTests
{
    [Test]
    public void NumericTopologyRoundTripsShiplessParticipantsAndUncontrolledEntities()
    {
        GameConfig.EnsureCompatibilityData();
        var first = Guid.NewGuid().ToString("N");
        var second = Guid.NewGuid().ToString("N");
        var topology = new WorldTopology([new(1, 2, ""), new(2, 3, first)],
            [new(first, "fighter", new(10, 20), 0.5f, new(2, -3)), new(second, "fighter", new(-7, 4), 1, Vector2.Zero)]);
        using var wire = WorldTopologyCodec.Encode(topology, name => name == first ? 7 : 9);
        AssertScalars(wire);
        Assert.That(wire[0].AsGodotArray()[1].AsGodotArray()[2].AsInt32(), Is.EqualTo(7));
        Assert.That(WorldTopologyCodec.TryRead(wire, index => index == 7 ? first : index == 9 ? second : null, out var decoded), Is.True);
        Assert.That(decoded.Participants, Is.EqualTo(topology.Participants));
        Assert.That(decoded.Entities, Is.EqualTo(topology.Entities));
        Assert.That(WorldTopologyCodec.TryRead(wire, _ => null, out _), Is.False);
    }

    [TestCase("unknown")]
    [TestCase("zero")]
    [TestCase("overflow")]
    [TestCase("legacyString")]
    [TestCase("vector")]
    [TestCase("nonfinite")]
    [TestCase("duplicate")]
    public void MalformedTopologyIsRejected(string kind)
    {
        GameConfig.EnsureCompatibilityData();
        var entity = Guid.NewGuid().ToString("N");
        var topology = new WorldTopology([new(1, 1, entity)], [new(entity, "fighter", Vector2.Zero, 0, Vector2.Zero)]);
        using var wire = WorldTopologyCodec.Encode(topology, _ => 7);
        using var participants = wire[0].AsGodotArray();
        using var participant = participants[0].AsGodotArray();
        using var entities = wire[1].AsGodotArray();
        using var row = entities[0].AsGodotArray();
        switch (kind)
        {
            case "unknown": participant[2] = 8; break;
            case "zero": row[0] = 0; break;
            case "overflow": row[0] = (long)int.MaxValue + 1; break;
            case "legacyString": participant[2] = entity; break;
            case "vector": row[2] = Vector2.Zero; break;
            case "nonfinite": row[2] = double.PositiveInfinity; break;
            case "duplicate": entities.Add(row); break;
        }
        Assert.That(WorldTopologyCodec.TryRead(wire, index => index == 7 ? entity : null, out _), Is.False);
    }

    private static void AssertScalars(Godot.Collections.Array values)
    {
        foreach (var value in values)
        {
            if (value.VariantType == Variant.Type.Array)
            {
                using var nested = value.AsGodotArray();
                AssertScalars(nested);
            }
            else Assert.That(value.VariantType, Is.AnyOf(Variant.Type.Int, Variant.Type.Float, Variant.Type.String, Variant.Type.Bool));
        }
    }
}
