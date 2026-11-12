using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.EquipmentSystem;

public interface IEquipmentView
{
    IReadOnlyList<EquipmentSlotDefinition> Definitions { get; }
    EquipmentSnapshot Snapshot { get; }
    event Action<EquipmentSnapshot>? Changed;
}
