using sMPGWM.Scripts.Autoload.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public partial class StatSynchronizer : AbstractSingleton<StatSynchronizer>
{
    //  private static int FastSyncInterval => GlobalStateManager.FastSyncInterval;
    // private static int SlowSyncInterval => GlobalStateManager.SlowSyncInterval;
    //
    // private long _lastFastSyncTick;
    // private long _lastSlowSyncTick;
    //
    // private readonly Dictionary<int, LivingEntityStateTracker> _mpControllerTrackers = new();
    // private readonly Dictionary<int, LivingEntityStateTracker> _agentControllerTrackers = new();
    //
    // private static Dictionary<int, MpController> MpControllers =>
    //     GlobalStateManager.MpControllers;
    //
    // private static Dictionary<int, AgentController> AgentControllers =>
    //     GlobalStateManager.AgentControllers;
    //
    // protected override void Create()
    // {
    //     PlayerController.OnPlayerSpawned += RegisterPlayer;
    //     AgentController.OnAgentCreated += RegisterAgent;
    // }
    //
    //
    // // ++ MpController Sync ++
    //
    // // Register Player
    // private void RegisterPlayer(int peerId)
    // {
    //     if (IsHost)
    //     {
    //         _mpControllerTrackers[peerId] =
    //             new LivingEntityStateTracker(MpControllers[peerId].LivingEntityState);
    //     }
    //     else
    //     {
    //         RpcId(GlobalStateManager.HostPeerId, nameof(RegisterPlayerOnHost),
    //             MpControllers[peerId].LivingEntityState.ToVariant());
    //     }
    // }
    //
    // // TODO Currently not called - verify and if necessary add to OnPeerRemoved logic 
    // private void DeRegisterPlayer(int peerId)
    // {
    //     if (IsHost)
    //     {
    //         _mpControllerTrackers.Remove(peerId);
    //     }
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // public void RegisterPlayerOnHost(GCol.Array livingEntityStateVariant)
    // {
    //     var peerId = Multiplayer.GetRemoteSenderId();
    //     _mpControllerTrackers[peerId] = new LivingEntityStateTracker(MpControllers[peerId].LivingEntityState);
    //     _mpControllerTrackers[peerId].ApplyFullInitialState(livingEntityStateVariant);
    //     SyncFullMpControllerStatesToClients();
    // }
    //
    // // Sync FullState
    // private void SyncFullMpControllerStatesToClients()
    // {
    //     var currentFullStates = new GCol.Dictionary<int, GCol.Array>();
    //     foreach (var (peerId, livingEntityStateTracker) in _mpControllerTrackers)
    //     {
    //         currentFullStates[peerId] = livingEntityStateTracker.CurrentState.ToVariant();
    //     }
    //
    //     Rpc(nameof(ReceiveFullMpControllerStates), currentFullStates);
    // }
    //
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void ReceiveFullMpControllerStates(GCol.Dictionary<int, GCol.Array> fullStates)
    // {
    //     foreach (var (peerId, mpController) in MpControllers)
    //     {
    //         if (peerId == Multiplayer.GetUniqueId()) continue;
    //         mpController.LivingEntityState.UpdateFullState(fullStates[peerId]);
    //     }
    // }
    //
    // // Sync PermanentDeltas
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void ReceiveMpControllerPermanentDeltas(GCol.Dictionary<int, GCol.Dictionary<int, Variant>> deltaStats)
    // {
    //     foreach (var (peerId, updatedStats) in deltaStats)
    //     {
    //         MpControllers[peerId].LivingEntityState.ApplyPermanentDelta(updatedStats);
    //     }
    // }
    //
    // // Sync CurrentDeltas
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void ReceiveMpControllerCurrentDeltas(GCol.Dictionary<int, GCol.Dictionary<int, Variant>> deltaStats)
    // {
    //     foreach (var (peerId, updatedStats) in deltaStats)
    //     {
    //         MpControllers[peerId].LivingEntityState.ApplyCurrentDelta(updatedStats);
    //     }
    // }
    //
    // // ++ AgentController Sync ++
    //
    // // Register Agents
    // private void RegisterAgent(int agentId)
    // {
    //     if (IsHost)
    //     {
    //         _agentControllerTrackers[agentId] =
    //             new LivingEntityStateTracker(AgentControllers[agentId].LivingEntityState);
    //     }
    //     else
    //     {
    //         RpcId(GlobalStateManager.HostPeerId, nameof(RequestForFullAgentStateSync),
    //             agentId);
    //     }
    // }
    //
    // private void DeRegisterAgent(int agentId)
    // {
    //     if (IsHost)
    //     {
    //         Rpc(nameof(RemoveAgent), agentId);
    //     }
    // }
    //
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
    // private void RemoveAgent(int agentId)
    // {
    //     _agentControllerTrackers.Remove(agentId);
    // }
    //
    // // Sync FullState
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void RequestForFullAgentStateSync(int agentId)
    // {
    //     if (!IsHost)
    //     {
    //         Logger.Error($"Received request for agent {agentId} on client {Multiplayer.GetUniqueId()}");
    //     }
    //
    //     RpcId(Multiplayer.GetRemoteSenderId(), nameof(ReceiveFullAgentControllerState),
    //         _agentControllerTrackers[agentId].CurrentState.ToVariant());
    // }
    //
    //
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void ReceiveFullAgentControllerState(GCol.Array fullState)
    // {
    //     var agentId = fullState[12].AsInt32();
    //     AgentControllers[agentId].LivingEntityState.UpdateFullState(fullState);
    // }
    //
    // // Sync CurrentDeltas
    // [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = false)]
    // private void ReceiveAgentCurrentDeltas(GCol.Dictionary<int, GCol.Dictionary<int, Variant>> deltaStats)
    // {
    //     foreach (var (agentId, updatedStats) in deltaStats)
    //     {
    //         AgentControllers[agentId].LivingEntityState.ApplyCurrentDelta(updatedStats);
    //     }
    // }
    //
    // // ++ Common Syncing Methods ++
    // public override void _PhysicsProcess(double delta)
    // {
    //     if (IsHost && IsSpawnReady)
    //     {
    //         SyncStatesToClients(NetworkTicker.GetTicksSinceStart());
    //     }
    // }
    //
    // private void SyncStatesToClients(long ticksSinceStart)
    // {
    //     // PermanentDeltas - e.g. items - Syncs only between MpControllers
    //     if (ticksSinceStart - _lastSlowSyncTick >= SlowSyncInterval)
    //     {
    //         var permanentDeltas = new GCol.Dictionary<int, GCol.Dictionary<int, Variant>>();
    //         foreach (var (peerId, livingEntityStateTracker) in _mpControllerTrackers)
    //         {
    //             var permanentDelta = livingEntityStateTracker.CreatePermanentDelta();
    //
    //             if (permanentDelta.Count <= 0) continue;
    //             permanentDeltas[peerId] = permanentDelta;
    //         }
    //
    //         if (permanentDeltas.Count > 0)
    //         {
    //             Rpc(nameof(ReceiveMpControllerPermanentDeltas), permanentDeltas);
    //         }
    //
    //         _lastSlowSyncTick = ticksSinceStart;
    //     }
    //
    //     // CurrentDeltas - e.g. reduce/raise effects, buffs, etc.
    //     if (ticksSinceStart - _lastFastSyncTick >= FastSyncInterval)
    //     {
    //         var currentMpControllerDeltas = AssembleCurrentDeltaForSync(_mpControllerTrackers);
    //
    //         if (currentMpControllerDeltas.Count > 0)
    //         {
    //             Rpc(nameof(ReceiveMpControllerCurrentDeltas), currentMpControllerDeltas);
    //         }
    //
    //         var currentAgentControllerDeltas = AssembleCurrentDeltaForSync(_agentControllerTrackers);
    //
    //         if (currentAgentControllerDeltas.Count > 0)
    //         {
    //             Rpc(nameof(ReceiveAgentCurrentDeltas), currentAgentControllerDeltas);
    //         }
    //
    //         _lastFastSyncTick = ticksSinceStart;
    //     }
    // }
    //
    // private GCol.Dictionary<int, GCol.Dictionary<int, Variant>> AssembleCurrentDeltaForSync(
    //     Dictionary<int, LivingEntityStateTracker> trackers)
    // {
    //     var currentDeltas = new GCol.Dictionary<int, GCol.Dictionary<int, Variant>>();
    //     foreach (var (id, livingEntityStateTracker) in trackers)
    //     {
    //         var currentDelta = livingEntityStateTracker.CreateCurrentDelta();
    //
    //         if (currentDelta.Count <= 0) continue;
    //         currentDeltas[id] = currentDelta;
    //     }
    //
    //     return currentDeltas;
    // }
    //
    // // ++ Clean Up ++
    // public override void Cleanup()
    // {
    //     PlayerController.OnPlayerSpawned -= RegisterPlayer;
    //     AgentController.OnAgentCreated -= RegisterAgent;
    //     _mpControllerTrackers.Clear();
    //     _agentControllerTrackers.Clear();
    // }
}
