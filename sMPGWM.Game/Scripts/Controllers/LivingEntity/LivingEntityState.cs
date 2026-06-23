using System;
using Logger = sMPGWM.Scripts.Autoload.Logger;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;

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

    public void UpdateFullState(Array livingEntityStateVariant)
    {
        if (livingEntityStateVariant.Count != FullStateVariantCount)
            throw new ArgumentException("Invalid livingEntityStateVariant format");

        SetCondition((LivingEntityCondition)(int)livingEntityStateVariant[0]);
        Health = new Stat(livingEntityStateVariant[1].As<Array>());
        HealthRegeneration = new Stat(livingEntityStateVariant[2].As<Array>());
        Shield = new Stat(livingEntityStateVariant[3].As<Array>());
        ShieldRegeneration = new Stat(livingEntityStateVariant[4].As<Array>());
        Energy = new Stat(livingEntityStateVariant[5].As<Array>());
        EnergyRegeneration = new Stat(livingEntityStateVariant[6].As<Array>());
        Speed = new Stat(livingEntityStateVariant[7].As<Array>());
        Acceleration = new Stat(livingEntityStateVariant[8].As<Array>());
        AfterBurnerSpeed = new Stat(livingEntityStateVariant[9].As<Array>());
        AfterBurnerAcceleration = new Stat(livingEntityStateVariant[10].As<Array>());
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
            StatType.Condition => throw new ArgumentException(nameof(type), $"Condition is not stat!"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown StatType: {type}")
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

    public Array ToVariant()
    {
        return new Array
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

    public Array CreateDeltaVariant(LivingEntityState oldState, bool includePermanent)
    {
        return new Array
        {
            LivingEntityId,
            CreateCurrentDeltaVariant(oldState),
            includePermanent ? CreatePermanentDeltaVariant(oldState) : CreateEmptyDeltaSlots()
        };
    }

    public void ApplyDeltaVariant(Array deltaVariant)
    {
        if (deltaVariant.Count != DeltaVariantCount)
            throw new ArgumentException("Invalid living entity delta variant format");

        var livingEntityId = deltaVariant[DeltaLivingEntityIdIndex].AsInt32();
        if (LivingEntityId != 0 && LivingEntityId != livingEntityId)
        {
            Logger.Warning(
                $"Applying delta for living entity {livingEntityId} to state {LivingEntityId}.");
        }

        ApplyCurrentDeltaVariant(deltaVariant[DeltaCurrentIndex].AsGodotArray());
        ApplyPermanentDeltaVariant(deltaVariant[DeltaPermanentIndex].AsGodotArray());
    }

    public static bool DeltaVariantHasChanges(Array deltaVariant)
    {
        if (deltaVariant.Count != DeltaVariantCount)
            throw new ArgumentException("Invalid living entity delta variant format");

        return DeltaSlotsHaveChanges(deltaVariant[DeltaCurrentIndex].AsGodotArray()) ||
               DeltaSlotsHaveChanges(deltaVariant[DeltaPermanentIndex].AsGodotArray());
    }

    private Array CreateCurrentDeltaVariant(LivingEntityState oldState)
    {
        var result = CreateEmptyDeltaSlots();

        if (_condition != oldState._condition)
            result[(int)StatType.Condition] = new Array { (int)_condition };

        AddIfChanged(StatType.Health, Health, oldState.Health);
        AddIfChanged(StatType.HealthRegeneration, HealthRegeneration, oldState.HealthRegeneration);
        AddIfChanged(StatType.Shield, Shield, oldState.Shield);
        AddIfChanged(StatType.ShieldRegeneration, ShieldRegeneration, oldState.ShieldRegeneration);
        AddIfChanged(StatType.Energy, Energy, oldState.Energy);
        AddIfChanged(StatType.EnergyRegeneration, EnergyRegeneration, oldState.EnergyRegeneration);
        AddIfChanged(StatType.Speed, Speed, oldState.Speed);
        AddIfChanged(StatType.Acceleration, Acceleration, oldState.Acceleration);
        AddIfChanged(StatType.AfterBurnerSpeed, AfterBurnerSpeed, oldState.AfterBurnerSpeed);
        AddIfChanged(StatType.AfterBurnerAcceleration, AfterBurnerAcceleration, oldState.AfterBurnerAcceleration);

        return result;

        void AddIfChanged(StatType type, Stat current, Stat old)
        {
            if (current.HasCurrentDelta(old))
                result[(int)type] = current.ToCurrentVariant();
        }
    }

    private Array CreatePermanentDeltaVariant(LivingEntityState oldState)
    {
        var result = CreateEmptyDeltaSlots();

        AddIfChanged(StatType.Health, Health, oldState.Health);
        AddIfChanged(StatType.HealthRegeneration, HealthRegeneration, oldState.HealthRegeneration);
        AddIfChanged(StatType.Shield, Shield, oldState.Shield);
        AddIfChanged(StatType.ShieldRegeneration, ShieldRegeneration, oldState.ShieldRegeneration);
        AddIfChanged(StatType.Energy, Energy, oldState.Energy);
        AddIfChanged(StatType.EnergyRegeneration, EnergyRegeneration, oldState.EnergyRegeneration);
        AddIfChanged(StatType.Speed, Speed, oldState.Speed);
        AddIfChanged(StatType.Acceleration, Acceleration, oldState.Acceleration);
        AddIfChanged(StatType.AfterBurnerSpeed, AfterBurnerSpeed, oldState.AfterBurnerSpeed);
        AddIfChanged(StatType.AfterBurnerAcceleration, AfterBurnerAcceleration, oldState.AfterBurnerAcceleration);

        return result;

        void AddIfChanged(StatType type, Stat current, Stat old)
        {
            if (current.HasPermaDelta(old))
                result[(int)type] = current.ToPermanentVariant();
        }
    }

    private void ApplyCurrentDeltaVariant(Array delta)
    {
        ValidateDeltaSlots(delta);

        var conditionDelta = delta[(int)StatType.Condition].AsGodotArray();
        if (conditionDelta.Count > 0)
            SetCondition((LivingEntityCondition)conditionDelta[0].AsInt32());

        ApplyStat(StatType.Health, Health);
        ApplyStat(StatType.HealthRegeneration, HealthRegeneration);
        ApplyStat(StatType.Shield, Shield);
        ApplyStat(StatType.ShieldRegeneration, ShieldRegeneration);
        ApplyStat(StatType.Energy, Energy);
        ApplyStat(StatType.EnergyRegeneration, EnergyRegeneration);
        ApplyStat(StatType.Speed, Speed);
        ApplyStat(StatType.Acceleration, Acceleration);
        ApplyStat(StatType.AfterBurnerSpeed, AfterBurnerSpeed);
        ApplyStat(StatType.AfterBurnerAcceleration, AfterBurnerAcceleration);

        void ApplyStat(StatType type, Stat stat)
        {
            var slot = delta[(int)type].AsGodotArray();
            if (slot.Count > 0)
                stat.ApplyCurrentSync(slot);
        }
    }

    private void ApplyPermanentDeltaVariant(Array delta)
    {
        ValidateDeltaSlots(delta);

        ApplyStat(StatType.Health, Health);
        ApplyStat(StatType.HealthRegeneration, HealthRegeneration);
        ApplyStat(StatType.Shield, Shield);
        ApplyStat(StatType.ShieldRegeneration, ShieldRegeneration);
        ApplyStat(StatType.Energy, Energy);
        ApplyStat(StatType.EnergyRegeneration, EnergyRegeneration);
        ApplyStat(StatType.Speed, Speed);
        ApplyStat(StatType.Acceleration, Acceleration);
        ApplyStat(StatType.AfterBurnerSpeed, AfterBurnerSpeed);
        ApplyStat(StatType.AfterBurnerAcceleration, AfterBurnerAcceleration);

        void ApplyStat(StatType type, Stat stat)
        {
            var slot = delta[(int)type].AsGodotArray();
            if (slot.Count > 0)
                stat.ApplyPermanentSync(slot);
        }
    }

    private static Array CreateEmptyDeltaSlots()
    {
        var result = new Array();
        for (var i = 0; i < StatSlotCount; i++)
            result.Add(new Array());

        return result;
    }

    private static bool DeltaSlotsHaveChanges(Array deltaSlots)
    {
        ValidateDeltaSlots(deltaSlots);

        for (var i = 0; i < deltaSlots.Count; i++)
        {
            if (deltaSlots[i].AsGodotArray().Count > 0)
                return true;
        }

        return false;
    }

    private static void ValidateDeltaSlots(Array deltaSlots)
    {
        if (deltaSlots.Count != StatSlotCount)
            throw new ArgumentException("Invalid living entity delta slots format");
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
