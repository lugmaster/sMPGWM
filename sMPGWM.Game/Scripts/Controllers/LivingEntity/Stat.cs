using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public class Stat
{
    public float BaseValue { get; private set; }
    public float CurrentValue { get; private set; }
    public float MaxValue { get; private set; }
    public float PermanentMaxValue { get; private set; }

    public readonly List<StatModifier> PermanentModifiers = [];
    public readonly List<TemporaryStatModifier> TemporaryModifiers = [];
    public readonly List<RecurringStatModifier> RecurringModifiers = [];

    private static int RoundingPrecision => 1;

    // --- Constructors ---
    public Stat(float baseValue)
    {
        BaseValue = baseValue;
        RecalculatePermanent();
        RecalculateTemporary();
        CurrentValue = MaxValue;
    }

    public Stat(float baseValue, float currentValue)
    {
        BaseValue = baseValue;

        RecalculatePermanent();
        RecalculateTemporary();

        CurrentValue = currentValue;

        if (CurrentValue < 0 || CurrentValue > MaxValue)
        {
            Logger.Warning(
                $"Initializing {nameof(Stat)} with CurrentValue {CurrentValue}, " +
                $"expected range [0, {MaxValue}].");
        }
    }

    public Stat(Array variant)
    {
        if (variant.Count != 5)
            throw new ArgumentException("Invalid stat variant format");

        BaseValue = (float)variant[0];
        CurrentValue = (float)variant[1];

        PermanentModifiers.Clear();
        foreach (var item in variant[2].AsGodotArray())
            PermanentModifiers.Add(new StatModifier(item.AsGodotArray()));

        TemporaryModifiers.Clear();
        foreach (var item in variant[3].AsGodotArray())
            TemporaryModifiers.Add(new TemporaryStatModifier(item.AsGodotArray()));

        RecurringModifiers.Clear();
        foreach (var item in variant[4].AsGodotArray())
            RecurringModifiers.Add(new RecurringStatModifier(item.AsGodotArray(), this));

        RecalculatePermanent();
        RecalculateTemporary();
    }

    // --- Modifier Handling ---
    public void AddModifier(StatModifier modifier)
    {
        switch (modifier)
        {
            case RecurringStatModifier recurring:
                RecurringModifiers.Add(recurring);
                break;
            case TemporaryStatModifier temporary:
                TemporaryModifiers.Add(temporary);
                RecalculateTemporary();
                break;
            default:
                PermanentModifiers.Add(modifier);
                RecalculatePermanent();
                RecalculateTemporary();
                break;
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        switch (modifier)
        {
            case RecurringStatModifier recurring:
                RecurringModifiers.Remove(recurring);
                break;
            case TemporaryStatModifier temporary:
                TemporaryModifiers.Remove(temporary);
                RecalculateTemporary();
                break;
            default:
                PermanentModifiers.Remove(modifier);
                RecalculatePermanent();
                RecalculateTemporary();
                break;
        }
    }

    public void AddRecurringModifier(RecurringStatModifier recurringStat)
    {
        RecurringModifiers.Add(recurringStat);
    }

    public void SetBaseValue(float value)
    {
        BaseValue = value;
        RecalculatePermanent();

        var flatBonus = 0f;
        var percentBonus = 0f;

        foreach (var mod in TemporaryModifiers)
        {
            switch (mod.Type)
            {
                case ModifierType.Flat: flatBonus += mod.Value; break;
                case ModifierType.Percent: percentBonus += mod.Value; break;
            }
        }

        MaxValue = Round((PermanentMaxValue + flatBonus) * (1 + percentBonus));
        CurrentValue = MaxValue;
    }

    // --- Sync / Serialization ---
    public void ApplyCurrentSync(Array variant)
    {
        if (variant.Count != 4)
            throw new ArgumentException("Invalid stat delta variant");

        CurrentValue = (float)variant[1];

        TemporaryModifiers.Clear();
        foreach (var item in variant[2].AsGodotArray())
            TemporaryModifiers.Add(new TemporaryStatModifier(item.AsGodotArray()));

        RecurringModifiers.Clear();
        foreach (var item in variant[3].AsGodotArray())
            RecurringModifiers.Add(new RecurringStatModifier(item.AsGodotArray(), this));

        RecalculateTemporary();
    }

    public void ApplyPermanentSync(Array variant)
    {
        if (variant.Count < 2)
            throw new ArgumentException("Invalid permanent stat sync format");

        BaseValue = (float)variant[0];
        PermanentModifiers.Clear();

        foreach (var item in variant[1].AsGodotArray())
            PermanentModifiers.Add(new StatModifier(item.AsGodotArray()));

        RecalculatePermanent();
        RecalculateTemporary();
    }

    public Array ToPermanentVariant()
    {
        var permArray = new Array();
        foreach (var mod in PermanentModifiers)
            permArray.Add(mod.ToVariant());

        return new Array { BaseValue, permArray };
    }

    public Array ToCurrentVariant()
    {
        var tempArray = new Array();
        foreach (var mod in TemporaryModifiers)
            tempArray.Add(mod.ToVariant());

        var recArray = new Array();
        foreach (var mod in RecurringModifiers)
            recArray.Add(mod.ToVariant());

        return new Array
        {
            BaseValue,
            CurrentValue,
            tempArray,
            recArray
        };
    }

    public Array ToFullVariant()
    {
        var permArray = new Array();
        foreach (var mod in PermanentModifiers)
            permArray.Add(mod.ToVariant());

        var tempArray = new Array();
        foreach (var mod in TemporaryModifiers)
            tempArray.Add(mod.ToVariant());

        var recArray = new Array();
        foreach (var mod in RecurringModifiers)
            recArray.Add(mod.ToVariant());

        return new Array
        {
            BaseValue,
            CurrentValue,
            permArray,
            tempArray,
            recArray
        };
    }

    public Stat DeepCopy()
    {
        var copy = new Stat(BaseValue)
        {
            CurrentValue = CurrentValue
        };

        foreach (var mod in PermanentModifiers)
            copy.PermanentModifiers.Add(new StatModifier(mod.ToVariant()));

        foreach (var tempMod in TemporaryModifiers)
            copy.TemporaryModifiers.Add(new TemporaryStatModifier(tempMod.ToVariant()));

        foreach (var recMod in RecurringModifiers)
            copy.RecurringModifiers.Add(new RecurringStatModifier(recMod.ToVariant(), copy));

        copy.RecalculatePermanent();
        copy.RecalculateTemporary();

        return copy;
    }

    // --- Runtime Logic ---
    public void Tick(float delta)
    {
        var changed = false;

        for (var i = TemporaryModifiers.Count - 1; i >= 0; i--)
        {
            TemporaryModifiers[i].Tick(delta);
            if (!TemporaryModifiers[i].IsExpired) continue;
            TemporaryModifiers.RemoveAt(i);
            changed = true;
        }

        if (changed)
            RecalculateTemporary();

        for (var i = RecurringModifiers.Count - 1; i >= 0; i--)
        {
            RecurringModifiers[i].Tick(delta);
            if (RecurringModifiers[i].IsExpired)
                RecurringModifiers.RemoveAt(i);
        }
    }

    public bool ReduceAndCheckZero(float amount)
    {
        Reduce(amount);
        return CurrentValue == 0f;
    }

    public float ReduceAndGetOverflow(float amount)
    {
        if (CurrentValue >= amount)
        {
            CurrentValue -= amount;
            return 0f;
        }

        float overflow = amount - CurrentValue;
        CurrentValue = 0f;
        return overflow;
    }

    public void Reduce(float amount)
    {
        CurrentValue = MathF.Max(CurrentValue - amount, 0f);
    }

    public void Raise(float amount)
    {
        CurrentValue = MathF.Min(CurrentValue + amount, MaxValue);
    }

    public void RestoreToMax()
    {
        CurrentValue = MaxValue;
    }

    public void DrainToZero()
    {
        CurrentValue = 0f;
    }

    public bool HasCurrentDelta(Stat oldStat)
    {
        if (!Mathf.IsEqualApprox(oldStat.CurrentValue, CurrentValue))
            return true;

        if (TemporaryModifiers.Count != oldStat.TemporaryModifiers.Count ||
            RecurringModifiers.Count != oldStat.RecurringModifiers.Count)
            return true;

        for (int i = 0; i < TemporaryModifiers.Count; i++)
        {
            if (!TemporaryModifiers[i].Equals(oldStat.TemporaryModifiers[i]))
                return true;
        }

        for (int i = 0; i < RecurringModifiers.Count; i++)
        {
            if (!RecurringModifiers[i].Equals(oldStat.RecurringModifiers[i]))
                return true;
        }

        return false;
    }

    public bool HasPermaDelta(Stat oldStat)
    {
        if (!Mathf.IsEqualApprox(oldStat.BaseValue, BaseValue) || !Mathf.IsEqualApprox(oldStat.MaxValue, MaxValue))
            return true;

        if (PermanentModifiers.Count != oldStat.PermanentModifiers.Count)
            return true;

        for (int i = 0; i < PermanentModifiers.Count; i++)
        {
            if (!PermanentModifiers[i].Equals(oldStat.PermanentModifiers[i]))
                return true;
        }

        return false;
    }

    // --- Private Helpers ---
    private void RecalculatePermanent()
    {
        var flatBonus = 0f;
        var percentBonus = 0f;

        foreach (var mod in PermanentModifiers)
        {
            switch (mod.Type)
            {
                case ModifierType.Flat: flatBonus += mod.Value; break;
                case ModifierType.Percent: percentBonus += mod.Value; break;
            }
        }

        PermanentMaxValue = Round((BaseValue + flatBonus) * (1 + percentBonus));
    }

    private void RecalculateTemporary()
    {
        var flatBonus = 0f;
        var percentBonus = 0f;

        foreach (var mod in TemporaryModifiers)
        {
            switch (mod.Type)
            {
                case ModifierType.Flat: flatBonus += mod.Value; break;
                case ModifierType.Percent: percentBonus += mod.Value; break;
            }
        }

        var oldMax = MaxValue;
        MaxValue = Round((PermanentMaxValue + flatBonus) * (1 + percentBonus));

        var delta = MaxValue - oldMax;
        CurrentValue += delta;
        CurrentValue = Round(Math.Clamp(CurrentValue, 0f, MaxValue));
    }

    private static float Round(float value) => MathF.Round(value, RoundingPrecision);
}