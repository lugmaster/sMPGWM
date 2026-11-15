namespace sMPGWM.Scripts.CombatSystem;

public readonly record struct DamageResult(double ShieldDamage, double HealthDamage, bool ShieldContact, bool Killed);
