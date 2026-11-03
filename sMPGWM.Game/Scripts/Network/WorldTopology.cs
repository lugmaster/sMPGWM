using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.Network;

public sealed record ParticipantBinding(int Peer, long Generation, string Entity);

/// <summary>Reliable participant presence, entity existence and control assignments in one atomic transaction.</summary>
public sealed record WorldTopology(ParticipantBinding[] Participants, EntitySpawnRecord[] Entities)
{
    public const int MaxEntities = 128;
    public string Identity => string.Join(";", Participants.OrderBy(p => p.Peer).Select(p => $"{p.Peer}:{p.Generation}:{p.Entity}")) +
        "|" + string.Join(";", Entities.OrderBy(e => e.EntityId).Select(e => e.EntityId));
    public Godot.Collections.Array ToVariant()
    {
        var participants = new Godot.Collections.Array();
        foreach (var p in Participants) participants.Add(new Godot.Collections.Array { p.Peer, p.Generation, p.Entity });
        var entities = new Godot.Collections.Array();
        foreach (var entity in Entities) entities.Add(entity.ToVariant());
        return new Godot.Collections.Array { participants, entities };
    }
    public bool IsValid()
    {
        if (Participants.Length > MovementWire.MaxPlayers || Entities.Length > MaxEntities) return false;
        var peers = new HashSet<int>(); var bindings = new HashSet<string>(); var entities = new HashSet<string>();
        foreach (var e in Entities)
            if (!Guid.TryParseExact(e.EntityId, "N", out _) || !entities.Add(e.EntityId) ||
                !ShipRegistry.TryGetById(e.ShipId, out _) || !e.Position.IsFinite() ||
                !float.IsFinite(e.Rotation) || !e.Velocity.IsFinite()) return false;
        foreach (var p in Participants)
            if (p.Peer <= 0 || !peers.Add(p.Peer) || p.Generation < 0 || p.Generation > MovementWire.MaxTick ||
                (p.Entity != "" && (!entities.Contains(p.Entity) || !bindings.Add(p.Entity)))) return false;
        return true;
    }
    public static bool TryRead(Godot.Collections.Array payload, out WorldTopology topology)
    {
        topology = new(Array.Empty<ParticipantBinding>(), Array.Empty<EntitySpawnRecord>());
        if (payload.Count != 2 || payload[0].VariantType != Variant.Type.Array || payload[1].VariantType != Variant.Type.Array) return false;
        var ps = payload[0].AsGodotArray(); var es = payload[1].AsGodotArray();
        if (ps.Count > MovementWire.MaxPlayers || es.Count > MaxEntities) return false;
        var participants = new List<ParticipantBinding>(); var entities = new List<EntitySpawnRecord>();
        foreach (var value in ps)
        {
            if (value.VariantType != Variant.Type.Array) return false;
            var p = value.AsGodotArray();
            if (p.Count != 3 || p[0].VariantType != Variant.Type.Int || p[1].VariantType != Variant.Type.Int || p[2].VariantType != Variant.Type.String ||
                p[0].AsInt64() <= 0 || p[0].AsInt64() > int.MaxValue) return false;
            participants.Add(new(p[0].AsInt32(), p[1].AsInt64(), p[2].AsString()));
        }
        foreach (var value in es)
        {
            if (!EntitySpawnRecord.TryRead(value, out var entity)) return false;
            entities.Add(entity);
        }
        topology = new(participants.ToArray(), entities.ToArray());
        return topology.IsValid();
    }
}
