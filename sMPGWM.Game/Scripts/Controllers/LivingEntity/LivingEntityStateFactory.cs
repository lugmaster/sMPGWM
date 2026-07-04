using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Creates initial living-entity states with fixed capacities and default stat values.
/// </summary>
public static class LivingEntityStateFactory
{
    private const int FirstAgentLivingEntityId = 1_000_000;

    /// <summary>
    /// Creates a default player-controlled living-entity state for the supplied peer id.
    /// </summary>
    public static LivingEntityState CreateDefaultPlayerState(int peerId)
    {
        var state = new LivingEntityState(StateEngine.Config.PlayerActiveEffectCapacity)
        {
            Condition = LivingEntityCondition.None,
            LivingEntityType = LivingEntityType.MpController,
            LivingEntityId = peerId
        };

        InitializeSharedDefaults(state);
        return state;
    }

    /// <summary>
    /// Creates a default agent living-entity state with an id in the reserved agent range.
    /// </summary>
    public static LivingEntityState CreateDefaultAgentState(int agentId)
    {
        var state = new LivingEntityState(StateEngine.Config.SimpleNpcActiveEffectCapacity)
        {
            Condition = LivingEntityCondition.None,
            LivingEntityType = LivingEntityType.AgentController,
            LivingEntityId = ToAgentLivingEntityId(agentId)
        };

        InitializeSharedDefaults(state);
        return state;
    }

    /// <summary>
    /// Converts an agent id into the reserved living-entity id range.
    /// </summary>
    public static int ToAgentLivingEntityId(int agentId)
    {
        if (agentId < 0)
            throw new ArgumentOutOfRangeException(nameof(agentId), "Agent id must not be negative.");

        return FirstAgentLivingEntityId + agentId;
    }

    private static void InitializeSharedDefaults(LivingEntityState state)
    {
        state.InitializeStat(StatType.Health, 100f);
        state.InitializeStat(StatType.EnergyStorage, 100f);
        state.InitializeStat(StatType.Shield, 100f);
        state.InitializeStat(StatType.Armor, 0f);
        state.InitializeStat(StatType.Speed, 0f);
        state.InitializeStat(StatType.Acceleration, 0f);
        state.InitializeStat(StatType.OxygenStorage, 100f);
        state.InitializeStat(StatType.FoodStorage, 100f);
        state.InitializeStat(StatType.WaterStorage, 100f);
        state.InitializeStat(StatType.CriticalHitChance, 0f);
        state.InitializeStat(StatType.CriticalHitBonusDamage, 0f);
        state.InitializeStat(StatType.AttackSpeed, 0f);
        state.InitializeStat(StatType.HealthRegeneration, 0f);
        state.InitializeStat(StatType.EnergyProduction, 0f);
        state.InitializeStat(StatType.EnergyConsumption, 0f);
        state.InitializeStat(StatType.ShieldRegeneration, 0f);
        state.InitializeStat(StatType.ShieldRechargeCooldown, 0f);
        state.InitializeStat(StatType.OxygenConsumption, 0f);
        state.InitializeStat(StatType.OxygenGeneration, 0f);
        state.InitializeStat(StatType.FoodConsumption, 0f);
        state.InitializeStat(StatType.FoodGeneration, 0f);
        state.InitializeStat(StatType.WaterConsumption, 0f);
        state.InitializeStat(StatType.WaterGeneration, 0f);
        state.ClearDirty();
    }
}
