using sMPGWM.Scripts.UiSystem.MessageStackSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

internal static class GameOverlayProvider
{
    private const string GameOverlayMenuPath = "res://scenes/ui/menu/game_overlay_menu/";
    private const string MainMenuPath = GameOverlayMenuPath + "main_menu.tscn";
    private const string InventoryMenuPath = GameOverlayMenuPath + "inventory_menu.tscn";
    private const string MapMenuPath = GameOverlayMenuPath + "map_menu.tscn";
    private const string MessageChildrenPath = GameOverlayMenuPath + "message_menu/";
    public static GameOverlayMenu CreateMainMenu() => SceneLoader.LoadAndInstantiate<GameOverlayMenu>(MainMenuPath);
    public static GameOverlayMenu CreateInventoryMenu() => SceneLoader.LoadAndInstantiate<GameOverlayMenu>(InventoryMenuPath);
    public static GameOverlayMenu CreateMapMenu() => SceneLoader.LoadAndInstantiate<GameOverlayMenu>(MapMenuPath);
    public static MessageMenu CreateMessageMenu() => SceneLoader.LoadAndInstantiate<MessageMenu>("res://scenes/ui/player_hud/message_menu.tscn");
    public static MessageEntry CreateMessageEntry() => SceneLoader.LoadAndInstantiate<MessageEntry>(MessageChildrenPath + "message_entry.tscn");
    public static SenderGroup CreateSenderGroup() => SceneLoader.LoadAndInstantiate<SenderGroup>(MessageChildrenPath + "sender_group.tscn");
    public static MessageListItem CreateMessageListItem() => SceneLoader.LoadAndInstantiate<MessageListItem>(MessageChildrenPath + "message_list_item.tscn");
}
