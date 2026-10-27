#nullable enable
using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.MessageSystem;

public sealed class MessageSnapshot
{
    public IReadOnlyList<PlayerMessage> Messages { get; }
    public int UnreadCount { get; }

    internal MessageSnapshot(PlayerMessage[] messages)
    {
        Array.Sort(messages, Compare);
        Messages = Array.AsReadOnly(messages);
        foreach (var message in messages) if (!message.IsRead) UnreadCount++;
    }

    public PlayerMessage? Find(string? id)
    {
        foreach (var message in Messages) if (message.Id == id) return message;
        return null;
    }

    public int UnreadForSender(string id)
    {
        var count = 0;
        foreach (var message in Messages) if (message.Sender.Id == id && !message.IsRead) count++;
        return count;
    }

    private static int Compare(PlayerMessage a, PlayerMessage b)
    {
        var order = StringComparer.OrdinalIgnoreCase.Compare(a.Sender.DisplayName, b.Sender.DisplayName);
        if (order == 0) order = StringComparer.Ordinal.Compare(a.Sender.Id, b.Sender.Id);
        if (order == 0) order = b.ReceivedAt.CompareTo(a.ReceivedAt);
        return order == 0 ? b.ArrivalSequence.CompareTo(a.ArrivalSequence) : order;
    }
}
