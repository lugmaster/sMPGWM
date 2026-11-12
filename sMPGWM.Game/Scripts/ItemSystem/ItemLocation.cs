namespace sMPGWM.Scripts.ItemSystem;

/// <summary>A concrete storage address, independent of the player or UI displaying it.</summary>
public readonly record struct ItemLocation(ItemContainerId ContainerId, int SlotIndex);
