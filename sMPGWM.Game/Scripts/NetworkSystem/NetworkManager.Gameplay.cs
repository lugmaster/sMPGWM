using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.AbilitySystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, ActivationHistory> _abilityResults = new();
    private GameplaySimulation? _simulation;
    public GameplaySimulation Simulation
    {
        get => _simulation ??= new GameplaySimulation(World, Abilities);
        init => _simulation = value;
    }
    private string _pendingCaster = "";
    private long _pendingGeneration;
    private long _nextRequest;
    public AbilityRuntime Abilities { get; init; } = null!;
    private bool _gameplayDirty = true;
    public event Action<AbilityActivationResult>? AbilityFeedback;

    public void ActivateAbility(string ability, string targetEntity, string slot, long source)
        => ActivateAbility(ability, targetEntity, AbilitySource.Equipment(slot, source));

    public void ActivateAbility(string ability, string targetEntity, AbilitySource source)
    {
        if (_stopped || Synchronization == null || !source.IsValid ||
            World.LocalPlayer?.Participant?.ControlledShip is not { } ship ||
            (!IsHost() && (_clientJoin.Phase != ClientJoinPhase.Ready ||
                source.Kind == AbilitySourceKind.Equipment && !ItemBaselineReady))) return;
        var owner = World.LocalPlayer.Participant;
        _pendingCaster = ship.EntityId; _pendingGeneration = owner.ControlGeneration;
        var request = ++_nextRequest;
        var target = targetEntity;
        AbilityFeedback?.Invoke(AbilityActivationResult.Pending);
        if (IsHost()) QueueAbility(1, Synchronization.Session.ToString("N"), request, ship.EntityId, owner.ControlGeneration,
            target, ability, source.Key, source.Entity, (int)source.Kind);
        else if (RpcId(1, nameof(RequestAbility), Synchronization.Session.ToString("N"), request, ship.EntityId,
                     owner.ControlGeneration, target, ability, source.Key, source.Entity, (int)source.Kind) != Error.Ok)
            AbilityFeedback?.Invoke(AbilityActivationResult.SendFailed);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestAbility(string session, long request, string caster, long generation, string target,
        string ability, string slot, long source, int sourceKind)
        => QueueAbility(Multiplayer.GetRemoteSenderId(), session, request, caster, generation, target, ability, slot, source, sourceKind);

    private void QueueAbility(int peer, string session, long request, string caster, long generation, string target,
        string ability, string slot, long source, int sourceKind)
    {
        var origin = new AbilitySource((AbilitySourceKind)sourceKind, slot, source);
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            request <= 0 || caster.Length != 32 || target.Length != 32 || ability.Length is < 1 or > 64 || !origin.IsValid) return;
        if (!_abilityResults.TryGetValue(peer, out var history)) _abilityResults.Add(peer, history = new());
        if (!history.Admit(request, Synchronization.CompletedTick, out var result))
        { ReplyAbility(peer, request, result); return; }
        if (Simulation.AbilityCommands.Count >= 32)
        {
            history.Complete(request, AbilityActivationResult.Busy);
            ReplyAbility(peer, request, AbilityActivationResult.Busy); return;
        }
        Simulation.AbilityCommands.Enqueue((peer, request, caster, generation, target, ability, origin));
    }

    public void PublishGameplay(long tick)
    {
        if (!IsHost() || tick % 2 != 0) return;
        PublishWorldTopology();
        PublishItemChanges();
        PublishWeaponStates(tick);
        PublishGameplayDeltas();
    }

    internal void CompleteAbility(int peer, long request, AbilityActivationResult result)
    {
        if (_abilityResults.TryGetValue(peer, out var history)) history.Complete(request, result);
        ReplyAbility(peer, request, result);
    }

    private Godot.Collections.Array GameplaySnapshot()
    {
        var rows = new Godot.Collections.Array();
        foreach (var ship in World.Entities.Values)
        {
            var row = GameplaySnapshotCodec.Encode(ship.EntityId, ship.Runtime);
            row[0] = GetWeaponEntityIndex(ship.EntityId);
            rows.Add(row);
        }
        return rows;
    }

    private bool ApplyGameplay(Array rows, bool requireComplete)
    {
        using var decodedRows = new Array();
        foreach (var value in rows)
        {
            if (value.VariantType != Variant.Type.Array) return false;
            var row = value.AsGodotArray();
            if (row.Count != 19 || row[0].VariantType != Variant.Type.Int || row[0].AsInt64() is <= 0 or > int.MaxValue ||
                ResolveNetworkEntity(row[0].AsInt32()) is not { } entity) return false;
            var copy = row.Duplicate();
            copy[0] = entity;
            decodedRows.Add(copy);
        }
        if (!GameplaySnapshotCodec.TryDecode(decodedRows, Abilities.Definitions, out var entries)) return false;
        if (requireComplete)
        {
            if (entries.Length != World.Entities.Count) return false;
            foreach (var entry in entries)
                if (!World.Entities.ContainsKey(entry.Entity)) return false;
        }
        foreach (var entry in entries)
            if (World.Entities.TryGetValue(entry.Entity, out var ship))
                ship.Runtime.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
                    entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable,
                    entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive,
                    entry.HealthCapacity, entry.HealthCurrent, entry.Components);
        SeedGameplayDeltas(entries);
        return true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveGameplay(string session, Godot.Collections.Array rows)
    {
        if (IsHostMessage() && _clientJoin.HasWorld && session == Synchronization!.Session.ToString("N") &&
            !ApplyGameplay(rows, _clientJoin.Phase != ClientJoinPhase.Ready))
            FailSession("Invalid gameplay snapshot.");
    }
    private void ReplyAbility(int peer, long request, AbilityActivationResult result)
    {
        if (peer == 1)
        {
            if (request == _nextRequest && World.LocalPlayer?.Participant?.ControlledShip?.EntityId == _pendingCaster &&
                World.LocalPlayer.Participant.ControlGeneration == _pendingGeneration) AbilityFeedback?.Invoke(result);
        }
        else RpcId(peer, nameof(AbilityResult), Synchronization!.Session.ToString("N"), request, (int)result);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AbilityResult(string session, long request, int result)
    {
        if (IsHostMessage() && session == Synchronization!.Session.ToString("N") && request == _nextRequest &&
            Enum.IsDefined((AbilityActivationResult)result) &&
            World.LocalPlayer?.Participant?.ControlledShip?.EntityId == _pendingCaster &&
            World.LocalPlayer.Participant.ControlGeneration == _pendingGeneration)
            AbilityFeedback?.Invoke((AbilityActivationResult)result);
    }
    private void ClearGameplay()
    {
        _simulation?.Dispose(); _abilityResults.Clear(); AbilityFeedback = null;
        ClearGameplayDeltas();
        Abilities?.Dispose();
    }
}
