namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemEntity
{
    public ItemEntityId Id { get; }
    public ItemDefinition Definition { get; }
    public string DefinitionId => Definition.Id;
    public int Count { get; }
    public WeaponSystem.WeaponExecutionPlan? WeaponPlan { get; }
    public WeaponSystem.WeaponRuntimeState? WeaponState { get; internal set; }

    // Mutable execution state is not part of item placement/value equality.
    public bool Equals(ItemEntity? other) => other != null && Id == other.Id && Definition == other.Definition && Count == other.Count;
    public override int GetHashCode() => System.HashCode.Combine(Id, Definition, Count);

    internal ItemEntity(ItemEntityId id, ItemDefinition definition, int count)
    {
        Id = id;
        Definition = definition;
        Count = count;
        WeaponPlan = WeaponSystem.WeaponDefinitionCompiler.Prepare(definition);
        if (definition is WeaponItemDefinition weapon) WeaponState = new(weapon.Supply);
    }
}
