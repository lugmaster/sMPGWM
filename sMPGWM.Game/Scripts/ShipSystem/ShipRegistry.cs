using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShipRegistry
{
    public static ShipData GetById(string id) => GameplayDefinitions.Current.Ships.GetById(id);
    public static bool TryGetById(string id, out ShipData data) => GameplayDefinitions.Current.Ships.TryGetById(id, out data!);
    public static IReadOnlyList<ShipData> GetAll() => GameplayDefinitions.Current.Ships.GetAll();
}
