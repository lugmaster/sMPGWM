namespace sMPGWM.Scripts.Controllers.Player;

public interface IInputMode
{
    void Update();
    bool IsActive();
    void ResetState();
}