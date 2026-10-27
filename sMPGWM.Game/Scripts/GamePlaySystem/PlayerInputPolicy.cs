using System;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class PlayerInputPolicy
{
    private bool _sessionIsActive;
    private bool _hasBlockingWindow;

    private bool _hasPendingAcknowledgement;
    public bool CanControlPlayer => _sessionIsActive && !_hasBlockingWindow && !_hasPendingAcknowledgement;

    public void SetHasPendingAcknowledgement(bool pending)
    {
        var wasAllowed = CanControlPlayer;
        _hasPendingAcknowledgement = pending;
        NotifyChanged(wasAllowed);
    }
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
