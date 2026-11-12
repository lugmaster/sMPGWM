using System;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;

namespace sMPGWM.Scripts.AbilitySystem;

public readonly record struct AbilitySource(AbilitySourceKind Kind, string Key, long Entity)
{
    public static AbilitySource Equipment(string slot, long entity) => new(AbilitySourceKind.Equipment, slot, entity);
    public static AbilitySource Grant(string source) => new(AbilitySourceKind.Grant, source, 0);
    public static AbilitySource AllShips => new(AbilitySourceKind.AllShips, "", 0);

    public bool IsValid => Kind switch
    {
        AbilitySourceKind.Equipment => Key is { Length: > 0 and <= 64 } && Entity > 0,
        AbilitySourceKind.Grant => Key is { Length: > 0 and <= 64 } && Entity == 0 &&
            !Key.StartsWith("item:", StringComparison.Ordinal),
        AbilitySourceKind.AllShips => Key == "" && Entity == 0,
        _ => false
    };

    public bool IsAvailable(AbilityDefinition ability, ShipRuntime caster, EquipmentController? items) =>
        IsValid && caster.AbilityAdmissionAvailable && (Kind switch
        {
            AbilitySourceKind.Equipment => Provides(items, ability.Id),
            AbilitySourceKind.Grant => caster.Grants.ContainsSource(Key, ability.Id),
            AbilitySourceKind.AllShips => ability.Availability == AbilityAvailability.AllShips,
            _ => false
        });
    private bool Provides(sMPGWM.Scripts.Items.Equipment.EquipmentController? equipment, string ability)
    {
        if (equipment == null) return false;
        var index = equipment.FindSlot(Key);
        if (index < 0 || equipment.Snapshot.Slots[index] is not { } item || item.Id.Value != (ulong)Entity) return false;
        foreach (var id in item.Definition.AbilityIds)
            if (id == ability) return true;
        return false;
    }
}
