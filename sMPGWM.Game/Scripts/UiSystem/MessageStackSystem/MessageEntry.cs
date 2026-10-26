using System;
using Godot;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageEntry : PanelContainer
{
    private float _autoDismissTime = 3.5f;
    public bool AutoDismissible = true;
    public Action OnClosedCallback { get; set; }

    public override void _Ready()
    {
        var closeButton = GetNode<Button>("HBoxContainer/CloseButton");
        closeButton.Pressed += OnClosePressed;

        if (AutoDismissible)
        {
            GetTree().CreateTimer(_autoDismissTime).Timeout += () =>
            {
                if (IsInsideTree())
                    QueueFree();
            };
        }
        
    }

    public void SetMessage(string text)
    {
        var label = GetNode<Label>("HBoxContainer/MessageLabel");
        label.Text = text;
    }

    private void OnClosePressed()
    {
        QueueFree();
        OnClosedCallback?.Invoke();
    }
    
}