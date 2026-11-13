using Godot;
using sMPGWM.Scripts.VfxSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private ShotPresentationController? _shotPresentation;
    private void InitializeWeaponPresentation()
    {
        if (_shotPresentation != null) return;
        _shotPresentation = new ShotPresentationController { Name = "ShotPresentation" };
        World.AddChild(_shotPresentation);
        _shotPresentation.Initialize(World);
    }
    private void PresentShot(AcceptedShot shot) => _shotPresentation?.Enqueue(shot);
    private void TickWeaponPresentation(double tick) => _shotPresentation?.Advance(tick);
    private void ClearWeaponPresentation(long cutoffTick) => _shotPresentation?.Clear(cutoffTick);
    private void DisposeWeaponPresentation()
    {
        if (!GodotObject.IsInstanceValid(_shotPresentation)) return;
        _shotPresentation!.Clear(long.MaxValue);
        _shotPresentation.QueueFree();
        _shotPresentation = null;
    }
}
