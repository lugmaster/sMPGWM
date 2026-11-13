using System;
using Godot;

namespace sMPGWM.Scripts.VfxSystem;

public sealed class WeaponPresentationProfile
{
    public string Id { get; }
    public PackedScene Muzzle { get; }
    public PackedScene Tracer { get; }
    public PackedScene Impact { get; }
    public AudioStream Audio { get; }
    public float ImpactDuration { get; }

    public WeaponPresentationProfile(string id)
    {
        if (id is not ("smg_standard" or "laser_standard")) throw new ArgumentException("Unknown weapon presentation profile: " + id);
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
