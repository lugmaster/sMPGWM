using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.ShipSystem;
using System.Reflection;
using Godot;
using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Scripts.Util.Mocks;

/// <summary>Explicitly mounted by the headless synchronization smoke only.</summary>
public partial class SynchronizationSmokeInput : Node
{
    private int _tick;
    private readonly Random _random = new(1729);
    private readonly List<(ulong Due, Action Send)> _pending = new();
    private bool _attached, _stalled;
    public override void _Process(double delta)
    {
        if (!OS.GetCmdlineUserArgs().Contains("impaired")) return;
        var network = NetworkManager.Instance;
        if (network != null && !_attached)
        {
            _attached = true;
            network.MovementDeliveryOverride = send =>
            {
                // Movement-only deterministic impairment: 50 +/-25 ms each direction,
                // 10% random loss, 5% duplication, natural reordering and a 250 ms burst.
                if (_pending.Count >= 128 || _random.NextDouble() < .1 || (_tick > 210 && _tick < 225)) return;
                ulong due = Time.GetTicksMsec() + (ulong)_random.Next(25, 76);
                _pending.Add((due, send));
                if (_random.NextDouble() < .05 && _pending.Count < 128) _pending.Add((due + 10, send));
            };
        }
        foreach (var item in _pending.Where(p => p.Due <= Time.GetTicksMsec()).ToArray())
        {
            _pending.Remove(item); item.Send();
        }
        if (!_stalled && _tick > 360 && network != null && !network.IsHost())
        {
            _stalled = true;
            System.Threading.Thread.Sleep(750);
        }
    }
    public override void _Ready()
    {
        Engine.MaxFps = OS.GetCmdlineUserArgs().Contains("a") ? 30 : OS.GetCmdlineUserArgs().Contains("b") ? 144 : 60;
    }
    public override void _PhysicsProcess(double delta)
    {
        // A headless window cannot receive OS focus; exercise the normal input pipeline after this boundary.
        typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused),
            BindingFlags.Public | BindingFlags.Static)!.SetValue(null, true);
        _tick++;
        var network = NetworkManager.Instance;
        if (network?.IsHost() == true && OS.GetCmdlineUserArgs().Contains("lifecycle"))
        {
            var owner = network.World.MpControllers.FirstOrDefault(p => p.Key != 1).Value;
            if (_tick == 480 && owner?.ControlledShip is { } teleported)
                teleported.Teleport(teleported.GlobalPosition + new Vector2(200, 100), 1, Vector2.Zero);
            if (_tick == 540 && owner?.ControlledShip is { } previous)
                owner.AssignShip(ShipFactory.Create(previous.Data, Guid.NewGuid().ToString("N"),
                    previous.GlobalPosition + new Vector2(100, 100), 0, Vector2.Zero));
        }
        int phase = _tick % 600;
        if (phase < 480)
        {
            Input.ActionRelease("Break");
            Input.ActionPress("Forwards");
            if (phase > 180 && phase < 300) Input.ActionPress("AfterBurner");
            else Input.ActionRelease("AfterBurner");
        }
        else
        {
            Input.ActionRelease("Forwards"); Input.ActionRelease("AfterBurner");
            Input.ActionPress("Break");
        }
    }
    public override void _ExitTree()
    {
        Input.ActionRelease("Forwards"); Input.ActionRelease("AfterBurner"); Input.ActionRelease("Break");
    }
}
