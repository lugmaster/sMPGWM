namespace sMPGWM.Scripts.VfxSystem;

public readonly record struct EffectPoolMetrics(int Capacity, int Active, int HighWater, long Dropped);
