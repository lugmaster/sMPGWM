using System;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class PlayerInputPolicy
{
    private bool _sessionIsActive;
    private bool _hasBlockingWindow;

    public bool CanControlPlayer => _sessionIsActive && !_hasBlockingWindow;
    public event Action<bool>? CanControlPlayerChanged;

    public void SetSessionActive(bool active)
    {
        var wasAllowed = CanControlPlayer;
        _sessionIsActive = active;
        NotifyChanged(wasAllowed);
    }

    public void SetHasBlockingWindow(bool blocking)
    {
        var wasAllowed = CanControlPlayer;
        _hasBlockingWindow = blocking;
        NotifyChanged(wasAllowed);
    }

    private void NotifyChanged(bool wasAllowed)
    {
        if (wasAllowed != CanControlPlayer)
            CanControlPlayerChanged?.Invoke(CanControlPlayer);
    }
}
