using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class HotbarIconTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ActionTexturesSurviveRefreshMoveAndRebind(bool namedActions)
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
        using var controller = new ItemHotbarController(inventory, equipment, transfers);
        var firstKey = namedActions ? "ability:shieldCapacityBoost" : "0";
        var secondKey = namedActions ? "ability:revive" : "1";
        controller.BindAction(0, firstKey);
        controller.BindAction(1, secondKey);
        var session = transfers.OpenEditSession();
        controller.SetEditSession(session);
        var first = Icons.Weather.SunnyWarmHotSummerSeason;
        var second = Icons.Misc.MedkitMedicineHealthPickup;
        var named = new Dictionary<string, IconDefinition> { [firstKey] = first, [secondKey] = second };
        var view = GD.Load<PackedScene>("res://scenes/ui/player_hud/hotbar_control.tscn").Instantiate<HotbarControl>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(view);
        try
        {
            if (namedActions) view.Bind(controller, named);
            else view.Bind(controller, new[] { first, second });
            Assert.That(view.Icons[0].Icon, Is.SameAs(first.IconTexture));
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture));
            view.Refresh();
            Assert.That(view.Icons[0].Icon, Is.SameAs(first.IconTexture));
            Assert.That(controller.Move(session, controller.Revision, 0, 7), Is.EqualTo(HotbarResult.Success));
            Assert.That(view.Icons[0].Icon, Is.Null);
            Assert.That(view.Icons[7].Icon, Is.SameAs(first.IconTexture));
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture));
            if (namedActions) view.Bind(controller, named);
            else view.Bind(controller, new[] { first, second });
            Assert.That(view.Icons[7].Icon, Is.SameAs(first.IconTexture));
            Assert.That(view.Icons[1].Icon, Is.SameAs(second.IconTexture));
            Assert.That(controller.Clear(session, controller.Revision, 7), Is.EqualTo(HotbarResult.Success));
            Assert.That(view.Icons[7].Icon, Is.Null);
        }
        finally { view.Free(); }
    }
}
