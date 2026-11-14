using System;
using Godot;

namespace sMPGWM.Scripts.VfxSystem;

public sealed class WeaponPresentationProfile
{
    public const int Count = 2;

    public static bool TryGetIndex(string id, out int index)
    {
        index = id switch { "smg_standard" => 0, "laser_standard" => 1, _ => -1 };
        return index >= 0;
    }

    public static string GetId(int index) => index switch
    {
        0 => "smg_standard",
        1 => "laser_standard",
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public static WeaponPresentationProfile[] CreateAll()
    {
        var profiles = new WeaponPresentationProfile[Count];
        for (var i = 0; i < profiles.Length; i++) profiles[i] = new WeaponPresentationProfile(GetId(i));
        return profiles;
    }

    public string Id { get; }
    public PackedScene Muzzle { get; }
    public PackedScene Tracer { get; }
    public PackedScene Impact { get; }
    public AudioStream Audio { get; }
    public float ImpactDuration { get; }
    public int MuzzleCapacity { get; }
    public int TracerCapacity { get; }
    public int ImpactCapacity { get; }

    public WeaponPresentationProfile(string id)
        : this(id, 128, 256, 128) { }

    public WeaponPresentationProfile(string id, int muzzleCapacity, int tracerCapacity, int impactCapacity)
    {
        if (!TryGetIndex(id, out _)) throw new ArgumentException("Unknown weapon presentation profile: " + id);
        if (muzzleCapacity is < 0 or > 1024 || tracerCapacity is < 0 or > 1024 || impactCapacity is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(muzzleCapacity), "Presentation capacities must be between zero and 1024.");
        MuzzleCapacity = muzzleCapacity;
        TracerCapacity = tracerCapacity;
        ImpactCapacity = impactCapacity;
        Id = id;
        Muzzle = Load<PackedScene>($"res://scenes/vfx/muzzle_flashes/muzzle_{id}/muzzle_{id}.tscn");
        Tracer = Load<PackedScene>($"res://scenes/vfx/tracers/tracer_{id}/tracer_{id}.tscn");
        Impact = Load<PackedScene>($"res://scenes/vfx/impacts/metal_impact_{id}/metal_impact_{id}.tscn");
        Audio = Load<AudioStream>($"res://assets/audio/sound_effects/shooting_{id}/shooting_{id}.wav");
        ImpactDuration = id == "laser_standard" ? .425f : .3f;
    }

    private static T Load<T>(string path) where T : Resource => ResourceLoader.Load<T>(path) ??
        throw new InvalidOperationException("Missing weapon presentation resource: " + path);
}
