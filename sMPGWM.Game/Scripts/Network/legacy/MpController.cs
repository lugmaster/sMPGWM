using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Constants;
using FlatEarthUniverse.scripts.Controllers.LivingEntity;
using FlatEarthUniverse.scripts.Controllers.MultiplayerController.Actions;
using FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;
using FlatEarthUniverse.scripts.Controllers.MultiplayerController.Sound;
using FlatEarthUniverse.scripts.Controllers.MultiplayerController.Visuals;
using FlatEarthUniverse.scripts.GameObjects.Ships;
using FlatEarthUniverse.scripts.GameObjects.Weapons;
using Godot;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController;

public partial class MpController : CharacterBody2D
{
    public ShipType ShipType { get; set; }
    public MpControllerType MpControllerType { get; set; }
    public MpAnimationController MpAnimationController { get; set; }
    public CollisionPolygon2D CollisionPolygon2D { get; set; }
    public MpTextureController MpTextureController { get; set; }
    public MpSoundController MpSoundController { get; set; }
    public ActionController ActionController;
    public LivingEntityState LivingEntityState;
    public MovementController MovementController;
    public MpControllerConditionSystem MpControllerConditionSystem;
    public WeaponSystem WeaponSystem {get; set;}

    /// <summary>
    /// Reflects the <c>peerId</c> of the player who is actually controlling this entity.
    /// </summary>
    /// <remarks>
    /// This value only diverges on dummy controllers.
    /// At all times, there is exactly one player <c>MpController</c>:
    /// one on the host, and one on each client for their local player.
    /// On the host and on the client, the owningPeerId is identical to Multiplayer.GetUniqueId()
    /// </remarks>
    public int OwningPeerId;

    public bool ScheduledForDisconnection;

    private bool _initialized;
    public bool IsHost { get; set; }

    public override void _Ready()
    {
        CallDeferred(nameof(Initialize));
    }

    public virtual void Initialize()
    {
        LivingEntityState.OnConditionChanged += OnConditionChanged;
        _initialized = true;
    }

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        OnProcess((float)delta);
    }
    
    protected virtual void OnProcess(float delta) { }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized) return;
        OnPhysicsProcess((float)delta);
    }
    protected virtual void OnPhysicsProcess(float delta) { }

    public GCol.Array GetSpawnableData() =>
        new MpControllerSpawningData(OwningPeerId, GlobalPosition, Rotation, Velocity, ShipType).ToVariant();

    public virtual void Cleanup()
    {
        LivingEntityState.OnConditionChanged -= OnConditionChanged;
        ActionController?.Cleanup();
        MpAnimationController?.CleanUp();
        MpTextureController?.QueueFree();
        CollisionPolygon2D?.QueueFree();
    }

    public void ReinitializeForRespawn(Vector2 newPosition)
    {
        Logger.Log($"Reinitializing player with owningPeerId {OwningPeerId} on MultiplayerUniqueId: {Multiplayer.GetUniqueId()} with spawn position: {newPosition}", 0);
        MovementController.ReinitializeForRespawn(newPosition);
        GlobalStateManager.MpInstantiator.ConfirmRespawnReadyOnHost();
    }
    
    public void OnConditionChanged(LivingEntityCondition condition, bool applyVisualEffects)
    {
        MpControllerConditionSystem.UpdateCondition(condition, applyVisualEffects);
    }
}