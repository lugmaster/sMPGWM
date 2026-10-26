#nullable enable

using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record InventoryCellBinding(int Address, ItemEntity? Entity, long Revision);
