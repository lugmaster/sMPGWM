using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Mocks;

public partial class PlayerMock: AbstractSingleton<PlayerMock>
{
    
    public readonly LivingEntityState LivingEntityState = new LivingEntityState
    {
        Condition = LivingEntityCondition.None,
        Health = new Stat(100,15),
        HealthRegeneration = new Stat(0),
        Shield = new Stat(175,168),
        ShieldRegeneration = new Stat(0),
        Energy = new Stat(120, 77),
        EnergyRegeneration = new Stat(0),
        Speed = new Stat(0),
        Acceleration = new Stat(0),
        AfterBurnerSpeed = new Stat(0),
        AfterBurnerAcceleration = new Stat(0),
        LivingEntityType = LivingEntityType.MpController,
        LivingEntityId = 1
    };
}