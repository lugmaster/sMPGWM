using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.VfxSystem;

public partial class WeaponPresentationBinding : Node
{
    private NetworkManager _network = null!;
    private ShotPresentationController _effects = null!;

    public void Initialize(NetworkManager network, ShotPresentationController effects)
    {
        _network = network;
        _effects = effects;
        network.ShotReceived += Receive;
        network.ShotPresentationCleared += Clear;
    }

    private void Receive(AcceptedShot shot, ShipController? ship, double tick)
    {
        _effects.Enqueue(shot, ship, _network.IsHost() || ship?.LocallyControlled == true);
    }

    private void Clear(long cutoff) => _effects.Clear(cutoff, _network.Synchronization?.Session ?? default);

    private void Advance()
    {
        if (_network.Synchronization is not { } synchronization) return;
        var fraction = _network.IsHost() ? Engine.GetPhysicsInterpolationFraction() : 0;
        _effects.Advance(synchronization.WeaponLocalPresentationTick + fraction, synchronization.PresentationTick + fraction);
    }

    public override void _Process(double delta) => Advance();

    public override void _ExitTree()
    {
        if (_network == null) return;
        _network.ShotReceived -= Receive;
        _network.ShotPresentationCleared -= Clear;
    }
}
