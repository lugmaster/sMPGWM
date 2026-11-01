// using System;
// using System.Collections.Generic;
// using System.Linq;
// using FlatEarthUniverse.scripts.Autoloads;
// using FlatEarthUniverse.scripts.Constants;
// using FlatEarthUniverse.scripts.Controllers.MultiplayerController;
// using FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;
// using FlatEarthUniverse.scripts.GameObjects.Ships;
// using FlatEarthUniverse.scripts.GameObjects.World;
// using FlatEarthUniverse.scripts.Helpers;
// using Godot;
// using GCol = Godot.Collections;
//
// namespace FlatEarthUniverse.scripts.Network;
//
// public partial class MpInstantiator : BaseNode
// {
//     private static Node2D MpControllerSpawnRoot => GlobalStateManager.MpControllerSpawnRoot;
//     private static MpManager MpManager => GlobalStateManager.MpManager;
//     private Dictionary<int, MpController> MpControllers => GlobalStateManager.MpControllers;
//     private static int RespawnTime => GlobalStateManager.RespawnTime;
//
//
//     protected override void Initialize()
//     {
//         if (PackedSceneManager.MultiplayerControllerScene == null ||
//             PackedSceneManager.ClientControllerScene == null ||
//             PackedSceneManager.HostControllerScene == null ||
//             PackedSceneManager.DummyControllerScene == null)
//         {
//             GameHandler.QuitToMainMenuBlocking("MultiplayerControllerScene or one of its ancestors is not set");
//         }
//     }
//
//     public void StartSpawning(int peerId, ShipType shipType)
//     {
//         if (!Initialized)
//         {
//             Logger.Error("MpInstantiator: StartSpawning called, but not initialized");
//         }
//
//         if (!IsHost)
//         {
//             Logger.Error($"Client with peerId {Multiplayer.GetUniqueId()} trys to start spawning.");
//         }
//
//         try
//         {
//             Logger.Log($"Trying to instantiate mpController on {Multiplayer.GetUniqueId()} with owningPeerId {peerId} and shipType {shipType}", 0);
//             InstantiateMpControllerOnHost(peerId, shipType);
//         }
//         catch (Exception e)
//         {
//             Logger.Error($"Error on spawning from owningPeerId{peerId} with shipType {shipType}: {e}");
//             MpManager.RemovePeer(peerId);
//         }
//     }
//
//     private void InstantiateMpControllerOnHost(int peerId, ShipType shipType)
//     {
//         var spawnHost = peerId == 1;
//         var informOthers = !spawnHost;
//         var spawnPosition = GetSpawnPosition(peerId);
//         var spawningData = MpControllerSpawningData.CreateDefault(peerId, spawnPosition, shipType);
//         var mpController = MpControllerFactory.CreateMpController(
//             spawnHost ? MpControllerType.Host : MpControllerType.Dummy, 
//             IsHost, spawningData
//             );
//         
//         MpControllerSpawnRoot.AddChild(mpController);
//         
//         MpControllers.Add(peerId, mpController);
//         if (informOthers)
//         {
//             CallDeferred(nameof(BroadCastMpControllerSpawnEvent));
//         }
//
//         LogSpawnEvent(Multiplayer.GetUniqueId(), GlobalStateManager.MultiplayerRole,
//             mpController.MpControllerType, spawningData);
//     }
//
//     private void InstantiateMpControllerAtClient(MpControllerSpawningData mpControllerSpawningData)
//     {
//         var owningPeerId = mpControllerSpawningData.GetPeerId();
//         if (IsHost)
//         {
//             GameHandler.QuitToMainMenuBlocking(
//                 $"Host with peerId {Multiplayer.GetUniqueId()} is trying to spawn a mpController with peerId {owningPeerId} on a client");
//         }
//
//         var mpController = MpControllerFactory.CreateMpController(
//             mpControllerSpawningData.GetPeerId() == Multiplayer.GetUniqueId() ? MpControllerType.Client : MpControllerType.Dummy, 
//             IsHost, 
//             mpControllerSpawningData);
//         MpControllers.Add(owningPeerId, mpController);
//         LogSpawnEvent(Multiplayer.GetUniqueId(), GlobalStateManager.MultiplayerRole,
//             mpController.MpControllerType, mpControllerSpawningData);
//         MpControllerSpawnRoot.AddChild(mpController);
//     }
//
//     public void BroadCastMpControllerSpawnEvent()
//     {
//         if (!IsHost)
//         {
//             Logger.Log($"Client {Multiplayer.GetUniqueId()} is trying to broadcast a spawning event to other Clients",
//                 1);
//         }
//
//         var spawnsToSend = new GCol.Dictionary<int, GCol.Array>();
//
//         foreach (var (peerId, mpController) in MpControllers)
//         {
//             spawnsToSend.Add(peerId, mpController.GetSpawnableData());
//         }
//         
//         Logger.Log($"BroadCastMpControllerSpawnEvent: Spawning {spawnsToSend.Count} mpControllers: {spawnsToSend}", 0);
//         Rpc(nameof(ReceiveMpControllerSpawningEventBroadCast), spawnsToSend);
//     }
//
//     [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false,
//         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
//     public void ReceiveMpControllerSpawningEventBroadCast(GCol.Dictionary<int, GCol.Array> receivedSpawns)
//     {
//         var spawns = receivedSpawns;
//         var hostFound = false;
//         Logger.Log($"Spawnable data received: {receivedSpawns} at peer {Multiplayer.GetUniqueId()}", 0);
//         foreach (var (peerId, spawningDataVariant) in spawns)
//         {
//             if (MpControllers.ContainsKey(peerId)) continue;
//             if (peerId == 1)
//             {
//                 hostFound = true;
//                 continue;
//             }
//             InstantiateMpControllerAtClient(MpControllerSpawningData.FromVariant(spawningDataVariant));
//         }
//
//         if (hostFound)
//         {
//             var hostSpawningDataVariant = spawns[1];
//             InstantiateMpControllerAtClient(MpControllerSpawningData.FromVariant(hostSpawningDataVariant));
//         }
//     }
//
//     private static Vector2 GetSpawnPosition(int peerId)
//     {
//         var area = WorldBorderController.GetSpawnableArea();
//         const float minDistance = 64f;
//         const float minDistanceSquared = minDistance * minDistance;
//
//         // Retry until a valid position is found
//         for (var attempts = 0; attempts < 20; attempts++)
//         {
//             var rng = new RandomNumberGenerator();
//             rng.Seed = (ulong)(peerId * 7919 + attempts);
//
//             var x = rng.RandfRange(area.Position.X, area.Position.X + area.Size.X);
//             var y = rng.RandfRange(area.Position.Y, area.Position.Y + area.Size.Y);
//             var candidate = new Vector2(x, y);
//
//             var tooClose = false;
//
//             foreach (var other in GlobalStateManager.MpControllers.Values)
//             {
//                 if (other == null || !other.LivingEntityState.IsAlive())
//                     continue;
//
//                 var distanceSquared = other.GlobalPosition.DistanceSquaredTo(candidate);
//                 if (distanceSquared < minDistanceSquared)
//                 {
//                     tooClose = true;
//                     break;
//                 }
//             }
//
//             if (!tooClose)
//                 return candidate; // Valid spawn point
//         }
//
//         Logger.Log("Failed to find safe spawn, falling back to random position.", 1);
//         return area.Position + area.Size * 0.5f;
//     }
//
//     public void RespawnMpController(int peerId)
//     {
//         Logger.Log(
//             $"[RespawnMpController] Starting respawn timer for peerId={peerId} on MultiplayerUniqueId: {Multiplayer.GetUniqueId()}, duration={RespawnTime}",
//             0);
//
//         var timer = new Timer
//         {
//             WaitTime = RespawnTime,
//             OneShot = true,
//             Autostart = true
//         };
//
//         AddChild(timer);
//
//         timer.Timeout += () =>
//         {
//             var respawnPosition = GetSpawnPosition(peerId);
//             Logger.Log(
//                 $"[RespawnMpController] Respawn timer finished, sending RPC to peerId={peerId} from MultiplayerUniqueId: {Multiplayer.GetUniqueId()} with respawnPosition: {respawnPosition} ",
//                 0);
//             RpcId(peerId, nameof(ScheduleMpControllerForRespawning), ToRespawnData(respawnPosition, peerId));
//             timer.QueueFree();
//         };
//     }
//
//     [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
//     public void ScheduleMpControllerForRespawning(Variant respawnData)
//     {
//         var array = (Godot.Collections.Array)respawnData;
//         var position = array[0].AsVector2();
//         var peerId = array[1].AsInt32();
//
//         Logger.Log(
//             $"[ScheduleMpControllerForRespawning] Called with peerId={peerId} on MultiplayerUniqueId: {Multiplayer.GetUniqueId()}, position={position}",
//             0);
//
//         if (!MpControllers.TryGetValue(peerId, out var mpController))
//         {
//             Logger.Error(
//                 $"[ScheduleMpControllerForRespawning] PeerId {peerId} could not be found for respawning.");
//             return;
//         }
//
//         Logger.Log(
//             $"[ScheduleMpControllerForRespawning] Found MpController, calling ReinitializeForRespawn for peerId={peerId} on MultiplayerUniqueId: {Multiplayer.GetUniqueId()}",
//             0);
//         mpController.ReinitializeForRespawn(position);
//     }
//
//     public void ConfirmRespawnReadyOnHost()
//     {
//         if (IsHost)
//         {
//             GlobalStateManager.PlayerController.LivingEntityState.Revive();
//             return;
//         }
//
//         Logger.Log($"[ConfirmRespawnReadyOnHost] Sending ready confirmation to host", 0);
//         RpcId(1, nameof(ReceiveRespawnReadyConfirmation));
//     }
//
//     [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
//     public void ReceiveRespawnReadyConfirmation()
//     {
//         var senderId = Multiplayer.GetRemoteSenderId();
//         Logger.Log($"[ReceiveRespawnReadyConfirmation] Received from peerId={senderId}", 0);
//
//         if (!MpControllers.TryGetValue(senderId, out var mpController))
//         {
//             Logger.Error(
//                 $"[ReceiveRespawnReadyConfirmation] PeerId {senderId} could not be confirmed after ready to respawn.");
//             return;
//         }
//
//         Logger.Log($"[ReceiveRespawnReadyConfirmation] Calling Revive on LivingEntityState for peerId={senderId}", 0);
//         mpController.LivingEntityState.Revive();
//     }
//
//
//     private static Variant ToRespawnData(Vector2 newPosition, int peerId)
//     {
//         var array = new GCol.Array
//         {
//             newPosition,
//             peerId
//         };
//         return array;
//     }
//
//     private void LogSpawnEvent(int multiplayerUniqueId, MultiplayerRole role,
//         MpControllerType mpControllerType, MpControllerSpawningData mpControllerSpawningData)
//     {
//         Logger.Log(
//             $"Spawned mpController on peer {multiplayerUniqueId} for MultiplayerRole {role} and MpControlelrType {mpControllerType} with {mpControllerSpawningData}",
//             0);
//     }
// }