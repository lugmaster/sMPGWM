using System;
using System.Collections.Generic;
using System.Text.Json;

namespace sMPGWM.Scripts.InputSystem;

public static class InputContextNames
{
    private static readonly Dictionary<string, InputContext> ContextsBySectionId = CreateLookup();

    public static bool TryGetContext(string sectionId, out InputContext context) =>
        ContextsBySectionId.TryGetValue(sectionId, out context);

    private static Dictionary<string, InputContext> CreateLookup()
    {
        var contexts = Enum.GetValues<InputContext>();
        var lookup = new Dictionary<string, InputContext>(contexts.Length, StringComparer.Ordinal);
        foreach (var context in contexts)
            lookup.Add(JsonNamingPolicy.SnakeCaseLower.ConvertName(context.ToString()), context);
        return lookup;
    }
}
