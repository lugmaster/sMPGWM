using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.WorldSystem;

/// <summary>In-memory authoritative state at a completed physics step, independent of a storage format.</summary>
public sealed class RestoredWorldState
{
    public long CompletedTick { get; }
    public long AbilitySequence { get; }
    public int NextSpawnIndex { get; }
    public IReadOnlyList<RestoredShipState> Ships { get; }
    public IReadOnlyList<RestoredPlayerState> Players { get; }
    public ItemHotbarSnapshot? LocalHotbar { get; }

    public RestoredWorldState(long completedTick, long abilitySequence, int nextSpawnIndex,
        IReadOnlyList<RestoredShipState> ships, IReadOnlyList<RestoredPlayerState> players,
        ItemHotbarSnapshot? localHotbar = null)
    {
        if (ships.Count > WorldTopology.MaxEntities || players.Count > MovementWire.MaxPlayers ||
            completedTick < 0 || completedTick >= MovementWire.MaxTick || abilitySequence < 0 ||
            abilitySequence == long.MaxValue || nextSpawnIndex < 0 || nextSpawnIndex == int.MaxValue)
            throw new ArgumentException("Invalid restored world continuity metadata.");
        CompletedTick = completedTick;
        AbilitySequence = abilitySequence;
        NextSpawnIndex = nextSpawnIndex;
        var shipCopy = new RestoredShipState[ships.Count];
        for (var i = 0; i < shipCopy.Length; i++) shipCopy[i] = ships[i];
        var playerCopy = new RestoredPlayerState[players.Count];
        for (var i = 0; i < playerCopy.Length; i++) playerCopy[i] = players[i];
        Ships = Array.AsReadOnly(shipCopy);
        Players = Array.AsReadOnly(playerCopy);
        LocalHotbar = localHotbar;
    }
}
