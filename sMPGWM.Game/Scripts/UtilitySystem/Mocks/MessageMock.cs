using sMPGWM.Scripts.MessageSystem;

namespace sMPGWM.Scripts.UtilitySystem.Mocks;

internal static class MessageMock
{
    public static void SendAcknowledgementMessage(MessageController controller)
    {
        controller.RegisterSender(new MessageSender("system", "System"));
        controller.Receive("mock-acknowledgement", "system", "Acknowledgement required",
            "You have played for 30 seconds. Please click OK to acknowledge this test message.",
            MessageType.Blocking);
    }

    public static void PopulateWelcomeMessage(MessageController store)
    {
        store.RegisterSender(new MessageSender("system", "System"));
        store.Receive("welcome", "system", "Welcome", "Welcome, this is test message", MessageType.NonBlocking);
    }
}
