using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, ActivationHistory> _abilityResults = new();
    private readonly Queue<(int Peer, long Request, string Caster, long Generation, string Target, string Ability)> _abilityQueue = new();
    private string _pendingCaster = "";
    private long _pendingGeneration;
    private long _nextRequest;
    public AbilityRuntime Abilities { get; init; } = null!;
    private bool _gameplayDirty = true;
    public string SelectedTarget { get; set; } = "";
    public event Action<string>? AbilityFeedback;

    public void ActivateAbility(string ability)
    {
        if (_stopped || Synchronization == null || World.LocalPlayer?.Participant?.ControlledShip is not { } ship) return;
        var owner = World.LocalPlayer.Participant;
        _pendingCaster = ship.EntityId; _pendingGeneration = owner.ControlGeneration;
        var request = ++_nextRequest;
        var target = SelectedTarget.Length == 0 ? ship.EntityId : SelectedTarget;
        AbilityFeedback?.Invoke("Pending");
        if (IsHost()) QueueAbility(1, Synchronization.Session.ToString("N"), request, ship.EntityId, owner.ControlGeneration, target, ability);
        else RpcId(1, nameof(RequestAbility), Synchronization.Session.ToString("N"), request, ship.EntityId, owner.ControlGeneration, target, ability);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestAbility(string session, long request, string caster, long generation, string target, string ability)
        => QueueAbility(Multiplayer.GetRemoteSenderId(), session, request, caster, generation, target, ability);

    private void QueueAbility(int peer, string session, long request, string caster, long generation, string target, string ability)
    {
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            request <= 0 || caster.Length != 32 || target.Length != 32 || ability.Length > 64) return;
        if (!_abilityResults.TryGetValue(peer, out var history)) _abilityResults.Add(peer, history = new());
        if (!history.Admit(request, Synchronization.CompletedTick, out var result))
        { ReplyAbility(peer, request, result); return; }
        if (_abilityQueue.Count >= 32)
        {
            history.Complete(request, "Rejected: busy");
            ReplyAbility(peer, request, "Rejected: busy"); return;
        }
        _abilityQueue.Enqueue((peer, request, caster, generation, target, ability));
    }

    public void StepGameplay(long tick)
    {
        if (!IsHost()) return;
        foreach (var ship in World.Entities.Values) _gameplayDirty |= ship.Gameplay.Expire(tick);
        while (_abilityQueue.TryDequeue(out var request))
        {
            var result = "Accepted";
            if (!MovementReady(request.Peer) || !World.Players.TryGetValue(request.Peer, out var player) ||
                player.ControlledShip?.EntityId != request.Caster || player.ControlGeneration != request.Generation)
                result = "Rejected: caster changed";
            else if (!World.Entities.TryGetValue(request.Target, out var target) || target.ControllingPlayer == null)
                result = "Rejected: target is not a living ally";
            else
            {
                // No current producer can add conditions; future condition state supplies this context.
                var context = new AbilityActivationContext(player.ControlledShip.Gameplay, target.Gameplay, true,
                    player.ControlledShip.GlobalPosition.DistanceSquaredTo(target.GlobalPosition), Array.Empty<string>());
                result = Abilities.Activate(request.Ability, context, tick);
                _gameplayDirty |= result == "Accepted";
            }
            if (_abilityResults.TryGetValue(request.Peer, out var history)) history.Complete(request.Request, result);
            ReplyAbility(request.Peer, request.Request, result);
        }
        if (_gameplayDirty && tick % 2 == 0)
        {
            PublishWorldTopology();
            var state = GameplaySnapshot();
            foreach (var (peer, join) in _joins.Snapshot())
                if (join.ReceivesWorld) RpcId(peer, nameof(ReceiveGameplay), Synchronization!.Session.ToString("N"), state);
            _gameplayDirty = false;
        }
    }

    private Godot.Collections.Array GameplaySnapshot()
    {
        var rows = new Godot.Collections.Array();
        foreach (var ship in World.Entities.Values)
            rows.Add(GameplaySnapshotCodec.Encode(ship.EntityId, ship.Gameplay));
        return rows;
    }

    private bool ApplyGameplay(Godot.Collections.Array rows)
    {
        if (!GameplaySnapshotCodec.TryDecode(rows, Abilities.Definitions, out var entries)) return false;
        foreach (var entry in entries)
            if (World.Entities.TryGetValue(entry.Entity, out var ship))
                ship.Gameplay.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours);
        return true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveGameplay(string session, Godot.Collections.Array rows)
    {
        if (IsHostMessage() && _clientJoin.HasWorld && session == Synchronization!.Session.ToString("N") && !ApplyGameplay(rows))
            FailSession("Invalid gameplay snapshot.");
    }
    private void ReplyAbility(int peer, long request, string result)
    {
        if (peer == 1) AbilityFeedback?.Invoke(result);
        else RpcId(peer, nameof(AbilityResult), Synchronization!.Session.ToString("N"), request, result);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AbilityResult(string session, long request, string result)
    {
        if (IsHostMessage() && session == Synchronization!.Session.ToString("N") && request == _nextRequest && result.Length < 128 &&
            World.LocalPlayer?.Participant?.ControlledShip?.EntityId == _pendingCaster &&
            World.LocalPlayer.Participant.ControlGeneration == _pendingGeneration)
            AbilityFeedback?.Invoke(result);
    }
    private void ClearGameplay()
    {
        _abilityQueue.Clear(); _abilityResults.Clear(); AbilityFeedback = null; SelectedTarget = "";
        Abilities?.Dispose();
    }
}
