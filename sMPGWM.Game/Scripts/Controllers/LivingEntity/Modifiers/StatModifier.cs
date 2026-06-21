using Godot.Collections;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class StatModifier(float value, ModifierType type)
{
    public StatModifier(Array values) : this((float)values[0], (ModifierType)(int)values[1])
    {
    }

    public float Value { get; } = value;
    public ModifierType Type { get; } = type;

    public Array ToVariant()
    {
        return new Array
        {
            Value,
            (int)Type
        };
    }
}