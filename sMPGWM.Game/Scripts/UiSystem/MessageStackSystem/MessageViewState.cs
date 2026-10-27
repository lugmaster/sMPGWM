#nullable enable
using System.Collections.Generic;
namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;
internal sealed class MessageViewState
{
    internal string? SelectedId;
    internal readonly HashSet<string> CollapsedSenders = new();
    internal int SenderScroll;
    internal int BodyScroll;
}
