namespace sMPGWM.Scripts.GameplayDataSystem;

public sealed record CatalogueSource(string FileName, string SchemaName, string RootKey,
    string IdentityField = "Id", string UserDirectory = "gameplay/", string DefaultDirectory = "")
{
    public string UserPath => "user://config/" + UserDirectory + FileName;
    public string DefaultPath => "res://assets/game_data/" + DefaultDirectory + FileName;
    public string SchemaPath => "res://assets/game_data/" + DefaultDirectory + SchemaName;
}
