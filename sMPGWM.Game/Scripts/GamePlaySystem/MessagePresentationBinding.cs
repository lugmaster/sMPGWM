using System;
using Godot;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed class MessagePresentationBinding : IDisposable
{
    private readonly MessageController _messages;
    private readonly UiCanvas _canvas;
    private readonly PlayerInputPolicy _inputPolicy;
    private bool _disposed;

    public MessagePresentationBinding(MessageController messages, UiCanvas canvas, PlayerInputPolicy inputPolicy)
    {
        _messages = messages;
        _canvas = canvas;
        _inputPolicy = inputPolicy;
        messages.AcknowledgementChanged += RefreshAcknowledgement;
        canvas.MessageAcknowledged += messages.AcknowledgeCurrent;
        try
        {
            canvas.BindMessages(messages);
            RefreshAcknowledgement();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void RefreshAcknowledgement()
    {
        var required = _messages.PendingAcknowledgement;
        _inputPolicy.SetHasPendingAcknowledgement(required != null);
        if (_messages.Active) _canvas.PresentRequiredMessage(required);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _messages.AcknowledgementChanged -= RefreshAcknowledgement;
        try
        {
            if (GodotObject.IsInstanceValid(_canvas))
            {
                _canvas.MessageAcknowledged -= _messages.AcknowledgeCurrent;
                _canvas.EndMessageSession();
            }
        }
        finally { _inputPolicy.SetHasPendingAcknowledgement(false); }
    }
}
