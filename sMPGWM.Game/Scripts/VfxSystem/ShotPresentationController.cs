using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.VfxSystem;

public partial class ShotPresentationController : Node2D
{
    public const int QueueCapacity = 512;
    public const double TracerDuration = .08;
    public const double ShieldDuration = .15;
    private readonly NodePath _shipVisualPath = new("ShipVisuals");
    private readonly Pending[] _pending = new Pending[QueueCapacity];
    private readonly ShotSequenceWindow _dedup = new();
    private readonly Pool[] _pools = new Pool[7];
    private readonly AudioStreamPlayer2D[] _voices = new AudioStreamPlayer2D[24];
    private readonly double[] _voiceTicks = new double[24];
    private WeaponPresentationProfile[] _profiles = [];
    private int _pendingCount;
    private long _cutoff = -1;
    private Guid _session;
    private double _localTick;
    private double _remoteTick;
    private bool _initialized;
    public int PendingCount => _pendingCount;
    public int QueueHighWater { get; private set; }
    public long QueueDrops { get; private set; }
    public long AudioDrops { get; private set; }
    public long IdentityDrops => _dedup.CapacityDrops;
    public int ActiveEffects
    {
        get
        {
            var count = 0;
            if (_initialized) for (var i = 0; i < _pools.Length; i++) count += _pools[i].ActiveCount;
            return count;
        }
    }

    public EffectPoolMetrics MuzzleMetrics(int profile) => Metrics(profile, 0);
    public EffectPoolMetrics TracerMetrics(int profile) => Metrics(profile, 1);
    public EffectPoolMetrics ImpactMetrics(int profile) => Metrics(profile, 2);
    public EffectPoolMetrics ShieldMetrics => _initialized ? _pools[6].Metrics : default;
    private EffectPoolMetrics Metrics(int profile, int kind)
    {
        if (profile is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(profile));
        return _initialized ? _pools[profile * 3 + kind].Metrics : default;
    }

