using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using GameOverlayMenu = sMPGWM.Scripts.UtilitySystem.UiComponents.GameOverlayMenu;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

public partial class UiWindowManager : Control
{
    private static readonly Dictionary<UiWindowId, StringName> MenuActions =
        Enum.GetValues<UiWindowId>().ToDictionary(action => action, action => new StringName(action.ToString()));
    private readonly HashSet<UiWindowId> _pressedMenuActions = new();
    private readonly UiWindowRegistry _registry = new();
    private readonly Dictionary<UiWindowId, GameOverlayMenu> _windows = new();
    private readonly Dictionary<UiWindowId, Action> _closeHandlers = new();
    private ScrollContainer _scrollContainer = null!;
    private HFlowContainer _windowRow = null!;
    private bool _navigationEnabled;
    private Control _modalBlocker = null!;

    private bool _requiredMessage;
    internal event Action? MessagesActivated;
    internal event Action? MenuActivating;

    private bool SettingsOpen => _windows.TryGetValue(UiWindowId.MainMenu, out var view) && view is SettingsOverlayMenu;
    private void ClearPressedActions() => _pressedMenuActions.Clear();

    internal void Activate(UiWindowId id)
    {
        if (!_navigationEnabled) return;
        if (_requiredMessage)
        {
            if (id == UiWindowId.MessageMenu) Focus(id);
            return;
        }
        if (SettingsOpen)
        {
            if (id == UiWindowId.MainMenu) ReplaceMainView(false);
            return;
        }
        MenuActivating?.Invoke();
        if (id == UiWindowId.MessageMenu)
        {
            Open(id);
            MessagesActivated?.Invoke();
        }
        else Toggle(id);
    }

    internal void SetRequiredMessage(bool required)
    {
        if (required && !_requiredMessage) CloseAll();
        _requiredMessage = required;
        if (required) Open(UiWindowId.MessageMenu);
        _modalBlocker.Visible = required;
    }

    internal bool HasBlockingWindow => _registry.HasBlockingWindow;
    internal event Action<UiWindowId, GameOverlayMenu>? WindowOpening;
    internal event Action<UiWindowId>? WindowClosed;

    internal event Action<bool>? BlockingChanged
    {
        add => _registry.BlockingChanged += value;
        remove => _registry.BlockingChanged -= value;
    }

    public override void _Ready()
    {
        InputSystem.KeyBindingSettings.BindingsChanged += ClearPressedActions;
        MouseFilter = MouseFilterEnum.Ignore;
        _modalBlocker = GetNode<Control>("../../ModalBlocker");
        _scrollContainer = GetNode<ScrollContainer>("WindowScroll");
        _windowRow = GetNode<HFlowContainer>("WindowScroll/WindowRow");
        _windowRow.SortChildren += QueueEnsureFocusedWindowVisible;
        _windowRow.MinimumSizeChanged += RefreshScrollExtent;
        _scrollContainer.Resized += QueueEnsureFocusedWindowVisible;
    }

    public override void _ExitTree()
    {
        InputSystem.KeyBindingSettings.BindingsChanged -= ClearPressedActions;
        _windowRow.SortChildren -= QueueEnsureFocusedWindowVisible;
        _windowRow.MinimumSizeChanged -= RefreshScrollExtent;
        _scrollContainer.Resized -= QueueEnsureFocusedWindowVisible;
        _pressedMenuActions.Clear();
        _requiredMessage = false;
        CloseAll();
    }

    internal void SetNavigationEnabled(bool enabled)
    {
        _navigationEnabled = enabled;
        if (!enabled)
        {
            _pressedMenuActions.Clear();
            _requiredMessage = false;
            CloseAll();
        }
    }

    internal void Open(UiWindowId id)
    {
        if (!_navigationEnabled) return;
        if (_requiredMessage && id != UiWindowId.MessageMenu) return;
        if (SettingsOpen && id != UiWindowId.MainMenu) return;
        if (id != UiWindowId.MainMenu) Close(UiWindowId.MainMenu);
        if (_registry.IsOpen(id))
        {
            Focus(id);
            return;
        }

        if (id == UiWindowId.MainMenu) CloseAll();

        var window = CreateWindow(id);
        WindowOpening?.Invoke(id, window);
        if (window is MainMenu main) main.SettingsRequested += ShowSettings;
        var closeHandler = () => Close(id);
        window.OnMenuClosed += closeHandler;
        _closeHandlers.Add(id, closeHandler);
        _windows.Add(id, window);
        _scrollContainer.Show();
        _windowRow.AddChild(window);
        _registry.Open(id);
        _modalBlocker.Visible = _requiredMessage;
        window.ReleaseExternalFocus();
        QueueEnsureFocusedWindowVisible();
    }

    internal void Close(UiWindowId id)
    {
        if (_requiredMessage) return;
        if (!_windows.Remove(id, out var window)) return;
        if (window is MainMenu main) main.SettingsRequested -= ShowSettings;
        window.OnMenuClosed -= _closeHandlers[id];
        _closeHandlers.Remove(id);
        window.Hide();
        _windowRow.RemoveChild(window);
        window.QueueFree();
        _registry.Close(id);
        _modalBlocker.Visible = _requiredMessage;
        WindowClosed?.Invoke(id);
        _scrollContainer.Visible = _windows.Count > 0;
        FocusTopmost();
    }

    internal void Toggle(UiWindowId id)
    {
        if (_requiredMessage) return;
        if (_registry.IsOpen(id)) Close(id);
        else Open(id);
    }

