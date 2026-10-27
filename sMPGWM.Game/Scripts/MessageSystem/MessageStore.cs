#nullable enable
using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.MessageSystem;

public sealed class MessageStore : IDisposable
{
    private readonly Dictionary<string, MessageSender> _senders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PlayerMessage> _messages = new(StringComparer.Ordinal);
    private readonly Action<Exception>? _reportObserverError;
    private bool _publishing;
    private bool _disposed;
    private long _sequence;
    public MessageSnapshot Snapshot { get; private set; } = new(Array.Empty<PlayerMessage>());
    public event Action<MessageSnapshot>? Changed;

    public MessageStore(Action<Exception>? reportObserverError = null) => _reportObserverError = reportObserverError;

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
            ++_sequence, type, false));
        Publish();
        return true;
    }

    public bool MarkRead(string id)
    {
        if (_disposed || _publishing || !_messages.TryGetValue(id, out var message) || message.IsRead) return false;
        _messages[id] = message with { IsRead = true };
        Publish();
        return true;
    }

    private void Publish()
    {
        var messages = new PlayerMessage[_messages.Count];
        _messages.Values.CopyTo(messages, 0);
        Snapshot = new MessageSnapshot(messages);
        _publishing = true;
        try
        {
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

    public void Dispose() { _disposed = true; Changed = null; }
}
