
using System;
using System.Collections.Generic;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Scripts.MessageSystem;

public sealed class MessageController : IDisposable
{
    private readonly Dictionary<string, MessageSender> _senders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PlayerMessage> _messages = new(StringComparer.Ordinal);
    private readonly Action<Exception>? _reportObserverError;
    private bool _publishing;
    private bool _disposed;
    private long _sequence;
    private UiCanvas? _canvas;
    private PlayerInputPolicy? _inputPolicy;
    private bool _active;

    public PlayerMessage? PendingAcknowledgement
    {
        get
        {
            PlayerMessage? first = null;
            foreach (var message in _messages.Values)
                if (message.OutstandingAcknowledgement &&
                    (first == null || message.ArrivalSequence < first.ArrivalSequence)) first = message;
            return first;
        }
    }

    internal MessageController(UiCanvas canvas, PlayerInputPolicy inputPolicy, Action<Exception> reportObserverError)
        : this(reportObserverError)
    {
        _canvas = canvas;
        _inputPolicy = inputPolicy;
        canvas.MessageAcknowledged += AcknowledgeCurrent;
        try { canvas.BindMessages(this); }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void SetActive(bool active)
    {
        if (_disposed) return;
        _active = active;
        if (active) RefreshAcknowledgement();
    }

    private void RefreshAcknowledgement()
    {
        var required = PendingAcknowledgement;
        _inputPolicy?.SetHasPendingAcknowledgement(required != null);
        if (_active) _canvas?.PresentRequiredMessage(required);
    }

    internal void AcknowledgeCurrent()
    {
        if (_disposed || _publishing || PendingAcknowledgement is not { } message) return;
        _messages[message.Id] = message with { OutstandingAcknowledgement = false };
        Publish(acknowledgementChanged: true);
    }
    public MessageSnapshot Snapshot { get; private set; } = new(Array.Empty<PlayerMessage>());
    public event Action<MessageSnapshot>? Changed;

    public MessageController(Action<Exception>? reportObserverError = null) => _reportObserverError = reportObserverError;

    public bool RegisterSender(MessageSender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        if (string.IsNullOrWhiteSpace(sender.Id) || string.IsNullOrWhiteSpace(sender.DisplayName))
            throw new ArgumentException("A sender needs an identity and display name.", nameof(sender));
        return !_disposed && !_publishing && _senders.TryAdd(sender.Id, sender);
    }

    public bool Receive(string id, string senderId, string subject, string body, MessageType type,
        DateTimeOffset? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(senderId))
            throw new ArgumentException("Message and sender identities are required.");
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(body);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (!_senders.TryGetValue(senderId, out var sender)) throw new ArgumentException("Unknown sender.", nameof(senderId));
        if (_disposed || _publishing || _messages.ContainsKey(id)) return false;
        _messages.Add(id, new PlayerMessage(id, sender, subject, body, receivedAt ?? DateTimeOffset.UtcNow,
            ++_sequence, type, false, type == MessageType.Blocking));
        Publish(acknowledgementChanged: type == MessageType.Blocking);
        return true;
    }

    public bool MarkRead(string id)
    {
        if (_disposed || _publishing || !_messages.TryGetValue(id, out var message) || message.IsRead) return false;
        _messages[id] = message with { IsRead = true };
        Publish();
        return true;
    }

    private void Publish(bool acknowledgementChanged = false)
    {
        var messages = new PlayerMessage[_messages.Count];
        _messages.Values.CopyTo(messages, 0);
        Snapshot = new MessageSnapshot(messages);
        _publishing = true;
        try
        {
            if (acknowledgementChanged) RefreshAcknowledgement();
            if (Changed == null) return;
            foreach (Action<MessageSnapshot> observer in Changed.GetInvocationList())
                try { observer(Snapshot); }
                catch (Exception exception)
                {
                    try { _reportObserverError?.Invoke(exception); }
                    catch (Exception reportingError) { Console.Error.WriteLine($"Message observer error reporter failed: {reportingError}"); }
                }
        }
        finally { _publishing = false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_canvas != null && Godot.GodotObject.IsInstanceValid(_canvas))
            {
                _canvas.MessageAcknowledged -= AcknowledgeCurrent;
                _canvas.EndMessageSession();
            }
        }
        finally
        {
            _inputPolicy?.SetHasPendingAcknowledgement(false);
            _canvas = null;
            _inputPolicy = null;
            Changed = null;
        }
    }
}
