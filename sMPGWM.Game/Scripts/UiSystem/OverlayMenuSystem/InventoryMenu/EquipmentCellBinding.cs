#nullable enable

using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record EquipmentCellBinding(string SlotKey, ItemEntity? Entity, long Revision);
