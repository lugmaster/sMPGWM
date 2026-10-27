#nullable enable

using System;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem.MessageStackSystem;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.Util.UiComponents;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Scripts.UiSystem;

public partial class UiCanvas : CanvasLayer
{
    private MessageStore? _messages;
    private MessageMenu? _messageMenu;
    private MessageViewState _messageViewState = new();
    private string? _requiredMessageId;
    internal event Action? MessageAcknowledged;

    internal void BindMessages(MessageStore store)
    {
        if (_requiredMessageId != null)
        {
            if (ReferenceEquals(_messages, store)) return;
            throw new InvalidOperationException("Cannot replace messages while acknowledgement is required.");
        }
        UnbindMessages();
        _messages = store;
        _messageViewState = new MessageViewState();
        store.Changed += RefreshMessages;
        _messageMenu?.Bind(store, _messageViewState);
        RefreshMessages(store.Snapshot);
    }

    internal void UnbindMessages()
    {
        if (_requiredMessageId != null)
            throw new InvalidOperationException("Cannot unbind messages while acknowledgement is required.");
        if (_messages != null) _messages.Changed -= RefreshMessages;
        _messages = null;
        _messageMenu?.SetRequired(null);
        _messageMenu?.Unbind();
        _messageViewState = new MessageViewState();
        _playerHud.SetUnreadMessages(0);
    }

    internal void EndMessageSession()
    {
        _requiredMessageId = null;
        _windowManager.SetRequiredMessage(false);
        UnbindMessages();
    }

    private void RefreshMessages(MessageSnapshot snapshot) => _playerHud.SetUnreadMessages(snapshot.UnreadCount);
    private void CancelItemInteraction() => _inventoryMenu?.CancelInteraction();
    private void ActivateMenu(UiWindowId id) => _windowManager.Activate(id);
    private void SelectFirstUnread() => _messageMenu?.SelectFirstUnread();
    private void AcknowledgeMessage() => MessageAcknowledged?.Invoke();

    internal void PresentRequiredMessage(PlayerMessage? message)
    {
        _requiredMessageId = message?.Id;
        if (message != null) SetInventoryEditing(false);
        _windowManager.SetRequiredMessage(message != null);
        _messageMenu?.SetRequired(message?.Id);
        if (message == null) _windowManager.Close(UiWindowId.MessageMenu);
    }

    private PlayerHud _playerHud = null!;
    private UiWindowManager _windowManager = null!;
    private ItemPointerOverlay _itemPointerOverlay = null!;
    private InventoryImpl? _inventory;
    private EquipmentImpl? _equipment;
    private InventoryEquipmentTransferService? _transfers;
    private InventoryViewState _inventoryViewState = new();
    private InventoryMenu? _inventoryMenu;
    private bool _hasApplicationFocus = true;
    private ItemHotbarState? _hotbar;
    private ItemHotbarAdapter? _hotbarAdapter;

    internal bool HasBlockingWindow => _windowManager.HasBlockingWindow;
    internal event Action<bool>? BlockingChanged;
    internal event Action<int>? OnRequestHotbarAction;
    internal event Action<bool>? InventoryEditAvailabilityChanged;

    public override void _EnterTree()
    {
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _windowManager = GetNode<UiWindowManager>("%MenuHost");
        _itemPointerOverlay = GetNode<ItemPointerOverlay>("%ItemPointerOverlay");
    }

    public override void _Ready()
    {
        _playerHud.MenuRequested += ActivateMenu;
        _windowManager.MessagesActivated += SelectFirstUnread;
        _windowManager.MenuActivating += CancelItemInteraction;
        _playerHud.HotbarSlotPressed += RequestHotbarAction;
        _windowManager.BlockingChanged += OnBlockingChanged;
        _windowManager.WindowOpening += OnWindowOpening;
        _windowManager.WindowClosed += OnWindowClosed;
    }

    public override void _ExitTree()
    {
        EndMessageSession();
        UnbindInventory();
        _playerHud.MenuRequested -= ActivateMenu;
        _windowManager.MessagesActivated -= SelectFirstUnread;
        _windowManager.MenuActivating -= CancelItemInteraction;
        _playerHud.HotbarSlotPressed -= RequestHotbarAction;
        _windowManager.BlockingChanged -= OnBlockingChanged;
        _windowManager.WindowOpening -= OnWindowOpening;
        _windowManager.WindowClosed -= OnWindowClosed;
    }

