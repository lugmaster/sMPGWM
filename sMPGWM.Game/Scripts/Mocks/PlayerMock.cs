using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

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

    public IReadOnlyList<IconDefinition> GetHotBarSkills()
    {
        return
        [
            CreateIcon("res://assets/mocks/compass.png"),
            CreateIcon("res://assets/mocks/diamond.png"),
            CreateIcon("res://assets/mocks/Diamond-02.png"),
            CreateIcon("res://assets/mocks/Dumbbell.png"),
            CreateIcon("res://assets/mocks/Dumbbell-02.png"),

            CreateIcon("res://assets/mocks/fishhook.png"),
            CreateIcon("res://assets/mocks/fishing-rod.png"),
            CreateIcon("res://assets/mocks/funnel.png"),
            CreateIcon("res://assets/mocks/hair-dryer.png"),
            CreateIcon("res://assets/mocks/hammer.png"),
        ];
    }

    private static IconDefinition CreateIcon(string texturePath)
    {
        return new IconDefinition
        {
            IconTexture = GD.Load<Texture2D>(texturePath),
            IconSize = IconSize.Medium
        };
    }

    public void OnHotbarSlotPressed(int slotIndex)
    {
        Logger.Info($"Request skill launch from hotbar slot {slotIndex}");

        // Later:
        // SkillController.RequestLaunchSkill(slotIndex);
    }
}