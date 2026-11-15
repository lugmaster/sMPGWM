using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayDefinitions
{
    private sealed record PreparedData(GameplayCatalogues Catalogues, string Checksum,
        IReadOnlyDictionary<string, string> Inputs);

    private static PreparedData? Data;
    public static GameplayCatalogues Current => Data?.Catalogues ??
        throw new InvalidOperationException("Definitions have not been initialized. Complete definition startup before accessing them.");
    internal static string Checksum => Data?.Checksum ?? string.Empty;

    public static void Initialize()
    {
        if (Data != null) return;
        if (string.IsNullOrEmpty(GameConfig.ConfigDataChecksum))
            throw new InvalidOperationException("Configuration is not loaded.");
        var inputs = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var result = GameplayCatalogueLoader.TryLoad(source => FileLoader.LoadYaml(
                source.UserPath, source.DefaultPath, source.SchemaPath, inputs), Icons.Get,
                path => ResourceLoader.Load<Texture2D>(path) is { } texture &&
                    texture.GetWidth() > 0 && texture.GetHeight() > 0);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error!.ToDisplayString());
            var catalogues = result.Value!;
            foreach (var definition in catalogues.Items.All)
                WeaponSystem.WeaponDefinitionCompiler.Prepare(definition);
            inputs.Add("GameplayProtocol", "6:complete-initial-state-readiness");
            var checksum = ChecksumGenerator.Compute(inputs);
            foreach (var diagnostic in catalogues.SupportDiagnostics) Logger.Warning(diagnostic);
            foreach (var input in inputs) Logger.Info($"Compatibility file={input.Key} hash={ChecksumGenerator.Hash(input.Value)}");
            Logger.Info($"Compatibility development={GameConfig.Development} config={GameConfig.ConfigDataChecksum} data={checksum}");
            Data = new PreparedData(catalogues, checksum, new ReadOnlyDictionary<string, string>(inputs));
        }
        catch (ArgumentException error)
        {
            throw new InvalidOperationException(new DataLoadError("Invalid gameplay definitions", error.Message,
                Exception: error).ToDisplayString(), error);
        }
    }
}
