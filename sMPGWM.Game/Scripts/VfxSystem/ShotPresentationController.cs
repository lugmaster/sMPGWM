using System;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.VfxSystem;

public partial class ShotPresentationController : Node2D
{
    private const int QueueCapacity = 512;
    private const int DedupCapacity = 512;
    private readonly AcceptedShot[] _pending = new AcceptedShot[QueueCapacity];
    private readonly ItemEntityId[] _items = new ItemEntityId[DedupCapacity];
    private readonly ulong[] _sequences = new ulong[DedupCapacity];
    private readonly ulong[] _windows = new ulong[DedupCapacity];
    private readonly long[] _seenTicks = new long[DedupCapacity];
    private readonly Effect[] _effects = new Effect[1152];
    private readonly AudioStreamPlayer2D[] _voices = new AudioStreamPlayer2D[24];
    private readonly long[] _voiceTicks = new long[24];
    private WeaponPresentationProfile[] _profiles = null!;
    private GameWorld _world = null!;
    private int _pendingCount;
    private int _activeCount;
    private long _cutoff = -1;
    private double _presentationTick;
    public long DroppedEffects { get; private set; }
    public int HighWater { get; private set; }
    public int ActiveEffects => _activeCount;

    public void Initialize(GameWorld world)
    {
        _world = world;
        _profiles = [new("smg_standard"), new("laser_standard")];
        var index = 0;
        // Budgets cap the 5,400 shots/second ceiling; exhausted cosmetics never suppress gameplay.
        for (var profile = 0; profile < _profiles.Length; profile++)
        {
            for (var i = 0; i < 128; i++) AddEffect(index++, _profiles[profile].Muzzle.Instantiate<Node2D>(), profile, 0);
            for (var i = 0; i < 256; i++) AddEffect(index++, _profiles[profile].Tracer.Instantiate<Node2D>(), profile, 1);
            for (var i = 0; i < 128; i++) AddEffect(index++, _profiles[profile].Impact.Instantiate<Node2D>(), profile, 2);
        }
        for (; index < _effects.Length; index++) AddEffect(index, new ShieldFlash(), -1, 3);
        for (var i = 0; i < _voices.Length; i++)
        {
            _voices[i] = new AudioStreamPlayer2D { MaxDistance = 1600, VolumeDb = -16, MaxPolyphony = 1 };
            AddChild(_voices[i]);
        }
    }

    private void AddEffect(int index, Node2D node, int profile, int kind)
    {
        node.Visible = false;
        node.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(node);
        _effects[index] = new Effect(node, node.GetNodeOrNull<AnimatedSprite2D>("Visual"), profile, kind);
    }

    public void Enqueue(AcceptedShot shot)
    {
        if (shot.Tick <= _cutoff || shot.Sequence == 0 || shot.ProfileId is not ("smg_standard" or "laser_standard")) return;
        if (shot.Tick < _presentationTick - 60 || shot.Tick > _presentationTick + 120) return;
        if (!Remember(shot)) return;
        if (_pendingCount == _pending.Length) { DroppedEffects++; return; }
        _pending[_pendingCount++] = shot;
    }

    private bool Remember(AcceptedShot shot)
    {
        var replacement = 0;
        for (var i = 0; i < _items.Length; i++)
        {
            if (_items[i] == shot.ItemId && _sequences[i] != 0)
            {
                if (shot.Sequence > _sequences[i])
                {
                    var delta = shot.Sequence - _sequences[i];
                    _windows[i] = delta >= 64 ? 1UL : (_windows[i] << (int)delta) | 1;
                    _sequences[i] = shot.Sequence;
                }
                else
                {
                    var delta = _sequences[i] - shot.Sequence;
                    if (delta >= 64 || (_windows[i] & (1UL << (int)delta)) != 0) return false;
                    _windows[i] |= 1UL << (int)delta;
                }
                _seenTicks[i] = Math.Max(_seenTicks[i], shot.Tick);
                return true;
            }
            if (_sequences[i] == 0) replacement = i;
            else if (_sequences[replacement] != 0 && _seenTicks[i] < _seenTicks[replacement]) replacement = i;
        }
        _items[replacement] = shot.ItemId;
        _sequences[replacement] = shot.Sequence;
        _windows[replacement] = 1;
        _seenTicks[replacement] = shot.Tick;
        return true;
    }

