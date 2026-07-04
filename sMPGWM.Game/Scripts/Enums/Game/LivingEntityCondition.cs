using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Enums.Game;

/// <summary>
/// Shared condition vocabulary for entity state, effect classification, blocking, and removal matching.
/// </summary>
[Flags]
public enum LivingEntityCondition : ulong
{
    None = 0,
    Dead = 1UL << 0,
    Immobilized = 1UL << 1,
    Invulnerable = 1UL << 2,
    Untargetable = 1UL << 3,
    EnergyDepleted = 1UL << 4,
    Invisible = 1UL << 5,
    Poison = 1UL << 6,
    Burn = 1UL << 7,
    Bleed = 1UL << 8,
    Buff = 1UL << 9,
    Debuff = 1UL << 10,
    DamageOverTime = 1UL << 11,
    HealOverTime = 1UL << 12,
    Movement = 1UL << 13,
    Energy = 1UL << 14,
    Shield = 1UL << 15,
    Survival = 1UL << 16,
    Permanent = 1UL << 17,
    Internal = 1UL << 18
}

/// <summary>
/// Provides flag helpers for working with living-entity condition masks.
/// </summary>
public static class LivingEntityConditionExtensions
{
    /// <summary>
    /// Lists every non-empty condition flag in deterministic declaration order.
    /// </summary>
    public static readonly LivingEntityCondition[] Flags =
    [
        LivingEntityCondition.Dead,
        LivingEntityCondition.Immobilized,
        LivingEntityCondition.Invulnerable,
        LivingEntityCondition.Untargetable,
        LivingEntityCondition.EnergyDepleted,
        LivingEntityCondition.Invisible,
        LivingEntityCondition.Poison,
        LivingEntityCondition.Burn,
        LivingEntityCondition.Bleed,
        LivingEntityCondition.Buff,
        LivingEntityCondition.Debuff,
        LivingEntityCondition.DamageOverTime,
        LivingEntityCondition.HealOverTime,
        LivingEntityCondition.Movement,
        LivingEntityCondition.Energy,
        LivingEntityCondition.Shield,
        LivingEntityCondition.Survival,
        LivingEntityCondition.Permanent,
        LivingEntityCondition.Internal
    ];

    /// <summary>
    /// Returns a condition mask with the specified flag added.
    /// </summary>
    public static LivingEntityCondition Add(this LivingEntityCondition state, LivingEntityCondition flag)
    {
        return state | flag;
    }

    /// <summary>
    /// Returns a condition mask with all specified flags added.
    /// </summary>
    public static LivingEntityCondition Add(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (var i = 0; i < flags.Length; i++)
            state |= flags[i];
        return state;
    }

    /// <summary>
    /// Returns a condition mask with the specified flag removed.
    /// </summary>
    public static LivingEntityCondition Remove(this LivingEntityCondition state, LivingEntityCondition flag)
    {
        return state & ~flag;
    }

    /// <summary>
    /// Returns a condition mask with all specified flags removed.
    /// </summary>
    public static LivingEntityCondition Remove(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (var i = 0; i < flags.Length; i++)
            state &= ~flags[i];
        return state;
    }

    /// <summary>
    /// Returns true when all bits in the requested flag mask are present.
    /// </summary>
    public static bool Contains(this LivingEntityCondition state, LivingEntityCondition flag)
    {
        return (state & flag) == flag;
    }
    
    /// <summary>
    /// Returns true when none of the supplied condition flags are present.
    /// </summary>
    public static bool NotContains(this LivingEntityCondition state, params LivingEntityCondition[] flags)
    {
        for (var i = 0; i < flags.Length; i++)
        {
            if ((state & flags[i]) == flags[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true when the condition mask allows normal targeting.
    /// </summary>
    public static bool IsTargetAble(this LivingEntityCondition state)
    {
        return state.NotContains(LivingEntityCondition.Dead, LivingEntityCondition.Untargetable);
    }
    
    /// <summary>
    /// Formats active condition flags for debug output.
    /// </summary>
    public static string ToFlagListString(this LivingEntityCondition state)
    {
        var result = new List<string>();

        for (var i = 0; i < Flags.Length; i++)
        {
            var flag = Flags[i];
            if (state.Contains(flag))
                result.Add(flag.ToString());
        }

        return result.Count == 0 ? "[_]" : "[" + string.Join(", ", result) + "]";
    }

}
