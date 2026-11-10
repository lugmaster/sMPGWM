using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record BehaviourSelector(IReadOnlyList<string> ClassificationIds, BehaviourDisposition? Disposition, IReadOnlyList<string> ModifiedStatIds);
