using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Scripts.Util.Mocks; // TEST-SUPPORT: SHIELD-RUN-001 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup

public partial class ShieldSliceVerification : Node // TEST-SUPPORT: SHIELD-RUN-002 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
{
    private double _elapsed; // TEST-SUPPORT: SHIELD-RUN-003 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private double _active; // TEST-SUPPORT: SHIELD-RUN-004 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private int _step; // TEST-SUPPORT: SHIELD-RUN-005 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private int _logged; // TEST-SUPPORT: SHIELD-RUN-006 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private bool _started; // TEST-SUPPORT: SHIELD-RUN-007 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private bool _boostSeen; // TEST-SUPPORT: SHIELD-RUN-008 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private bool _expiredSeen; // TEST-SUPPORT: SHIELD-RUN-009 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    private bool _host; // TEST-SUPPORT: SHIELD-RUN-010 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    public override void _Process(double delta) // TEST-SUPPORT: SHIELD-RUN-011 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    {
        _elapsed += delta; // TEST-SUPPORT: SHIELD-RUN-012 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        if (!_started && _elapsed > 1) // TEST-SUPPORT: SHIELD-RUN-013 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        {
            _started = true; // TEST-SUPPORT: SHIELD-RUN-014 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            _host = Array.IndexOf(OS.GetCmdlineUserArgs(), "--shield-check-host") >= 0; // TEST-SUPPORT: SHIELD-RUN-015 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            GameFlowManager.Instance.StartNetworkGame(new(_host ? NetworkRole.Host : NetworkRole.Client, NetworkType.Lan, 10, 15000, "127.0.0.1", Array.IndexOf(OS.GetCmdlineUserArgs(), "--shield-check-proxy") >= 0 ? 55672 : 55671)); // TEST-SUPPORT: SHIELD-RUN-016 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        }
        if (GameFlowManager.Instance.GameFlowState == GameFlowState.InGame) // TEST-SUPPORT: SHIELD-RUN-017 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        {
            _active += delta; // TEST-SUPPORT: SHIELD-RUN-018 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            var network = NetworkManager.Instance; // TEST-SUPPORT: SHIELD-RUN-019 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            var local = network.World.LocalPlayer!.Participant!.ControlledShip!; // TEST-SUPPORT: SHIELD-RUN-020 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            if (_active > 2 + _step * 4 && _step < 4) // TEST-SUPPORT: SHIELD-RUN-021 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            {
                network.SelectedTarget = ""; // TEST-SUPPORT: SHIELD-RUN-022 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                if (_step == 1 || _step == 3) // TEST-SUPPORT: SHIELD-RUN-023 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    foreach (var player in network.World.Players.Values) // TEST-SUPPORT: SHIELD-RUN-024 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        if (player.ControlledShip != null && player.ControlledShip != local && (_step != 3 || _host || player.ControllingPeerId != 1)) // TEST-SUPPORT: SHIELD-RUN-025 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        { network.SelectedTarget = player.ControlledShip.EntityId; break; } // TEST-SUPPORT: SHIELD-RUN-026 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, true); // TEST-SUPPORT: SHIELD-RUN-027 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                var nodes = GetTree().CurrentScene.FindChildren("*", "", true, false); // TEST-SUPPORT: SHIELD-RUN-028 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                foreach (var node in nodes) // TEST-SUPPORT: SHIELD-RUN-029 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    if (node is HotbarControl hotbar) hotbar.Icons[0].EmitSignal(Button.SignalName.Pressed); // TEST-SUPPORT: SHIELD-RUN-030 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                GD.Print($"SHIELD_CHECK activate step={_step} tick={network.Synchronization!.CompletedTick} target={network.SelectedTarget}"); // TEST-SUPPORT: SHIELD-RUN-031 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                _step++; // TEST-SUPPORT: SHIELD-RUN-032 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            }
            if ((int)_active > _logged) // TEST-SUPPORT: SHIELD-RUN-033 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            {
                _logged = (int)_active; // TEST-SUPPORT: SHIELD-RUN-034 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                foreach (var ship in network.World.Entities.Values) // TEST-SUPPORT: SHIELD-RUN-035 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                {
                    var state = ship.Gameplay; // TEST-SUPPORT: SHIELD-RUN-036 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    GD.Print($"SHIELD_CHECK state time={_active:F1} entity={ship.EntityId} current={state.Shield.Current} max={state.Shield.Capacity} instance={state.BehaviourInstance} deadline={state.Deadline} revision={state.Revision}"); // TEST-SUPPORT: SHIELD-RUN-037 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                }
                _boostSeen |= local.Gameplay.Shield.Capacity == 150; // TEST-SUPPORT: SHIELD-RUN-038 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                _expiredSeen |= _boostSeen && _active > 40 && local.Gameplay.Shield.Capacity == 100; // TEST-SUPPORT: SHIELD-RUN-039 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            }
        }
        if (_elapsed > (_host ? 95 : 70)) // TEST-SUPPORT: SHIELD-RUN-040 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        {
            var success = _boostSeen && _expiredSeen; // TEST-SUPPORT: SHIELD-RUN-041 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            GD.Print($"SHIELD_CHECK complete success={success} active={_active:F1}"); // TEST-SUPPORT: SHIELD-RUN-042 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            GameFlowManager.Instance.QuitToStartMenu(); // TEST-SUPPORT: SHIELD-RUN-043 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            GetTree().Quit(success ? 0 : 2); // TEST-SUPPORT: SHIELD-RUN-044 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        }
    }
}
