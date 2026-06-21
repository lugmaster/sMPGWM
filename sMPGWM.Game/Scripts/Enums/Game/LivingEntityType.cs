namespace sMPGWM.Scripts.Enums.Game;

public enum LivingEntityType
{
    MpController,
    AgentController
}

public static class LivingEntityTypeExtension
{
    public static bool IsAgent(this LivingEntityType type)
    {
        return type is LivingEntityType.AgentController;
    }

    public static bool IsMpController(this LivingEntityType type)
    {
        return type is LivingEntityType.MpController;
    }
}