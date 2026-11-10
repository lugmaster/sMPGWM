using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameConfigSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, ActivationHistory> _abilityResults = new();
    private readonly Queue<(int Peer, long Request, string Caster, long Generation, string Target, string Ability)> _abilityQueue = new();
    private string _pendingCaster = "";
    private long _pendingGeneration;
    private long _nextRequest;
    private long _nextBehaviour;
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
            var definition = GameplayDefinitions.Boost;
            var result = "Accepted";
            if (!MovementReady(request.Peer) || !World.Players.TryGetValue(request.Peer, out var player) ||
                player.ControlledShip?.EntityId != request.Caster || player.ControlGeneration != request.Generation)
                result = "Rejected: caster changed";
            else if (!GameConfig.Development || request.Ability != definition.AbilityId) result = "Rejected: ability not granted";
            else if (!World.Entities.TryGetValue(request.Target, out var target) || target.ControllingPlayer == null)
                result = "Rejected: target is not a living ally";
            else if (player.ControlledShip.GlobalPosition.DistanceSquaredTo(target.GlobalPosition) > definition.Range * definition.Range)
                result = "Rejected: out of range";
            else
            {
                target.Gameplay.Apply(definition, tick, ++_nextBehaviour);
                _gameplayDirty = true;
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
        {
            var state = ship.Gameplay;
            rows.Add(new Godot.Collections.Array { ship.EntityId, state.Revision, state.Shield.Capacity,
                state.Shield.Current, state.BehaviourInstance, state.Deadline,
                state.BehaviourInstance == 0 ? "" : GameplayDefinitions.Boost.BehaviourId });
        }
        return rows;
    }

    private bool ApplyGameplay(Godot.Collections.Array rows)
    {
        if (rows.Count > 64) return false;
        var seen = new HashSet<string>();
        foreach (var entry in rows)
        {
            if (entry.VariantType != Variant.Type.Array) return false;
            var row = entry.AsGodotArray();
            if (row.Count != 7 || row[0].VariantType != Variant.Type.String || row[1].VariantType != Variant.Type.Int ||
                row[2].VariantType != Variant.Type.Float || row[3].VariantType != Variant.Type.Float ||
                row[4].VariantType != Variant.Type.Int || row[5].VariantType != Variant.Type.Int || row[6].VariantType != Variant.Type.String) return false;
            var entity = row[0].AsString();
            var capacity = row[2].AsDouble(); var current = row[3].AsDouble();
            if (!seen.Add(entity) || !double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current ||
                row[1].AsInt64() < 0 || row[4].AsInt64() < 0 || row[5].AsInt64() < 0 ||
                (row[4].AsInt64() == 0) != (row[5].AsInt64() == 0) ||
                row[6].AsString() != (row[4].AsInt64() == 0 ? "" : GameplayDefinitions.Boost.BehaviourId)) return false;
        }
        foreach (var entry in rows)
        {
            var row = entry.AsGodotArray();
            if (World.Entities.TryGetValue(row[0].AsString(), out var ship))
                ship.Gameplay.Confirm(row[1].AsInt64(), row[2].AsDouble(), row[3].AsDouble(), row[4].AsInt64(), row[5].AsInt64());
        }
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
    }
}
