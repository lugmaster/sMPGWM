using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.TagSystem.Exceptions;

/// <summary>
/// Reports diagnostics produced when tag registry compilation fails.
/// </summary>
/// <param name="diagnostics">The validation diagnostics reported during compilation.</param>
public sealed class TagValidationException(IReadOnlyList<TagValidationDiagnostic> diagnostics)
    : TagException(CreateMessage(diagnostics))
{
    public IReadOnlyList<TagValidationDiagnostic> Diagnostics { get; } = diagnostics;

    /// <summary>
    /// Formats validation diagnostics into an exception message.
    /// </summary>
    private static string CreateMessage(IReadOnlyList<TagValidationDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return "Gameplay tag validation failed.";
        }

        return "Gameplay tag validation failed: " +
               string.Join(", ", diagnostics.Select(d => $"{d.Code} at {d.FieldPath}"));
    }
}
