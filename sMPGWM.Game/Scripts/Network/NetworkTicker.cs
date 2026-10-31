using System;
using Godot;
using sMPGWM.scripts.Network;

namespace sMPGWM.Scripts.Network;

public partial class NetworkTicker: Node
{
    public static event Action<float> HostDelta;

    private static long _tickNumber;
    private static long _localTickBeforeCorrection;
    private NetworkManager _networkManager;
    public bool Initialized { get; set; }
    public bool IsHost { get; set; }

    protected void Initialize()
    {
        
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Initialized) return;
        _localTickBeforeCorrection = _tickNumber;
        _tickNumber++;
        _localTickBeforeCorrection++;
        if (_networkManager.IsHost())
        {
            Rpc(nameof(EmitTick), _tickNumber, delta);
            HostDelta?.Invoke((float)delta);
        }
    }

    public override void _Ready()
    {
        _localTickBeforeCorrection = _tickNumber;
        _networkManager = GetNode<NetworkManager>("NetworkManager");
        IsHost = _networkManager.IsHost();
        Initialized = true;
    }

    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void EmitTick(long tick, double delta)
    {
        if (Math.Abs(_tickNumber - tick) > 2)
        {
            // Logger.Log($"Correcting tick {_tickNumber} with received host tick value {tick}", 0);
            _localTickBeforeCorrection = _tickNumber;
            _tickNumber = tick;
        }
        EmitHostDelta((float)delta);
    }
    
    private void EmitHostDelta(float delta)
    {
        HostDelta?.Invoke(delta);
    }

    public static long GetTicksSinceStart() => _tickNumber;
    public static long GetUncorrectedTick() => _localTickBeforeCorrection;

    public static string GetLogInfo()
    {
        return $"local Tick: {_tickNumber}({_localTickBeforeCorrection})";
    }

    public override void _ExitTree()
    {
        _tickNumber = 0;
        _localTickBeforeCorrection = -1;
    }
}