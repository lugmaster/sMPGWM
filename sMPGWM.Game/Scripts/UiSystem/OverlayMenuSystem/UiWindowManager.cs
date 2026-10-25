using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

public partial class UiWindowManager : Control
{
    private static readonly string[] MenuActions = ["MainMenu", "InventoryMenu", "MapMenu"];
    private readonly HashSet<string> _pressedMenuActions = new();
    private readonly UiWindowRegistry _registry = new();
    private readonly Dictionary<UiWindowId, GameOverlayMenu> _windows = new();
    private readonly Dictionary<UiWindowId, Action> _closeHandlers = new();
    private ScrollContainer _scrollContainer = null!;
    private HBoxContainer _windowRow = null!;
    private bool _navigationEnabled;

    public bool HasBlockingWindow => _registry.HasBlockingWindow;

    public event Action<bool>? BlockingChanged
    {
        add => _registry.BlockingChanged += value;
        remove => _registry.BlockingChanged -= value;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _scrollContainer = GetNode<ScrollContainer>("WindowScroll");
        _windowRow = GetNode<HBoxContainer>("WindowScroll/WindowRow");
        _windowRow.SortChildren += QueueEnsureFocusedWindowVisible;
        _scrollContainer.Resized += QueueEnsureFocusedWindowVisible;
    }

    public override void _ExitTree()
    {
        _windowRow.SortChildren -= QueueEnsureFocusedWindowVisible;
        _scrollContainer.Resized -= QueueEnsureFocusedWindowVisible;
        _pressedMenuActions.Clear();
        CloseAll();
    }

    public void SetNavigationEnabled(bool enabled)
    {
        _navigationEnabled = enabled;
        if (!enabled)
        {
            _pressedMenuActions.Clear();
            CloseAll();
        }
    }

    public void Open(UiWindowId id)
    {
        if (!_navigationEnabled) return;
        if (_registry.IsOpen(id))
        {
            Focus(id);
            return;
        }

        var window = CreateWindow(id);
        Action closeHandler = () => Close(id);
        window.OnMenuClosed += closeHandler;
        _closeHandlers.Add(id, closeHandler);
        _windows.Add(id, window);
        _scrollContainer.Show();
        _windowRow.AddChild(window);
        _registry.Open(id);
        window.GrabWindowFocus();
        QueueEnsureFocusedWindowVisible();
    }

    public void Close(UiWindowId id)
    {
        if (!_windows.Remove(id, out var window)) return;
        window.OnMenuClosed -= _closeHandlers[id];
        _closeHandlers.Remove(id);
        window.Hide();
        _windowRow.RemoveChild(window);
        window.QueueFree();
        _registry.Close(id);
        _scrollContainer.Visible = _windows.Count > 0;
        FocusTopmost();
    }

    public void Toggle(UiWindowId id)
    {
        if (_registry.IsOpen(id)) Close(id);
        else Open(id);
    }

    public void CloseTopmost()
    {
        if (_registry.Topmost is { } id) Close(id);
    }

    public void CloseAll()
    {
        foreach (var entry in _windows)
        {
            var window = entry.Value;
            window.OnMenuClosed -= _closeHandlers[entry.Key];
            window.Hide();
            _windowRow.RemoveChild(window);
            window.QueueFree();
        }

        _windows.Clear();
        _closeHandlers.Clear();
        _registry.CloseAll();
        _scrollContainer.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        foreach (var action in MenuActions)
            if (@event.IsActionReleased(action)) _pressedMenuActions.Remove(action);

        if (!_navigationEnabled || @event is not InputEventMouseButton { Pressed: true } mouse) return;
        if (!_scrollContainer.GetGlobalRect().HasPoint(mouse.Position)) return;
        var scrollbar = _scrollContainer.GetHScrollBar();
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

        foreach (var action in MenuActions)
        {
            if (!@event.IsActionPressed(action, true)) continue;
            GetViewport().SetInputAsHandled();
            if (@event.IsEcho() || !_pressedMenuActions.Add(action)) return;

            switch (action)
            {
                case "MainMenu":
                    if (_registry.Topmost != null) CloseAll();
                    else Open(UiWindowId.MainMenu);
                    break;
                case "InventoryMenu":
                    Toggle(UiWindowId.Inventory);
                    break;
                case "MapMenu":
                    Toggle(UiWindowId.Map);
                    break;
            }

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
        _windows[id].GrabWindowFocus();
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

    private void EnsureFocusedWindowVisible()
    {
        if (_registry.Topmost is { } id)
            _scrollContainer.EnsureControlVisible(_windows[id]);
    }

    private static GameOverlayMenu CreateWindow(UiWindowId id) => id switch
    {
        UiWindowId.Inventory => GameOverlayProvider.CreateInventoryMenu(),
        UiWindowId.Map => GameOverlayProvider.CreateMapMenu(),
        UiWindowId.MainMenu => GameOverlayProvider.CreateMainMenu(),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };
}
