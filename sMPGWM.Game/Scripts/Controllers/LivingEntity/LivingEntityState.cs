using System;
using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public class LivingEntityState
{
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

    // replace with changed event to let ui listen to changes? good or bad idea?
    public event Action<LivingEntityCondition, bool> OnConditionChanged;

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
        if (livingEntityStateVariant.Count != 13)
            throw new ArgumentException("Invalid livingEntityStateVariant format");

        SetCondition((LivingEntityCondition)(int)livingEntityStateVariant[0], false);
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
    private void SetCondition(LivingEntityCondition newCondition, bool applyVisualEffects = true)
    {
        if (_condition == newCondition) return;

        _condition = newCondition;
        OnConditionChanged?.Invoke(_condition, applyVisualEffects);
    }

    // Creates a new Condition from provided bitflag - must be a new one to trigger comparison
    private void RemoveCondition(LivingEntityCondition conditionToRemove, bool applyVisualEffects = true)
    {
        SetCondition(_condition.Remove(conditionToRemove), applyVisualEffects);
    }

    // Creates a new Condition from provided bitflag - must be a new one to trigger comparison
    private void AddCondition(LivingEntityCondition conditionToAdd, bool applyVisualEffects = true)
    {
        SetCondition(_condition.Add(conditionToAdd), applyVisualEffects);
    }

    public void ApplyDamage(float amount, Node2D source)
    {
        // if (!AllowOnlyHostAccess()) return;
        if (Shield.CurrentValue > 0)
        {
            var leftover = Shield.ReduceAndGetOverflow(amount);
            if (leftover > 0)
                ReduceHealth(leftover, source);
        }
        else
        {
            ReduceHealth(amount, source);
        }
    }

    private void ReduceHealth(float amount, Node2D source)
    {
        if (Health.ReduceAndCheckZero(amount))
        {
            KillPlayer();
            // if (LivingEntityType.IsMpController() && source is MpController controller)
            // {
            //     CombatMetricsSynchronizer.ConfirmKill(controller.OwningPeerId, LivingEntityId);
            // }
        }
    }

    public void KillPlayer()
    {
        // if (!AllowOnlyHostAccess()) return;
        AddCondition(_condition | LivingEntityCondition.Dead);
        SetVitalsToZero();
    }

    public void SetVitalsToZero()
    {
        // if (!AllowOnlyHostAccess()) return;
        Health.DrainToZero();
        Shield.DrainToZero();
        Energy.DrainToZero();
    }

    public void RestoreVitalsToMax()
    {
        // if (!AllowOnlyHostAccess()) return;
        Health.RestoreToMax();
        Shield.RestoreToMax();
        Energy.RestoreToMax();
    }


    public GCol.Dictionary<int, Variant> CreateCurrentDelta(LivingEntityState oldState)
    {
        var result = new GCol.Dictionary<int, Variant>();

        if (_condition != oldState._condition)
        {
            result.Add((int)StatType.Condition, (int)_condition);
        }

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
            {
                var entry = current.ToCurrentVariant();
                result.Add((int)type, entry);
            }
        }
    }

    public GCol.Dictionary<int, Variant> CreatePermanentDelta(LivingEntityState oldState)
    {
        var result = new GCol.Dictionary<int, Variant>();

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
            {
                var entry = current.ToPermanentVariant();
                result.Add((int)type, entry);
            }
        }
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

    public void ApplyCurrentDelta(GCol.Dictionary<int, Variant> delta)
    {
        foreach (var entry in delta)
        {
            switch ((StatType)entry.Key)
            {
                case StatType.Health:
                    Health.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.HealthRegeneration:
                    HealthRegeneration.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Shield:
                    Shield.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.ShieldRegeneration:
                    ShieldRegeneration.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Energy:
                    Energy.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.EnergyRegeneration:
                    EnergyRegeneration.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Speed:
                    Speed.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Acceleration:
                    Acceleration.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.AfterBurnerSpeed:
                    AfterBurnerSpeed.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.AfterBurnerAcceleration:
                    AfterBurnerAcceleration.ApplyCurrentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Condition:
                    SetCondition((LivingEntityCondition)(int)entry.Value);
                    break;
                default:
                    Logger.Debug($"Unknown stat index: {entry.Key}");
                    break;
            }
        }
    }

    public void ApplyPermanentDelta(GCol.Dictionary<int, Variant> delta)
    {
        foreach (var entry in delta)
        {
            switch ((StatType)entry.Key)
            {
                case StatType.Health:
                    Health.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.HealthRegeneration:
                    HealthRegeneration.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Shield:
                    Shield.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.ShieldRegeneration:
                    ShieldRegeneration.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Energy:
                    Energy.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.EnergyRegeneration:
                    EnergyRegeneration.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Speed:
                    Speed.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.Acceleration:
                    Acceleration.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.AfterBurnerSpeed:
                    AfterBurnerSpeed.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                case StatType.AfterBurnerAcceleration:
                    AfterBurnerAcceleration.ApplyPermanentSync(entry.Value.AsGodotArray());
                    break;
                default:
                    Logger.Debug($"Unknown stat index: {entry.Key}");
                    break;
            }
        }
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