    internal void BindInventory(InventoryImpl inventory, EquipmentImpl equipment,
        InventoryEquipmentTransferService? transfers = null)
    {
        UnbindInventory();
        _inventory = inventory;
        _equipment = equipment;
        _transfers = transfers;
        _inventoryViewState = new InventoryViewState();
        _inventoryMenu?.Bind(inventory, equipment, _inventoryViewState);
        InventoryEditAvailabilityChanged?.Invoke(_inventoryMenu != null && _hasApplicationFocus);
    }

    internal void UnbindInventory()
    {
        InventoryEditAvailabilityChanged?.Invoke(false);
        _inventoryMenu?.Unbind();
        if (_inventory != null) _inventory.Changed -= RefreshHotbarInventory;
        if (_equipment != null) _equipment.Changed -= RefreshHotbarEquipment;
        _inventory = null;
        _equipment = null;
        _transfers = null;
        _playerHud.Hotbar.Unbind();
        _hotbar = null;
        _hotbarAdapter = null;
    }

    internal void SetInventoryEditing(bool available)
    {
        _transfers?.RevokeEditSession();
        var session = available && _transfers != null ? _transfers.OpenEditSession() : Guid.Empty;
        _hotbar?.SetEditSession(session);
        _playerHud.Hotbar.SetEditing(session != Guid.Empty);
        _inventoryMenu?.SetInteraction(_transfers, session);
        _inventoryMenu?.BindHotbar(_hotbar, _hotbarAdapter, _playerHud.Hotbar, session);
    }

    internal void BindHotbar(ItemHotbarState state, ItemHotbarAdapter adapter, ItemCatalog catalog, IReadOnlyList<IconDefinition> actions)
    {
        _hotbar = state;
        _hotbarAdapter = adapter;
        _playerHud.Hotbar.Bind(state, adapter, catalog, actions);
        _inventory!.Changed += RefreshHotbarInventory;
        _equipment!.Changed += RefreshHotbarEquipment;
    }

    private void RefreshHotbarInventory(InventorySnapshot snapshot) => _playerHud.Hotbar.Refresh();
    private void RefreshHotbarEquipment(EquipmentSnapshot snapshot) => _playerHud.Hotbar.Refresh();

    private void OnWindowOpening(UiWindowId id, GameOverlayMenu window)
    {
        CancelItemInteraction();
        if (id == UiWindowId.MessageMenu)
        {
            _messageMenu = (MessageMenu)window;
            _messageMenu.Bind(_messages, _messageViewState);
            _messageMenu.SetRequired(_requiredMessageId);
            _messageMenu.SelectFirstUnread();
            _messageMenu.Acknowledged += AcknowledgeMessage;
            return;
        }
        if (id != UiWindowId.InventoryMenu) return;
        _inventoryMenu = (InventoryMenu)window;
        _inventoryMenu.SetPointerOverlay(_itemPointerOverlay);
        if (_inventory != null && _equipment != null)
        {
            _inventoryMenu.Bind(_inventory, _equipment, _inventoryViewState);
            InventoryEditAvailabilityChanged?.Invoke(_hasApplicationFocus);
        }
    }

    private void OnWindowClosed(UiWindowId id)
    {
        if (id == UiWindowId.MessageMenu)
        {
            if (_messageMenu != null)
            {
                _messageMenu.Acknowledged -= AcknowledgeMessage;
                _messageMenu.Unbind();
            }
            _messageMenu = null;
            return;
        }
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
            _inventoryMenu?.DismissTooltips();
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

    internal void SetNavigationEnabled(bool enabled)
    {
        _windowManager.SetNavigationEnabled(enabled);
        _playerHud.SetNavigationEnabled(enabled);
    }

    internal void SetGameplayInputEnabled(bool enabled) => _playerHud.SetGameplayInputEnabled(enabled);

    private void RequestHotbarAction(int slotIndex) => OnRequestHotbarAction?.Invoke(slotIndex);

    internal bool IsPointerOverInteractiveUi()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && IsAncestorOf(hovered);
    }

    private void OnBlockingChanged(bool blocking) => BlockingChanged?.Invoke(blocking);
}
