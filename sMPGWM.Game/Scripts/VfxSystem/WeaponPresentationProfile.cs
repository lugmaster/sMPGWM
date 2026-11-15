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
    internal double[] MuzzleFrameEnds { get; }
    internal double[] ImpactFrameEnds { get; }
    public double MuzzleDuration => MuzzleFrameEnds[^1];
    public double ImpactDuration => ImpactFrameEnds[^1];

    public WeaponPresentationProfile(string id)
    {
        if (WeaponPresentationProfiles.IndexOf(id) < 0) throw new ArgumentException("Unknown weapon presentation profile: " + id);
        Id = id;
        Muzzle = Load<PackedScene>($"res://scenes/vfx/muzzle_flashes/muzzle_{id}/muzzle_{id}.tscn");
        Tracer = Load<PackedScene>($"res://scenes/vfx/tracers/tracer_{id}/tracer_{id}.tscn");
        Impact = Load<PackedScene>($"res://scenes/vfx/impacts/metal_impact_{id}/metal_impact_{id}.tscn");
        Audio = Load<AudioStream>($"res://assets/audio/sound_effects/shooting_{id}/shooting_{id}.wav");
        MuzzleFrameEnds = ReadFrameEnds(Muzzle);
        ImpactFrameEnds = ReadFrameEnds(Impact);
    }

    private static double[] ReadFrameEnds(PackedScene scene)
    {
        var node = scene.Instantiate<Node2D>();
        try
        {
            var sprite = node.GetNode<AnimatedSprite2D>("Visual");
            var frames = sprite.SpriteFrames;
            var animation = sprite.Animation;
            var count = frames.GetFrameCount(animation);
            var speed = frames.GetAnimationSpeed(animation);
            if (count == 0 || !double.IsFinite(speed) || speed <= 0) throw new InvalidOperationException("Invalid weapon animation.");
            var ends = new double[count];
            var duration = 0d;
            for (var i = 0; i < count; i++)
            {
                var frame = frames.GetFrameDuration(animation, i) / speed;
                if (!double.IsFinite(frame) || frame <= 0) throw new InvalidOperationException("Invalid weapon animation frame.");
                ends[i] = duration += frame;
            }
            return ends;
        }
        finally { node.Free(); }
    }

    private static T Load<T>(string path) where T : Resource => ResourceLoader.Load<T>(path) ??
        throw new InvalidOperationException("Missing weapon presentation resource: " + path);
}
