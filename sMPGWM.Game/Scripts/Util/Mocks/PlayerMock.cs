using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class PlayerMock: AbstractSingleton<PlayerMock>
{
    
    // public readonly LivingEntityState LivingEntityState = new LivingEntityState(StateEngine.Config.PlayerActiveEffectCapacity)
    // {
    //     Condition = LivingEntityCondition.None,
    //     Health = new Stat(100,15),
    //     Shield = new Stat(175,168),
    //     EnergyStorage = new Stat(120, 77),
    //     Speed = new Stat(0),
    //     Acceleration = new Stat(0),
    //     LivingEntityType = LivingEntityType.MpController,
    //     LivingEntityId = 1
    // };

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
            IconSize = IconSize.Large
        };
    }

    public void OnHotbarSlotPressed(int slotIndex)
    {
        Logger.Debug($"{nameof(PlayerMock)}.{nameof(OnHotbarSlotPressed)} requested skill launch from hotbar slot {slotIndex}.");

        // Later:
        // SkillController.RequestLaunchSkill(slotIndex);
    }
}
