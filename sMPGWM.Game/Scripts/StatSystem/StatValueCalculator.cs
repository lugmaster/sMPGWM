using System;

namespace sMPGWM.Scripts.StatSystem;

public static class StatValueCalculator
{
    public static double Calculate(double baseline, double flat, double fraction)
    {
        if (!double.IsFinite(baseline) || !double.IsFinite(flat) || !double.IsFinite(fraction))
            throw new ArgumentOutOfRangeException(nameof(baseline));
        var subtotal = baseline + flat;
        var multiplier = 1 + fraction;
        var result = subtotal * multiplier;
        if (!double.IsFinite(subtotal) || !double.IsFinite(multiplier) || !double.IsFinite(result))
            throw new InvalidOperationException("Stat calculation overflow.");
        return Math.Max(0, result);
    }
}
