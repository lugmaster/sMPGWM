using System;
using Godot;

namespace sMPGWM.Scripts.Network;

public partial class NetworkTicker : Node
{
    public static event Action<float> HostDelta;
    private static long _tickNumber;
    public override void _PhysicsProcess(double delta)
    {
        _tickNumber++;
        if (_tickNumber % 2 == 0) HostDelta?.Invoke((float)(delta * 2));
    }
    public static long GetTicksSinceStart() => _tickNumber;
    public static long GetUncorrectedTick() => _tickNumber;
    public static string GetLogInfo() => $"local Tick: {_tickNumber}";
    public override void _ExitTree() { _tickNumber = 0; HostDelta = null; }
}
