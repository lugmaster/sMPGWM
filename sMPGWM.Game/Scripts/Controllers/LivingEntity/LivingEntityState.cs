using System;
using Logger = sMPGWM.Scripts.Autoload.Logger;
using sMPGWM.Scripts.Enums.Game;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public class LivingEntityState
{
    private const int FullStateVariantCount = 13;
    private const int DeltaVariantCount = 3;
    private const int DeltaLivingEntityIdIndex = 0;
    private const int DeltaCurrentIndex = 1;
    private const int DeltaPermanentIndex = 2;
    private const int StatSlotCount = (int)StatType.AfterBurnerAcceleration + 1;

    public Stat Health;
    public Stat HealthRegeneration;
    public Stat Shield;
    public Stat ShieldRegeneration;
    public Stat Energy;
    public Stat EnergyRegeneration;
    public Stat Speed;
    public Stat Acceleration;
    public Stat AfterBurnerSpeed;
    public Stat AfterBurnerAcceleration;
    public int LivingEntityId;
    public LivingEntityType LivingEntityType;
    // private static bool IsHost => GlobalStateManager.PlayerController.IsHost;

    private LivingEntityCondition _condition;

    public LivingEntityCondition Condition
    {
        get => _condition;
        init => SetCondition(value);
    }

    public LivingEntityState()
    {
    }

    private LivingEntityState(
        LivingEntityCondition condition,
        Stat health,
        Stat healthRegen,
        Stat shield,
        Stat shieldRegen,
        Stat energy,
        Stat energyRegen,
        Stat speed,
        Stat acceleration,
        Stat afterBurnerSpeed,
        Stat afterBurnerAcceleration,
        LivingEntityType livingEntityType,
        int livingEntityId)
    {
        Condition = condition;
        Health = health;
        HealthRegeneration = healthRegen;
        Shield = shield;
        ShieldRegeneration = shieldRegen;
        Energy = energy;
        EnergyRegeneration = energyRegen;
        Speed = speed;
        Acceleration = acceleration;
        AfterBurnerSpeed = afterBurnerSpeed;
        AfterBurnerAcceleration = afterBurnerAcceleration;
        LivingEntityType = livingEntityType;
        LivingEntityId = livingEntityId;
    }

    public void UpdateFullState(GCol.Array livingEntityStateVariant)
    {
        if (livingEntityStateVariant.Count != FullStateVariantCount)
        {
            Logger.Error($"{nameof(LivingEntityState)}.{nameof(UpdateFullState)} received invalid variant count {livingEntityStateVariant.Count}.");
            throw new ArgumentException("Invalid livingEntityStateVariant format");
        }

        SetCondition((LivingEntityCondition)(int)livingEntityStateVariant[0]);
        Health = new Stat(livingEntityStateVariant[1].As<GCol.Array>());
        HealthRegeneration = new Stat(livingEntityStateVariant[2].As<GCol.Array>());
        Shield = new Stat(livingEntityStateVariant[3].As<GCol.Array>());
        ShieldRegeneration = new Stat(livingEntityStateVariant[4].As<GCol.Array>());
        Energy = new Stat(livingEntityStateVariant[5].As<GCol.Array>());
        EnergyRegeneration = new Stat(livingEntityStateVariant[6].As<GCol.Array>());
        Speed = new Stat(livingEntityStateVariant[7].As<GCol.Array>());
        Acceleration = new Stat(livingEntityStateVariant[8].As<GCol.Array>());
        AfterBurnerSpeed = new Stat(livingEntityStateVariant[9].As<GCol.Array>());
        AfterBurnerAcceleration = new Stat(livingEntityStateVariant[10].As<GCol.Array>());
        LivingEntityType = (LivingEntityType)(int)livingEntityStateVariant[11];
        LivingEntityId = livingEntityStateVariant[12].AsInt32();
    }

    public Stat GetStatByType(StatType type)
    {
        // Condition is not a Stat 
        return type switch
        {
            StatType.Health => Health,
            StatType.HealthRegeneration => HealthRegeneration,
            StatType.Shield => Shield,
            StatType.ShieldRegeneration => ShieldRegeneration,
            StatType.Energy => Energy,
            StatType.EnergyRegeneration => EnergyRegeneration,
            StatType.Speed => Speed,
            StatType.Acceleration => Acceleration,
            StatType.AfterBurnerSpeed => AfterBurnerSpeed,
            StatType.AfterBurnerAcceleration => AfterBurnerAcceleration,
            StatType.Condition => throw CreateConditionIsNotStatException(type),
            _ => throw CreateUnknownStatTypeException(type)
        };
    }

    // THIS SHOULD ONLY BE USED FROM STATSYNCHRONIZER!
    // Use this to change all bitflags. for adding or removing single bit conditions use AddCondition or RemoveCondition methods below
    internal void SetCondition(LivingEntityCondition newCondition)
    {
        if (_condition == newCondition) return;

        _condition = newCondition;
    }

    // Creates a new Condition from provided bitflag - must be a new one to trigger comparison
    internal void RemoveCondition(LivingEntityCondition conditionToRemove)
    {
        SetCondition(_condition.Remove(conditionToRemove));
    }

    // Creates a new Condition from provided bitflag - must be a new one to trigger comparison
    internal void AddCondition(LivingEntityCondition conditionToAdd)
    {
        SetCondition(_condition.Add(conditionToAdd));
    }

    internal void ReduceStatToZero(StatType type)
    {
        GetStatByType(type).DrainToZero();
    }

    public LivingEntityState DeepCopy()
    {
        return new LivingEntityState(
            _condition,
            Health.DeepCopy(),
            HealthRegeneration.DeepCopy(),
            Shield.DeepCopy(),
            ShieldRegeneration.DeepCopy(),
            Energy.DeepCopy(),
            EnergyRegeneration.DeepCopy(),
            Speed.DeepCopy(),
            Acceleration.DeepCopy(),
            AfterBurnerSpeed.DeepCopy(),
            AfterBurnerAcceleration.DeepCopy(),
            LivingEntityType,
            LivingEntityId
        );
    }

    public GCol.Array ToVariant()
    {
        return new GCol.Array
        {
            (int)_condition,
            Health.ToFullVariant(),
            HealthRegeneration.ToFullVariant(),
            Shield.ToFullVariant(),
            ShieldRegeneration.ToFullVariant(),
            Energy.ToFullVariant(),
            EnergyRegeneration.ToFullVariant(),
            Speed.ToFullVariant(),
            Acceleration.ToFullVariant(),
            AfterBurnerSpeed.ToFullVariant(),
            AfterBurnerAcceleration.ToFullVariant(),
            (int)LivingEntityType,
            LivingEntityId
        };
    }

    public bool IsTargetable()
    {
        return Condition.IsTargetAble();
    }

    public bool IsAlive()
    {
        return !Condition.Contains(LivingEntityCondition.Dead);
    }

    // public void Revive()
    // {
    //     if (!AllowOnlyHostAccess()) return;
    //     RemoveCondition(LivingEntityCondition.Dead);
    // }

    public float GetCurrentSpeed(bool isAfterBurnerPressed)
    {
        return isAfterBurnerPressed ? AfterBurnerSpeed.CurrentValue : Speed.CurrentValue;
    }

    public float GetCurrentAcceleration(bool isAfterBurnerPressed)
    {
        return isAfterBurnerPressed ? AfterBurnerAcceleration.CurrentValue : Acceleration.CurrentValue;
    }

    public StatBreakdown GetStatBreakdown(StatType type)
    {
        return GetStatByType(type).CreateBreakdown(type);
    }

    public GCol.Array CreateDeltaVariant(LivingEntityState oldState, bool includePermanent)
    {
        return new GCol.Array
        {
            LivingEntityId,
            CreateCurrentDeltaVariant(oldState),
            includePermanent ? CreatePermanentDeltaVariant(oldState) : CreateEmptyDeltaSlots()
        };
    }

    public void ApplyDeltaVariant(GCol.Array deltaVariant)
    {
        if (deltaVariant.Count != DeltaVariantCount)
        {
            Logger.Error($"{nameof(LivingEntityState)}.{nameof(ApplyDeltaVariant)} received invalid variant count {deltaVariant.Count}.");
            throw new ArgumentException("Invalid living entity delta variant format");
        }

        var livingEntityId = deltaVariant[DeltaLivingEntityIdIndex].AsInt32();
        if (LivingEntityId != 0 && LivingEntityId != livingEntityId)
        {
            Logger.Warning(
                $"{nameof(LivingEntityState)}.{nameof(ApplyDeltaVariant)} applying delta for living entity {livingEntityId} to state {LivingEntityId}.");
        }

        ApplyCurrentDeltaVariant(deltaVariant[DeltaCurrentIndex].AsGodotArray());
        ApplyPermanentDeltaVariant(deltaVariant[DeltaPermanentIndex].AsGodotArray());
    }

    public static bool DeltaVariantHasChanges(GCol.Array deltaVariant)
    {
        if (deltaVariant.Count != DeltaVariantCount)
        {
            Logger.Error($"{nameof(LivingEntityState)}.{nameof(DeltaVariantHasChanges)} received invalid variant count {deltaVariant.Count}.");
            throw new ArgumentException("Invalid living entity delta variant format");
        }

        return DeltaSlotsHaveChanges(deltaVariant[DeltaCurrentIndex].AsGodotArray()) ||
               DeltaSlotsHaveChanges(deltaVariant[DeltaPermanentIndex].AsGodotArray());
    }

    private GCol.Array CreateCurrentDeltaVariant(LivingEntityState oldState)
    {
        var result = CreateEmptyDeltaSlots();

        if (_condition != oldState._condition)
            result[(int)StatType.Condition] = new GCol.Array { (int)_condition };

        AddCurrentIfChanged(result, StatType.Health, Health, oldState.Health);
        AddCurrentIfChanged(result, StatType.HealthRegeneration, HealthRegeneration, oldState.HealthRegeneration);
        AddCurrentIfChanged(result, StatType.Shield, Shield, oldState.Shield);
        AddCurrentIfChanged(result, StatType.ShieldRegeneration, ShieldRegeneration, oldState.ShieldRegeneration);
        AddCurrentIfChanged(result, StatType.Energy, Energy, oldState.Energy);
        AddCurrentIfChanged(result, StatType.EnergyRegeneration, EnergyRegeneration, oldState.EnergyRegeneration);
        AddCurrentIfChanged(result, StatType.Speed, Speed, oldState.Speed);
        AddCurrentIfChanged(result, StatType.Acceleration, Acceleration, oldState.Acceleration);
        AddCurrentIfChanged(result, StatType.AfterBurnerSpeed, AfterBurnerSpeed, oldState.AfterBurnerSpeed);
        AddCurrentIfChanged(result, StatType.AfterBurnerAcceleration, AfterBurnerAcceleration, oldState.AfterBurnerAcceleration);

        return result;
    }

    private GCol.Array CreatePermanentDeltaVariant(LivingEntityState oldState)
    {
        var result = CreateEmptyDeltaSlots();

        AddPermanentIfChanged(result, StatType.Health, Health, oldState.Health);
        AddPermanentIfChanged(result, StatType.HealthRegeneration, HealthRegeneration, oldState.HealthRegeneration);
        AddPermanentIfChanged(result, StatType.Shield, Shield, oldState.Shield);
        AddPermanentIfChanged(result, StatType.ShieldRegeneration, ShieldRegeneration, oldState.ShieldRegeneration);
        AddPermanentIfChanged(result, StatType.Energy, Energy, oldState.Energy);
        AddPermanentIfChanged(result, StatType.EnergyRegeneration, EnergyRegeneration, oldState.EnergyRegeneration);
        AddPermanentIfChanged(result, StatType.Speed, Speed, oldState.Speed);
        AddPermanentIfChanged(result, StatType.Acceleration, Acceleration, oldState.Acceleration);
        AddPermanentIfChanged(result, StatType.AfterBurnerSpeed, AfterBurnerSpeed, oldState.AfterBurnerSpeed);
        AddPermanentIfChanged(result, StatType.AfterBurnerAcceleration, AfterBurnerAcceleration, oldState.AfterBurnerAcceleration);

        return result;
    }

    private void ApplyCurrentDeltaVariant(GCol.Array delta)
    {
        ValidateDeltaSlots(delta);

        var conditionDelta = delta[(int)StatType.Condition].AsGodotArray();
        if (conditionDelta.Count > 0)
            SetCondition((LivingEntityCondition)conditionDelta[0].AsInt32());

        ApplyCurrentStat(delta, StatType.Health, Health);
        ApplyCurrentStat(delta, StatType.HealthRegeneration, HealthRegeneration);
        ApplyCurrentStat(delta, StatType.Shield, Shield);
        ApplyCurrentStat(delta, StatType.ShieldRegeneration, ShieldRegeneration);
        ApplyCurrentStat(delta, StatType.Energy, Energy);
        ApplyCurrentStat(delta, StatType.EnergyRegeneration, EnergyRegeneration);
        ApplyCurrentStat(delta, StatType.Speed, Speed);
        ApplyCurrentStat(delta, StatType.Acceleration, Acceleration);
        ApplyCurrentStat(delta, StatType.AfterBurnerSpeed, AfterBurnerSpeed);
        ApplyCurrentStat(delta, StatType.AfterBurnerAcceleration, AfterBurnerAcceleration);
    }

    private void ApplyPermanentDeltaVariant(GCol.Array delta)
    {
        ValidateDeltaSlots(delta);

        ApplyPermanentStat(delta, StatType.Health, Health);
        ApplyPermanentStat(delta, StatType.HealthRegeneration, HealthRegeneration);
        ApplyPermanentStat(delta, StatType.Shield, Shield);
        ApplyPermanentStat(delta, StatType.ShieldRegeneration, ShieldRegeneration);
        ApplyPermanentStat(delta, StatType.Energy, Energy);
        ApplyPermanentStat(delta, StatType.EnergyRegeneration, EnergyRegeneration);
        ApplyPermanentStat(delta, StatType.Speed, Speed);
        ApplyPermanentStat(delta, StatType.Acceleration, Acceleration);
        ApplyPermanentStat(delta, StatType.AfterBurnerSpeed, AfterBurnerSpeed);
        ApplyPermanentStat(delta, StatType.AfterBurnerAcceleration, AfterBurnerAcceleration);
    }

    private static GCol.Array CreateEmptyDeltaSlots()
    {
        var result = new GCol.Array();
        for (var i = 0; i < StatSlotCount; i++)
            result.Add(new GCol.Array());

        return result;
    }

    private static bool DeltaSlotsHaveChanges(GCol.Array deltaSlots)
    {
        ValidateDeltaSlots(deltaSlots);

        for (var i = 0; i < deltaSlots.Count; i++)
        {
            if (deltaSlots[i].AsGodotArray().Count > 0)
                return true;
        }

        return false;
    }

    private static void ValidateDeltaSlots(GCol.Array deltaSlots)
    {
        if (deltaSlots.Count != StatSlotCount)
        {
            Logger.Error($"{nameof(LivingEntityState)}.{nameof(ValidateDeltaSlots)} received invalid slot count {deltaSlots.Count}.");
            throw new ArgumentException("Invalid living entity delta slots format");
        }
    }

    private static void AddCurrentIfChanged(GCol.Array result, StatType type, Stat current, Stat old)
    {
        if (current.HasCurrentDelta(old))
            result[(int)type] = current.ToCurrentVariant();
    }

    private static void AddPermanentIfChanged(GCol.Array result, StatType type, Stat current, Stat old)
    {
        if (current.HasPermaDelta(old))
            result[(int)type] = current.ToPermanentVariant();
    }

    private static void ApplyCurrentStat(GCol.Array delta, StatType type, Stat stat)
    {
        var slot = delta[(int)type].AsGodotArray();
        if (slot.Count > 0)
            stat.ApplyCurrentSync(slot);
    }

    private static void ApplyPermanentStat(GCol.Array delta, StatType type, Stat stat)
    {
        var slot = delta[(int)type].AsGodotArray();
        if (slot.Count > 0)
            stat.ApplyPermanentSync(slot);
    }

    private static ArgumentException CreateConditionIsNotStatException(StatType type)
    {
        Logger.Error($"{nameof(LivingEntityState)}.{nameof(GetStatByType)} received {nameof(type)}={type}; condition is not a stat.");
        return new ArgumentException(nameof(type), "Condition is not stat!");
    }

    private static ArgumentOutOfRangeException CreateUnknownStatTypeException(StatType type)
    {
        Logger.Error($"{nameof(LivingEntityState)}.{nameof(GetStatByType)} received unknown {nameof(type)}={type}.");
        return new ArgumentOutOfRangeException(nameof(type), $"Unknown StatType: {type}");
    }

    // private static bool AllowOnlyHostAccess([CallerMemberName] string memberName = "")
    // {
    //     if (!IsHost)
    //     {
    //         Logger.Error($"Non Host State is trying to change state: {memberName}");
    //         return false;
    //     }
    //
    //     return true;
    // }
}
