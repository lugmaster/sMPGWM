using System.Globalization;

namespace sMPGWM.Scripts.TagSystem.Ids;

/// <summary>
/// Identifies a query within one compiled registry; zero is invalid.
/// </summary>
/// <param name="Value">The registry-relative numeric query identifier; zero is invalid.</param>
public readonly record struct TagQueryId(ushort Value)
{
    public static readonly TagQueryId Invalid = new(0);

    public bool IsValid => Value != 0;

    /// <summary>
    /// Returns the invariant numeric identifier, or Invalid when its value is zero.
    /// </summary>
    /// <returns>The invariant identifier text, or Invalid.</returns>
    public override string ToString()
    {
        return IsValid ? Value.ToString(CultureInfo.InvariantCulture) : "Invalid";
    }
}
