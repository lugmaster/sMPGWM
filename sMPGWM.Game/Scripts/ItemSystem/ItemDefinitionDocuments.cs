using System;
using Newtonsoft.Json.Linq;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemDefinitionDocuments
{
    private static readonly string[] Files =
    [
        "items/engine.yml",
        "items/hull.yml",
        "items/shield_generator.yml",
        "items/reactor.yml",
        "items/light_weapon.yml",
        "items/medium_weapon.yml",
        "items/heavy_weapon.yml",
        "items/consumable.yml",
        "items/resource.yml",
        "items/battery.yml",
        "items/weapon_fixture.yml",
    ];

    public static JObject Load(Func<string, JObject> load)
    {
        JObject? combined = null;
        foreach (var file in Files)
        {
            var document = load(file);
            if ((string?)document["Version"] != "3.0.0")
                throw new InvalidOperationException($"{file}: item format 3.0.0 required; migrate weapon metadata and supply fields in all eleven item files together. Packaged light_weapon.yml provides Vulcan and Laser examples.");
            if (combined is null)
            {
                combined = document;
                continue;
            }
            if (!JToken.DeepEquals(combined["Version"], document["Version"]))
                throw new InvalidOperationException($"Item definition version mismatch in {file}.");
            foreach (var item in (JArray)document["Items"]!)
                ((JArray)combined["Items"]!).Add(item);
        }
        return combined!;
    }
}
