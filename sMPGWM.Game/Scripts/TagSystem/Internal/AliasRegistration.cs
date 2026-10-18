namespace sMPGWM.Scripts.TagSystem.Internal;

/// <summary>
/// Stores an alias and its canonical target with the original registration position.
/// </summary>
internal sealed record AliasRegistration(string AliasName, string CanonicalTargetName, int Sequence);
