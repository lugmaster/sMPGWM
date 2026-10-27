namespace sMPGWM.scripts.Network;

public partial class MpManager
{
    // public static event Action ConnectionVerified;
    // public static event Action OnHostConnectionLost;
    // public static event Action<int> OnPlayerDespawned;
    //
    // private static NetworkManager NetworkManager => GlobalStateManager.NetworkManager;
    // private readonly ConcurrentDictionary<int, bool> _outstandingSpawners = new();
    // private static MpInstantiator Instantiator => GlobalStateManager.MpInstantiator;
    // private static Dictionary<int, MpController> MpControllers => GlobalStateManager.MpControllers;
    // private bool _confirmConnectionAnswerReceived;
    //
    // private MultiplayerPeer.ConnectionStatus _lastStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
    //
    // private void InvokeConnectionVerified() => ConnectionVerified?.Invoke();
    //
    // #region Multiplayer Initialization
    //
    // protected override void Initialize()
    // {
    //     Logger.Log($"RefuseNewConnections: {Multiplayer.MultiplayerPeer.RefuseNewConnections}", 0);
    //     Logger.Log($"Connection status: {Multiplayer.MultiplayerPeer.GetConnectionStatus()}", 0);
    //
    //     if (IsHost)
    //     {
    //         Logger.Log("Initializing as Host", 0);
    //         Multiplayer.MultiplayerPeer.PeerConnected += OnPeerConnected;
    //         Multiplayer.MultiplayerPeer.PeerDisconnected += OnPeerDisconnected;
    //     }
    //     else
    //     {
    //         Logger.Log("Initializing as Client", 0);
    //         ConnectionVerified += RequestSpawnFromHost;
    //     }
    // }
    //
    // protected override void SpawnReady()
    // {
    //     if (IsHost)
    //     {
    //         Logger.Log("MpManager: Initiating StartSpawningHost", 0);
    //         StartSpawningHost();
    //     }
    //     else
    //     {
    //         Logger.Log($"MpManager: Initiating StartConnectionVerification for peerId: {Multiplayer.GetUniqueId()}", 0);
    //         StartConnectionVerification(GlobalStateManager.ConnectionTimeout);
    //     }
    // }
    //
    // private void StartSpawningHost()
    // {
    //     Instantiator.StartSpawning(Multiplayer.GetUniqueId(), GlobalStateManager.ShipType);
    // }
    //
    // #endregion
    //
    // #region Connection Verification
    //
    // private async void StartConnectionVerification(int timeoutMs)
    // {
    //     try
    //     {
    //         var elapsed = 0;
    //         const int interval = 500;
    //
    //         Logger.Log($"Verifying connection (timeout: {timeoutMs / 1000}s)...", 0);
    //
    //         while (elapsed < timeoutMs && !_confirmConnectionAnswerReceived)
    //         {
    //             await Task.Delay(interval);
    //             elapsed += interval;
    //
    //             if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
    //                 RpcLogAndCall(GlobalStateManager.HostPeerId, nameof(ConfirmConnectionOnHost));
    //         }
    //
    //         if (_confirmConnectionAnswerReceived)
    //         {
    //             Logger.Log($"Connection verified after {elapsed / 1000}s. Proceed with InvokeConnectionVerified", 0);
    //             CallDeferred(nameof(InvokeConnectionVerified));
    //         }
    //         else
    //         {
    //             GameHandler.QuitToMainMenuBlocking($"Connection verification timed out after {timeoutMs / 1000}s.");
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         GameHandler.QuitToMainMenuBlocking($"Connection verification error. Verifying connection: {e.Message}");
    //     }
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void ConfirmConnectionOnClient()
    // {
    //     Logger.Log($"Client confirmed connection from {Multiplayer.GetRemoteSenderId()}", 1);
    //     _confirmConnectionAnswerReceived = true;
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void ConfirmConnectionOnHost()
    // {
    //     Logger.Log($"Host received connection confirmation from {Multiplayer.GetRemoteSenderId()}", 1);
    //     RpcLogAndCall(Multiplayer.GetRemoteSenderId(), nameof(ConfirmConnectionOnClient));
    // }
    //
    // #endregion
    //
    // #region Peer Events (Host)
    //
    // private void OnPeerConnected(long peerId)
    // {
    //     Logger.Log($"Peer connected: {peerId}", 1);
    //     _outstandingSpawners.TryAdd((int)peerId, false);
    //     RpcLogAndCall((int)peerId, nameof(ConfirmTestMessageOnClient));
    //     MonitorPeerRegistration((int)peerId);
    // }
    //
    // private void OnPeerDisconnected(long peerId)
    // {
    //     Logger.Log($"Peer disconnected: {peerId}", 0);
    //     Rpc(nameof(RemovePeer),peerId);
    // }
    //
    // #endregion
    //
    // #region Spawning Logic
    //
    // private async void MonitorPeerRegistration(int peerId)
    // {
    //     try
    //     {
    //         var timeoutMs = GlobalStateManager.ConnectionTimeout;
    //         var elapsed = 0;
    //         const int interval = 1000;
    //
    //         Logger.Log($"Waiting for peer {peerId} to register...", 0);
    //
    //         while (elapsed < timeoutMs)
    //         {
    //             await Task.Delay(interval);
    //             elapsed += interval;
    //
    //             if (_outstandingSpawners.TryGetValue(peerId, out var registered) && registered)
    //             {
    //                 Logger.Log($"Peer {peerId} registered after {elapsed / 1000}s.", 0);
    //                 return;
    //             }
    //         }
    //
    //         Logger.Log($"Peer {peerId} failed to register in time.",1);
    //         RemovePeer(peerId);
    //     }
    //     catch (Exception e)
    //     {
    //         RemovePeer(peerId);
    //         GameHandler.QuitToMainMenuBlocking($"Error monitoring peer {peerId}: {e.Message}");
    //     }
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true,
    //     TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    // public void RemovePeer(long peerId)
    // {
    //     Logger.Log($"Try removing peerId {peerId}", 0);
    //     if (MpControllers.TryGetValue((int)peerId, out var controller))
    //     {
    //         Logger.Log($"Found. Start removing peerId {peerId} on peerdId {Multiplayer.GetUniqueId()}", 1);
    //         OnPlayerDespawned?.Invoke((int)peerId);
    //         controller.SetProcess(false);
    //         controller.ScheduledForDisconnection = true;
    //         controller.Cleanup();
    //         CallDeferred(nameof(FreeController), controller);
    //         MpControllers.Remove((int)peerId);
    //     }
    //     else
    //     {
    //         Logger.Log($"Disconnect failed: peer {peerId} not found.",1);
    //     }
    //
    //     if (IsHost)
    //     {
    //         var removed = _outstandingSpawners.TryRemove((int)peerId, out _);
    //         Logger.Log($"Peer {peerId} scheduled for removal. Was tracked: {removed}", 0);
    //     }
    // }
    //
    // private void RequestSpawnFromHost()
    // {
    //     Logger.Log($"PeerId {Multiplayer.GetUniqueId()} request spawn from host", 0);
    //     _lastStatus = MultiplayerPeer.ConnectionStatus.Connected;
    //     RpcLogAndCall(GlobalStateManager.HostPeerId, nameof(AcknowledgeSpawnRequest),
    //         GlobalStateManager.GameDataChecksum);
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void AcknowledgeSpawnRequest(string clientChecksum)
    // {
    //     var senderId = Multiplayer.GetRemoteSenderId();
    //
    //     if (!_outstandingSpawners.ContainsKey(senderId))
    //     {
    //         Logger.Log($"Spawn request from unknown peer {senderId}",1);
    //         return;
    //     }
    //
    //     if (clientChecksum != GlobalStateManager.GameDataChecksum)
    //     {
    //         Logger.Log(
    //             $"GameData Checksum mismatch from peer {senderId}. Host: {GlobalStateManager.GameDataChecksum}, Client: {clientChecksum} - Disconnecting",1);
    //
    //         RpcId(senderId, nameof(NotifyChecksumMismatchAndDisconnect), "config");
    //         CallDeferred(nameof(RemovePeer), (long)senderId);
    //         return;
    //     }
    //
    //     RpcLogAndCall(senderId, nameof(RequestShipData));
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void NotifyChecksumMismatchAndDisconnect(string fileTypes)
    // {
    //     GameHandler.QuitToMainMenuBlocking(
    //         $"Your {fileTypes} files are not compatible with the host.\nPlease verify your game version or mods.");
    // }
    //
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void RequestShipData()
    // {
    //     var hostId = Multiplayer.GetRemoteSenderId();
    //
    //     if (hostId != GlobalStateManager.HostPeerId)
    //     {
    //         Logger.Error($"Ship data request from unexpected peer: {hostId}");
    //         return;
    //     }
    //
    //     var spawningRequestData = new SpawningRequestData(Multiplayer.GetUniqueId(), GlobalStateManager.ShipType);
    //     Logger.Log($"PeerId {Multiplayer.GetUniqueId()} with ConnectionStatus: {NetworkManager.GetCurrentConnectionStatus()} sends spawningRequestData: {spawningRequestData} to host", 0);
    //     RpcId(hostId, nameof(DeliverShipDataToHost), spawningRequestData.ToVariant());
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void DeliverShipDataToHost(GCol.Array spawningRequestDataVariant)
    // {
    //     var senderId = Multiplayer.GetRemoteSenderId();
    //     if (!_outstandingSpawners.ContainsKey(senderId))
    //     {
    //         Logger.Log($"Received ship data from unknown peer {senderId}",1);
    //         return;
    //     }
    //
    //     _outstandingSpawners[senderId] = true;
    //     var spawningRequestData = SpawningRequestData.FromVariant(spawningRequestDataVariant);
    //     Logger.Log($"Peer {senderId} registered. Spawning now with spawningRequestData: {spawningRequestData}", 0);
    //     Instantiator.StartSpawning(spawningRequestData.GetPeerId(), spawningRequestData.GetShipType());
    // }
    //
    // #endregion
    //
    // #region Test Messaging (Dev Tooling)
    //
    // private void RpcLogAndCall(int peerId, string method)
    // {
    //     Logger.Log($"RPC call '{method}' to peer {peerId}", 0);
    //     RpcId(peerId, method);
    // }
    //
    // private void RpcLogAndCall(int peerId, string method, Variant data)
    // {
    //     Logger.Log($"RPC call '{method}' to peer {peerId} with data", 0);
    //     RpcId(peerId, method, data);
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void ConfirmTestMessageOnClient()
    // {
    //     var from = Multiplayer.GetRemoteSenderId();
    //     Logger.Log($"Test message confirmed on client from {from}", 1);
    //     RpcLogAndCall(from, nameof(ConfirmTestMessageOnHost));
    // }
    //
    // [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    // private void ConfirmTestMessageOnHost()
    // {
    //     var from = Multiplayer.GetRemoteSenderId();
    //     Logger.Log($"Test message confirmed on host from {from}", 1);
    // }
    //
    // public override void Cleanup()
    // {
    //     ConnectionVerified -= RequestSpawnFromHost;
    // }
    //
    // public static void FreeController(MpController controller)
    // {
    //     controller.QueueFree();
    // }
    //
    // #endregion
    //
    //
    //
    // public override void _Process(double delta)
    // {
    //     if (!_confirmConnectionAnswerReceived)
    //     {
    //         return;
    //     }
    //     if (!IsHost)
    //     {
    //         VerifyConnectionToHost();
    //     }
    // }
    //
    // private void VerifyConnectionToHost()
    // {
    //     var currentStatus = Multiplayer.MultiplayerPeer?.GetConnectionStatus() ?? MultiplayerPeer.ConnectionStatus.Disconnected;
    //
    //     if (_lastStatus == MultiplayerPeer.ConnectionStatus.Connected &&
    //         currentStatus == MultiplayerPeer.ConnectionStatus.Disconnected)
    //     {
    //         OnHostConnectionLost?.Invoke();
    //         GD.PrintErr("Disconnected from host unexpectedly.");
    //         GameHandler.QuitToMainMenuBlocking("Lost connection to host.");
    //     }
    //
    //     if (_lastStatus != currentStatus)
    //         Logger.Log($"Connection status changed: {_lastStatus} → {currentStatus}", 0);
    //     _lastStatus = currentStatus;
    // }
}