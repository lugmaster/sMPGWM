using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using sMPGWM.Scripts.Gameplay.Tags;

namespace sMPGWM.Tests.TagSystem;

[TestFixture]
public class TagSystemIntegratedTests
{
    [Test]
    public void Contract_EntityConditionsAndEffectClassificationsRemainSeparateDomains()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var effectSet = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);

        conditionSet.Add(TagSystemFixtureFactory.Tag(registry, "Condition.Poisoned"));
        effectSet.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.Throws<GameplayTagException>(() =>
            conditionSet.Matches(TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal")));
    }

    [Test]
    public void Contract_EffectClassificationsAreStaticMetadataTagSets()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var effectSet = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);

        effectSet.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Debuff"));

        Assert.That(effectSet.HasExpanded(TagSystemFixtureFactory.Tag(registry, "Effect.Debuff")), Is.True);
        Assert.That(conditionSet.HasExpanded(TagSystemFixtureFactory.Tag(registry, "Condition.Poisoned")), Is.False);
    }

    [Test]
    public void Contract_BlockingByConditionUsesEntityConditionDomain()
    {
        var builder = TagSystemFixtureFactory.CreateSmallGameplayBuilder()
            .RegisterQuery(
                "query.condition.block_heal",
                TagSystemFixtureFactory.EntityCondition,
                any: ["Condition.Dead", "Condition.Incurable"]);

        var registry = builder.CompileAndFreeze();
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        conditionSet.Add(TagSystemFixtureFactory.Tag(registry, "Condition.Dead"));

        Assert.That(conditionSet.Matches(TagSystemFixtureFactory.Query(registry, "query.condition.block_heal")), Is.True);
    }

    [Test]
    public void Contract_RemovalByClassificationUsesEffectClassificationDomain()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var effectSet = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        effectSet.Add(TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"));

        Assert.That(effectSet.Matches(TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal")), Is.True);
    }

    [Test]
    public void Contract_TagSystemDoesNotOwnConditionContributorsOrEffectLifetime()
    {
        var publicMethods = typeof(GameplayTagSet).GetMethods();

        Assert.That(publicMethods.Select(m => m.Name), Does.Not.Contain("AddContributor"));
        Assert.That(publicMethods.Select(m => m.Name), Does.Not.Contain("RemoveContributor"));
        Assert.That(publicMethods.Select(m => m.Name), Does.Not.Contain("ExpireEffect"));
        Assert.That(publicMethods.Select(m => m.Name), Does.Not.Contain("StackEffect"));
        Assert.That(publicMethods.Select(m => m.Name), Does.Not.Contain("ExecuteEffect"));
    }

    [Test]
    public void Stress_30Tps_BaselineGameplayFixture_WritesBenchmarkArtifacts()
    {
        var registry = TagSystemFixtureFactory.CreateSmallGameplayRegistry();
        var result = RunBaselineStress(registry);
        var artifactDirectory = FindRepositoryRoot().CreateSubdirectory("artifacts").FullName;
        var markdownPath = Path.Combine(artifactDirectory, "tag_system_benchmark_report.md");
        var jsonPath = Path.Combine(artifactDirectory, "tag_system_benchmark_report.json");

        File.WriteAllText(markdownPath, CreateMarkdownReport(registry, result));
        File.WriteAllText(jsonPath, CreateJsonReport(registry, result));

        Assert.That(File.Exists(markdownPath), Is.True);
        Assert.That(File.Exists(jsonPath), Is.True);
        Assert.That(result.TicksRun, Is.EqualTo(120));
        Assert.That(result.TotalOperations, Is.GreaterThan(0));
        Assert.That(result.AllocatedBytesDuringMeasuredTicks, Is.GreaterThanOrEqualTo(0));
    }

    private static StressResult RunBaselineStress(GameplayTagRegistry registry)
    {
        var conditionSet = registry.CreateTagSet(TagSystemFixtureFactory.EntityCondition);
        var effectSet = registry.CreateTagSet(TagSystemFixtureFactory.EffectClassification);
        var actionSet = registry.CreateTagSet(TagSystemFixtureFactory.ActionClassification);
        var targetSet = registry.CreateTagSet(TagSystemFixtureFactory.TargetClassification);
        var cooldownSet = registry.CreateTagSet(TagSystemFixtureFactory.CooldownGroup);
        var damageSet = registry.CreateTagSet(TagSystemFixtureFactory.DamageKind);
        var factionSet = registry.CreateTagSet(TagSystemFixtureFactory.FactionRelation);

        var conditionTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Condition.Dead"),
            TagSystemFixtureFactory.Tag(registry, "Condition.Poisoned"),
            TagSystemFixtureFactory.Tag(registry, "Condition.Invulnerable"),
            TagSystemFixtureFactory.Tag(registry, "Condition.Incurable")
        };

        var effectTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Poison.Dot"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Damage.Thermal"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Debuff"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Immunity.Poison"),
            TagSystemFixtureFactory.Tag(registry, "Effect.Buff.ShieldCapacity")
        };

        var actionTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Action.Skill"),
            TagSystemFixtureFactory.Tag(registry, "Action.Offensive"),
            TagSystemFixtureFactory.Tag(registry, "Action.Defensive")
        };

        var targetTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Target.LivingEntity"),
            TagSystemFixtureFactory.Tag(registry, "Target.Hostile"),
            TagSystemFixtureFactory.Tag(registry, "Target.Friendly")
        };

        var cooldownTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Cooldown.Weapon.Light"),
            TagSystemFixtureFactory.Tag(registry, "Cooldown.Weapon.Heavy")
        };

        var damageTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Damage.Kinetic"),
            TagSystemFixtureFactory.Tag(registry, "Damage.Thermal"),
            TagSystemFixtureFactory.Tag(registry, "Damage.Explosive")
        };

        var factionTags = new[]
        {
            TagSystemFixtureFactory.Tag(registry, "Faction.Hostile"),
            TagSystemFixtureFactory.Tag(registry, "Faction.Friendly"),
            TagSystemFixtureFactory.Tag(registry, "Faction.Neutral")
        };

        var poisonRemoval = registry.GetQuery(TagSystemFixtureFactory.Query(registry, "query.effect.cleanse_poison_removal"));
        var deadOrIncurable = registry.GetQuery(TagSystemFixtureFactory.Query(registry, "query.condition.dead_or_incurable"));
        var isDamage = registry.GetQuery(TagSystemFixtureFactory.Query(registry, "query.effect.is_damage"));

        for (var i = 0; i < 8; i++)
        {
            WarmStressTick(conditionSet, effectSet, actionSet, targetSet, cooldownSet, damageSet, factionSet,
                conditionTags, effectTags, actionTags, targetTags, cooldownTags, damageTags, factionTags,
                poisonRemoval, deadOrIncurable, isDamage, i);
        }

        const int ticksToRun = 120;
        var tickDurations = new double[ticksToRun];
        var queryOperations = 0L;
        var addOperations = 0L;
        var removeOperations = 0L;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var tick = 0; tick < ticksToRun; tick++)
        {
            var started = Stopwatch.GetTimestamp();

            for (var operation = 0; operation < 32; operation++)
            {
                var selector = tick + operation;
                _ = conditionSet.Matches(deadOrIncurable);
                _ = effectSet.Matches(poisonRemoval);
                _ = effectSet.Matches(isDamage);
                queryOperations += 3;

                Mutate(conditionSet, conditionTags[selector % conditionTags.Length], selector, ref addOperations, ref removeOperations);
                Mutate(effectSet, effectTags[selector % effectTags.Length], selector + 1, ref addOperations, ref removeOperations);
                Mutate(actionSet, actionTags[selector % actionTags.Length], selector + 2, ref addOperations, ref removeOperations);
                Mutate(targetSet, targetTags[selector % targetTags.Length], selector + 3, ref addOperations, ref removeOperations);
                Mutate(cooldownSet, cooldownTags[selector % cooldownTags.Length], selector + 4, ref addOperations, ref removeOperations);
                Mutate(damageSet, damageTags[selector % damageTags.Length], selector + 5, ref addOperations, ref removeOperations);
                Mutate(factionSet, factionTags[selector % factionTags.Length], selector + 6, ref addOperations, ref removeOperations);
            }

            tickDurations[tick] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var totalOperations = queryOperations + addOperations + removeOperations;
        Array.Sort(tickDurations);

        return new StressResult(
            ticksToRun,
            totalOperations,
            queryOperations,
            addOperations,
            removeOperations,
            Mean(tickDurations),
            Percentile(tickDurations, 0.50),
            Percentile(tickDurations, 0.95),
            Percentile(tickDurations, 0.99),
            tickDurations[^1],
            allocatedBytes);
    }

    private static void WarmStressTick(
        GameplayTagSet conditionSet,
        GameplayTagSet effectSet,
        GameplayTagSet actionSet,
        GameplayTagSet targetSet,
        GameplayTagSet cooldownSet,
        GameplayTagSet damageSet,
        GameplayTagSet factionSet,
        GameplayTagId[] conditionTags,
        GameplayTagId[] effectTags,
        GameplayTagId[] actionTags,
        GameplayTagId[] targetTags,
        GameplayTagId[] cooldownTags,
        GameplayTagId[] damageTags,
        GameplayTagId[] factionTags,
        CompiledTagQuery poisonRemoval,
        CompiledTagQuery deadOrIncurable,
        CompiledTagQuery isDamage,
        int tick)
    {
        var addOperations = 0L;
        var removeOperations = 0L;
        _ = conditionSet.Matches(deadOrIncurable);
        _ = effectSet.Matches(poisonRemoval);
        _ = effectSet.Matches(isDamage);
        Mutate(conditionSet, conditionTags[tick % conditionTags.Length], tick, ref addOperations, ref removeOperations);
        Mutate(effectSet, effectTags[tick % effectTags.Length], tick + 1, ref addOperations, ref removeOperations);
        Mutate(actionSet, actionTags[tick % actionTags.Length], tick + 2, ref addOperations, ref removeOperations);
        Mutate(targetSet, targetTags[tick % targetTags.Length], tick + 3, ref addOperations, ref removeOperations);
        Mutate(cooldownSet, cooldownTags[tick % cooldownTags.Length], tick + 4, ref addOperations, ref removeOperations);
        Mutate(damageSet, damageTags[tick % damageTags.Length], tick + 5, ref addOperations, ref removeOperations);
        Mutate(factionSet, factionTags[tick % factionTags.Length], tick + 6, ref addOperations, ref removeOperations);
    }

    private static void Mutate(GameplayTagSet set, GameplayTagId tag, int selector, ref long addOperations, ref long removeOperations)
    {
        if ((selector & 1) == 0)
        {
            _ = set.TryAdd(tag);
            addOperations++;
            return;
        }

        _ = set.TryRemove(tag);
        removeOperations++;
    }

    private static double Mean(double[] sortedValues)
    {
        var sum = 0.0;
        for (var i = 0; i < sortedValues.Length; i++)
        {
            sum += sortedValues[i];
        }

        return sum / sortedValues.Length;
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        var index = (int)Math.Ceiling(sortedValues.Length * percentile) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "sMPGWM.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test directory.");
    }

    private static string CreateMarkdownReport(GameplayTagRegistry registry, StressResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Gameplay Tag System Benchmark Report");
        builder.AppendLine();
        builder.AppendLine("- Build/configuration summary: Debug test smoke scenario");
        builder.AppendLine($"- Runtime environment summary: {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}");
        builder.AppendLine($"- Registry summary: hash `{registry.RegistryHash}`, domains `{registry.Domains.Count}`, tags `{registry.Tags.Count}`, queries `{registry.Queries.Count}`");
        builder.AppendLine("- Scenario summary table");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Ticks | Operations | Mean ms | P95 ms | P99 ms | Max ms | Allocated bytes |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
        builder.AppendLine($"| Stress_30Tps_BaselineGameplayFixture | {result.TicksRun} | {result.TotalOperations} | {Invariant(result.MeanTickMs, "F4")} | {Invariant(result.P95TickMs, "F4")} | {Invariant(result.P99TickMs, "F4")} | {Invariant(result.MaxTickMs, "F4")} | {result.AllocatedBytesDuringMeasuredTicks} |");
        builder.AppendLine();
        builder.AppendLine("- First slowdown point: not evaluated by smoke benchmark");
        builder.AppendLine("- Allocation summary: measured tick allocation is reported above");
        builder.AppendLine("- Notes for follow-up optimization: run the full pressure sweep outside CI when larger fixtures exist");
        return builder.ToString();
    }

    private static string CreateJsonReport(GameplayTagRegistry registry, StressResult result)
    {
        return $$"""
        {
          "registryHash": "{{registry.RegistryHash}}",
          "environment": {
            "runtime": "{{RuntimeInformation.FrameworkDescription}}",
            "os": "{{RuntimeInformation.OSDescription}}",
            "cpu": "unknown",
            "configuration": "Debug"
          },
          "budget": {
            "targetTicksPerSecond": 30,
            "fullTickBudgetMs": 33.333,
            "tagSystemBudgetPercent": 10.0,
            "tagSystemBudgetMs": 3.333
          },
          "scenarios": [
            {
              "name": "Stress_30Tps_BaselineGameplayFixture",
              "parameters": {
                "ticksToRun": {{result.TicksRun}},
                "systemsPerTick": 7,
                "operationsPerInnerLoop": 32
              },
              "metrics": {
                "totalOperations": {{result.TotalOperations}},
                "queryOperations": {{result.QueryOperations}},
                "addOperations": {{result.AddOperations}},
                "removeOperations": {{result.RemoveOperations}},
                "meanTickMs": {{Invariant(result.MeanTickMs)}},
                "medianTickMs": {{Invariant(result.MedianTickMs)}},
                "p95TickMs": {{Invariant(result.P95TickMs)}},
                "p99TickMs": {{Invariant(result.P99TickMs)}},
                "maxTickMs": {{Invariant(result.MaxTickMs)}},
                "allocatedBytesDuringMeasuredTicks": {{result.AllocatedBytesDuringMeasuredTicks}}
              }
            }
          ]
        }
        """;
    }

    private static string Invariant(double value)
    {
        return Invariant(value, "F6");
    }

    private static string Invariant(double value, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    private sealed record StressResult(
        int TicksRun,
        long TotalOperations,
        long QueryOperations,
        long AddOperations,
        long RemoveOperations,
        double MeanTickMs,
        double MedianTickMs,
        double P95TickMs,
        double P99TickMs,
        double MaxTickMs,
        long AllocatedBytesDuringMeasuredTicks);
}
