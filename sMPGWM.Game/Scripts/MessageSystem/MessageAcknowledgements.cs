#nullable enable
using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.MessageSystem;

public sealed class MessageAcknowledgements : IDisposable
{
    private readonly MessageStore _store;
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly Queue<PlayerMessage> _pending = new();
    public PlayerMessage? Current => _pending.TryPeek(out var message) ? message : null;
    public event Action? Changed;

    public MessageAcknowledgements(MessageStore store)
    {
        _store = store;
        store.Changed += Refresh;
        Refresh(store.Snapshot);
    }

    private void Refresh(MessageSnapshot snapshot)
    {
        var arrivals = new List<PlayerMessage>();
        foreach (var message in snapshot.Messages)
            if (message.Type == MessageType.Blocking && _seen.Add(message.Id)) arrivals.Add(message);
        arrivals.Sort((a, b) => a.ArrivalSequence.CompareTo(b.ArrivalSequence));
        foreach (var message in arrivals) _pending.Enqueue(message);
        if (arrivals.Count > 0) Changed?.Invoke();
    }

    internal void AcknowledgeCurrent()
    {
        if (_pending.Count == 0) return;
        _pending.Dequeue();
        Changed?.Invoke();
    }

    public void Dispose() { _store.Changed -= Refresh; _pending.Clear(); _seen.Clear(); Changed = null; }
}
