namespace sMPGWM.Scripts.InputSystem;

public sealed record KeyBindingDefinition(string Action, string Label, string Section)
{
    public int Order { get; init; }
    public float Deadzone { get; init; }
}
