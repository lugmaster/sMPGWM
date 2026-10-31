using ItemLocation = sMPGWM.Scripts.EquipmentSystem.ItemLocation;


using System;
using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventoryVisualSmokeFixture : RefCounted
{
    private InventoryController? _inventory;
    private EquipmentController? _equipment;

    public void Show(GamePlayManager game)
    {
        _inventory = new InventoryController();
        _equipment = new EquipmentController(_inventory.ItemRequestService);
        foreach (var definition in ItemRegistry.GetAll().Where(d => d.Id.StartsWith("mock_standard_")))
            _inventory.ItemRequestService.CreateItems(_inventory.Container, definition.Id, definition.CanStack ? 11 : 1);
        var canvas = game.GetNode<UiCanvas>("UiCanvas");
        canvas.CloseAllMenus();
        game.BindInventoryPresentation(_inventory, _equipment);
        canvas.OpenMenu(UiWindowId.InventoryMenu);
    }

    public bool Verify(GamePlayManager game)
    {
        try
        {
            var definition = ItemRegistry.GetById("mock_standard_resource");
            Require(ItemIconResolver.Item(definition) == definition.Icon.IconTexture,
                "Item visuals must use the resolved definition icon.");
            var canvas = game.GetNode<UiCanvas>("UiCanvas");
            var resource = canvas.FindChildren("*", "ClickableIcon", true, false).OfType<ClickableIcon>().First();
            resource.SetDisplay(definition.Icon.IconTexture, new IconDisplayData("Large quantity", int.MaxValue));
            Require(resource.GetNode<Label>("QuantityLabel").Text == "2.1B", "Large stack badge must abbreviate.");
            resource.SetDisplay(resource.IconTexture, new IconDisplayData("Unavailable link", 0));
            Require(resource.GetNode<Label>("QuantityLabel").Text == "0", "Unavailable links must display zero.");
            resource.Disabled = true;
            Require(resource.TooltipText == "Unavailable link", "Disabled activation must retain tooltip data.");
            resource.DismissTooltip();
            Require(resource.TooltipText.Length == 0, "Explicit dismissal must clear tooltip text.");
            resource.SetDisplay(null, new IconDisplayData());
            Require(resource.IconTexture == null && resource.TooltipText.Length == 0 &&
                !resource.GetNode<Label>("QuantityLabel").Visible && resource.GetNode<Label>("QuantityLabel").Text.Length == 0,
                "Reused empty cells must clear all previous display fields.");
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"Inventory visual fixture failed: {exception}");
            return false;
        }
    }

    public bool MoveSource()
    {
        using var transfers = new InventoryEquipmentTransferService(_inventory!, _equipment!);
        var source = _inventory!.Snapshot.Slots[7]!;
        return transfers.Transfer(new TransferRequest(transfers.OpenEditSession(), ItemLocation.Inventory(7),
            source.Id, ItemLocation.Inventory(100), null, _inventory.Revision, _equipment!.Revision)) == TransferResult.Success;
    }

    public void Restore(GamePlayManager game)
    {
        var canvas = game.GetNode<UiCanvas>("UiCanvas");
        canvas.CloseAllMenus();
        game.BindInventoryPresentation(game.InventoryController, game.EquipmentController, game.ItemTransfers);
        _equipment?.Dispose();
        _inventory?.Dispose();
        _equipment = null;
        _inventory = null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
