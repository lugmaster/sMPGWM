#nullable enable

using System;
using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventoryVisualSmokeFixture : RefCounted
{
    private InventoryImpl? _inventory;
    private EquipmentImpl? _equipment;

    public void Show(GamePlayManager game)
    {
        var categories = ItemCategories.CreateDefault();
        var definitions = categories.All.Select(category => new ItemDefinition(
            new ItemDefinitionId($"visual_{category.Id.Value}"), $"Fixture {category.DisplayName}", category.Id,
            "Development visual fixture.", "res://assets/mocks/hammer.png",
            category.Id == ItemCategories.Consumable ? 10 : null)).ToList();
        definitions.Add(new ItemDefinition(new ItemDefinitionId("missing_icon"), "Missing icon", ItemCategories.Resource,
            "Identity remains visible with a placeholder.", "res://missing-fixture-icon.png"));
        var catalog = new ItemCatalog(categories, definitions);
        _inventory = new InventoryImpl(catalog);
        _equipment = new EquipmentImpl(catalog);
        foreach (var definition in definitions)
        {
            var count = definition.CategoryId == ItemCategories.Resource && definition.Id.Value != "missing_icon"
                ? (long)int.MaxValue * 2 : definition.CategoryId == ItemCategories.Consumable ? 11 : 1;
            if (_inventory.Add(definition.Id, count) != InventoryResult.Success)
                throw new InvalidOperationException("Visual fixture addition failed.");
        }
        var canvas = game.GetNode<UiCanvas>("UiCanvas");
        canvas.CloseAllMenus();
        canvas.BindInventory(_inventory, _equipment);
        canvas.OpenMenu(UiWindowId.InventoryMenu);
    }

    public bool Verify(GamePlayManager game)
    {
        try
        {
            var canvas = game.GetNode<UiCanvas>("UiCanvas");
            var menu = canvas.GetNode<HBoxContainer>("Root/MenuLayer/MenuHost/WindowScroll/WindowRow")
                .GetChild<InventoryMenu>(0);
            var grid = menu.GetNode<InventoryContainer>("%InventoryContainer")
                .GetNode<ItemGrid>("%ItemGrid").GetNode<GridContainer>("%SlotGrid");
            foreach (var icon in grid.GetChildren().Cast<ClickableIcon>())
            {
                var entity = ((InventoryCellBinding)icon.Binding!).Entity;
                var label = icon.GetNode<Label>("QuantityLabel");
                Require(label.MouseFilter == Control.MouseFilterEnum.Ignore && label.FocusMode == Control.FocusModeEnum.None,
                    "Quantity overlay must not intercept input.");
                Require(label.Size.X <= icon.Size.X && label.Size.Y <= icon.Size.Y,
                    "Quantity overlay must fit the icon.");
                if (entity == null) continue;
                Require(icon.IconTexture != null && (icon.TooltipText.Contains("Fixture") ||
                    icon.TooltipText.Contains("Missing icon")), "Occupied cells must have visuals and identity.");
                Require(!label.Visible || label.GetMinimumSize().X <= label.Size.X, "Quantity text must fit.");
            }
            var resource = grid.GetChildren().Cast<ClickableIcon>().First(icon => icon.TooltipText.Contains("2147483647"));
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
        canvas.BindInventory(game.InventoryImpl, game.EquipmentImpl, game.ItemTransfers);
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
