using sMPGWM.Scripts.AbilitySystem;
namespace sMPGWM.Scripts.GamePlaySystem;
public sealed class AbilityAdmissionSystem
{
    internal bool Allows(AbilityDefinition definition, AbilityGrants grants) =>
        definition.Availability == AbilityAvailability.AllShips || grants.Contains(definition.Id);
}
