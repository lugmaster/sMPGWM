namespace sMPGWM.Scripts.AbilitySystem;

public sealed record BehaviourStacking(StackingScope Scope, StackingPolicy Policy, int? MaxStacks, StackOverflowPolicy? Overflow);
