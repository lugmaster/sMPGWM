#nullable enable

namespace sMPGWM.Scripts.FileLoadingSystem;

// Newtonsoft.Json instantiates these records when deserializing category YAML through DefinitionDeserializer.
// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ItemCategoryDefinition(
    string Id,
    string DisplayName,
    int DisplayOrder,
    bool CanStack,
    string? IconAssetPath = null)
{
    public string? Icon { get; init; }
}
