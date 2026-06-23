using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public static class LivingEntityStateFactory
{
    private const int FirstAgentLivingEntityId = 1_000_000;

    public static LivingEntityState CreateDefaultPlayerState(int peerId)
    {
        return new LivingEntityState
        {
            Condition = LivingEntityCondition.None,
            Health = new Stat(100f),
            HealthRegeneration = new Stat(0f),
            Shield = new Stat(100f),
            ShieldRegeneration = new Stat(0f),
            Energy = new Stat(100f),
            EnergyRegeneration = new Stat(0f),
            Speed = new Stat(0f),
            Acceleration = new Stat(0f),
            AfterBurnerSpeed = new Stat(0f),
            AfterBurnerAcceleration = new Stat(0f),
            LivingEntityType = LivingEntityType.MpController,
            LivingEntityId = peerId
        };
    }

    public static LivingEntityState CreateDefaultAgentState(int agentId)
    {
        return new LivingEntityState
        {
            Condition = LivingEntityCondition.None,
            Health = new Stat(100f),
            HealthRegeneration = new Stat(0f),
            Shield = new Stat(100f),
            ShieldRegeneration = new Stat(0f),
            Energy = new Stat(100f),
            EnergyRegeneration = new Stat(0f),
            Speed = new Stat(0f),
            Acceleration = new Stat(0f),
            AfterBurnerSpeed = new Stat(0f),
            AfterBurnerAcceleration = new Stat(0f),
            LivingEntityType = LivingEntityType.AgentController,
            LivingEntityId = ToAgentLivingEntityId(agentId)
        };
    }

    public static int ToAgentLivingEntityId(int agentId)
    {
        if (agentId < 0)
            throw new ArgumentOutOfRangeException(nameof(agentId), "Agent id must not be negative.");

        return FirstAgentLivingEntityId + agentId;
    }
}
