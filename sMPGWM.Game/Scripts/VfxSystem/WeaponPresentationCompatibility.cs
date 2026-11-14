using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Godot;

namespace sMPGWM.Scripts.VfxSystem;

internal static class WeaponPresentationCompatibility
{
    internal static void Capture(Dictionary<string, string> inputs)
    {
        CaptureResource(ShieldVisual.ShaderPath, inputs);
        for (var i = 0; i < WeaponPresentationProfile.Count; i++)
        {
            var id = WeaponPresentationProfile.GetId(i);
            _ = new WeaponPresentationProfile(id);
            CaptureResource($"res://scenes/vfx/muzzle_flashes/muzzle_{id}/muzzle_{id}.tscn", inputs);
            CaptureResource($"res://scenes/vfx/tracers/tracer_{id}/tracer_{id}.tscn", inputs);
            CaptureResource($"res://scenes/vfx/impacts/metal_impact_{id}/metal_impact_{id}.tscn", inputs);
            CaptureResource($"res://assets/audio/sound_effects/shooting_{id}/shooting_{id}.wav", inputs);
        }
    }

    private static void CaptureResource(string path, Dictionary<string, string> inputs)
    {
        if (inputs.ContainsKey(path)) return;
        var physicalPath = path;
        if (!FileAccess.FileExists(physicalPath))
        {
            var import = new ConfigFile();
            var loaded = import.Load(path + ".import") == Error.Ok || import.Load(path + ".remap") == Error.Ok;
            if (!loaded || !import.HasSectionKey("remap", "path"))
                throw new InvalidOperationException("Cannot capture weapon presentation resource: " + path);
            physicalPath = import.GetValue("remap", "path").AsString();
        }
        var bytes = FileAccess.GetFileAsBytes(physicalPath);
        if (bytes.Length == 0) throw new InvalidOperationException("Empty weapon presentation resource: " + physicalPath);
        inputs.Add(path, Convert.ToHexString(SHA256.HashData(bytes)));
        foreach (var dependency in ResourceLoader.GetDependencies(path))
        {
            var separator = dependency.LastIndexOf("::", StringComparison.Ordinal);
            var dependencyPath = separator >= 0 ? dependency[(separator + 2)..] : dependency;
            if (dependencyPath.StartsWith("res://", StringComparison.Ordinal)) CaptureResource(dependencyPath, inputs);
        }
    }
}
