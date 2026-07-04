using System;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Complete mutable simulation state for one living entity at a network tick.
/// </summary>
public sealed class LivingEntityState
{
    /// <summary>
    /// Gets the number of stats represented by the fixed base, max, and current arrays.
    /// </summary>
    public const int StatCount = (int)StatType.WaterGeneration + 1;

    private ulong _explicitConditionFlags;
    private ulong _derivedConditionFlags;
    private ulong _effectiveConditionFlags;

    /// <summary>
    /// Creates a living-entity state with fixed stat arrays and active-effect capacity.
    /// </summary>
    public LivingEntityState(int activeEffectCapacity = 35)
    {
        if (activeEffectCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(activeEffectCapacity), "Active effect capacity must be positive.");

        BaseValues = new float[StatCount];
        MaxValues = new float[StatCount];
        CurrentValues = new float[StatCount];
        ActiveEffects = new ActiveEffect[activeEffectCapacity];
    }

    /// <summary>
    /// Gets or sets the stable simulation entity id.
    /// </summary>
    public long EntityId { get; set; }

    /// <summary>
    /// Gets or sets the fast-lane network tick represented by this state.
    /// </summary>
    public long NetworkTick { get; set; }

    /// <summary>
    /// Gets the base stat values indexed by <see cref="StatType"/>.
    /// </summary>
    public float[] BaseValues { get; }

    /// <summary>
    /// Gets the resolved maximum stat values indexed by <see cref="StatType"/>.
    /// </summary>
    public float[] MaxValues { get; }

    /// <summary>
    /// Gets the current stat values indexed by <see cref="StatType"/>.
    /// </summary>
    public float[] CurrentValues { get; }

    /// <summary>
    /// Gets the fixed active-effect buffer owned by this entity state.
    /// </summary>
    public ActiveEffect[] ActiveEffects { get; }

    /// <summary>
    /// Gets the number of occupied entries in <see cref="ActiveEffects"/>.
    /// </summary>
    public int ActiveEffectCount { get; internal set; }

    /// <summary>
    /// Gets the bit mask of stats changed since the previous delta baseline.
    /// </summary>
    public ulong DirtyStats { get; internal set; }

    /// <summary>
    /// Gets whether the effective condition mask changed since the previous delta baseline.
    /// </summary>
    public bool DirtyConditions { get; internal set; }

    /// <summary>
    /// Gets whether active effects changed since the previous delta baseline.
    /// </summary>
    public bool DirtyEffects { get; internal set; }

    /// <summary>
    /// Gets the number of effects rejected because the fixed active-effect capacity was full.
    /// </summary>
    public int CapacityOverflowCount { get; internal set; }

    /// <summary>
    /// Gets or sets the broad living-entity owner type used for startup capacity choices.
    /// </summary>
    public LivingEntityType LivingEntityType { get; set; }

    /// <summary>
    /// Gets or sets the legacy integer living-entity id mapped to <see cref="EntityId"/>.
    /// </summary>
    public int LivingEntityId
    {
        get => (int)EntityId;
        set => EntityId = value;
    }

    /// <summary>
    /// Gets the explicit condition flags set directly by effects or input state.
    /// </summary>
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

    /// <summary>
    /// Gets the condition flags derived from active effects and inferred state.
    /// </summary>
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

    /// <summary>
    /// Gets the final effective condition flags, including ancestor closure.
    /// </summary>
    public ulong ConditionFlags => _effectiveConditionFlags;

    /// <summary>
    /// Gets or sets the effective condition mask as the public condition enum.
    /// </summary>
    public LivingEntityCondition Condition
    {
        get => (LivingEntityCondition)_effectiveConditionFlags;
        set => SetCondition(value);
    }

    /// <summary>
    /// Gets the fixed active-effect capacity for this state.
    /// </summary>
    public int ActiveEffectCapacity => ActiveEffects.Length;

    /// <summary>
    /// Gets or sets the health stat snapshot.
    /// </summary>
    public Stat Health
    {
        get => GetStatByType(StatType.Health);
        set => SetStat(StatType.Health, value);
    }

    /// <summary>
    /// Gets or sets the energy-storage stat snapshot.
    /// </summary>
    public Stat EnergyStorage
    {
        get => GetStatByType(StatType.EnergyStorage);
        set => SetStat(StatType.EnergyStorage, value);
    }

