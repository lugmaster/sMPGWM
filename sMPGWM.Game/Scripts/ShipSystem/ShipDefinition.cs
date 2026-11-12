using System.Collections.Generic;
using Newtonsoft.Json;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipDefinition
{
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = "";
    [JsonProperty("texture_path", Required = Required.Always)]
    public string TexturePath { get; set; } = "";
    [JsonProperty("collider_points", Required = Required.Always)]
    public List<double[]> ColliderPoints { get; set; } = new();
    [JsonProperty("mass", Required = Required.Always)]
    public double Mass { get; set; }
    [JsonProperty("health", Required = Required.Always)]
    public double Health { get; set; }
    [JsonProperty("stat_modifiers", Required = Required.Always)]
    public ShipStatModifiersDefinition Stats { get; set; } = new();
    [JsonProperty("weapon_slots", Required = Required.Always)]
    public ShipWeaponSlotsDefinition WeaponSlots { get; set; } = new();
}
