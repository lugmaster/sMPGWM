using Newtonsoft.Json;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShieldGeometryDefinition
{
    [JsonProperty("shape", Required = Required.Always)]
    public string Shape { get; set; } = "";
    [JsonProperty("center", Required = Required.Always)]
    public double[] Center { get; set; } = [];
    [JsonProperty("radius", Required = Required.DisallowNull)]
    public double? Radius { get; set; }
    [JsonProperty("radii", Required = Required.DisallowNull)]
    public double[]? Radii { get; set; }
    [JsonProperty("rotation_degrees", Required = Required.DisallowNull)]
    public double? RotationDegrees { get; set; }
}
