

using System;
using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.TagSystem;

public sealed record GameplayTagValidationDiagnostic(
    string Code,
    string FieldPath,
    string Message,
    string? InvalidValue = null);

public class GameplayTagException : InvalidOperationException
{
    public GameplayTagException(string message)
        : base(message)
    {
    }
}

public sealed class GameplayTagValidationException : GameplayTagException
{
    public GameplayTagValidationException(IReadOnlyList<GameplayTagValidationDiagnostic> diagnostics)
        : base(CreateMessage(diagnostics))
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<GameplayTagValidationDiagnostic> Diagnostics { get; }

    private static string CreateMessage(IReadOnlyList<GameplayTagValidationDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return "Gameplay tag validation failed.";
        }

        return "Gameplay tag validation failed: " +
               string.Join(", ", diagnostics.Select(d => $"{d.Code} at {d.FieldPath}"));
    }
}
