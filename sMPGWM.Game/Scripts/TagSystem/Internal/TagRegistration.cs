namespace sMPGWM.Scripts.TagSystem.Internal;

/// <summary>
/// Stores a canonical tag registration and its optional assignment override.
/// </summary>
internal sealed record TagRegistration(string DomainKey, string CanonicalName, bool? IsAssignable);
