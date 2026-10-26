#nullable enable

using System;
using System.Globalization;

namespace sMPGWM.Scripts.Util.UiComponents;

public sealed record IconDisplayData(string Tooltip = "", long? Quantity = null)
{
    public static string FormatQuantity(long quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        if (quantity < 1000) return quantity.ToString(CultureInfo.InvariantCulture);
        var divisor = 1000m;
        var suffix = "K";
        if (quantity >= 1_000_000) { divisor = 1_000_000m; suffix = "M"; }
        if (quantity >= 1_000_000_000) { divisor = 1_000_000_000m; suffix = "B"; }
        if (quantity >= 1_000_000_000_000) { divisor = 1_000_000_000_000m; suffix = "T"; }
        if (quantity >= 1_000_000_000_000_000) { divisor = 1_000_000_000_000_000m; suffix = "Q"; }
        if (quantity >= 1_000_000_000_000_000_000) { divisor = 1_000_000_000_000_000_000m; suffix = "E"; }
        return (decimal.Floor(quantity / divisor * 10) / 10).ToString("0.#", CultureInfo.InvariantCulture) + suffix;
    }
}
