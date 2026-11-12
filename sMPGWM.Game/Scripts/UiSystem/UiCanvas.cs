using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.MessageStackSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using GameOverlayMenu = sMPGWM.Scripts.UtilitySystem.UiComponents.GameOverlayMenu;

namespace sMPGWM.Scripts.UiSystem;

public partial class UiCanvas : CanvasLayer
{
    internal Func<bool>? CancelGameplayAction { get => _windowManager.CancelGameplayAction; set => _windowManager.CancelGameplayAction = value; }
    private Label? _gameplayFeedback;
    private double _feedbackSeconds;

    internal void ShowGameplayFeedback(string text)
    {
        if (_gameplayFeedback == null)
        {
            _gameplayFeedback = new Label { MouseFilter = Control.MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center };
            GetNode<Control>("Root").AddChild(_gameplayFeedback);
            _gameplayFeedback.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
            _gameplayFeedback.OffsetLeft = -350;
            _gameplayFeedback.OffsetRight = 350;
            _gameplayFeedback.OffsetTop = -160;
            _gameplayFeedback.OffsetBottom = -120;
            _gameplayFeedback.Size = new Vector2(700, 40);
        }
        _gameplayFeedback.Text = text;
        _feedbackSeconds = 4;
    }

    public override void _Process(double delta)
    {
        if (_feedbackSeconds <= 0) return;
        _feedbackSeconds -= delta;
        if (_feedbackSeconds <= 0 && _gameplayFeedback != null) _gameplayFeedback.Text = "";
    }
    private MessageController? _messages;
    private MessageMenu? _messageMenu;
    private MessageViewState _messageViewState = new();
    private string? _requiredMessageId;
    internal event Action? MessageAcknowledged;

    internal void BindMessages(MessageController store)
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
        if (message != null) InventoryEditAvailabilityChanged?.Invoke(false);
        _windowManager.SetRequiredMessage(message != null);
        _messageMenu?.SetRequired(message?.Id);
        if (message == null) _windowManager.Close(UiWindowId.MessageMenu);
    }

    private PlayerHud _playerHud = null!;
    private UiWindowManager _windowManager = null!;
    private ItemPointerOverlay _itemPointerOverlay = null!;
    private IInventoryView? _inventory;
    private IEquipmentView? _equipment;
    private IItemTransfers? _transfers;
    private InventoryViewState _inventoryViewState = new();
    private InventoryMenu? _inventoryMenu;
    private bool _hasApplicationFocus = true;
    private ItemHotbarController? _hotbar;

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

    internal void BindInventory(IInventoryView inventory, IEquipmentView equipment,
        IItemTransfers? transfers = null)
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
        _inventory = null;
        _equipment = null;
        _transfers = null;
        _playerHud.Hotbar.Unbind();
        _hotbar = null;
    }

    internal void SetInventoryEditSession(ulong session)
    {
        _hotbar?.SetEditSession(session);
        _playerHud.Hotbar.SetEditing(session != 0UL);
        _inventoryMenu?.SetInteraction(_transfers, session);
        _inventoryMenu?.BindHotbar(_hotbar, _playerHud.Hotbar);
    }

    internal void BindGameplay(NetworkSystem.NetworkManager? network)
    {
        if (network == null && _gameplayFeedback != null) _gameplayFeedback.Text = "";
        var panels = FindChildren("*", "", true, false);
        foreach (var panel in panels)
            if (panel is HudSystem.PlayerStatsPanel stats) stats.Bind(network);
    }

    internal void BindHotbar(ItemHotbarController controller, IReadOnlyList<IconDefinition> actions)
    {
        _hotbar = controller;
        _playerHud.Hotbar.Bind(controller, actions);
        InventoryEditAvailabilityChanged?.Invoke(_inventoryMenu != null && _hasApplicationFocus);
    }

    internal void BindHotbar(ItemHotbarController controller, IReadOnlyDictionary<string, IconDefinition> actions)
    {
        _hotbar = controller;
        _playerHud.Hotbar.Bind(controller, actions);
        InventoryEditAvailabilityChanged?.Invoke(_inventoryMenu != null && _hasApplicationFocus);
    }


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
