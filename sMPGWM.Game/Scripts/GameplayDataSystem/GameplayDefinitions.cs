using System;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayDefinitions
{
    private static readonly Lazy<GameplayCatalogues> Data = new(Load);
    public static GameplayCatalogues Current => Data.Value;

    private static GameplayCatalogues Load()
    {
        try
        {
            var catalogues = GameplayCatalogueLoader.Load(source => FileLoader.LoadYaml(
                "user://config/gameplay/" + source.FileName, "res://assets/game_data/" + source.FileName,
                "res://assets/game_data/" + source.SchemaName));
            GameplayCatalogueValidator.ValidateItems(catalogues, ItemRegistry.GetAll());
            foreach (var diagnostic in catalogues.Abilities.GetSupportDiagnostics(ItemRegistry.GetAll())) Logger.Warning(diagnostic);
            return catalogues;
        }
        catch (ArgumentException error)
        {
            FileLoader.AbortLoad(new DataLoadError("Invalid gameplay definitions", error.Message, Exception: error));
            throw;
        }
    }
}
