namespace sMPGWM.Scripts.TagSystem.Internal;

/// <summary>
/// Stores a domain registration and its original position for validation diagnostics.
/// </summary>
internal sealed record DomainRegistration(string DomainKey, string RootPrefix, string Description, int Sequence);
