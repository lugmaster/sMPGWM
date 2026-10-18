namespace sMPGWM.Scripts.TagSystem.Queries;

/// <summary>
/// Selects whether a query examines direct assignments or assignments expanded to include ancestors.
/// </summary>
public enum TagQueryMatchMode
{
    /// <summary>Matches direct assignments and their canonical ancestors.</summary>
    Expanded = 0,
    /// <summary>Matches only tags explicitly assigned to the set.</summary>
    Direct = 1
}
