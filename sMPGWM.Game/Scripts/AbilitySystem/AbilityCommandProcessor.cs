using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.World;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityCommandProcessor
{
    private readonly Queue<(int Peer, long Request, string Caster, long Generation, string Target, string Ability, AbilitySource Source)> _queue = new(32);
    public int Count => _queue.Count;
    public void Clear() => _queue.Clear();
    public void Enqueue((int Peer, long Request, string Caster, long Generation, string Target, string Ability, AbilitySource Source) command)
    {
        if (_queue.Count >= 32) throw new InvalidOperationException("Ability queue is full.");
        _queue.Enqueue(command);
    }
    public void Step(long tick, GameWorld world, AbilityRuntime abilities,
        IReadOnlyDictionary<int, PlayerItemState> playerItems, Func<int, bool> isReady,
        Action<int, long, AbilityActivationResult> complete)
    {
        while (_queue.TryDequeue(out var request))
        {
            var result = AbilityActivationResult.Accepted;
            if (!isReady(request.Peer) || !world.Players.TryGetValue(request.Peer, out var player) ||
                player.ControlledShip?.EntityId != request.Caster || player.ControlGeneration != request.Generation)
                result = AbilityActivationResult.CasterChanged;
            else if (!abilities.Definitions.Abilities.TryGetById(request.Ability, out var definition))
                result = AbilityActivationResult.UnknownAbility;
            else if (!request.Source.IsAvailable(definition, player.ControlledShip.Gameplay,
                         playerItems.TryGetValue(request.Peer, out var items) ? items : null))
                result = AbilityActivationResult.AbilitySourceChanged;
            else if (!world.Entities.TryGetValue(request.Target, out var target))
                result = AbilityActivationResult.TargetIsNotALivingAlly;
            else
            {
                var context = new AbilityActivationContext(player.ControlledShip.Gameplay, target.Gameplay,
                    player.ControlledShip.GlobalPosition.DistanceSquaredTo(target.GlobalPosition));
                result = abilities.Activate(request.Ability, context, tick);
            }
            complete(request.Peer, request.Request, result);
        }
    }
}
