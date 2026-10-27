using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;

namespace sMPGWM.Scripts.Network.NetworkInternal;

public partial class BaseNetwork : Node
{
    protected static int ConnectionTimeout => GameConfig.ConnectionTimeout;
    
    public static event Action<bool, string> OnConnected;

    protected static void EmitConnected(bool connected, string reason = "success")
    {
        OnConnected?.Invoke(connected, reason);
    }

    public virtual void Cleanup()
    {
        OnConnected = null;
    }

    protected void EmitConnectedFalseAndQuit(string reason)
    {
        EmitConnected(false, reason);
    }
}