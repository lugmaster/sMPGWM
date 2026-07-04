using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Static condition metadata registry with deterministic ancestor and descendant closure helpers.
/// </summary>
public static class ConditionRegistry
{
    private static readonly ConditionDefinition[] Definitions =
    [
        new(LivingEntityCondition.Dead, LivingEntityCondition.None, true, true, false, "Entity is dead."),
        new(LivingEntityCondition.Movement, LivingEntityCondition.None, true, false, true, "Movement related state or effect."),
        new(LivingEntityCondition.Immobilized, LivingEntityCondition.Movement, true, true, true, "Entity cannot move."),
        new(LivingEntityCondition.Invulnerable, LivingEntityCondition.None, true, true, false, "Entity cannot take normal damage."),
        new(LivingEntityCondition.Untargetable, LivingEntityCondition.None, true, true, false, "Entity cannot be targeted."),
        new(LivingEntityCondition.Energy, LivingEntityCondition.None, true, false, true, "Energy related state or effect."),
        new(LivingEntityCondition.EnergyDepleted, LivingEntityCondition.Energy, true, true, true, "Entity has no energy remaining."),
        new(LivingEntityCondition.Invisible, LivingEntityCondition.None, true, true, false, "Entity is invisible."),
        new(LivingEntityCondition.Shield, LivingEntityCondition.None, true, false, true, "Shield related effect."),
        new(LivingEntityCondition.Survival, LivingEntityCondition.None, true, false, true, "Survival related state or effect."),
        new(LivingEntityCondition.Buff, LivingEntityCondition.None, true, false, true, "Beneficial effect."),
        new(LivingEntityCondition.HealOverTime, LivingEntityCondition.Buff, true, false, true, "Healing over time effect."),
        new(LivingEntityCondition.Debuff, LivingEntityCondition.None, true, false, true, "Harmful effect."),
        new(LivingEntityCondition.DamageOverTime, LivingEntityCondition.Debuff, true, false, true, "Damage over time effect."),
        new(LivingEntityCondition.Poison, LivingEntityCondition.DamageOverTime, true, true, true, "Poison effect."),
        new(LivingEntityCondition.Burn, LivingEntityCondition.DamageOverTime, true, true, true, "Burn effect."),
        new(LivingEntityCondition.Bleed, LivingEntityCondition.DamageOverTime, true, true, true, "Bleed effect."),
        new(LivingEntityCondition.Permanent, LivingEntityCondition.None, true, false, true, "Permanent effect."),
        new(LivingEntityCondition.Internal, LivingEntityCondition.None, false, false, true, "Internal engine effect.")
    ];

    private static readonly ulong[] ParentByBit = new ulong[64];
    private static readonly ulong[] AncestorClosureByBit = new ulong[64];
    private static readonly ulong[] DescendantClosureByBit = new ulong[64];
    private static readonly ulong KnownConditionMask;

    static ConditionRegistry()
    {
        for (var i = 0; i < Definitions.Length; i++)
        {
            var condition = (ulong)Definitions[i].Condition;
            KnownConditionMask |= condition;
            if (condition == 0)
                continue;

            ParentByBit[BitIndex(condition)] = (ulong)Definitions[i].ParentCondition;
        }

        for (var i = 0; i < Definitions.Length; i++)
        {
            var condition = (ulong)Definitions[i].Condition;
            if (condition == 0)
                continue;

            var conditionBit = BitIndex(condition);
            var ancestorMask = 0UL;
            var parent = ParentByBit[conditionBit];
            while (parent != 0)
            {
                ancestorMask |= parent;
                parent = ParentByBit[BitIndex(parent)];
            }

            AncestorClosureByBit[conditionBit] = ancestorMask;

            var cursor = ParentByBit[conditionBit];
            while (cursor != 0)
            {
                DescendantClosureByBit[BitIndex(cursor)] |= condition;
                cursor = ParentByBit[BitIndex(cursor)];
            }
        }
    }

    /// <summary>
    /// Gets all registered condition definitions in deterministic order.
    /// </summary>
    public static ReadOnlySpan<ConditionDefinition> All => Definitions;

    /// <summary>
    /// Returns true when the supplied condition mask contains only known condition bits.
    /// </summary>
    public static bool IsKnown(LivingEntityCondition condition)
    {
        var mask = (ulong)condition;
        return (mask & ~KnownConditionMask) == 0;
    }

    /// <summary>
    /// Builds the ancestor-condition closure for explicit condition bits.
    /// </summary>
    public static ulong BuildAncestorClosure(ulong explicitFlags)
    {
        var result = 0UL;
        var mask = explicitFlags;
        while (mask != 0)
        {
            var bit = mask & (~mask + 1);
            result |= AncestorClosureByBit[BitIndex(bit)];
            mask &= ~bit;
        }

        return result;
    }

    /// <summary>
    /// Builds effective condition flags by adding all ancestors to explicit flags.
    /// </summary>
    public static ulong BuildEffectiveFlags(ulong explicitFlags)
    {
        return explicitFlags | BuildAncestorClosure(explicitFlags);
    }

    /// <summary>
    /// Expands a condition mask to include every descendant condition.
    /// </summary>
    public static ulong ExpandWithDescendants(LivingEntityCondition condition)
    {
        return ExpandWithDescendants((ulong)condition);
    }

    /// <summary>
    /// Expands a raw condition mask to include every descendant condition.
    /// </summary>
    public static ulong ExpandWithDescendants(ulong conditionMask)
    {
        var result = conditionMask;
        var mask = conditionMask;
        while (mask != 0)
        {
            var bit = mask & (~mask + 1);
            result |= DescendantClosureByBit[BitIndex(bit)];
            mask &= ~bit;
        }

        return result;
    }

    private static int BitIndex(ulong singleBit)
    {
        var index = 0;
        while ((singleBit >>= 1) != 0)
            index++;
        return index;
    }
}
