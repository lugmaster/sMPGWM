#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.Util.UiComponents;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Scripts.UiSystem;

public partial class UiCanvas : CanvasLayer
{
    private PlayerHud _playerHud = null!;
    private UiWindowManager _windowManager = null!;
    private InventoryImpl? _inventory;
    private EquipmentImpl? _equipment;
    private InventoryViewState _inventoryViewState = new();
    private InventoryMenu? _inventoryMenu;
    private bool _hasApplicationFocus = true;

    internal bool HasBlockingWindow => _windowManager.HasBlockingWindow;
    internal event Action<bool>? BlockingChanged;
    internal event Action<int>? OnRequestHotbarAction;
    internal event Action<bool>? InventoryEditAvailabilityChanged;

    public override void _EnterTree()
    {
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _windowManager = GetNode<UiWindowManager>("%MenuHost");
    }

    public override void _Ready()
    {
        _playerHud.HotbarSlotPressed += RequestHotbarAction;
        _windowManager.BlockingChanged += OnBlockingChanged;
        _windowManager.WindowOpening += OnWindowOpening;
        _windowManager.WindowClosed += OnWindowClosed;
    }

    public override void _ExitTree()
    {
        UnbindInventory();
        _playerHud.HotbarSlotPressed -= RequestHotbarAction;
        _windowManager.BlockingChanged -= OnBlockingChanged;
        _windowManager.WindowOpening -= OnWindowOpening;
        _windowManager.WindowClosed -= OnWindowClosed;
    }

    internal void BindInventory(InventoryImpl inventory, EquipmentImpl equipment)
    {
        UnbindInventory();
        _inventory = inventory;
        _equipment = equipment;
        _inventoryViewState = new InventoryViewState();
        _inventoryMenu?.Bind(inventory, equipment, _inventoryViewState);
        InventoryEditAvailabilityChanged?.Invoke(_inventoryMenu != null && _hasApplicationFocus);
    }

    internal void UnbindInventory()
    {
        InventoryEditAvailabilityChanged?.Invoke(false);
        _inventoryMenu?.Unbind();
        _inventory = null;
        _equipment = null;
    }

    private void OnWindowOpening(UiWindowId id, GameOverlayMenu window)
    {
        if (id != UiWindowId.InventoryMenu) return;
        _inventoryMenu = (InventoryMenu)window;
        if (_inventory != null && _equipment != null)
        {
            _inventoryMenu.Bind(_inventory, _equipment, _inventoryViewState);
            InventoryEditAvailabilityChanged?.Invoke(_hasApplicationFocus);
        }
    }

    private void OnWindowClosed(UiWindowId id)
    {
        if (id != UiWindowId.InventoryMenu) return;
        _inventoryMenu?.Unbind();
        _inventoryMenu = null;
        InventoryEditAvailabilityChanged?.Invoke(false);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
        {
            _hasApplicationFocus = false;
            InventoryEditAvailabilityChanged?.Invoke(false);
        }
        else if (what == NotificationApplicationFocusIn || what == NotificationWMWindowFocusIn)
        {
            if (_hasApplicationFocus) return;
            _hasApplicationFocus = true;
            InventoryEditAvailabilityChanged?.Invoke(_inventoryMenu != null && _inventory != null);
        }
    }

    public void OpenMenu(UiWindowId id) => _windowManager.Open(id);

    public void CloseMenu(UiWindowId id) => _windowManager.Close(id);

    public void ToggleMenu(UiWindowId id) => _windowManager.Toggle(id);

    public void CloseAllMenus() => _windowManager.CloseAll();

    internal void SetHotbarIcons(IReadOnlyList<IconDefinition> icons) => _playerHud.SetHotbarIcons(icons);

    internal void SetNavigationEnabled(bool enabled) => _windowManager.SetNavigationEnabled(enabled);

    internal void SetGameplayInputEnabled(bool enabled) => _playerHud.SetGameplayInputEnabled(enabled);

    private void RequestHotbarAction(int slotIndex) => OnRequestHotbarAction?.Invoke(slotIndex);

    internal bool IsPointerOverInteractiveUi()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && IsAncestorOf(hovered);
    }

    private void OnBlockingChanged(bool blocking) => BlockingChanged?.Invoke(blocking);
}
