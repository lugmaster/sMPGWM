namespace sMPGWM.Scripts.TagSystem.Internal;

/// <summary>
/// Stores the validated canonical tag metadata used during compilation.
/// </summary>
internal sealed record TagCompileInput(string DomainKey, string CanonicalName, bool? IsAssignable, bool IsRoot);
