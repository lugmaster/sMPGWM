using sMPGWM.Scripts.MessageSystem;

namespace sMPGWM.Scripts.Util.Mocks;

internal static class MessageMock
{
    public static void PopulateWelcomeMessage(MessageController store)
    {
        store.RegisterSender(new MessageSender("system", "System"));
        store.Receive("welcome", "system", "Welcome", "Welcome, this is test message", MessageType.NonBlocking);
    }
}
