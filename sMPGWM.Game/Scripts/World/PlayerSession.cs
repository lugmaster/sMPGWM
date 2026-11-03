using System;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.World;

/// <summary>A connection's control binding. The world owns the ship's lifetime.</summary>
public sealed class PlayerSession
{
    public int ControllingPeerId { get; init; }
    public long ControlGeneration { get; private set; }
    public ShipController? ControlledShip { get; private set; }
    public event Action<ShipController?, ShipController?>? ControlledShipChanged;

    public void AssignShip(ShipController? ship)
    {
        if (ship == ControlledShip) return;
        if (ship?.ControllingPlayer is { } owner && owner != this) owner.AssignShip(null);
        var previous = ControlledShip;
        if (previous != null)
        {
            previous.SetLocalControl(false);
            previous.ControllingPlayer = null;
        }
        if (ship != null) ship.ControllingPlayer = this;
        checked { ControlGeneration++; }
        ControlledShip = ship;
        ControlledShipChanged?.Invoke(previous, ship);
    }

    internal void ApplyGeneration(long generation) => ControlGeneration = generation;
}
