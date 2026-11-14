using System;
using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed class CombatPresentationBinding : IDisposable
{
    private readonly NetworkManager _network;
    private readonly ShotPresentationController _presentation;
    private bool _disposed;

    public CombatPresentationBinding(NetworkManager network, ShotPresentationController presentation)
    {
        _network = network;
        _presentation = presentation;
        network.ShotReceived += presentation.Enqueue;
        network.ShieldCollisionReceived += presentation.EnqueueShieldCollision;
        network.PresentationAdvanced += presentation.Advance;
        network.PresentationCleared += presentation.Clear;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _network.ShotReceived -= _presentation.Enqueue;
        _network.ShieldCollisionReceived -= _presentation.EnqueueShieldCollision;
        _network.PresentationAdvanced -= _presentation.Advance;
        _network.PresentationCleared -= _presentation.Clear;
        if (GodotObject.IsInstanceValid(_presentation)) _presentation.Clear(long.MaxValue);
    }
}
