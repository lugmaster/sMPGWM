using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Tests;

internal static class GameplayTestData
{
    private static readonly object DocumentLock = new();
    private static readonly Dictionary<(string File, string Schema), JObject> Documents = new();

    internal static GameplayCatalogues Load(bool variants = false, Action<string, JObject>? edit = null)
    {
        return GameplayCatalogueLoader.Load(source =>
        {
            var document = Read(source.FileName, source.SchemaName);
            if (!variants && edit == null) return document;
            var originalDocument = document.DeepClone();
            if (variants)
            {
                if (source.RootKey == "AbilityDefinitions")
                {
                    var rows = (JArray)document[source.RootKey]!;
                    var original = Find(rows, "shieldCapacityBoost");
                    var variant = (JObject)original.DeepClone();
                    variant["Id"] = "shieldVariant"; variant["EffectId"] = "shieldVariant";
                    rows.Add(variant);
                    var alias = (JObject)original.DeepClone();
                    alias["Id"] = "shieldAlias";
                    rows.Add(alias);
                }
                if (source.RootKey == "EffectDefinitions" || source.RootKey == "BehaviourDefinitions")
                {
                    var rows = (JArray)document[source.RootKey]!;
                    var variant = (JObject)Find(rows, "shieldCapacityBoost").DeepClone();
                    variant["Id"] = "shieldVariant";
                    if (source.RootKey == "EffectDefinitions") variant["BehaviourId"] = "shieldVariant";
                    else
                    {
                        variant["Lifetime"]!["DurationMs"] = 10000;
                        variant["Modifiers"]![0]!["Value"] = 25;
                    }
                    rows.Add(variant);
                }
            }
            edit?.Invoke(source.FileName, document);
            if (JToken.DeepEquals(document, originalDocument)) return document;
            var schema = File.ReadAllText(Path.Combine(DirectoryPath, source.SchemaName));
            var result = YamlParser.ValidateAndReturn(document.ToString(), schema, source.FileName, source.SchemaName);
            if (!result.IsSuccess) throw new ArgumentException(result.Error!.ToDisplayString());
            return (JObject)result.Value!;
        });
    }
    internal static string DirectoryPath => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(GameplayTestData).Assembly.Location)!,
        "../../../../sMPGWM.Game/assets/game_data"));
    internal static JObject Read(string file, string schema)
    {
        lock (DocumentLock)
        {
            var key = (file, schema);
            if (!Documents.TryGetValue(key, out var document))
            {
                if (file == "ItemDefinitions.yml")
                    document = sMPGWM.Scripts.ItemSystem.ItemDefinitionDocuments.Load(part => Read(part, part.Replace(".yml", ".schema.yml")));
                else
                {
                    var result = YamlParser.ValidateAndReturn(File.ReadAllText(Path.Combine(DirectoryPath, file)),
                        File.ReadAllText(Path.Combine(DirectoryPath, schema)), file, schema);
                    if (!result.IsSuccess) throw new ArgumentException(result.Error!.ToDisplayString());
                    document = (JObject)result.Value!;
                }
                Documents.Add(key, document);
            }
            return (JObject)document.DeepClone();
        }
    }
    internal static JObject Find(JArray rows, string id)
    {
        foreach (JObject row in rows)
            if ((string?)row["Id"] == id) return row;
        throw new InvalidOperationException("Missing fixture definition: " + id);
    }
}
