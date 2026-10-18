using System.Collections.Generic;
using sMPGWM.Scripts.TagSystem.Queries;

namespace sMPGWM.Scripts.TagSystem.Internal;

/// <summary>
/// Stores the copied tag selectors and matching options for an uncompiled query.
/// </summary>
internal sealed record QueryRegistration(
    string QueryKey,
    string DomainKey,
    IReadOnlyList<string> All,
    IReadOnlyList<string> Any,
    IReadOnlyList<string> None,
    TagQueryMatchMode MatchMode,
    bool MatchAll);
