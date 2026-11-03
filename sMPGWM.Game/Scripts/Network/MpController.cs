using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
namespace sMPGWM.Scripts.Network;
public partial class MpController : Node
{
    public int ControllingPeerId { get; init; }
    public ShipController? ControlledShip { get; private set; }
    public event Action<ShipController?, ShipController?>? ControlledShipChanged;
    public void AssignShip(ShipController? ship)
    {
        if (ship == ControlledShip) return;
        if (ship?.GetParent() is MpController owner && owner != this) owner.AssignShip(null);
        var previous = ControlledShip;
        if (previous != null)
        {
            previous.SetLocalControl(false);
            previous.Reparent(GetParent(), true);
        }
        if (ship != null)
        {
            if (ship.GetParent() == null) AddChild(ship);
            else ship.Reparent(this, true);
        }
        ControlledShip = ship;
        ControlledShipChanged?.Invoke(previous, ship);
    }
    public void RemoveShip()
    {
        var ship = ControlledShip;
        if (ship == null) return;
        AssignShip(null);
        ship.QueueFree();
    }
}
