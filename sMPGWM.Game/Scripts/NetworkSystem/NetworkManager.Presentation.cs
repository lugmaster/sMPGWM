using System;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    internal event Action<ShieldCollisionEvent>? ShieldCollisionReceived;
    internal event Action<double>? PresentationAdvanced;
    internal event Action<long>? PresentationCleared;

    private void PresentShieldCollision(ShieldCollisionEvent contact) => ShieldCollisionReceived?.Invoke(contact);
    private void TickWeaponPresentation(double tick) => PresentationAdvanced?.Invoke(tick);
    private void ClearWeaponPresentation(long cutoffTick) => PresentationCleared?.Invoke(cutoffTick);
}
