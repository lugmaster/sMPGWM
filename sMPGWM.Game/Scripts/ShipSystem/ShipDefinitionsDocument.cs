using System.Collections.Generic;
using Newtonsoft.Json;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipDefinitionsDocument
{
    [JsonProperty("ships", Required = Required.Always)]
    public List<ShipDefinition> Ships { get; set; } = new();
}

public sealed class ShipDefinition
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = "";
    [JsonProperty("texture_path", Required = Required.Always)]
    public string TexturePath { get; set; } = "";
    [JsonProperty("collider_points", Required = Required.Always)]
    public List<double[]> ColliderPoints { get; set; } = new();
    [JsonProperty("ship_stats", Required = Required.Always)]
    public ShipStatsDefinition Stats { get; set; } = new();
    [JsonProperty("weapon_slots", Required = Required.Always)]
    public ShipWeaponSlotsDefinition WeaponSlots { get; set; } = new();
}

[JsonObject(ItemRequired = Required.Always)]
public sealed class ShipStatsDefinition
{
    public double Speed { get; set; }
    public double AfterBurnerSpeed { get; set; }
    public double RotationSpeed { get; set; }
    public double Acceleration { get; set; }
    public double AfterBurnerAcceleration { get; set; }
    public double ShipMass { get; set; }
}

public sealed class ShipWeaponSlotsDefinition
{
    [JsonProperty("light", Required = Required.Always)]
    public int Light { get; set; }
    [JsonProperty("heavy", Required = Required.Always)]
    public int Heavy { get; set; }
}
