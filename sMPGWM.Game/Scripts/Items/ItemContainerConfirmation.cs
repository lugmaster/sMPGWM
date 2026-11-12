namespace sMPGWM.Scripts.Items;

internal readonly record struct ItemContainerConfirmation(ItemContainer Container, ItemEntity?[] Slots, long Revision);
