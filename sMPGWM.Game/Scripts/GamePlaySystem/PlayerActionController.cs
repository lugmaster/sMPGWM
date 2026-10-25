using System;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class PlayerActionController(
    PlayerInputPolicy inputPolicy,
    Action<int> activateHotbarSlot,
    Func<bool> isInputAvailable)
{
    public void RequestHotbarActivation(int slotIndex)
    {
        if (!inputPolicy.CanControlPlayer || !isInputAvailable()) return;
        activateHotbarSlot(slotIndex);
    }
}
