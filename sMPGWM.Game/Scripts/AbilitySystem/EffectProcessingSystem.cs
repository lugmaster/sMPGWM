using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.AbilitySystem;
public sealed class EffectProcessingSystem
{
    internal static void ResolvePresence(ShipStatState stats, bool present) => stats.SetEffectsEnabled(present);
}
