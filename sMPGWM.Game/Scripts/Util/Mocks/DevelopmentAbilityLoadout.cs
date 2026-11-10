using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.Items.Hotbar;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Util.Mocks; // TEST-SUPPORT: ABILITY-LOADOUT-001 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge

internal sealed class DevelopmentAbilityLoadout // TEST-SUPPORT: ABILITY-LOADOUT-002 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
{
    private readonly IReadOnlyList<string> _ids; // TEST-SUPPORT: ABILITY-LOADOUT-003 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    private readonly AbilityRuntime _runtime; // TEST-SUPPORT: ABILITY-LOADOUT-004 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    public DevelopmentAbilityLoadout(AbilityRuntime runtime) // TEST-SUPPORT: ABILITY-LOADOUT-005 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    {
        _runtime = runtime; // TEST-SUPPORT: ABILITY-LOADOUT-006 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        _ids = runtime.Definitions.Loadouts.GetById("development").AbilityIds; // TEST-SUPPORT: ABILITY-LOADOUT-007 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        foreach (var id in _ids) // TEST-SUPPORT: ABILITY-LOADOUT-008 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if (!runtime.IsSupported(id, out var reason)) throw new InvalidOperationException("Development loadout: " + reason); // TEST-SUPPORT: ABILITY-LOADOUT-009 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    }
    public void Grant(ShipController ship) => ship.Gameplay.Grants.SetSource("development", _ids); // TEST-SUPPORT: ABILITY-LOADOUT-010 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    public void Bind(ItemHotbarController hotbar, UiCanvas canvas) // TEST-SUPPORT: ABILITY-LOADOUT-011 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    {
        var icons = new IconDefinition[_ids.Count]; // TEST-SUPPORT: ABILITY-LOADOUT-012 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for (var i = 0; i < _ids.Count; i++) // TEST-SUPPORT: ABILITY-LOADOUT-013 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            var ability = _runtime.Definitions.Abilities.GetById(_ids[i]); // TEST-SUPPORT: ABILITY-LOADOUT-014 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            var key = "ability:" + ability.Id; // TEST-SUPPORT: ABILITY-LOADOUT-015 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            hotbar.SetActionTitle(key, ability.DisplayName); // TEST-SUPPORT: ABILITY-LOADOUT-016 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            hotbar.BindAction(i, key); // TEST-SUPPORT: ABILITY-LOADOUT-017 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            icons[i] = Icons.Get(ability.Icon ?? "Software.WarningSignCircleQuestionMarkHelp"); // TEST-SUPPORT: ABILITY-LOADOUT-018 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        }
        canvas.BindHotbar(hotbar, icons); // TEST-SUPPORT: ABILITY-LOADOUT-019 | Tests: sMPGWM.Tests.ShieldGameplayIntegrationTests.HostAdmissionRefreshDuplicateAndExpiryUseNormalSession, sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    }
}
