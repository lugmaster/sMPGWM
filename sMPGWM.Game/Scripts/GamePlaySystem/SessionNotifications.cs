using System;
using sMPGWM.Scripts.MessageSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed class SessionNotifications(MessageController messages)
{
    public void MembershipChanged(int peer, bool joined, string reason)
    {
        var text = $"Player {peer} {(joined ? "joined" : "left")}" +
                   (string.IsNullOrEmpty(reason) ? "." : $": {reason}");
        Logger.Info($"NET membership {text}");
        Receive(Guid.NewGuid().ToString("N"), text);
    }

    public void LocalJoined(string gameName)
    {
        var text = string.IsNullOrWhiteSpace(gameName) ? "You joined the game." : $"You joined game {gameName}.";
        Logger.Info($"NET local_join {text}");
        Receive("network-local-join", text);
    }

    private void Receive(string id, string text)
    {
        messages.RegisterSender(new MessageSender("network", "Session"));
        messages.Receive(id, "network", text, text, MessageType.NonBlocking);
    }
}