    /// <summary>
    /// Gets or sets the legacy energy alias for <see cref="EnergyStorage"/>.
    /// </summary>
    public Stat Energy
    {
        get => EnergyStorage;
        set => EnergyStorage = value;
    }

    /// <summary>
    /// Gets or sets the shield stat snapshot.
    /// </summary>
    public Stat Shield
    {
        get => GetStatByType(StatType.Shield);
        set => SetStat(StatType.Shield, value);
    }

    /// <summary>
    /// Gets or sets the armor stat snapshot.
    /// </summary>
    public Stat Armor
    {
        get => GetStatByType(StatType.Armor);
        set => SetStat(StatType.Armor, value);
    }

    /// <summary>
    /// Gets or sets the speed stat snapshot.
    /// </summary>
    public Stat Speed
    {
        get => GetStatByType(StatType.Speed);
        set => SetStat(StatType.Speed, value);
    }

    /// <summary>
    /// Gets or sets the acceleration stat snapshot.
    /// </summary>
    public Stat Acceleration
    {
        get => GetStatByType(StatType.Acceleration);
        set => SetStat(StatType.Acceleration, value);
    }

    /// <summary>
    /// Gets or sets the oxygen-storage stat snapshot.
    /// </summary>
    public Stat OxygenStorage
    {
        get => GetStatByType(StatType.OxygenStorage);
        set => SetStat(StatType.OxygenStorage, value);
    }

    /// <summary>
    /// Gets or sets the food-storage stat snapshot.
    /// </summary>
    public Stat FoodStorage
    {
        get => GetStatByType(StatType.FoodStorage);
        set => SetStat(StatType.FoodStorage, value);
    }

    /// <summary>
    /// Gets or sets the water-storage stat snapshot.
    /// </summary>
    public Stat WaterStorage
    {
        get => GetStatByType(StatType.WaterStorage);
        set => SetStat(StatType.WaterStorage, value);
    }

    /// <summary>
    /// Gets or sets the critical-hit-chance stat snapshot.
    /// </summary>
    public Stat CriticalHitChance
    {
        get => GetStatByType(StatType.CriticalHitChance);
        set => SetStat(StatType.CriticalHitChance, value);
    }

    /// <summary>
    /// Gets or sets the critical-hit-bonus-damage stat snapshot.
    /// </summary>
    public Stat CriticalHitBonusDamage
    {
        get => GetStatByType(StatType.CriticalHitBonusDamage);
        set => SetStat(StatType.CriticalHitBonusDamage, value);
    }

    /// <summary>
    /// Gets or sets the attack-speed stat snapshot.
    /// </summary>
    public Stat AttackSpeed
    {
        get => GetStatByType(StatType.AttackSpeed);
        set => SetStat(StatType.AttackSpeed, value);
    }

    /// <summary>
    /// Gets or sets the health-regeneration stat snapshot.
    /// </summary>
    public Stat HealthRegeneration
    {
        get => GetStatByType(StatType.HealthRegeneration);
        set => SetStat(StatType.HealthRegeneration, value);
    }

    /// <summary>
    /// Gets or sets the energy-production stat snapshot.
    /// </summary>
    public Stat EnergyProduction
    {
        get => GetStatByType(StatType.EnergyProduction);
        set => SetStat(StatType.EnergyProduction, value);
    }

    /// <summary>
    /// Gets or sets the legacy energy-regeneration alias for <see cref="EnergyProduction"/>.
    /// </summary>
    public Stat EnergyRegeneration
    {
        get => EnergyProduction;
        set => EnergyProduction = value;
    }

    /// <summary>
    /// Gets or sets the energy-consumption stat snapshot.
    /// </summary>
    public Stat EnergyConsumption
    {
        get => GetStatByType(StatType.EnergyConsumption);
        set => SetStat(StatType.EnergyConsumption, value);
    }

    /// <summary>
    /// Gets or sets the shield-regeneration stat snapshot.
    /// </summary>
    public Stat ShieldRegeneration
    {
        get => GetStatByType(StatType.ShieldRegeneration);
        set => SetStat(StatType.ShieldRegeneration, value);
    }

    /// <summary>
    /// Gets or sets the shield-recharge-cooldown stat snapshot.
    /// </summary>
    public Stat ShieldRechargeCooldown
    {
        get => GetStatByType(StatType.ShieldRechargeCooldown);
        set => SetStat(StatType.ShieldRechargeCooldown, value);
    }

