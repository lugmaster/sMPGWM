#nullable enable

using System;
using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventorySlotSmokeFixture : RefCounted
{
    private ItemCatalog? _loadedCatalog;
    private ulong _gameId;

    public bool VerifyLoadedItems(GamePlayManager game, bool seeded)
    {
        try
        {
            Require(game.Items.Definitions.Count == 16, "Startup must load all packaged item definitions even without seeding.");
            Require(ReferenceEquals(game.Items, game.InventoryImpl.Catalog), "Inventory must reference the game catalog.");
            Require(ReferenceEquals(game.ItemCategories, game.Items.Categories), "Items must reference the loaded categories.");
            if (_loadedCatalog != null)
                Require(ReferenceEquals(_loadedCatalog, game.Items) == (_gameId == game.GetInstanceId()),
                    "Menu reopen must reuse the catalog; another game must get an independent catalog.");
            _loadedCatalog = game.Items;
            _gameId = game.GetInstanceId();
            Require(game.InventoryImpl.Snapshot.Occupied.Count == (seeded ? 34 : 0),
                "Mock seeding must remain optional.");
            if (seeded)
            {
                foreach (var definition in game.Items.Definitions)
                {
                    var counts = game.InventoryImpl.Snapshot.Occupied
                        .Where(entry => entry.Entity.DefinitionId == definition.Id)
                        .Select(entry => entry.Entity.Count).ToArray();
                    var expected = game.ItemCategories.Get(definition.CategoryId).CanStack
                        ? definition.Id.Value.StartsWith("mock_standard_", StringComparison.Ordinal)
                            ? new[] { 10, 10, 7 } : new[] { 1000, 250 }
                        : new[] { 1, 1 };
                    Require(counts.SequenceEqual(expected), $"Unexpected mock stacks for {definition.Id}.");
                }
            }
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"Loaded item fixture failed: {exception}");
            return false;
        }
    }

    public bool Run(GamePlayManager game)
    {
        var canvas = game.GetNode<UiCanvas>("UiCanvas");
        var engine = new ItemDefinitionId("smoke_engine");
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(engine, "Smoke Engine", ItemCategories.Engine)
        });
        using var inventory = new InventoryImpl(catalog);
        using var equipment = new EquipmentImpl(catalog);
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
        var canEdit = false;
        Action<bool> trackEditing = available => canEdit = available;
        canvas.InventoryEditAvailabilityChanged += trackEditing;
        try
        {
            canvas.CloseAllMenus();
            canvas.BindInventory(inventory, equipment);
            canvas.OpenMenu(UiWindowId.InventoryMenu);
            Require(canEdit, "Opening inventoryImpl must authorize editing.");
            canvas.Notification((int)Node.NotificationApplicationFocusOut);
            Require(!canEdit, "Focus loss must revoke inventoryImpl editing.");
            canvas.Notification((int)Node.NotificationApplicationFocusIn);
            Require(canEdit, "Focus recovery must authorize a fresh inventoryImpl edit session.");
            var row = canvas.GetNode<HBoxContainer>("Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow");
            var menu = row.GetChild<InventoryMenu>(0);
            var grid = menu.GetNode<InventoryContainer>("%InventoryContainer")
                .GetNode<ItemGrid>("%ItemGrid").GetNode<GridContainer>("%SlotGrid");
            var firstCell = grid.GetChild<ClickableIcon>(0);
            Require(inventory.Add(engine, 129) == InventoryResult.Success, "Fixture addition failed.");
            Require(grid.GetChildCount() == 192 && grid.GetChild(0) == firstCell,
                "Live growth must append cells and reuse existing controls.");
            var source = inventory.Snapshot.Slots[2]!;
            var request = new TransferRequest(transfers.OpenEditSession(), ItemLocation.Inventory(2), source.Id,
                ItemLocation.Equipment("engine"), null, inventory.Revision, equipment.Revision);
            Require(transfers.Transfer(request) == TransferResult.Success, "Fixture equip failed.");
            var binding = (ItemCellBinding)grid.GetChild<ClickableIcon>(2).Binding!;
            var projected = InventoryProjection.Create(inventory.Snapshot, catalog, new InventoryViewState());
            Require(binding == projected[2],
                "Compacted cells must retain their real address and entity expectation.");
            var emptyBinding = (ItemCellBinding)grid.GetChild<ClickableIcon>(128).Binding!;
            Require(emptyBinding.Address == 2 && emptyBinding.Entity == null,
                "The vacated address must become a genuine trailing empty destination.");
            var equipmentCell = menu.GetNode<EquipmentContainer>("%EquipmentContainer").GetChild<IconWithLabel>(0).Cell;
            Require(equipmentCell.Binding is ItemCellBinding { SlotKey: "engine" } equipped &&
                    equipped.Entity?.Id == source.Id, "Equipment must refresh its binding after commit.");
            canvas.OpenMenu(UiWindowId.MapMenu);
            canvas.CloseMenu(UiWindowId.InventoryMenu);
            Require(!canEdit, "Map remaining open must not retain inventoryImpl edit permission.");
            var detachedBinding = firstCell.Binding;
            Require(inventory.Add(engine, 1) == InventoryResult.Success, "Closed-view addition failed.");
            Require(ReferenceEquals(firstCell.Binding, detachedBinding),
                "A closed view must detach its state subscriptions before deletion.");
            canvas.OpenMenu(UiWindowId.InventoryMenu);
            Require(canEdit, "Reopening inventoryImpl must authorize fresh editing.");
            var reopened = row.GetChild<InventoryMenu>(1);
            var reopenedGrid = reopened.GetNode<InventoryContainer>("%InventoryContainer")
                .GetNode<ItemGrid>("%ItemGrid").GetNode<GridContainer>("%SlotGrid");
            Require(reopenedGrid.GetChildCount() == 192, "Reopen must retain expanded capacity.");
            Require(((ItemCellBinding)reopenedGrid.GetChild<ClickableIcon>(128).Binding!).Entity != null,
                "Reopen must immediately render additions made while closed.");
            canvas.BindInventory(inventory, equipment);
            Require(reopenedGrid.GetChildCount() == 192, "Rebinding a ready view must not duplicate cells.");
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"Inventory slot fixture failed: {exception}");
            return false;
        }
        finally
        {
            canvas.InventoryEditAvailabilityChanged -= trackEditing;
            canvas.CloseAllMenus();
            canvas.BindInventory(game.InventoryImpl, game.EquipmentImpl, game.ItemTransfers);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
