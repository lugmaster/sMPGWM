using System.Collections.Generic;
using Newtonsoft.Json;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipDefinitionsDocument
{
    [JsonProperty("Version", Required = Required.Always)]
    public string Version { get; set; } = "";
    [JsonProperty("ships", Required = Required.Always)]
    public List<ShipDefinition> Ships { get; set; } = new();
}