    /// <summary>
    /// Gets or sets the oxygen-consumption stat snapshot.
    /// </summary>
    public Stat OxygenConsumption
    {
        get => GetStatByType(StatType.OxygenConsumption);
        set => SetStat(StatType.OxygenConsumption, value);
    }

    /// <summary>
    /// Gets or sets the oxygen-generation stat snapshot.
    /// </summary>
    public Stat OxygenGeneration
    {
        get => GetStatByType(StatType.OxygenGeneration);
        set => SetStat(StatType.OxygenGeneration, value);
    }

    /// <summary>
    /// Gets or sets the food-consumption stat snapshot.
    /// </summary>
    public Stat FoodConsumption
    {
        get => GetStatByType(StatType.FoodConsumption);
        set => SetStat(StatType.FoodConsumption, value);
    }

    /// <summary>
    /// Gets or sets the food-generation stat snapshot.
    /// </summary>
    public Stat FoodGeneration
    {
        get => GetStatByType(StatType.FoodGeneration);
        set => SetStat(StatType.FoodGeneration, value);
    }

    /// <summary>
    /// Gets or sets the water-consumption stat snapshot.
    /// </summary>
    public Stat WaterConsumption
    {
        get => GetStatByType(StatType.WaterConsumption);
        set => SetStat(StatType.WaterConsumption, value);
    }

    /// <summary>
    /// Gets or sets the water-generation stat snapshot.
    /// </summary>
    public Stat WaterGeneration
    {
        get => GetStatByType(StatType.WaterGeneration);
        set => SetStat(StatType.WaterGeneration, value);
    }

    /// <summary>
    /// Gets a read-only stat snapshot for the requested stat type.
    /// </summary>
    public Stat GetStatByType(StatType type)
    {
        var index = ValidateStatType(type);
        return new Stat(BaseValues[index], MaxValues[index], CurrentValues[index]);
    }

    /// <summary>
    /// Sets the base, max, and current values for the requested stat type.
    /// </summary>
    public void SetStat(StatType type, Stat stat)
    {
        var index = ValidateStatType(type);
        BaseValues[index] = stat.BaseValue;
        MaxValues[index] = stat.MaxValue;
        CurrentValues[index] = stat.CurrentValue;
        MarkStatDirty(type);
    }

    /// <summary>
    /// Initializes a stat with a base value and optional current value.
    /// </summary>
    public void InitializeStat(StatType type, float baseValue, float? currentValue = null)
    {
        var max = MathF.Max(0f, baseValue);
        SetStat(type, new Stat(max, max, currentValue ?? max));
    }

    /// <summary>
    /// Sets explicit base, max, and current values for a stat.
    /// </summary>
    public void SetBaseMaxCurrent(StatType type, float baseValue, float maxValue, float currentValue)
    {
        SetStat(type, new Stat(baseValue, maxValue, currentValue));
    }

    /// <summary>
    /// Replaces explicit conditions and rebuilds the effective condition mask.
    /// </summary>
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

    /// <summary>
    /// Clears stat, condition, and effect dirty markers.
    /// </summary>
    public void ClearDirty()
    {
        DirtyStats = 0;
        DirtyConditions = false;
        DirtyEffects = false;
    }

    /// <summary>
    /// Creates a deep simulation copy of this state.
    /// </summary>
    public LivingEntityState DeepCopy()
    {
        var copy = new LivingEntityState(ActiveEffects.Length);
        CopyTo(copy);
        return copy;
    }

    /// <summary>
    /// Copies this state into an existing target state with sufficient effect capacity.
    /// </summary>
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

    /// <summary>
    /// Replaces this state with values copied from another state.
    /// </summary>
    public void CopyFrom(LivingEntityState source)
    {
        source.CopyTo(this);
    }

    /// <summary>
    /// Returns true when the effective conditions allow targeting this entity.
    /// </summary>
    public bool IsTargetable()
    {
        return Condition.IsTargetAble();
    }

    /// <summary>
    /// Returns true when the effective conditions do not include death.
    /// </summary>
    public bool IsAlive()
    {
        return !Condition.Contains(LivingEntityCondition.Dead);
    }

    /// <summary>
    /// Gets the current movement speed; afterburner-specific stats are out of current PRD scope.
    /// </summary>
    public float GetCurrentSpeed(bool isAfterBurnerPressed)
    {
        return Speed.CurrentValue;
    }

    /// <summary>
    /// Gets the current movement acceleration; afterburner-specific stats are out of current PRD scope.
    /// </summary>
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
