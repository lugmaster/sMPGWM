namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemEntity
{
    public ItemEntityId Id { get; }
    public ItemDefinitionId DefinitionId { get; }
    public int Count { get; }

    internal ItemEntity(ItemEntityId id, ItemDefinitionId definitionId, int count)
    {
        Id = id;
        DefinitionId = definitionId;
        Count = count;
    }
}
