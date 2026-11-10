namespace sMPGWM.Scripts.AbilitySystem;

public sealed record ModifierDefinition(string StatId, ModifierValueKind ValueKind, ModifierOperation Operation, double Value);
