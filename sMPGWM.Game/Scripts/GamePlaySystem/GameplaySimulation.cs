using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class GameplaySimulation : IDisposable
{
    private const int MaxTransfers = 32;
    private readonly GameWorld _world;
    private readonly AbilityRuntime _abilities;
    private readonly Queue<PendingTransfer> _transfers = new(MaxTransfers);
    private readonly PendingTransfer[] _completedTransfers = new PendingTransfer[MaxTransfers];
    private readonly TransferResult[] _transferResults = new TransferResult[MaxTransfers];
    private readonly (int Peer, long Request, AbilityActivationResult Result)[] _abilityResults = new (int, long, AbilityActivationResult)[32];
    private readonly Action<int, long, AbilityActivationResult> _recordAbility;
    private int _completedAbilities;
    private bool _disposed;
    private bool _stepping;
    public AbilityCommandProcessor AbilityCommands { get; } = new();
    public long CompletedTick { get; private set; } = -1;

    public GameplaySimulation(GameWorld world, AbilityRuntime abilities)
    {
        _world = world;
        _abilities = abilities;
        _recordAbility = RecordAbility;
    }

    public bool QueueTransfer(int peer, string entity, long generation, TransferRequest request,
        Action<TransferResult> completed)
    {
        if (_disposed || _transfers.Count >= MaxTransfers) return false;
        _transfers.Enqueue(new PendingTransfer(peer, entity, generation, request, completed));
        return true;
    }

    public void Step(long tick, Func<int, bool> ready, Action<int, long, AbilityActivationResult> completed)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GameplaySimulation));
        if (_stepping || tick <= CompletedTick) throw new InvalidOperationException("Gameplay ticks must advance exactly once.");
        _stepping = true;
        try
        {
            var count = _transfers.Count;
            for (var i = 0; i < count; i++)
            {
                var command = _transfers.Dequeue();
                var result = TransferResult.InvalidSession;
                if (ready(command.Peer) && _world.Players.TryGetValue(command.Peer, out var player) &&
                    player.ControlGeneration == command.Generation &&
                    (player.ControlledShip?.EntityId ?? "") == command.Entity)
                    result = player.Transfer(command.Request);
                _completedTransfers[i] = command;
                _transferResults[i] = result;
            }
            foreach (var ship in _world.Entities.Values)
            {
                ship.Runtime.CommitConfiguration(tick);
            }
            _completedAbilities = 0;
            AbilityCommands.Step(tick, _world, _abilities, ready, _recordAbility);
            foreach (var ship in _world.Entities.Values)
            {
                var input = ship.InputState;
                ship.Runtime.SetAfterburner(input.Afterburner && !input.Brake && input.Direction.LengthSquared() > .01f);
                ship.Runtime.AdvanceResources(tick);
                ship.RefreshMovementConfiguration();
            }
            foreach (var player in _world.Players.Values) player.ControlledShip?.Simulate(1.0 / 60);
            CompletedTick = tick;
            for (var i = 0; i < count; i++)
            {
                var command = _completedTransfers[i];
                _completedTransfers[i] = default;
                command.Completed(_transferResults[i]);
            }
            for (var i = 0; i < _completedAbilities; i++)
            {
                var result = _abilityResults[i];
                completed(result.Peer, result.Request, result.Result);
            }
        }
        finally { _stepping = false; }
    }

    private void RecordAbility(int peer, long request, AbilityActivationResult result) =>
        _abilityResults[_completedAbilities++] = (peer, request, result);

    public void RemovePeer(int peer)
    {
        AbilityCommands.RemovePeer(peer);
        var count = _transfers.Count;
        for (var i = 0; i < count; i++)
        {
            var command = _transfers.Dequeue();
            if (command.Peer != peer) _transfers.Enqueue(command);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transfers.Clear();
        Array.Clear(_completedTransfers);
        AbilityCommands.Clear();
    }

    private readonly record struct PendingTransfer(int Peer, string Entity, long Generation,
        TransferRequest Request, Action<TransferResult> Completed);
}
