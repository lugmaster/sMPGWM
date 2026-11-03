using System;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShipDefinitionLoader
{
    public static ShipDefinitionsDocument Load()
    {
        var token = FileLoader.LoadYaml(FilePaths.UserShipDataYaml,
            FilePaths.ShipDataYaml, FilePaths.ShipDataYamlSchema);
        var result = DefinitionDeserializer.Deserialize<ShipDefinitionsDocument>(token,
            FilePaths.UserShipDataYaml, FilePaths.ShipDataYamlSchema);
        if (result.IsSuccess) return result.Value!;
        FileLoader.AbortLoad(result.Error!);
        throw new InvalidOperationException(result.Error!.ToDisplayString());
    }
}
