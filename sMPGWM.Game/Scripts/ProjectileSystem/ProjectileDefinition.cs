using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ProjectileSystem;

public sealed record ProjectileDefinition(string Id, string DisplayName, string Description,
    double Speed, double MaxLifetimeMs, ProjectileCollision Collision, ProjectileExpiration ExpireWithoutImpact)
    : GameplayDefinition(Id, DisplayName, Description, null);
