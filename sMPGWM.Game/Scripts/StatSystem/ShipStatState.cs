using System;

namespace sMPGWM.Scripts.StatSystem;

public sealed class ShipStatState
{
    public const int MaximumContributions = 64;
    private readonly Contribution[] _contributions = new Contribution[MaximumContributions];
    private int _count;
    private bool _effectsEnabled = true;
    public ResolvedShipStats EquipmentInputs { get; private set; }
    public ResolvedShipStats Effective { get; private set; }
    private readonly record struct Contribution(StatContributionSource Source, double Flat, double Fraction);

    public ResolvedShipStats Preview(ResolvedShipStats inputs, bool? effectsEnabled = null)
    {
        if (!inputs.IsValid) throw new ArgumentException("Invalid equipment inputs.");
        var flat = inputs.ShieldFlatModifier;
        var fraction = 0d;
        for (var i = 0; i < _count; i++)
        {
            var contribution = _contributions[i];
            if (contribution.Source.Kind == StatContributionKind.Effect && !(effectsEnabled ?? _effectsEnabled)) continue;
            flat += contribution.Flat;
            fraction += contribution.Fraction;
        }
        var result = inputs with { ShieldCapacity = inputs.HasShieldGenerator ? StatValueCalculator.Calculate(inputs.ShieldCapacity, flat, fraction) : 0, ShieldFlatModifier = 0 };
        if (!result.IsValid) throw new InvalidOperationException("Contributions resolve to invalid stats.");
        return result;
    }

    public void SetEquipment(ResolvedShipStats inputs)
    {
        var effective = Preview(inputs);
        EquipmentInputs = inputs;
        Effective = effective;
    }

    public void SetEffectsEnabled(bool enabled)
    {
        if (_effectsEnabled == enabled) return;
        var effective = Preview(EquipmentInputs, enabled);
        _effectsEnabled = enabled;
        Effective = effective;
    }

    public void SetContribution(StatContributionSource source, double flat, double fraction)
    {
        if (source.Identity <= 0 || !Enum.IsDefined(source.Kind) || !double.IsFinite(flat) || !double.IsFinite(fraction))
            throw new ArgumentOutOfRangeException(nameof(source));
        var index = 0;
        while (index < _count && Compare(_contributions[index].Source, source) < 0) index++;
        var existing = index < _count && _contributions[index].Source == source;
        var previous = existing ? _contributions[index] : default;
        if (!existing)
        {
            if (_count == MaximumContributions) throw new InvalidOperationException("Contribution limit reached.");
            Array.Copy(_contributions, index, _contributions, index + 1, _count - index);
            _count++;
        }
        _contributions[index] = new(source, flat, fraction);
        try { Effective = Preview(EquipmentInputs); }
        catch
        {
            if (existing) _contributions[index] = previous;
            else { Array.Copy(_contributions, index + 1, _contributions, index, --_count - index); _contributions[_count] = default; }
            throw;
        }
    }

    public bool RemoveSource(StatContributionSource source)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_contributions[i].Source != source) continue;
            var previous = _contributions[i];
            Array.Copy(_contributions, i + 1, _contributions, i, --_count - i);
            try { Effective = Preview(EquipmentInputs); }
            catch
            {
                Array.Copy(_contributions, i, _contributions, i + 1, _count++ - i);
                _contributions[i] = previous;
                throw;
            }
            _contributions[_count] = default;
            return true;
        }
        return false;
    }

    internal void Confirm(ResolvedShipStats stats)
    {
        if (!stats.IsValid || stats.ShieldFlatModifier != 0) throw new ArgumentException("Invalid effective stats.");
        Array.Clear(_contributions);
        _count = 0;
        EquipmentInputs = Effective = stats;
    }

    internal void Clear() { Array.Clear(_contributions); _count = 0; }
    private static int Compare(StatContributionSource left, StatContributionSource right)
    {
        var kind = left.Kind.CompareTo(right.Kind);
        return kind != 0 ? kind : left.Identity.CompareTo(right.Identity);
    }
}
