
using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.FileLoadingSystem;

public sealed record DataLoadError(
    string Title,
    string Message,
    string? SourcePath = null,
    string? SchemaPath = null,
    Exception? Exception = null)
{
    public string ToDisplayString()
    {
        var lines = new List<string> { Title, Message };

        if (!string.IsNullOrWhiteSpace(SourcePath))
            lines.Add($"Source: {SourcePath}");

        if (!string.IsNullOrWhiteSpace(SchemaPath))
            lines.Add($"Schema: {SchemaPath}");

        if (Exception is not null)
        {
            lines.Add($"Exception: {Exception.GetType().Name}");
            lines.Add(Exception.Message);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
