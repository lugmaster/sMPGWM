using System;

namespace sMPGWM.Scripts.MessageSystem;

public sealed record PlayerMessage(string Id, MessageSender Sender, string Subject, string Body,
    DateTimeOffset ReceivedAt, long ArrivalSequence, MessageType Type, bool IsRead);
