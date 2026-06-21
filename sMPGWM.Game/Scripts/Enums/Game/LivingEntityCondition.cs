using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Enums.Game;

[Flags]
public enum LivingEntityCondition
{
    None = 0,
    Dead = 1 << 1,
    Immobilized = 1 << 2,
    Invulnerable = 1 << 3,
    Untargetable = 1 << 4,
    EnergyDepleted = 1 << 5,
    Invisible = 1 << 6,
}

public static class LivingEntityConditionExtensions
{
    public static LivingEntityCondition Add(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (int i = 0; i < flags.Length; i++)
            state |= flags[i];
        return state;
    }


    public static LivingEntityCondition Remove(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (int i = 0; i < flags.Length; i++)
            state &= ~flags[i];
        return state;
    }

    public static bool Contains(this LivingEntityCondition state, LivingEntityCondition flag)
    {
        return (state & flag) == flag;
    }
    
    public static bool NotContains(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (var i = 0; i < flags.Length; i++)
        {
            if ((state & flags[i]) == flags[i])
                return false;
        }
        return true;
    }

    public static bool IsTargetAble(this LivingEntityCondition state)
    {
        return state.NotContains(LivingEntityCondition.Dead, LivingEntityCondition.Untargetable);
    }
    
    public static string ToFlagListString(this LivingEntityCondition state)
    {
        var activeFlags = Enum.GetValues<LivingEntityCondition>();
        var result = new List<string>();

        foreach (var flag in activeFlags)
        {
            if (state.HasFlag(flag))
                result.Add(flag.ToString());
        }

        return result.Count == 0 ? "[_]" : "[" + string.Join(", ", result) + "]";
    }

}