    internal void CloseAll()
    {
        if (_requiredMessage) return;
        foreach (var entry in _windows)
        {
            var window = entry.Value;
            if (window is MainMenu main) main.SettingsRequested -= ShowSettings;
            window.OnMenuClosed -= _closeHandlers[entry.Key];
            window.Hide();
            _windowRow.RemoveChild(window);
            window.QueueFree();
            WindowClosed?.Invoke(entry.Key);
        }

        _windows.Clear();
        _closeHandlers.Clear();
        _registry.CloseAll();
        _modalBlocker.Hide();
        _scrollContainer.Hide();
    }

    private void ShowSettings() => ReplaceMainView(true);

    private void ReplaceMainView(bool settings)
    {
        if (!_navigationEnabled || _requiredMessage || !_windows.TryGetValue(UiWindowId.MainMenu, out var old)) return;
        old.OnMenuClosed -= _closeHandlers[UiWindowId.MainMenu];
        if (old is MainMenu main) main.SettingsRequested -= ShowSettings;
        old.Hide();
        _windowRow.RemoveChild(old);
        old.QueueFree();
        var window = settings ? GameOverlayProvider.CreateSettingsMenu() : GameOverlayProvider.CreateMainMenu();
        Action close = settings ? () => ReplaceMainView(false) : () => Close(UiWindowId.MainMenu);
        if (window is MainMenu next) next.SettingsRequested += ShowSettings;
        window.OnMenuClosed += close;
        _closeHandlers[UiWindowId.MainMenu] = close;
        _windows[UiWindowId.MainMenu] = window;
        _pressedMenuActions.Clear();
        _windowRow.AddChild(window);
        Focus(UiWindowId.MainMenu);
    }

    internal Func<bool>? CancelGameplayAction { get; set; }

    private InputRestriction InputRestrictions =>
        (_requiredMessage ? InputRestriction.RequiredAcknowledgement : InputRestriction.None) |
        (SettingsOpen && ((SettingsOverlayMenu)_windows[UiWindowId.MainMenu]).Bindings.IsCapturingInput
            ? InputRestriction.BindingCapture : InputRestriction.None) |
        (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit ? InputRestriction.TextEntry : InputRestriction.None);

    public override void _Input(InputEvent @event)
    {
        var restrictions = InputRestrictions;
        if ((restrictions & InputRestriction.BindingCapture) != 0) return;
        foreach (var (action, actionName) in MenuActions)
            if (@event.IsActionReleased(actionName)) _pressedMenuActions.Remove(action);

        // Escape must run before focused buttons or text fields can consume ui_cancel.
        if (_navigationEnabled)
            foreach (var (action, actionName) in MenuActions)
                if (action == UiWindowId.MainMenu && @event.IsActionPressed(actionName, true))
                {
                    if (!@event.IsEcho() && CancelGameplayAction?.Invoke() == true)
                    {
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    _UnhandledInput(@event);
                    return;
                }

        if (!_navigationEnabled || @event is not InputEventMouseButton { Pressed: true } mouse) return;
        if (mouse.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return;
        var focused = GetViewport().GuiGetFocusOwner();
        if (focused != null && !focused.GetGlobalRect().HasPoint(mouse.Position))
            focused.ReleaseFocus();
        if (!_scrollContainer.GetGlobalRect().HasPoint(mouse.Position)) return;
        var scrollbar = _scrollContainer.GetVScrollBar();
        if (scrollbar.Visible && scrollbar.GetGlobalRect().HasPoint(mouse.Position)) return;

        var order = _registry.FocusOrder;
        for (var index = order.Count - 1; index >= 0; index--)
        {
            var id = order[index];
            if (!_windows[id].GetGlobalRect().HasPoint(mouse.Position)) continue;
            Focus(id);
            break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_navigationEnabled) return;

        var restrictions = InputRestrictions;
        foreach (var (action, actionName) in MenuActions)
        {
            if (!@event.IsActionPressed(actionName, true)) continue;
            if (!PlayerInputPolicy.AllowsMenuShortcut(restrictions, action == UiWindowId.MainMenu))
            {
                if ((restrictions & InputRestriction.RequiredAcknowledgement) != 0) GetViewport().SetInputAsHandled();
                return;
            }
            GetViewport().SetInputAsHandled();
            if (@event.IsEcho() || !_pressedMenuActions.Add(action)) return;
            Activate(action);

            return;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
            _pressedMenuActions.Clear();
    }

    private void Focus(UiWindowId id)
    {
        _registry.Focus(id);
        _windows[id].ReleaseExternalFocus();
        QueueEnsureFocusedWindowVisible();
    }

    private void FocusTopmost()
    {
        if (_registry.Topmost is { } id)
        {
            Focus(id);
        }
    }

    private void QueueEnsureFocusedWindowVisible() => CallDeferred(nameof(EnsureFocusedWindowVisible));

    private void RefreshScrollExtent()
    {
        // Refresh the wrapped content height before updating the vertical scroll extent.
        _scrollContainer.GetMinimumSize();
        _scrollContainer.QueueSort();
        QueueEnsureFocusedWindowVisible();
    }

    private void EnsureFocusedWindowVisible()
    {
        if (_registry.Topmost is { } id)
            _scrollContainer.EnsureControlVisible(_windows[id]);
    }

    private static GameOverlayMenu CreateWindow(UiWindowId id) => id switch
    {
        UiWindowId.InventoryMenu => GameOverlayProvider.CreateInventoryMenu(),
        UiWindowId.MapMenu => GameOverlayProvider.CreateMapMenu(),
        UiWindowId.MessageMenu => GameOverlayProvider.CreateMessageMenu(),
        UiWindowId.MainMenu => GameOverlayProvider.CreateMainMenu(),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };
}
