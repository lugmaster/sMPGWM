using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WorldSystem;

public static class NewWorldState
{
    public static ShipSpawnRecord CreatePlayer(int peer, PlayerStartupData startup, int spawnIndex)
    {
        if (peer <= 0) throw new ArgumentOutOfRangeException(nameof(peer));
        if (spawnIndex < 0) throw new ArgumentOutOfRangeException(nameof(spawnIndex));
        if (!ShipRegistry.TryGetById(startup.ShipId, out _))
            throw new ArgumentException("Unknown ship: " + startup.ShipId, nameof(startup));
        return new ShipSpawnRecord(peer, Guid.NewGuid().ToString("N"), startup.ShipId,
            new Vector2(640 + spawnIndex % 4 * 140, 360 + spawnIndex / 4 * 140), 0, Vector2.Zero);
    }
}
