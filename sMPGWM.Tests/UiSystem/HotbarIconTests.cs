using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: HOTBAR-ICON-001 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind

[Category("Godot")] // TEST-SUPPORT: HOTBAR-ICON-002 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
public sealed class HotbarIconTests // TEST-SUPPORT: HOTBAR-ICON-003 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
{
    [TestCase(false)] // TEST-SUPPORT: HOTBAR-ICON-004 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
    [TestCase(true)] // TEST-SUPPORT: HOTBAR-ICON-005 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
    public void ActionTexturesSurviveRefreshMoveAndRebind(bool namedActions) // TEST-SUPPORT: HOTBAR-ICON-006 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
    {
        using var inventory = new InventoryController(); // TEST-SUPPORT: HOTBAR-ICON-007 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        using var equipment = new EquipmentController(inventory.Items); // TEST-SUPPORT: HOTBAR-ICON-008 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment); // TEST-SUPPORT: HOTBAR-ICON-009 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        using var controller = new ItemHotbarController(inventory, equipment, transfers); // TEST-SUPPORT: HOTBAR-ICON-010 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var firstKey = namedActions ? "ability:shieldCapacityBoost" : "0"; // TEST-SUPPORT: HOTBAR-ICON-011 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var secondKey = namedActions ? "ability:revive" : "1"; // TEST-SUPPORT: HOTBAR-ICON-012 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        controller.BindAction(0, firstKey); // TEST-SUPPORT: HOTBAR-ICON-013 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        controller.BindAction(1, secondKey); // TEST-SUPPORT: HOTBAR-ICON-014 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var session = transfers.OpenEditSession(); // TEST-SUPPORT: HOTBAR-ICON-015 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        controller.SetEditSession(session); // TEST-SUPPORT: HOTBAR-ICON-016 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var first = Icons.Weather.SunnyWarmHotSummerSeason; // TEST-SUPPORT: HOTBAR-ICON-017 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var second = Icons.Misc.MedkitMedicineHealthPickup; // TEST-SUPPORT: HOTBAR-ICON-018 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var named = new Dictionary<string, IconDefinition> { [firstKey] = first, [secondKey] = second }; // TEST-SUPPORT: HOTBAR-ICON-019 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var view = GD.Load<PackedScene>("res://scenes/ui/player_hud/hotbar_control.tscn").Instantiate<HotbarControl>(); // TEST-SUPPORT: HOTBAR-ICON-020 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: HOTBAR-ICON-021 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        root.AddChild(view); // TEST-SUPPORT: HOTBAR-ICON-022 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        try // TEST-SUPPORT: HOTBAR-ICON-023 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        {
            if (namedActions) view.Bind(controller, named); // TEST-SUPPORT: HOTBAR-ICON-024 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            else view.Bind(controller, new[] { first, second }); // TEST-SUPPORT: HOTBAR-ICON-025 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[0].Icon, Is.SameAs(first.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-026 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-027 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            view.Refresh(); // TEST-SUPPORT: HOTBAR-ICON-028 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[0].Icon, Is.SameAs(first.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-029 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(controller.Move(session, controller.Revision, 0, 7), Is.EqualTo(HotbarResult.Success)); // TEST-SUPPORT: HOTBAR-ICON-030 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[0].Icon, Is.Null); // TEST-SUPPORT: HOTBAR-ICON-031 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[7].Icon, Is.SameAs(first.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-032 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-033 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            if (namedActions) view.Bind(controller, named); // TEST-SUPPORT: HOTBAR-ICON-034 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            else view.Bind(controller, new[] { first, second }); // TEST-SUPPORT: HOTBAR-ICON-035 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[7].Icon, Is.SameAs(first.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-036 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture)); // TEST-SUPPORT: HOTBAR-ICON-037 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(controller.Clear(session, controller.Revision, 7), Is.EqualTo(HotbarResult.Success)); // TEST-SUPPORT: HOTBAR-ICON-038 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
            Assert.That(view.Icons[7].Icon, Is.Null); // TEST-SUPPORT: HOTBAR-ICON-039 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
        }
        finally { view.Free(); } // TEST-SUPPORT: HOTBAR-ICON-040 | Tests: sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind
    }
}
