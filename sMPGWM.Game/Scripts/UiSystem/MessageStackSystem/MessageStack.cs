using System;
using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageStack : VBoxContainer
{
    
    public void AddMessage(string text, bool autoDismissible)
    {
        var message = GameOverlayProvider.CreateMessageStack();
        message.SetMessage(text);
        message.AutoDismissible = autoDismissible;
        AddChild(message);
    }
    
    public void AddMessage(string text, bool autoDismissible, Action onClosed)
    {
        var message = GameOverlayProvider.CreateMessageStack();
        message.SetMessage(text);
        message.AutoDismissible = autoDismissible;
        message.OnClosedCallback = onClosed;
        AddChild(message);
    }
}