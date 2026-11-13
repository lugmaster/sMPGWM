using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShipDefinitionLoader
{
    public static LoadResult<ShipDefinitionsDocument> Load(JObject yaml, string sourcePath, string schemaPath)
    {
        if ((string?)yaml["Version"] != "3.0.0")
            return LoadResult<ShipDefinitionsDocument>.Failure(new DataLoadError("Ship format migration required",
                "Version 3.0.0 requires two authored muzzle_offsets, for example [[1,-15],[1,15]] for fighter; preserve your ship geometry and add your own offsets.", sourcePath, schemaPath));
        if (yaml["ships"] is JArray ships)
            foreach (var ship in ships)
                if (ship["muzzle_offsets"] == null)
                    return LoadResult<ShipDefinitionsDocument>.Failure(new DataLoadError("Missing ship muzzle geometry",
                        $"Ship '{ship["name"]}': add muzzle_offsets with two finite [x,y] pairs (fighter example: [[1,-15],[1,15]]).", sourcePath, schemaPath));
        return DefinitionDeserializer.Deserialize<ShipDefinitionsDocument>(yaml, sourcePath, schemaPath);
    }
}
