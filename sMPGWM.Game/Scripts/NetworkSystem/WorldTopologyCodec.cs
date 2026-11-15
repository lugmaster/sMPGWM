using System;
using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.ShipSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class WorldTopologyCodec
{
    // Rows: [peer, generation, entity index]; [entity index, ship definition, x, y, rotation, vx, vy].
    internal static Array Encode(WorldTopology topology, Func<string, int> indexOf)
    {
        var participants = new Array();
        foreach (var participant in topology.Participants)
            participants.Add(new Array { participant.Peer, participant.Generation,
                participant.Entity.Length == 0 ? 0 : indexOf(participant.Entity) });
        var entities = new Array();
        foreach (var entity in topology.Entities)
            entities.Add(new Array { indexOf(entity.EntityId), entity.ShipId, entity.Position.X, entity.Position.Y,
                entity.Rotation, entity.Velocity.X, entity.Velocity.Y });
        return new Array { participants, entities };
    }

    internal static bool TryRead(Array payload, Func<int, string?> resolve, out WorldTopology topology)
    {
        topology = null!;
        if (payload.Count != 2 || payload[0].VariantType != Variant.Type.Array || payload[1].VariantType != Variant.Type.Array) return false;
        using var participantRows = payload[0].AsGodotArray();
        using var entityRows = payload[1].AsGodotArray();
        if (participantRows.Count > MovementWire.MaxPlayers || entityRows.Count > WorldTopology.MaxEntities) return false;
        var participants = new ParticipantBinding[participantRows.Count];
        var entities = new EntitySpawnRecord[entityRows.Count];
        for (var i = 0; i < participants.Length; i++)
        {
            if (participantRows[i].VariantType != Variant.Type.Array) return false;
            using var row = participantRows[i].AsGodotArray();
            if (row.Count != 3 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.Int ||
                row[2].VariantType != Variant.Type.Int || row[0].AsInt64() is <= 0 or > int.MaxValue ||
                row[2].AsInt64() is < 0 or > int.MaxValue) return false;
            var index = row[2].AsInt32();
            var entity = index == 0 ? "" : resolve(index);
            if (entity == null) return false;
            participants[i] = new(row[0].AsInt32(), row[1].AsInt64(), entity);
        }
        for (var i = 0; i < entities.Length; i++)
        {
            if (entityRows[i].VariantType != Variant.Type.Array) return false;
            using var row = entityRows[i].AsGodotArray();
            if (row.Count != 7 || row[0].VariantType != Variant.Type.Int || row[0].AsInt64() is <= 0 or > int.MaxValue ||
                row[1].VariantType != Variant.Type.String || resolve(row[0].AsInt32()) is not { } entity) return false;
            for (var field = 2; field < 7; field++) if (row[field].VariantType != Variant.Type.Float) return false;
            entities[i] = new(entity, row[1].AsString(), new(row[2].AsSingle(), row[3].AsSingle()),
                row[4].AsSingle(), new(row[5].AsSingle(), row[6].AsSingle()));
        }
        topology = new(participants, entities);
        return topology.IsValid();
    }
}
