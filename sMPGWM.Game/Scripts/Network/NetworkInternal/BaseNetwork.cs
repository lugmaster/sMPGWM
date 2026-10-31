using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;

namespace sMPGWM.Scripts.Network.NetworkInternal;

public partial class BaseNetwork : Node
{
    protected static int ConnectionTimeout => GameConfig.ConnectionTimeout;
    
    public event Action<bool, string>? ConnectionChanged;

    protected void EmitConnected(bool connected, string reason = "success")
    {
        ConnectionChanged?.Invoke(connected, reason);
    }

    public virtual void Cleanup()
    {
        ConnectionChanged = null;
    }

    protected void EmitConnectedFalseAndQuit(string reason)
    {
        EmitConnected(false, reason);
    }
}