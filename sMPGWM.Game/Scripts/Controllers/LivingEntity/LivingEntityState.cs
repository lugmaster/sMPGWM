using System;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityState
{
    public const int StatCount = (int)StatType.WaterGeneration + 1;

    private ulong _explicitConditionFlags;
    private ulong _derivedConditionFlags;
    private ulong _effectiveConditionFlags;

    public LivingEntityState(int activeEffectCapacity = 35)
    {
        if (activeEffectCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(activeEffectCapacity), "Active effect capacity must be positive.");

        BaseValues = new float[StatCount];
        MaxValues = new float[StatCount];
        CurrentValues = new float[StatCount];
        ActiveEffects = new ActiveEffect[activeEffectCapacity];
    }

    public long EntityId { get; set; }
    public long NetworkTick { get; set; }
    public float[] BaseValues { get; }
    public float[] MaxValues { get; }
    public float[] CurrentValues { get; }
    public ActiveEffect[] ActiveEffects { get; }
    public int ActiveEffectCount { get; internal set; }
    public ulong DirtyStats { get; internal set; }
    public bool DirtyConditions { get; internal set; }
    public bool DirtyEffects { get; internal set; }
    public int CapacityOverflowCount { get; internal set; }
    public LivingEntityType LivingEntityType { get; set; }

    public int LivingEntityId
    {
        get => (int)EntityId;
        set => EntityId = value;
    }

    public ulong ExplicitConditionFlags
    {
        get => _explicitConditionFlags;
        internal set
        {
            if (_explicitConditionFlags == value)
                return;

            _explicitConditionFlags = value;
            RebuildEffectiveConditionFlags();
        }
    }

    public ulong DerivedConditionFlags
    {
        get => _derivedConditionFlags;
        internal set
        {
            if (_derivedConditionFlags == value)
                return;

            _derivedConditionFlags = value;
            RebuildEffectiveConditionFlags();
        }
    }

    public ulong ConditionFlags => _effectiveConditionFlags;

    public LivingEntityCondition Condition
    {
        get => (LivingEntityCondition)_effectiveConditionFlags;
        set => SetCondition(value);
    }

    public int ActiveEffectCapacity => ActiveEffects.Length;

    public Stat Health
    {
        get => GetStatByType(StatType.Health);
        set => SetStat(StatType.Health, value);
    }

    public Stat EnergyStorage
    {
        get => GetStatByType(StatType.EnergyStorage);
        set => SetStat(StatType.EnergyStorage, value);
    }

    public Stat Energy
    {
        get => EnergyStorage;
        set => EnergyStorage = value;
    }

    public Stat Shield
    {
        get => GetStatByType(StatType.Shield);
        set => SetStat(StatType.Shield, value);
    }

    public Stat Armor
    {
        get => GetStatByType(StatType.Armor);
        set => SetStat(StatType.Armor, value);
    }

    public Stat Speed
    {
        get => GetStatByType(StatType.Speed);
        set => SetStat(StatType.Speed, value);
    }

    public Stat Acceleration
    {
        get => GetStatByType(StatType.Acceleration);
        set => SetStat(StatType.Acceleration, value);
    }

    public Stat OxygenStorage
    {
        get => GetStatByType(StatType.OxygenStorage);
        set => SetStat(StatType.OxygenStorage, value);
    }

    public Stat FoodStorage
    {
        get => GetStatByType(StatType.FoodStorage);
        set => SetStat(StatType.FoodStorage, value);
    }

    public Stat WaterStorage
    {
        get => GetStatByType(StatType.WaterStorage);
        set => SetStat(StatType.WaterStorage, value);
    }

    public Stat CriticalHitChance
    {
        get => GetStatByType(StatType.CriticalHitChance);
        set => SetStat(StatType.CriticalHitChance, value);
    }

    public Stat CriticalHitBonusDamage
    {
        get => GetStatByType(StatType.CriticalHitBonusDamage);
        set => SetStat(StatType.CriticalHitBonusDamage, value);
    }

    public Stat AttackSpeed
    {
        get => GetStatByType(StatType.AttackSpeed);
        set => SetStat(StatType.AttackSpeed, value);
    }

    public Stat HealthRegeneration
    {
        get => GetStatByType(StatType.HealthRegeneration);
        set => SetStat(StatType.HealthRegeneration, value);
    }

    public Stat EnergyProduction
    {
        get => GetStatByType(StatType.EnergyProduction);
        set => SetStat(StatType.EnergyProduction, value);
    }

    public Stat EnergyRegeneration
    {
        get => EnergyProduction;
        set => EnergyProduction = value;
    }

    public Stat EnergyConsumption
    {
        get => GetStatByType(StatType.EnergyConsumption);
        set => SetStat(StatType.EnergyConsumption, value);
    }

    public Stat ShieldRegeneration
    {
        get => GetStatByType(StatType.ShieldRegeneration);
        set => SetStat(StatType.ShieldRegeneration, value);
    }

    public Stat ShieldRechargeCooldown
    {
        get => GetStatByType(StatType.ShieldRechargeCooldown);
        set => SetStat(StatType.ShieldRechargeCooldown, value);
    }

    public Stat OxygenConsumption
    {
        get => GetStatByType(StatType.OxygenConsumption);
        set => SetStat(StatType.OxygenConsumption, value);
    }

    public Stat OxygenGeneration
    {
        get => GetStatByType(StatType.OxygenGeneration);
        set => SetStat(StatType.OxygenGeneration, value);
    }

    public Stat FoodConsumption
    {
        get => GetStatByType(StatType.FoodConsumption);
        set => SetStat(StatType.FoodConsumption, value);
    }

    public Stat FoodGeneration
    {
        get => GetStatByType(StatType.FoodGeneration);
        set => SetStat(StatType.FoodGeneration, value);
    }

    public Stat WaterConsumption
    {
        get => GetStatByType(StatType.WaterConsumption);
        set => SetStat(StatType.WaterConsumption, value);
    }

    public Stat WaterGeneration
    {
        get => GetStatByType(StatType.WaterGeneration);
        set => SetStat(StatType.WaterGeneration, value);
    }

    public Stat GetStatByType(StatType type)
    {
        var index = ValidateStatType(type);
        return new Stat(BaseValues[index], MaxValues[index], CurrentValues[index]);
    }

    public void SetStat(StatType type, Stat stat)
    {
        var index = ValidateStatType(type);
        BaseValues[index] = stat.BaseValue;
        MaxValues[index] = stat.MaxValue;
        CurrentValues[index] = stat.CurrentValue;
        MarkStatDirty(type);
    }

    public void InitializeStat(StatType type, float baseValue, float? currentValue = null)
    {
        var max = MathF.Max(0f, baseValue);
        SetStat(type, new Stat(max, max, currentValue ?? max));
    }

    public void SetBaseMaxCurrent(StatType type, float baseValue, float maxValue, float currentValue)
    {
        SetStat(type, new Stat(baseValue, maxValue, currentValue));
    }

    public void SetCondition(LivingEntityCondition condition)
    {
        var previous = _effectiveConditionFlags;
        _explicitConditionFlags = (ulong)condition;
        _derivedConditionFlags = 0;
        RebuildEffectiveConditionFlags();
        DirtyConditions |= previous != _effectiveConditionFlags;
    }

    internal void AddExplicitCondition(LivingEntityCondition condition)
    {
        var previous = _effectiveConditionFlags;
        _explicitConditionFlags |= ConditionRegistry.ExpandWithDescendants(condition) & (ulong)condition;
        RebuildEffectiveConditionFlags();
        DirtyConditions |= previous != _effectiveConditionFlags;
    }

    internal void RemoveExplicitConditionCascade(LivingEntityCondition condition)
    {
        var previous = _effectiveConditionFlags;
        var removalMask = ConditionRegistry.ExpandWithDescendants(condition);
        _explicitConditionFlags &= ~removalMask;
        RebuildEffectiveConditionFlags();
        DirtyConditions |= previous != _effectiveConditionFlags;
    }

    internal void SetDerivedConditionFlags(ulong derivedFlags)
    {
        var previous = _effectiveConditionFlags;
        _derivedConditionFlags = derivedFlags;
        RebuildEffectiveConditionFlags();
        DirtyConditions |= previous != _effectiveConditionFlags;
    }

    internal void SetEffectiveConditionFlagsFromDelta(ulong conditionFlags)
    {
        _explicitConditionFlags = conditionFlags;
        _derivedConditionFlags = 0;
        _effectiveConditionFlags = conditionFlags;
        DirtyConditions = false;
    }

    public void ClearDirty()
    {
        DirtyStats = 0;
        DirtyConditions = false;
        DirtyEffects = false;
    }

    public LivingEntityState DeepCopy()
    {
        var copy = new LivingEntityState(ActiveEffects.Length);
        CopyTo(copy);
        return copy;
    }

    public void CopyTo(LivingEntityState target)
    {
        if (target.ActiveEffects.Length < ActiveEffectCount)
            throw new InvalidOperationException("Target state has lower active effect capacity than source state.");

        target.EntityId = EntityId;
        target.NetworkTick = NetworkTick;
        target.LivingEntityType = LivingEntityType;
        Array.Copy(BaseValues, target.BaseValues, StatCount);
        Array.Copy(MaxValues, target.MaxValues, StatCount);
        Array.Copy(CurrentValues, target.CurrentValues, StatCount);
        target._explicitConditionFlags = _explicitConditionFlags;
        target._derivedConditionFlags = _derivedConditionFlags;
        target._effectiveConditionFlags = _effectiveConditionFlags;
        Array.Copy(ActiveEffects, target.ActiveEffects, ActiveEffectCount);
        target.ActiveEffectCount = ActiveEffectCount;
        target.DirtyStats = DirtyStats;
        target.DirtyConditions = DirtyConditions;
        target.DirtyEffects = DirtyEffects;
        target.CapacityOverflowCount = CapacityOverflowCount;
    }

    public void CopyFrom(LivingEntityState source)
    {
        source.CopyTo(this);
    }

    public bool IsTargetable()
    {
        return Condition.IsTargetAble();
    }

    public bool IsAlive()
    {
        return !Condition.Contains(LivingEntityCondition.Dead);
    }

    public float GetCurrentSpeed(bool isAfterBurnerPressed)
    {
        return Speed.CurrentValue;
    }

    public float GetCurrentAcceleration(bool isAfterBurnerPressed)
    {
        return Acceleration.CurrentValue;
    }

    internal void MarkStatDirty(StatType type)
    {
        DirtyStats |= 1UL << ValidateStatType(type);
    }

    internal static int ValidateStatType(StatType type)
    {
        var index = (int)type;
        if (index < 0 || index >= StatCount)
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown StatType.");

        return index;
    }

    private void RebuildEffectiveConditionFlags()
    {
        _effectiveConditionFlags = ConditionRegistry.BuildEffectiveFlags(_explicitConditionFlags | _derivedConditionFlags);
    }
}
