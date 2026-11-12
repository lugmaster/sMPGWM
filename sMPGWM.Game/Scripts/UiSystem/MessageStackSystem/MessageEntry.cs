using sMPGWM.Scripts.MessageSystem;
using Godot;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageEntry : VBoxContainer
{
    internal void Display(PlayerMessage message)
    {
        GetNode<Label>("Heading").Text = message?.Subject ?? "No message selected";
        GetNode<Label>("Heading").TooltipText = message?.Subject ?? "";
        GetNode<Label>("SenderLabel").Text = message?.Sender.DisplayName ?? "";
        GetNode<Label>("BodyScrollContainer/BodyLabel").Text = message?.Body ?? "";
    }
}
