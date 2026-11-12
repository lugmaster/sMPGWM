using System;
namespace sMPGWM.Scripts.GamePlaySystem;
public sealed class ShipComponentCollection
{
    public ShipComponentKind Presence { get; private set; } = ShipComponentKind.All;
    public PowerSystem? Power { get; private set; } = new();
    public ShieldSystem? Shield { get; private set; } = new();
    public MovementCapability? Movement { get; private set; } = new();
    public AbilityAdmissionSystem? AbilityAdmission { get; private set; } = new();
    public EffectProcessingSystem? Effects { get; private set; } = new();
    public bool Has(ShipComponentKind kind) => (Presence & kind) == kind;
    internal void Install(ShipComponentKind presence) => Prepare(presence)();
    internal Action Prepare(ShipComponentKind presence)
    {
        if ((presence & ~ShipComponentKind.All) != 0) throw new ArgumentOutOfRangeException(nameof(presence));
        // Prepare all component allocations before publishing any attachment.
        var power = (presence & ShipComponentKind.Power) != 0 ? Power ?? new PowerSystem() : null;
        var shield = (presence & ShipComponentKind.Shield) != 0 ? Shield ?? new ShieldSystem() : null;
        var movement = (presence & ShipComponentKind.Movement) != 0 ? Movement ?? new MovementCapability() : null;
        var admission = (presence & ShipComponentKind.AbilityAdmission) != 0 ? AbilityAdmission ?? new AbilityAdmissionSystem() : null;
        var effects = (presence & ShipComponentKind.Effects) != 0 ? Effects ?? new EffectProcessingSystem() : null;
        return () =>
        {
            Power = power; Shield = shield; Movement = movement; AbilityAdmission = admission; Effects = effects;
            Presence = presence;
        };
    }
    internal void Dispose() => Install(ShipComponentKind.None);
}
