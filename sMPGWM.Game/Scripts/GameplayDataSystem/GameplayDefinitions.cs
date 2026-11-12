using System;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayDefinitions
{
    private static Lazy<GameplayCatalogues> Data = new(Load);
    public static GameplayCatalogues Current => Data.Value;
    internal static void ResetPreparation() => Data = new(Load);

    private static GameplayCatalogues Load()
    {
        try
        {
            var result = GameplayCatalogueLoader.TryLoad(source => FileLoader.LoadYaml(
                "user://config/gameplay/" + source.FileName, "res://assets/game_data/" + source.FileName,
                "res://assets/game_data/" + source.SchemaName));
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error!.ToDisplayString());
            var catalogues = result.Value!;
            GameplayCatalogueValidator.ValidateItems(catalogues, ItemRegistry.GetAll());
            foreach (var diagnostic in catalogues.Abilities.GetSupportDiagnostics(ItemRegistry.GetAll())) Logger.Warning(diagnostic);
            return catalogues;
        }
        catch (ArgumentException error)
        {
            throw new InvalidOperationException(new DataLoadError("Invalid gameplay definitions", error.Message,
                Exception: error).ToDisplayString(), error);
        }
    }
}
