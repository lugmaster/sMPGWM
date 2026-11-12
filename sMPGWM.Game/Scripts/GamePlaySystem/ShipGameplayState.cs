using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ShipGameplayState
{
    public const int MaxBehaviours = 64;
    public const int MaxConditions = 64;
    public const int PhysicsStepsPerSecond = 60;
    private readonly List<BehaviourInstance> _behaviours = new();
    private readonly List<string> _conditions = new();
    private readonly ResourcePool _shield;
    private readonly ResourcePool _energy = new(0, 0);
    private ulong _batteryIdentity;
    private bool _initialized;
    private long _lastResourceTick = -1;
    public IReadOnlyList<BehaviourInstance> ActiveBehaviours { get; }
    public IReadOnlyList<string> Conditions { get; }
    public AbilityGrants Grants { get; } = new();
    public ResourceState Shield => new(_shield.BaseCapacity, _shield.Capacity, _shield.Current);
    public ResourceState Energy => new(_energy.BaseCapacity, _energy.Capacity, _energy.Current);
    public ResolvedShipStats Stats { get; private set; }
    public bool EnergyDepleted { get; private set; }
    public bool AfterburnerActive { get; private set; }
    public double EnergyConsumption => Stats.GetEnergyConsumption(AfterburnerActive);
    public bool PoweredSystemsAvailable => !EnergyDepleted;
    public bool MovementAvailable => Stats.HasEngine && PoweredSystemsAvailable;
    public bool ShieldProtectionAvailable => Stats.HasShieldGenerator && PoweredSystemsAvailable &&
        _shield.Current > 0 && AuthoritativeTick >= ShieldDowntimeDeadline;
    public long ShieldDowntimeDeadline { get; private set; }
    public long AuthoritativeTick { get; private set; }
    public long ConfigurationRevision { get; private set; } = 1;
    public int FactionId { get; private set; }
    public bool IsAlive => !_conditions.Contains("dead");
    public long Revision { get; private set; } = 1;
    public event Action? Changed;

    public ShipGameplayState() : this(0, 0)
    {
        Stats = default;
        _shield.SetBaseline(0, false);
    }

    public ShipGameplayState(double capacity, double current)
    {
        _shield = new(capacity, current);
        Stats = new(0, 0, 0, 0, 0, 0, 0, capacity, 5000, 0, 0, 0, 0, false, false, true, false, false);
        ActiveBehaviours = _behaviours.AsReadOnly();
        Conditions = _conditions.AsReadOnly();
    }

    public void ApplyEquipment(ResolvedShipStats stats, ulong batteryIdentity)
    {
        if (!stats.IsValid) throw new ArgumentException("Invalid resolved equipment stats.", nameof(stats));
        ValidateShieldRate(_shield.PreviewCapacity(stats.ShieldCapacity, stats.HasShieldGenerator, stats.ShieldFlatModifier), stats);
        if (!stats.HasBattery) batteryIdentity = 0;
        if (Stats == stats && _batteryIdentity == batteryIdentity) return;
        var beforeMovement = MovementAvailable;
        var movementChanged = Stats.Speed != stats.Speed || Stats.AfterBurnerSpeed != stats.AfterBurnerSpeed ||
            Stats.RotationSpeed != stats.RotationSpeed || Stats.Acceleration != stats.Acceleration ||
            Stats.AfterBurnerAcceleration != stats.AfterBurnerAcceleration || Stats.Mass != stats.Mass || Stats.HasEngine != stats.HasEngine;
        _shield.SetBaseline(stats.ShieldCapacity, stats.HasShieldGenerator, stats.ShieldFlatModifier);
        _energy.SetBaseline(stats.EnergyStorage, stats.HasBattery);
        if (_batteryIdentity != batteryIdentity) _energy.SetCurrent(0);
        Stats = stats;
        if (!stats.HasEngine) AfterburnerActive = false;
        _batteryIdentity = batteryIdentity;
        RefreshPower();
        if (movementChanged || beforeMovement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
        Revision = checked(Revision + 1);
        PublishChanges();
    }

    public void InitializeResources()
    {
        if (_initialized) return;
        _initialized = true;
        _shield.SetCurrent(_shield.Capacity);
        _energy.SetCurrent(0);
        var beforeMovement = MovementAvailable;
        RefreshPower();
        if (beforeMovement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
        Revision = checked(Revision + 1);
        PublishChanges();
    }

    public void SetAfterburner(bool active)
    {
        active &= Stats.HasEngine;
        if (AfterburnerActive == active) return;
        var beforeMovement = MovementAvailable;
        // Keep demand while depleted so holding afterburner cannot oscillate the power state.
        AfterburnerActive = active;
        RefreshPower();
        if (beforeMovement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
        Revision = checked(Revision + 1);
        PublishChanges();
    }

    public bool AdvanceResources(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (tick <= _lastResourceTick || tick < AuthoritativeTick) return false;
        _lastResourceTick = tick;
        AuthoritativeTick = tick;
        var beforeEnergy = _energy.Current;
        var beforeShield = _shield.Current;
        var beforeDepleted = EnergyDepleted;
        var beforeMovement = MovementAvailable;
        var previousDeadline = ShieldDowntimeDeadline;
        var net = Stats.EnergyProduction - EnergyConsumption;
        var delta = net / PhysicsStepsPerSecond;
        _energy.SetCurrent(delta >= 0 ? _energy.Current + Math.Min(delta, _energy.Capacity - _energy.Current) : Math.Max(0, _energy.Current + delta));
        RefreshPower();
        if (Stats.HasShieldGenerator && _shield.Capacity > 0)
        {
            var amount = _shield.Capacity * Math.Min(1, 1000d / PhysicsStepsPerSecond / Stats.RechargeTime);
            if (EnergyDepleted) ChangeShield(Math.Max(0, _shield.Current - amount));
            else if (tick >= ShieldDowntimeDeadline) ChangeShield(_shield.Current + Math.Min(amount, _shield.Capacity - _shield.Current));
        }
        if (beforeMovement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
        var changed = beforeEnergy != _energy.Current || beforeShield != _shield.Current ||
            beforeDepleted != EnergyDepleted || previousDeadline != ShieldDowntimeDeadline || tick == ShieldDowntimeDeadline;
        if (!changed) return false;
        Revision = checked(Revision + 1);
        PublishChanges();
        return true;
    }

    private void RefreshPower()
    {
        EnergyDepleted = Stats.EnergyProduction < EnergyConsumption && _energy.Current == 0;
        var index = _conditions.IndexOf("energyDepleted");
        if (EnergyDepleted && index < 0) _conditions.Add("energyDepleted");
        else if (!EnergyDepleted && index >= 0) _conditions.RemoveAt(index);
    }

    private void ChangeShield(double current)
    {
        var depleted = _shield.Current > 0 && current == 0 && Stats.HasShieldGenerator && _shield.Capacity > 0;
        var deadline = depleted ? checked(AuthoritativeTick + (long)Math.Ceiling(Stats.DownTime * PhysicsStepsPerSecond / 1000d)) : ShieldDowntimeDeadline;
        _shield.SetCurrent(current);
        ShieldDowntimeDeadline = deadline;
    }

    private static void ValidateShieldRate(double capacity, ResolvedShipStats stats)
    {
        if (stats.HasShieldGenerator && !double.IsFinite(capacity * (1000d / PhysicsStepsPerSecond / stats.RechargeTime)))
            throw new InvalidOperationException("Shield regeneration rate overflow.");
    }

    public bool IsAllyOf(ShipGameplayState other) => ReferenceEquals(this, other) ||
        FactionId != 0 && FactionId == other.FactionId;

    public bool SetFaction(int factionId)
    {
        if (factionId < 0) throw new ArgumentOutOfRangeException(nameof(factionId));
        if (FactionId == factionId) return false;
        var revision = checked(Revision + 1);
        FactionId = factionId;
        Revision = revision;
        PublishChanges();
        return true;
    }

    public bool SetCondition(ConditionDefinition condition, bool active)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (string.IsNullOrWhiteSpace(condition.Id) || condition.Id.Length > 64)
            throw new ArgumentException("Invalid condition ID.", nameof(condition));
        if (condition.Id == "energyDepleted") throw new InvalidOperationException("Energy depletion is derived from the power supply.");
        var index = _conditions.IndexOf(condition.Id);
        if (active == (index >= 0)) return false;
        if (active && _conditions.Count >= MaxConditions - 1) throw new InvalidOperationException("Condition limit reached.");
        var revision = checked(Revision + 1);
        if (active) _conditions.Add(condition.Id);
        else _conditions.RemoveAt(index);
        Revision = revision;
        PublishChanges();
        return true;
    }

    public bool SetShieldCurrent(double current)
    {
        if (current == _shield.Current) return false;
        var revision = checked(Revision + 1);
        ChangeShield(current);
        Revision = revision;
        PublishChanges();
        return true;
    }

    internal void PublishChanges() => Changed?.Invoke();

    internal bool ApplyBehaviour(SupportedBehaviour definition, long tick, long instance, out bool created)
    {
        if (tick < 0 || instance <= 0) throw new ArgumentOutOfRangeException(nameof(tick));
        var deadline = checked(tick + definition.DurationTicks);
        var nextRevision = checked(Revision + 1);
        created = false;
        for (var i = 0; i < _behaviours.Count; i++)
        {
            var active = _behaviours[i];
            if (active.DefinitionId != definition.Definition.Id) continue;
            _behaviours[i] = active with { Deadline = deadline };
            Revision = nextRevision;
            return true;
        }
        if (_behaviours.Count >= MaxBehaviours) return false;
        _shield.SetContribution(instance, definition.Flat, definition.Fraction);
        try { ValidateShieldRate(_shield.Capacity, Stats); }
        catch { _shield.RemoveOwner(instance); throw; }
        _behaviours.Add(new(definition.Definition.Id, instance, deadline));
        created = true;
        Revision = nextRevision;
        return true;
    }

    public bool Expire(long tick)
    {
        if (tick < AuthoritativeTick) return false;
        AuthoritativeTick = tick;
        var changed = false;
        for (var i = _behaviours.Count - 1; i >= 0; i--)
        {
            var active = _behaviours[i];
            if (tick < active.Deadline) continue;
            _shield.RemoveOwner(active.RuntimeId);
            _behaviours.RemoveAt(i);
            changed = true;
        }
        if (!changed) return false;
        Revision = checked(Revision + 1);
        PublishChanges();
        return true;
    }

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours)
        => Confirm(revision, capacity, current, behaviours, FactionId, Conditions);

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours,
        int factionId, IReadOnlyList<string> conditions)
    {
        if (revision <= Revision) return false;
        if (behaviours.Count > MaxBehaviours) throw new ArgumentException("Behaviour count exceeds protocol limit.");
        if (factionId < 0 || conditions.Count > MaxConditions) throw new ArgumentException("Invalid eligibility state.");
        _shield.Confirm(capacity, current);
        CopyConfirmedState(revision, behaviours, factionId, conditions);
        PublishChanges();
        return true;
    }

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours,
        int factionId, IReadOnlyList<string> conditions, ResolvedShipStats stats, double energyCurrent,
        bool energyDepleted, bool shieldProtectionAvailable, long shieldDowntimeDeadline, long authoritativeTick,
        long configurationRevision, bool afterburnerActive)
    {
        if (revision <= Revision || authoritativeTick < AuthoritativeTick || configurationRevision < ConfigurationRevision) return false;
        if (!stats.IsValid || !double.IsFinite(capacity) || capacity < 0 || !double.IsFinite(current) || current < 0 || current > capacity ||
            !double.IsFinite(energyCurrent) || energyCurrent < 0 || energyCurrent > stats.EnergyStorage ||
            shieldDowntimeDeadline < 0 || authoritativeTick < 0 || configurationRevision < 1 ||
            behaviours.Count > MaxBehaviours || factionId < 0 || conditions.Count > MaxConditions ||
            (!stats.HasShieldGenerator && (capacity != 0 || current != 0)) ||
            afterburnerActive && !stats.HasEngine ||
            energyDepleted != (stats.EnergyProduction < stats.GetEnergyConsumption(afterburnerActive) && energyCurrent == 0) ||
            shieldProtectionAvailable != (stats.HasShieldGenerator && !energyDepleted && current > 0 && authoritativeTick >= shieldDowntimeDeadline))
            throw new ArgumentException("Invalid confirmed gameplay state.");
        var hasDepleted = false;
        for (var i = 0; i < conditions.Count; i++) if (conditions[i] == "energyDepleted") hasDepleted = true;
        if (hasDepleted != energyDepleted) throw new ArgumentException("Inconsistent energy condition.");
        ValidateShieldRate(capacity, stats);
        _shield.Confirm(stats.ShieldCapacity, capacity, current, stats.HasShieldGenerator, stats.ShieldFlatModifier);
        _energy.Confirm(stats.EnergyStorage, stats.EnergyStorage, energyCurrent, stats.HasBattery);
        Stats = stats;
        AfterburnerActive = afterburnerActive;
        EnergyDepleted = energyDepleted;
        ShieldDowntimeDeadline = shieldDowntimeDeadline;
        AuthoritativeTick = authoritativeTick;
        ConfigurationRevision = configurationRevision;
        _initialized = true;
        CopyConfirmedState(revision, behaviours, factionId, conditions);
        PublishChanges();
        return true;
    }

    private void CopyConfirmedState(long revision, IReadOnlyList<BehaviourInstance> behaviours, int factionId, IReadOnlyList<string> conditions)
    {
        _behaviours.Clear();
        for (var i = 0; i < behaviours.Count; i++) _behaviours.Add(behaviours[i]);
        if (!ReferenceEquals(conditions, Conditions))
        {
            _conditions.Clear();
            for (var i = 0; i < conditions.Count; i++) _conditions.Add(conditions[i]);
        }
        FactionId = factionId;
        Revision = revision;
    }
}
