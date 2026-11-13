using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed record WeaponConfigurationSnapshot(long Revision, byte[] Groups, ItemEntityId[] Items);
