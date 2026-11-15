using System;

namespace sMPGWM.Scripts.InputSystem;

public sealed record KeyBindingDefinition(string Action, string Label, string SectionId)
{
    public InputContext Context => InputContextNames.TryGetContext(SectionId, out var context)
        ? context : throw new ArgumentException("Unknown input context section: " + SectionId, nameof(SectionId));
    public required string SectionDisplayName { get; init; }
    public int Order { get; init; }
    public float Deadzone { get; init; }
}
