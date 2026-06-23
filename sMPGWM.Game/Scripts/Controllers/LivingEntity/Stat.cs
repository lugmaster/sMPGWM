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
    private readonly List<StatModifier> _permanentModifiers = [];
    private readonly List<TemporaryStatModifier> _temporaryModifiers = [];
    private readonly List<RecurringStatModifier> _recurringModifiers = [];

    private static int RoundingPrecision => 3;

    public float BaseValue { get; private set; }
    public float CurrentValue { get; private set; }
    public float MaxValue { get; private set; }
    public float PermanentMaxValue { get; private set; }

    public IReadOnlyList<StatModifier> PermanentModifiers => _permanentModifiers.AsReadOnly();
    public IReadOnlyList<TemporaryStatModifier> TemporaryModifiers => _temporaryModifiers.AsReadOnly();
    public IReadOnlyList<RecurringStatModifier> RecurringModifiers => _recurringModifiers.AsReadOnly();

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

        CurrentValue = Round(currentValue);

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

        _permanentModifiers.Clear();
        foreach (var item in variant[2].AsGodotArray())
            _permanentModifiers.Add(new StatModifier(item.AsGodotArray()));

        _temporaryModifiers.Clear();
        foreach (var item in variant[3].AsGodotArray())
            _temporaryModifiers.Add(new TemporaryStatModifier(item.AsGodotArray()));

        _recurringModifiers.Clear();
        foreach (var item in variant[4].AsGodotArray())
            _recurringModifiers.Add(new RecurringStatModifier(item.AsGodotArray(), this));

        RecalculatePermanent();
        RecalculateTemporary(false);
        CurrentValue = Round(Math.Clamp(CurrentValue, 0f, MaxValue));
    }

    public void AddModifier(StatModifier modifier)
    {
        switch (modifier)
        {
            case RecurringStatModifier recurring:
                AddWithStackPolicy(_recurringModifiers, recurring);
                break;
            case TemporaryStatModifier temporary:
                AddWithStackPolicy(_temporaryModifiers, temporary);
                RecalculateTemporary();
                break;
            default:
                AddWithStackPolicy(_permanentModifiers, modifier);
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
                _recurringModifiers.Remove(recurring);
                break;
            case TemporaryStatModifier temporary:
                _temporaryModifiers.Remove(temporary);
                RecalculateTemporary();
                break;
            default:
                _permanentModifiers.Remove(modifier);
                RecalculatePermanent();
                RecalculateTemporary();
                break;
        }
    }

    public void AddRecurringModifier(RecurringStatModifier recurringStat)
    {
        AddWithStackPolicy(_recurringModifiers, recurringStat);
    }

    public void SetBaseValue(float value)
    {
        BaseValue = value;
        RecalculatePermanent();
        RecalculateTemporary();
    }

    public void ApplyCurrentSync(Array variant)
    {
        if (variant.Count != 4)
            throw new ArgumentException("Invalid stat delta variant");

        CurrentValue = Round((float)variant[1]);

        _temporaryModifiers.Clear();
        foreach (var item in variant[2].AsGodotArray())
            _temporaryModifiers.Add(new TemporaryStatModifier(item.AsGodotArray()));

        _recurringModifiers.Clear();
        foreach (var item in variant[3].AsGodotArray())
            _recurringModifiers.Add(new RecurringStatModifier(item.AsGodotArray(), this));

        RecalculateTemporary(false);
        CurrentValue = Round(Math.Clamp(CurrentValue, 0f, MaxValue));
    }

    public void ApplyPermanentSync(Array variant)
    {
        if (variant.Count < 2)
            throw new ArgumentException("Invalid permanent stat sync format");

        BaseValue = (float)variant[0];
        _permanentModifiers.Clear();

        foreach (var item in variant[1].AsGodotArray())
            _permanentModifiers.Add(new StatModifier(item.AsGodotArray()));

        RecalculatePermanent();
        RecalculateTemporary();
    }

    public Array ToPermanentVariant()
    {
        var permArray = new Array();
        foreach (var mod in _permanentModifiers)
            permArray.Add(mod.ToVariant());

        return new Array { BaseValue, permArray };
    }

    public Array ToCurrentVariant()
    {
        var tempArray = new Array();
        foreach (var mod in _temporaryModifiers)
            tempArray.Add(mod.ToVariant());

        var recArray = new Array();
        foreach (var mod in _recurringModifiers)
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
        foreach (var mod in _permanentModifiers)
            permArray.Add(mod.ToVariant());

        var tempArray = new Array();
        foreach (var mod in _temporaryModifiers)
            tempArray.Add(mod.ToVariant());

        var recArray = new Array();
        foreach (var mod in _recurringModifiers)
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

        foreach (var mod in _permanentModifiers)
            copy._permanentModifiers.Add(new StatModifier(mod.ToVariant()));

        foreach (var tempMod in _temporaryModifiers)
            copy._temporaryModifiers.Add(new TemporaryStatModifier(tempMod.ToVariant()));

        foreach (var recMod in _recurringModifiers)
            copy._recurringModifiers.Add(new RecurringStatModifier(recMod.ToVariant(), copy));

        copy.RecalculatePermanent();
        copy.RecalculateTemporary(false);

        return copy;
    }

    public void Tick(float delta)
    {
        var changed = false;

        for (var i = _temporaryModifiers.Count - 1; i >= 0; i--)
        {
            _temporaryModifiers[i].Tick(delta);
            if (!_temporaryModifiers[i].IsExpired) continue;
            _temporaryModifiers.RemoveAt(i);
            changed = true;
        }

        if (changed)
            RecalculateTemporary();

        for (var i = _recurringModifiers.Count - 1; i >= 0; i--)
        {
            _recurringModifiers[i].Tick(delta);
            if (_recurringModifiers[i].IsExpired)
                _recurringModifiers.RemoveAt(i);
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
            CurrentValue = Round(CurrentValue - amount);
            return 0f;
        }

        var overflow = amount - CurrentValue;
        CurrentValue = 0f;
        return Round(overflow);
    }

    public void Reduce(float amount)
    {
        CurrentValue = Round(MathF.Max(CurrentValue - amount, 0f));
    }

    public void Raise(float amount)
    {
        CurrentValue = Round(MathF.Min(CurrentValue + amount, MaxValue));
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

        if (_temporaryModifiers.Count != oldStat._temporaryModifiers.Count ||
            _recurringModifiers.Count != oldStat._recurringModifiers.Count)
            return true;

        for (var i = 0; i < _temporaryModifiers.Count; i++)
        {
            if (!_temporaryModifiers[i].Equals(oldStat._temporaryModifiers[i]))
                return true;
        }

        for (var i = 0; i < _recurringModifiers.Count; i++)
        {
            if (!_recurringModifiers[i].Equals(oldStat._recurringModifiers[i]))
                return true;
        }

        return false;
    }

    public bool HasPermaDelta(Stat oldStat)
    {
        if (!Mathf.IsEqualApprox(oldStat.BaseValue, BaseValue) || !Mathf.IsEqualApprox(oldStat.MaxValue, MaxValue))
            return true;

        if (_permanentModifiers.Count != oldStat._permanentModifiers.Count)
            return true;

        for (var i = 0; i < _permanentModifiers.Count; i++)
        {
            if (!_permanentModifiers[i].Equals(oldStat._permanentModifiers[i]))
                return true;
        }

        return false;
    }

    public StatBreakdown CreateBreakdown(StatType statType)
    {
        return new StatBreakdown(
            statType,
            BaseValue,
            CurrentValue,
            MaxValue,
            CreateContributionArray(_permanentModifiers),
            CreateContributionArray(_temporaryModifiers),
            CreateContributionArray(_recurringModifiers));
    }

    private void RecalculatePermanent()
    {
        var flatBonus = 0f;
        var percentBonus = 0f;

        foreach (var mod in _permanentModifiers)
            AddModifierValue(mod, ref flatBonus, ref percentBonus);

        PermanentMaxValue = Round((BaseValue + flatBonus) * (1 + percentBonus));
    }

    private void RecalculateTemporary(bool applyMaxDelta = true)
    {
        var flatBonus = 0f;
        var percentBonus = 0f;

        foreach (var mod in _temporaryModifiers)
            AddModifierValue(mod, ref flatBonus, ref percentBonus);

        var oldMax = MaxValue;
        MaxValue = Round((PermanentMaxValue + flatBonus) * (1 + percentBonus));

        if (applyMaxDelta)
            ApplyMaxChange(oldMax);
    }

    private void ApplyMaxChange(float oldMax)
    {
        var delta = MaxValue - oldMax;
        if (delta > 0f)
            CurrentValue += delta;
        else if (CurrentValue > MaxValue)
            CurrentValue = MaxValue;

        CurrentValue = Round(Math.Clamp(CurrentValue, 0f, MaxValue));
    }

    private static void AddModifierValue(StatModifier mod, ref float flatBonus, ref float percentBonus)
    {
        switch (mod.Type)
        {
            case ModifierType.Flat:
                flatBonus += mod.Value;
                break;
            case ModifierType.Percent:
                percentBonus += mod.Value;
                break;
        }
    }

    private static void AddWithStackPolicy<T>(List<T> modifiers, T modifier) where T : StatModifier
    {
        var sameSourceIndex = modifiers.FindIndex(existing =>
            !string.IsNullOrEmpty(modifier.SourceId) &&
            existing.SourceId == modifier.SourceId &&
            existing.Type == modifier.Type);

        if (sameSourceIndex < 0)
        {
            modifiers.Add(modifier);
            return;
        }

        switch (modifier.StackPolicy)
        {
            case ModifierStackPolicy.RefreshDuration:
            case ModifierStackPolicy.Replace:
            case ModifierStackPolicy.StackAndRefresh:
            case ModifierStackPolicy.MaxStacksRefresh:
                modifiers[sameSourceIndex] = modifier;
                break;
            case ModifierStackPolicy.IgnoreIfActive:
                break;
            case ModifierStackPolicy.Stack:
            default:
                modifiers.Add(modifier);
                break;
        }
    }

    private static StatModifierContribution ToContribution(StatModifier modifier)
    {
        var timeRemaining = modifier is TemporaryStatModifier temporary ? temporary.TimeRemaining : (float?)null;
        return new StatModifierContribution(
            modifier.InstanceId,
            modifier.SourceId,
            modifier.SourceType,
            modifier.DisplayName,
            modifier.Type,
            modifier.Value,
            timeRemaining);
    }

    private static float Round(float value) => MathF.Round(value, RoundingPrecision);

    private static StatModifierContribution[] CreateContributionArray<T>(IReadOnlyList<T> modifiers)
        where T : StatModifier
    {
        var result = new StatModifierContribution[modifiers.Count];
        for (var i = 0; i < modifiers.Count; i++)
            result[i] = ToContribution(modifiers[i]);

        return result;
    }
}
