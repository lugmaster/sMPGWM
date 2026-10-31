namespace sMPGWM.Scripts.Network;

public sealed record DiagnosticResult(bool Succeeded, string Reason, string Correlation, string HostSession);
