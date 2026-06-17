using System;
using Godot;
using sMPGWM.Scripts.Ui.MainMenu.Base;

namespace sMPGWM.Scripts.Provider;

public static class InGameScreenSceneProvider
{
    private const string MainMenuPath = "res://scenes/ui/screens/in_game/main_menu.tscn";
    private const string InventoryMenuPath = "res://scenes/ui/screens/in_game/inventory_menu.tscn";
    private const string MapMenuPath = "res://scenes/ui/screens/in_game/map_menu.tscn";

    public static InGameMenuScreen CreateMainMenu()
    {
        return LoadAndInstantiate(MainMenuPath);
    }

    public static InGameMenuScreen CreateInventoryMenu()
    {
        return LoadAndInstantiate(InventoryMenuPath);
    }

    public static InGameMenuScreen CreateMapMenu()
    {
        return LoadAndInstantiate(MapMenuPath);
    }

    private static InGameMenuScreen LoadAndInstantiate(string path)
    {
        var scene = GD.Load<PackedScene>(path);

        if (scene == null)
            throw new InvalidOperationException($"Could not load in-game menu scene: {path}");

        var node = (InGameMenuScreen)scene.Instantiate();

        return node ?? throw new InvalidOperationException("Could not instantiate in-game menu scene");
    }
}