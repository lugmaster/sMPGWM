using Newtonsoft.Json;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipWeaponSlotsDefinition
{
    [JsonProperty("light", Required = Required.Always)]
    public int Light { get; set; }
    [JsonProperty("heavy", Required = Required.Always)]
    public int Heavy { get; set; }
}
