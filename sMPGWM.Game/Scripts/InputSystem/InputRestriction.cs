using System;

namespace sMPGWM.Scripts.InputSystem;

[Flags]
public enum InputRestriction
{
    None = 0,
    BindingCapture = 1,
    TextEntry = 2,
    RequiredAcknowledgement = 4
}
