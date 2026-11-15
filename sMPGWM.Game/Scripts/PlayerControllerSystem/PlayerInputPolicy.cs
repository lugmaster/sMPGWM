using System;
using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Scripts.PlayerControllerSystem;

public sealed class PlayerInputPolicy
{
    private bool _sessionIsActive;
    private bool _hasBlockingWindow;

    private bool _hasPendingAcknowledgement;
    private bool _abilityTargeting;
    private bool _abilityWaitingForRelease;
    private bool _inventoryInteractionAvailable;
    public bool CanControlPlayer => _sessionIsActive && !_hasBlockingWindow && !_hasPendingAcknowledgement;

    // Contexts overlap: targeting captures flight pointer actions while movement remains available.
    public bool IsContextAvailable(InputContext context) => context switch
    {
        InputContext.Flight => CanControlPlayer,
        InputContext.AbilityTargeting => CanControlPlayer && _abilityTargeting,
        InputContext.MenuShortcuts => _sessionIsActive && !_hasPendingAcknowledgement,
        InputContext.MenuInteraction => _sessionIsActive,
        InputContext.InventoryInteraction => _sessionIsActive && !_hasPendingAcknowledgement && _inventoryInteractionAvailable,
        _ => false
    };

    public bool CanUseFlightPointerActions(bool pointerOverUi) =>
        IsContextAvailable(InputContext.Flight) && !pointerOverUi && !_abilityTargeting && !_abilityWaitingForRelease;

    public void SetAbilityTargeting(bool active, bool waitingForRelease)
    {
        _abilityTargeting = active;
        _abilityWaitingForRelease = waitingForRelease;
    }

    public void SetInventoryInteractionAvailable(bool available) => _inventoryInteractionAvailable = available;

    // UI restrictions are evaluated at the input event, avoiding a second copy of focus/capture state.
    public static bool AllowsMenuShortcut(InputRestriction restrictions, bool isMainMenu) =>
        (restrictions & (InputRestriction.BindingCapture | InputRestriction.RequiredAcknowledgement)) == 0 &&
        (isMainMenu || (restrictions & InputRestriction.TextEntry) == 0);

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
