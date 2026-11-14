using System;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ShipSystem;

internal static class ShipCatalogueLoader
{
    internal static readonly CatalogueSource Source = new("Ships.yml", "ShipsSchema.yml", "ships",
        IdentityField: "name", UserDirectory: "ships/");

    internal static ShipDefinitions Load(Func<CatalogueSource, JObject> read, Func<string, bool> textureAvailable) =>
        new(CatalogueParser.Load(new Catalogue<ShipData>(Source,
            row => ShipDefinitionParser.Parse(row, textureAvailable)), read));
}
