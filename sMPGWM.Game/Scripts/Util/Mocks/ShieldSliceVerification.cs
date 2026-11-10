using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Scripts.Util.Mocks; // TEST-SUPPORT: SHIELD-RUN-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge

public partial class ShieldSliceVerification : Node // TEST-SUPPORT: SHIELD-RUN-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
{
    private double _elapsed; // TEST-SUPPORT: SHIELD-RUN-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private double _active; // TEST-SUPPORT: SHIELD-RUN-004 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private int _step; // TEST-SUPPORT: SHIELD-RUN-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private int _logged; // TEST-SUPPORT: SHIELD-RUN-006 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private bool _started; // TEST-SUPPORT: SHIELD-RUN-007 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private bool _boostSeen; // TEST-SUPPORT: SHIELD-RUN-008 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private bool _expiredSeen; // TEST-SUPPORT: SHIELD-RUN-009 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private bool _variants; // TEST-SUPPORT: ABILITY-RUN-004 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private bool _host; // TEST-SUPPORT: SHIELD-RUN-010 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    public override void _Process(double delta) // TEST-SUPPORT: SHIELD-RUN-011 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    {
        _elapsed += delta; // TEST-SUPPORT: SHIELD-RUN-012 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if (!_started && _elapsed > 1) // TEST-SUPPORT: SHIELD-RUN-013 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            _variants = Array.IndexOf(OS.GetCmdlineUserArgs(), "--ability-variants") >= 0; // TEST-SUPPORT: ABILITY-RUN-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            _started = true; // TEST-SUPPORT: SHIELD-RUN-014 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            _host = Array.IndexOf(OS.GetCmdlineUserArgs(), "--shield-check-host") >= 0; // TEST-SUPPORT: SHIELD-RUN-015 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            GameFlowManager.Instance.StartNetworkGame(new(_host ? NetworkRole.Host : NetworkRole.Client, NetworkType.Lan, 10, 15000, "127.0.0.1", Array.IndexOf(OS.GetCmdlineUserArgs(), "--shield-check-proxy") >= 0 ? 55672 : 55671)); // TEST-SUPPORT: SHIELD-RUN-016 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        }
        if (GameFlowManager.Instance.GameFlowState == GameFlowState.InGame) // TEST-SUPPORT: SHIELD-RUN-017 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            _active += delta; // TEST-SUPPORT: SHIELD-RUN-018 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            var network = NetworkManager.Instance; // TEST-SUPPORT: SHIELD-RUN-019 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            var local = network.World.LocalPlayer!.Participant!.ControlledShip!; // TEST-SUPPORT: SHIELD-RUN-020 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if (_active > 2 + _step * (_variants ? 2 : 4) && _step < (_variants ? 8 : 4)) // TEST-SUPPORT: SHIELD-RUN-021 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            {
                network.SelectedTarget = ""; // TEST-SUPPORT: SHIELD-RUN-022 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                if ((_variants ? _step / 2 : _step) is 1 or 3) // TEST-SUPPORT: SHIELD-RUN-023 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    foreach (var player in network.World.Players.Values) // TEST-SUPPORT: SHIELD-RUN-024 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        if (player.ControlledShip != null && player.ControlledShip != local && ((_variants ? _step / 2 : _step) != 3 || _host || player.ControllingPeerId != 1)) // TEST-SUPPORT: SHIELD-RUN-025 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        { network.SelectedTarget = player.ControlledShip.EntityId; break; } // TEST-SUPPORT: SHIELD-RUN-026 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, true); // TEST-SUPPORT: SHIELD-RUN-027 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                var nodes = GetTree().CurrentScene.FindChildren("*", "", true, false); // TEST-SUPPORT: SHIELD-RUN-028 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                foreach (var node in nodes) // TEST-SUPPORT: SHIELD-RUN-029 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    if (node is HotbarControl hotbar) hotbar.Icons[_variants ? _step % 2 : 0].EmitSignal(Button.SignalName.Pressed); // TEST-SUPPORT: SHIELD-RUN-030 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                GD.Print($"SHIELD_CHECK activate step={_step} tick={network.Synchronization!.CompletedTick} target={network.SelectedTarget}"); // TEST-SUPPORT: SHIELD-RUN-031 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                _step++; // TEST-SUPPORT: SHIELD-RUN-032 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            }
            if ((int)_active > _logged) // TEST-SUPPORT: SHIELD-RUN-033 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            {
                _logged = (int)_active; // TEST-SUPPORT: SHIELD-RUN-034 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                foreach (var ship in network.World.Entities.Values) // TEST-SUPPORT: SHIELD-RUN-035 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                {
                    var state = ship.Gameplay; // TEST-SUPPORT: SHIELD-RUN-036 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    var instances = new System.Text.StringBuilder(); // TEST-SUPPORT: ABILITY-RUN-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    foreach (var active in state.ActiveBehaviours) instances.Append($"{active.DefinitionId}:{active.RuntimeId}:{active.Deadline},"); // TEST-SUPPORT: ABILITY-RUN-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    GD.Print($"SHIELD_CHECK state time={_active:F1} entity={ship.EntityId} current={state.Shield.Current} max={state.Shield.Capacity} behaviours={instances} revision={state.Revision}"); // TEST-SUPPORT: ABILITY-RUN-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                }
                _boostSeen |= local.Gameplay.Shield.Capacity == (_variants ? 175 : 150); // TEST-SUPPORT: SHIELD-RUN-038 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                _expiredSeen |= _boostSeen && _active > 40 && local.Gameplay.Shield.Capacity == 100; // TEST-SUPPORT: SHIELD-RUN-039 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            }
        }
        if (_elapsed > (_host ? 95 : 70)) // TEST-SUPPORT: SHIELD-RUN-040 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            var success = _boostSeen && _expiredSeen; // TEST-SUPPORT: SHIELD-RUN-041 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            GD.Print($"SHIELD_CHECK complete success={success} active={_active:F1}"); // TEST-SUPPORT: SHIELD-RUN-042 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            GameFlowManager.Instance.QuitToStartMenu(); // TEST-SUPPORT: SHIELD-RUN-043 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            GetTree().Quit(success ? 0 : 2); // TEST-SUPPORT: SHIELD-RUN-044 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        }
    }
}
