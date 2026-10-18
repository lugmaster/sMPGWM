namespace sMPGWM.Scripts.TagSystem.Exceptions;

/// <summary>
/// Describes a tag registration error and the input field that caused it.
/// </summary>
/// <param name="Code">The machine-readable validation code.</param>
/// <param name="FieldPath">The path of the invalid registration field.</param>
/// <param name="Message">The human-readable validation description.</param>
/// <param name="InvalidValue">The invalid input value, when available.</param>
public sealed record TagValidationDiagnostic(
    string Code,
    string FieldPath,
    string Message,
    string? InvalidValue = null);
