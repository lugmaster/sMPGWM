namespace sMPGWM.Scripts.Items;

public sealed record ItemEntity
{
    public ItemEntityId Id { get; }
    public ItemDefinition Definition { get; }
    public string DefinitionId => Definition.Id;
    public int Count { get; }

    internal ItemEntity(ItemEntityId id, ItemDefinition definition, int count)
    {
        Id = id;
        Definition = definition;
        Count = count;
    }
}
