namespace sMPGWM.Scripts.InputSystem;

public interface IInputMode
{
    void Update();
    bool IsActive();
    void ResetState();
}