namespace sMPGWM.Scripts.ItemSystem;

internal readonly record struct ItemContainerConfirmation(ItemContainer Container, ItemEntity?[] Slots, long Revision);