    public void Advance(double presentationTick)
    {
        _presentationTick = presentationTick;
        for (var i = 0; i < _pendingCount;)
        {
            var shot = _pending[i];
            var local = _world.LocalPlayer?.Participant?.ControlledShip?.EntityId == shot.EntityId;
            if ((!local && shot.Tick > presentationTick) || !_world.Entities.ContainsKey(shot.EntityId) && shot.Tick > presentationTick - 12)
            { i++; continue; }
            _pending[i] = _pending[--_pendingCount];
            if (shot.Tick <= _cutoff || shot.Tick < presentationTick - 30) continue;
            Play(shot, local ? presentationTick : shot.Tick);
        }
        for (var i = 0; i < _effects.Length; i++)
        {
            var effect = _effects[i];
            if (!effect.Active) continue;
            var age = (float)((presentationTick - effect.StartTick) / 60);
            if (age >= effect.Duration)
            {
                effect.Active = false;
                effect.Node.Visible = false;
                effect.Ship = null;
                _activeCount--;
                continue;
            }
            age = Math.Max(0, age);
            if (effect.Kind == 0 && GodotObject.IsInstanceValid(effect.Ship))
            {
                effect.Node.GlobalPosition = effect.Ship!.Presentation.Position + effect.Offset.Rotated(effect.Ship.Presentation.Rotation);
                effect.Node.GlobalRotation = effect.Ship.Presentation.Rotation;
            }
            else if (effect.Kind == 1)
                effect.Node.GlobalPosition = effect.Origin.Lerp(effect.End, Math.Clamp(age / effect.Duration, 0, 1));
            if (effect.Animation != null)
            {
                var animation = effect.Animation.Animation;
                var frames = effect.Animation.SpriteFrames;
                effect.Animation.Frame = Math.Min(frames.GetFrameCount(animation) - 1, (int)(age * frames.GetAnimationSpeed(animation)));
            }
            effect.Node.Modulate = new Color(1, 1, 1, 1 - age / effect.Duration);
        }
    }

    private void Play(AcceptedShot shot, double startTick)
    {
        var profile = shot.ProfileId == "smg_standard" ? 0 : 1;
        _world.Entities.TryGetValue(shot.EntityId, out var ship);
        var direction = shot.End - shot.Origin;
        var angle = direction.Angle();
        var muzzle = Acquire(profile, 0, startTick, .05f, shot.Origin, angle);
        if (muzzle != null && ship != null)
        {
            muzzle.Ship = ship;
            muzzle.Offset = shot.MuzzleIndex < ship.Data.MuzzleOffsets.Count ? ship.Data.MuzzleOffsets[shot.MuzzleIndex] : Vector2.Zero;
        }
        var tracer = Acquire(profile, 1, startTick, .08f, shot.Origin, angle);
        if (tracer != null) { tracer.Origin = shot.Origin; tracer.End = shot.End; }
        if ((shot.Flags & (ShotResultFlags.Hull | ShotResultFlags.World | ShotResultFlags.Wreck)) != 0)
            Acquire(profile, 2, startTick, _profiles[profile].ImpactDuration, shot.End, shot.Normal.Angle());
        if ((shot.Flags & ShotResultFlags.Shield) != 0) Acquire(-1, 3, startTick, .15f, shot.End, shot.Normal.Angle());
        if (_presentationTick - startTick > 6) return;
        for (var i = 0; i < _voices.Length; i++)
        {
            if (_voices[i].Playing && _voiceTicks[i] > startTick - 15) continue;
            _voices[i].Stop();
            _voices[i].Stream = _profiles[profile].Audio;
            _voices[i].GlobalPosition = shot.Origin;
            _voices[i].Play();
            _voiceTicks[i] = (long)startTick;
            return;
        }
    }

    private Effect? Acquire(int profile, int kind, double tick, float duration, Vector2 position, float angle)
    {
        if (_presentationTick - tick >= duration * 60) return null;
        for (var i = 0; i < _effects.Length; i++)
        {
            var effect = _effects[i];
            if (effect.Active || effect.Profile != profile || effect.Kind != kind) continue;
            effect.Active = true;
            effect.StartTick = tick;
            effect.Duration = duration;
            effect.Ship = null;
            effect.Node.Transform = Transform2D.Identity;
            effect.Node.GlobalPosition = position;
            effect.Node.GlobalRotation = angle;
            effect.Node.Modulate = Colors.White;
            effect.Node.Visible = true;
            if (effect.Animation != null) { effect.Animation.Stop(); effect.Animation.Frame = 0; }
            _activeCount++;
            HighWater = Math.Max(HighWater, _activeCount);
            return effect;
        }
        DroppedEffects++;
        return null;
    }

    public void Clear(long cutoffTick)
    {
        _cutoff = cutoffTick;
        _presentationTick = cutoffTick;
        _pendingCount = 0;
        _activeCount = 0;
        Array.Clear(_sequences);
        Array.Clear(_windows);
        foreach (var effect in _effects)
            if (effect != null) { effect.Active = false; effect.Node.Visible = false; effect.Ship = null; }
        foreach (var voice in _voices) voice?.Stop();
    }

    internal sealed class Effect(Node2D node, AnimatedSprite2D? animation, int profile, int kind)
    {
        public readonly Node2D Node = node;
        public readonly AnimatedSprite2D? Animation = animation;
        public readonly int Profile = profile;
        public readonly int Kind = kind;
        public bool Active;
        public double StartTick;
        public float Duration;
        public Vector2 Origin;
        public Vector2 End;
        public Vector2 Offset;
        public ShipController? Ship;
    }
}
