

using System;
using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using ClickableIcon = sMPGWM.Scripts.UtilitySystem.UiComponents.ClickableIcon;
using NotificationIcon = sMPGWM.Scripts.UtilitySystem.UiComponents.NotificationIcon;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class MenuToolbarControl : PanelContainer
{
    internal event Action<UiWindowId>? MenuRequested;
    private NotificationIcon _messages = null!;
    private bool _navigationEnabled;
    private int _unreadCount;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Configure("InventoryButton", UiWindowId.InventoryMenu, "Inventory", Icons.Travel.BackpackBagSchool);
        Configure("MapButton", UiWindowId.MapMenu, "Map", Icons.MapMarkers.TravelMapFolded);
        Configure("MainMenuButton", UiWindowId.MainMenu, "Main Menu", Icons.Controller.ButtonsMenuOptionsSettings);
        _messages = GetNode<NotificationIcon>("ButtonContainer/NotificationContainer/NotificationIcon");
        _messages.Pressed += RequestMessages;
        PresentNotification(NotificationKind.UnreadMessages, 0);
        SetNavigationEnabled(_navigationEnabled);
        MinimumSizeChanged += ApplyLayout;
        sMPGWM.Scripts.InputSystem.KeyBindingSettings.BindingsChanged += RefreshTooltips;
        ApplyLayout();
    }

    private void RefreshTooltips()
    {
        RefreshTooltip("InventoryButton", UiWindowId.InventoryMenu, "Inventory");
        RefreshTooltip("MapButton", UiWindowId.MapMenu, "Map");
        RefreshTooltip("MainMenuButton", UiWindowId.MainMenu, "Main Menu");
        PresentNotification(NotificationKind.UnreadMessages, _unreadCount);
    }

    private void RefreshTooltip(string node, UiWindowId id, string title)
    {
        var button = GetNode<ClickableIcon>("ButtonContainer/MenuButtonContainer/" + node);
        button.SetDisplay(button.IconTexture, new IconDisplayData(Tooltip(id, title)));
    }

    private void ApplyLayout()
    {
        var minimum = GetCombinedMinimumSize();
        OffsetLeft = -minimum.X - 16;
        OffsetRight = -16;
        OffsetTop = 16;
        OffsetBottom = 16 + minimum.Y;
    }

    private void Configure(string node, UiWindowId id, string title, IconDefinition icon)
    {
        var button = GetNode<ClickableIcon>("ButtonContainer/MenuButtonContainer/" + node);
        button.IconSize = IconSize.Medium;
        button.SetDisplay(icon.IconTexture, new IconDisplayData(Tooltip(id, title)));
        button.Pressed += () => { if (_navigationEnabled) MenuRequested?.Invoke(id); };
    }

    private static string Tooltip(UiWindowId id, string title)
    {
        var events = InputMap.ActionGetEvents(id.ToString());
        return events.Count == 0 ? title : $"{title} ({events[0].AsText()})";
    }

    internal void PresentNotification(NotificationKind kind, int count)
    {
        if (kind != NotificationKind.UnreadMessages) throw new ArgumentOutOfRangeException(nameof(kind));
        _unreadCount = count;
        var tooltip = count == 0 ? "Messages, no unread messages" : $"Messages, {count} unread messages";
        _messages.Present(Icons.Software.EmailMessageUnreadClosed.IconTexture, Tooltip(UiWindowId.MessageMenu, tooltip), count);
    }

    internal void SetNavigationEnabled(bool enabled)
    {
        _navigationEnabled = enabled;
        if (!IsNodeReady()) return;
        foreach (var child in GetNode("ButtonContainer/MenuButtonContainer").GetChildren()) ((Button)child).Disabled = !enabled;
        _messages.Disabled = !enabled;
    }

    private void RequestMessages() { if (_navigationEnabled) MenuRequested?.Invoke(UiWindowId.MessageMenu); }

    public override void _ExitTree()
    {
        sMPGWM.Scripts.InputSystem.KeyBindingSettings.BindingsChanged -= RefreshTooltips;
        MinimumSizeChanged -= ApplyLayout;
        _messages.Pressed -= RequestMessages;
    }
}
