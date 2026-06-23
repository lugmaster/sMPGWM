using sMPGWM.Scripts.Autoload;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public interface ILivingEntityLogger
{
    void Info(string message);
}

public sealed class GodotLivingEntityLogger : ILivingEntityLogger
{
    public void Info(string message)
    {
        Logger.Info(message);
    }
}
