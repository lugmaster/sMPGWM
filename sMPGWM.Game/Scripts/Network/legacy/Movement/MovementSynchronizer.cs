using System.Collections.Generic;
using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Controllers.Agents;
using FlatEarthUniverse.scripts.Helpers;
using FlatEarthUniverse.scripts.Network;
using Godot;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public partial class MovementSynchronizer : BaseNode
{
    private static Dictionary<int, MpController> MpControllers =>
        GlobalStateManager.MpControllers;
    private static Dictionary<int, AgentController> AgentControllers =>
        GlobalStateManager.AgentControllers;

    private bool _hostConnectionAlive = true;

    protected override void Initialize()
    {
        MpManager.OnHostConnectionLost += HandleDisconnectedHost;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsHost && IsSpawnReady)
        {
            SyncToClients();
        }
    }

    private void SyncToClients()
    {
        var statesToSend = new GCol.Dictionary<int, Variant>();

        foreach (var (peerId, mpController) in MpControllers)
        {
            mpController.MovementController.AddStateForClientSync(statesToSend, peerId);
        }

        if (statesToSend.Count <= 0) return;
        Rpc(nameof(ReceiveUpdatedStatesFromHost), statesToSend);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
#pragma warning disable CA1822
    private void ReceiveUpdatedStatesFromHost(GCol.Dictionary<int, GCol.Array> movementStateVariant)
#pragma warning restore CA1822
    {
        foreach (var (peerId, updatedState) in movementStateVariant)
        {
            if (MpControllers.TryGetValue(peerId, out var mpController))
            {
                mpController.MovementController.ApplyUpdatesFromHost(updatedState);
            }

        }
    }

    public void SyncToHost(GCol.Array currentState)
    {
        if(!_hostConnectionAlive) return;
        RpcId(GlobalStateManager.HostPeerId, nameof(ReceiveClientMovement),
            currentState);
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void ReceiveClientMovement(GCol.Array receivedMovement)
    {
        if (MpControllers.TryGetValue(Multiplayer.GetRemoteSenderId(), out var mpController))
        {
            mpController.MovementController.ApplyReceivedMovementFromClient(receivedMovement);
        }
        else
        {
            Logger.Log($"Received Update from unknown peer {Multiplayer.GetRemoteSenderId()}",1);
        }
    }

    private void HandleDisconnectedHost()
    {
        _hostConnectionAlive = false;
    }

    public override void Cleanup()
    {
        MpManager.OnHostConnectionLost -= HandleDisconnectedHost;
    }
}