    public void Initialize()
    {
        if (_initialized) throw new InvalidOperationException("Weapon presentation is already initialized.");
        _profiles = [new("smg_standard"), new("laser_standard")];
        // ceil(90 weapons * profile rate * lifetime) + one synchronized 90-shot burst, rounded up.
        var capacities = new[] { 192, 224, 416, 128, 128, 224, 320 };
        for (var i = 0; i < _pools.Length; i++)
        {
            var profile = i / 3;
            var kind = i == 6 ? 3 : i % 3;
            var scene = kind switch { 0 => _profiles[profile].Muzzle, 1 => _profiles[profile].Tracer,
                2 => _profiles[profile].Impact, _ => null };
            var ends = kind switch { 0 => _profiles[profile].MuzzleFrameEnds, 2 => _profiles[profile].ImpactFrameEnds, _ => null };
            var duration = kind switch { 0 => _profiles[profile].MuzzleDuration, 1 => TracerDuration,
                2 => _profiles[profile].ImpactDuration, _ => ShieldDuration };
            var pool = _pools[i] = new(capacities[i], kind, duration, ends);
            for (var slot = 0; slot < pool.Effects.Length; slot++)
            {
                var node = scene == null ? new ShieldFlash() : scene.Instantiate<Node2D>();
                node.Name = "Effect" + i + "_" + slot;
                node.Visible = false;
                node.ProcessMode = ProcessModeEnum.Disabled;
                node.ZIndex = kind == 3 ? 11 : 10;
                AddChild(node);
                var sprite = node.GetNodeOrNull<AnimatedSprite2D>("Visual");
                sprite?.Stop();
                pool.Effects[slot] = new(node, sprite);
            }
        }
        for (var i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i] = new() { Name = "ShotVoice" + i, MaxDistance = 1600, VolumeDb = -16, MaxPolyphony = 1 };
            AddChild(voice);
        }
        _initialized = true;
    }

    public void Enqueue(AcceptedShot shot, ShipController? shooter = null, bool local = false)
    {
        if (!_initialized || shot.Session == Guid.Empty || shot.Tick <= _cutoff || shot.Sequence <= 0 ||
            shot.Item == 0 || shot.Profile is < 0 or > 1 || shot.Muzzle < 0 || !shot.Origin.IsFinite() ||
            !shot.Endpoint.IsFinite() || !shot.Normal.IsFinite() || shot.ShieldContact && !shot.Hit) return;
        if (_session == Guid.Empty) _session = shot.Session;
        if (shot.Session != _session) return;
        var now = local ? _localTick : _remoteTick;
        if (shot.Tick < now - 30 || shot.Tick > now + 120) return;
        if (!_dedup.Remember(shot.Item, shot.Sequence, shot.Tick, Math.Min(_localTick, _remoteTick))) return;
        if (_pendingCount == QueueCapacity) { QueueDrops++; return; }
        _pending[_pendingCount++] = new() { Shot = shot, Shooter = shooter, Local = local };
        QueueHighWater = Math.Max(QueueHighWater, _pendingCount);
    }

    public void Advance(double localTick, double remoteTick)
    {
        if (!_initialized || !double.IsFinite(localTick) || !double.IsFinite(remoteTick)) return;
        _localTick = Math.Max(_localTick, localTick);
        _remoteTick = Math.Max(_remoteTick, remoteTick);
        // Release expired slots before accepting this frame's simultaneous burst.
        for (var i = 0; i < _pools.Length; i++) UpdatePool(_pools[i]);
        for (var i = 0; i < _pendingCount;)
        {
            var pending = _pending[i];
            var now = pending.Local ? _localTick : _remoteTick;
            if (pending.Shot.Tick > now) { i++; continue; }
            _pending[i] = _pending[--_pendingCount];
            _pending[_pendingCount] = default;
            if (pending.Shot.Tick > _cutoff && pending.Shot.Tick >= now - 30) Play(pending, now);
        }
    }

    public void Advance(double presentationTick) => Advance(presentationTick, presentationTick);

    private void Play(Pending pending, double now)
    {
        var shot = pending.Shot;
        var direction = shot.Endpoint - shot.Origin;
        var angle = direction.Angle();
        var muzzle = Acquire(_pools[shot.Profile * 3], shot.Tick, now, pending.Local, shot.Origin, angle);
        if (muzzle != null && GodotObject.IsInstanceValid(pending.Shooter))
        {
            var shooter = pending.Shooter!;
            if (shot.Muzzle < shooter.Runtime.WeaponFixturePoints.Count)
            {
                muzzle.Owner = shooter.GetNodeOrNull<Node2D>(_shipVisualPath);
                muzzle.Offset = shooter.Runtime.WeaponFixturePoints[shot.Muzzle];
                UpdateEffect(_pools[shot.Profile * 3], muzzle, now);
            }
        }
        var tracerPool = _pools[shot.Profile * 3 + 1];
        var tracer = Acquire(tracerPool, shot.Tick, now, pending.Local, shot.Origin, angle);
        if (tracer != null)
        {
            tracer.Origin = shot.Origin;
            tracer.Endpoint = shot.Endpoint;
            UpdateEffect(tracerPool, tracer, now);
        }
        if (shot.Hit)
            Acquire(_pools[shot.Profile * 3 + 2], shot.Tick, now, pending.Local, shot.Endpoint, shot.Normal.Angle());
        if (shot.ShieldContact)
            PlayShieldContact(shot.Tick, now, pending.Local, shot.Endpoint, shot.Normal);
        if (now - shot.Tick > 6) return;
        for (var i = 0; i < _voices.Length; i++)
        {
            if (_voices[i].Playing && _voiceTicks[i] > _localTick - 15) continue;
            _voices[i].Stop();
            _voices[i].Stream = _profiles[shot.Profile].Audio;
            _voices[i].GlobalPosition = shot.Origin;
            _voices[i].Play();
            _voiceTicks[i] = _localTick;
            return;
        }
        AudioDrops++;
    }

    private void PlayShieldContact(long tick, double now, bool local, Vector2 position, Vector2 normal) =>
        Acquire(_pools[6], tick, now, local, position, normal.Angle());

    private static Effect? Acquire(Pool pool, long startTick, double now, bool local, Vector2 position, float angle)
    {
        if (now - startTick + 1e-9 >= pool.Duration * 60) return null;
        if (pool.FreeCount == 0) { pool.Dropped++; return null; }
        var index = pool.Free[--pool.FreeCount];
        pool.Active[pool.ActiveCount++] = index;
        pool.HighWater = Math.Max(pool.HighWater, pool.ActiveCount);
        var effect = pool.Effects[index];
        effect.StartTick = startTick;
        effect.Local = local;
        effect.Owner = null;
        effect.Offset = Vector2.Zero;
        effect.Origin = position;
        effect.Endpoint = position;
        effect.Node.Transform = Transform2D.Identity;
        effect.Node.GlobalPosition = position;
        effect.Node.GlobalRotation = angle;
        effect.Node.Modulate = Colors.White;
        effect.Node.SelfModulate = Colors.White;
        effect.Node.Visible = true;
        if (effect.Animation != null)
        {
            effect.Animation.Stop();
            effect.Animation.SpeedScale = 1;
            effect.Animation.SetFrameAndProgress(0, 0);
        }
        UpdateEffect(pool, effect, now);
        return effect;
    }

    private void UpdatePool(Pool pool)
    {
        for (var i = 0; i < pool.ActiveCount;)
        {
            var index = pool.Active[i];
            var effect = pool.Effects[index];
            var now = effect.Local ? _localTick : _remoteTick;
            if (now - effect.StartTick + 1e-9 >= pool.Duration * 60)
            {
                Release(effect);
                pool.Active[i] = pool.Active[--pool.ActiveCount];
                pool.Free[pool.FreeCount++] = index;
            }
            else { UpdateEffect(pool, effect, now); i++; }
        }
    }

    private static void UpdateEffect(Pool pool, Effect effect, double now)
    {
        var age = Math.Max(0, (now - effect.StartTick) / 60);
        if (pool.Kind == 0 && GodotObject.IsInstanceValid(effect.Owner))
        {
            effect.Node.GlobalPosition = effect.Owner!.GlobalTransform * effect.Offset;
            effect.Node.GlobalRotation = effect.Owner.GlobalRotation;
        }
        else if (pool.Kind == 1)
            effect.Node.GlobalPosition = effect.Origin.Lerp(effect.Endpoint, (float)Math.Clamp(age / pool.Duration, 0, 1));
        if (effect.Animation != null && pool.FrameEnds != null)
        {
            var frame = 0;
            while (frame < pool.FrameEnds.Length - 1 && age + 1e-9 >= pool.FrameEnds[frame]) frame++;
            var start = frame == 0 ? 0 : pool.FrameEnds[frame - 1];
            effect.Animation.SetFrameAndProgress(frame, (float)Math.Clamp((age - start) / (pool.FrameEnds[frame] - start), 0, 1));
        }
        if (pool.Kind == 3) effect.Node.Modulate = new Color(1, 1, 1, (float)(1 - age / pool.Duration));
    }

    private static void Release(Effect effect)
    {
        effect.Node.Visible = false;
        effect.Owner = null;
        effect.Offset = Vector2.Zero;
        effect.Origin = Vector2.Zero;
        effect.Endpoint = Vector2.Zero;
        effect.StartTick = 0;
        effect.Animation?.Stop();
    }

    public void Clear(long cutoffTick, Guid session = default)
    {
        _cutoff = cutoffTick;
        _localTick = _remoteTick = cutoffTick;
        _session = session;
        Array.Clear(_pending);
        _pendingCount = 0;
        _dedup.Clear();
        if (!_initialized) return;
        for (var i = 0; i < _pools.Length; i++)
        {
            var pool = _pools[i];
            for (var j = 0; j < pool.ActiveCount; j++) Release(pool.Effects[pool.Active[j]]);
            pool.ActiveCount = 0;
            pool.FreeCount = pool.Free.Length;
            for (var j = 0; j < pool.Free.Length; j++) pool.Free[j] = j;
        }
        for (var i = 0; i < _voices.Length; i++) { _voices[i].Stop(); _voices[i].Stream = null; _voiceTicks[i] = 0; }
    }

    public override void _ExitTree() => Clear(long.MaxValue);

    internal struct Pending
    {
        public AcceptedShot Shot;
        public ShipController? Shooter;
        public bool Local;
    }

    internal sealed class Pool
    {
        public readonly Effect[] Effects;
        public readonly int[] Free;
        public readonly int[] Active;
        public readonly int Kind;
        public readonly double Duration;
        public readonly double[]? FrameEnds;
        public int FreeCount;
        public int ActiveCount;
        public int HighWater;
        public long Dropped;
        public EffectPoolMetrics Metrics => new(Effects.Length, ActiveCount, HighWater, Dropped);

        public Pool(int capacity, int kind, double duration, double[]? frameEnds)
        {
            Effects = new Effect[capacity];
            Free = new int[capacity];
            Active = new int[capacity];
            Kind = kind;
            Duration = duration;
            FrameEnds = frameEnds;
            FreeCount = capacity;
            for (var i = 0; i < capacity; i++) Free[i] = i;
        }
    }

    internal sealed class Effect(Node2D node, AnimatedSprite2D? animation)
    {
        public readonly Node2D Node = node;
        public readonly AnimatedSprite2D? Animation = animation;
        public long StartTick;
        public bool Local;
        public Vector2 Origin;
        public Vector2 Endpoint;
        public Vector2 Offset;
        public Node2D? Owner;
    }
}
