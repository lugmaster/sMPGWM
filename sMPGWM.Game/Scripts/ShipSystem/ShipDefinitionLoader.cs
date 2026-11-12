using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShipDefinitionLoader
{
    public static LoadResult<ShipDefinitionsDocument> Load(JObject yaml, string sourcePath, string schemaPath) =>
        DefinitionDeserializer.Deserialize<ShipDefinitionsDocument>(yaml, sourcePath, schemaPath);
}
