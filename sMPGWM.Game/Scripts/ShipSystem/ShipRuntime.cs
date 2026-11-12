using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipRuntime : IDisposable
{
    public const int MaxBehaviours = 64;
    public const int MaxConditions = 64;
    public const int PhysicsStepsPerSecond = 60;
    public ShipEffectState Effects { get; }
    private readonly List<string> _conditions = new(MaxConditions);
    private ResourcePool _shield => Resources.ShieldPool;
    private ResourcePool _energy => Resources.EnergyPool;
    private readonly ShipStatState _stats = new();
    private ShipComponentKind? _pendingComponents;
    private bool _disposed;
    public ShipResources Resources { get; }
    public ShipComponentCollection Components { get; } = new();
    public bool AbilityAdmissionAvailable => !_disposed && Components.AbilityAdmission != null;
    public ResolvedShipStats EquipmentInputs => _stats.EquipmentInputs;
    private ulong _batteryIdentity;
    private bool _initialized;
    private long _lastResourceTick = -1;
    public IReadOnlyList<BehaviourInstance> ActiveBehaviours => Effects.Active;
    public IReadOnlyList<string> Conditions { get; }
    public AbilityGrants Grants { get; } = new();
    public ResourceState Shield => new(EquipmentInputs.ShieldCapacity, _shield.Capacity, _shield.Current);
    public ResourceState Energy => new(EquipmentInputs.EnergyStorage, _energy.Capacity, _energy.Current);
    public ResolvedShipStats Stats => _stats.Effective;
    public bool EnergyDepleted { get; private set; }
    public bool AfterburnerActive { get; private set; }
    public double EnergyConsumption => Stats.GetEnergyConsumption(AfterburnerActive);
    public bool PoweredSystemsAvailable => Components.Power != null && !EnergyDepleted;
    public bool MovementAvailable => Components.Movement?.Available(Stats, PoweredSystemsAvailable) == true;
    public bool ShieldProtectionAvailable => Components.Shield != null && Stats.HasShieldGenerator && PoweredSystemsAvailable &&
        _shield.Current > 0 && AuthoritativeTick >= ShieldDowntimeDeadline;
    public long ShieldDowntimeDeadline { get; private set; }
    public long AuthoritativeTick { get; private set; }
    public long ConfigurationRevision { get; private set; } = 1;
    public int FactionId { get; private set; }
    public bool IsAlive => !_disposed && !_conditions.Contains("dead");
    public long Revision { get; private set; } = 1;
    public event Action? Changed;

    public ShipRuntime() : this(0, 0)
    {
        _stats.SetEquipment(default);
        _shield.SetCapacity(0);
    }

    public ShipRuntime(double capacity, double current)
    {
        Resources = new(capacity, current);
        _stats.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, capacity, 5000, 0, 0, 0, 0, false, false, true, false, false));
        Effects = new(_stats);
        Conditions = _conditions.AsReadOnly();
    }

    public ShipRuntimeState CaptureState()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_initialized || _committing) throw new InvalidOperationException("Capture requires initialized gameplay state between commits.");
        var behaviours = new BehaviourInstance[ActiveBehaviours.Count];
        for (var i = 0; i < behaviours.Length; i++) behaviours[i] = ActiveBehaviours[i];
        return new(EquipmentInputs, _batteryIdentity, _shield.Current, _energy.Current,
            Revision, ConfigurationRevision, AuthoritativeTick, _lastResourceTick, ShieldDowntimeDeadline,
            FactionId, AfterburnerActive, _initialized, Components.Presence, _pendingComponents,
            Array.AsReadOnly(behaviours), Array.AsReadOnly(_conditions.ToArray()), Grants.CaptureState());
    }

    public void RestoreState(ShipRuntimeState state, GameplayCatalogues definitions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(definitions);
        if (_initialized || _committing) throw new InvalidOperationException("Restore requires an uninitialized authoritative runtime.");
        if (!state.Initialized || state.EquipmentInputs != EquipmentInputs || state.BatteryIdentity != _batteryIdentity ||
            state.Revision < 1 || state.Revision == long.MaxValue || state.ConfigurationRevision < 1 || state.ConfigurationRevision == long.MaxValue ||
            state.AuthoritativeTick < 0 || state.LastResourceTick < -1 || state.LastResourceTick > state.AuthoritativeTick ||
            state.ShieldDowntimeDeadline < 0 || state.FactionId < 0 || (state.Components & ~ShipComponentKind.All) != 0 ||
            state.PendingComponents is { } pending && (pending & ~ShipComponentKind.All) != 0 ||
            state.AfterburnerActive && (!state.EquipmentInputs.HasEngine || (state.Components & ShipComponentKind.Movement) == 0) ||
            state.Behaviours == null || state.Behaviours.Count > MaxBehaviours || state.Conditions == null || state.Conditions.Count > MaxConditions || state.Grants == null)
            throw new ArgumentException("Invalid restored runtime or equipment mismatch.");

        var prepared = new ShipStatState();
        prepared.SetEquipment(state.EquipmentInputs);
        prepared.SetEffectsEnabled((state.Components & ShipComponentKind.Effects) != 0);
        var behaviours = new BehaviourInstance[state.Behaviours.Count];
        var identities = new HashSet<long>();
        var behaviourIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < behaviours.Length; i++)
        {
            var instance = state.Behaviours[i];
            if (instance.RuntimeId <= 0 || instance.RuntimeId == long.MaxValue || instance.Deadline < 0 ||
                string.IsNullOrWhiteSpace(instance.DefinitionId) || !identities.Add(instance.RuntimeId) || !behaviourIds.Add(instance.DefinitionId))
                throw new ArgumentException("Invalid or duplicate restored behaviour instance.");
            SupportedBehaviour? supported = null;
            foreach (var ability in definitions.Abilities.GetAll())
                if (definitions.Abilities.TryGetPlan(ability.Id, out var plan) && plan.Behaviour.Definition.Id == instance.DefinitionId)
                {
                    supported = plan.Behaviour;
                    break;
                }
            if (supported == null) throw new ArgumentException("Unsupported restored behaviour: " + instance.DefinitionId);
            prepared.SetContribution(new(StatContributionKind.Effect, instance.RuntimeId), supported.Flat, supported.Fraction);
            behaviours[i] = instance;
        }
        var stats = prepared.Effective;
        ValidateShieldRate(stats.ShieldCapacity, stats);
        if (!double.IsFinite(state.ShieldCurrent) || state.ShieldCurrent < 0 || state.ShieldCurrent > stats.ShieldCapacity ||
            !double.IsFinite(state.EnergyCurrent) || state.EnergyCurrent < 0 || state.EnergyCurrent > stats.EnergyStorage)
            throw new ArgumentException("Invalid restored resource values.");
        var depleted = stats.EnergyProduction < stats.GetEnergyConsumption(state.AfterburnerActive) && state.EnergyCurrent == 0;
        var conditions = new string[state.Conditions.Count];
        var conditionIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < conditions.Length; i++)
        {
            var id = state.Conditions[i];
            if (string.IsNullOrWhiteSpace(id) || !definitions.Conditions.TryGetById(id, out _) || !conditionIds.Add(id))
                throw new ArgumentException("Invalid restored condition: " + id);
            conditions[i] = id;
        }
        if (conditionIds.Contains("energyDepleted") != depleted)
            throw new ArgumentException("Restored energy condition is inconsistent.");
        if (state.PendingComponents is { } proposed)
            prepared.Preview(state.EquipmentInputs, (proposed & ShipComponentKind.Effects) != 0);
        var installComponents = Components.Prepare(state.Components);
        var installGrants = Grants.PrepareRestore(state.Grants, definitions);

        _stats.Restore(prepared);
        Effects.Confirm(behaviours);
        installComponents();
        installGrants();
        _conditions.Clear();
        _conditions.AddRange(conditions);
        _shield.Confirm(stats.ShieldCapacity, state.ShieldCurrent);
        _energy.Confirm(stats.EnergyStorage, state.EnergyCurrent);
        _pendingComponents = state.PendingComponents;
        _lastResourceTick = state.LastResourceTick;
        ShieldDowntimeDeadline = state.ShieldDowntimeDeadline;
        AuthoritativeTick = state.AuthoritativeTick;
        AfterburnerActive = state.AfterburnerActive;
        EnergyDepleted = depleted;
        FactionId = state.FactionId;
        ConfigurationRevision = state.ConfigurationRevision;
        Revision = state.Revision;
        _initialized = true;
        PublishChanges();
    }

    public void ApplyEquipment(ResolvedShipStats stats, ulong batteryIdentity, bool publish = true)
    {
        ValidateEquipment(stats, batteryIdentity);
        if (!stats.HasBattery) batteryIdentity = 0;
        if (EquipmentInputs == stats && _batteryIdentity == batteryIdentity) return;
        var beforeMovement = MovementAvailable;
        var movementChanged = Stats.Speed != stats.Speed || Stats.AfterBurnerSpeed != stats.AfterBurnerSpeed ||
            Stats.RotationSpeed != stats.RotationSpeed || Stats.Acceleration != stats.Acceleration ||
            Stats.AfterBurnerAcceleration != stats.AfterBurnerAcceleration || Stats.Mass != stats.Mass || Stats.HasEngine != stats.HasEngine;
        _stats.SetEquipment(stats);
        _shield.SetCapacity(Stats.ShieldCapacity);
        _energy.SetCapacity(stats.EnergyStorage);
        if (_batteryIdentity != batteryIdentity) _energy.SetCurrent(0);
        if (!stats.HasEngine) AfterburnerActive = false;
        _batteryIdentity = batteryIdentity;
        RefreshPower();
        if (movementChanged || beforeMovement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
        Revision = checked(Revision + 1);
        if (publish) PublishChanges();
    }

    public void ValidateEquipment(ResolvedShipStats stats, ulong batteryIdentity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resolved = _stats.Preview(stats);
        ValidateShieldRate(resolved.ShieldCapacity, resolved);
        _ = checked(Revision + 1);
        _ = checked(ConfigurationRevision + 1);
    }

    public void InitializeResources()
    {
        if (_disposed || _initialized) return;
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
        if (_disposed) return;
        active &= Stats.HasEngine && Components.Movement != null;
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
        if (_disposed) return false;
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (tick <= _lastResourceTick || tick < AuthoritativeTick) return false;
        _lastResourceTick = tick;
        AuthoritativeTick = tick;
        var beforeEnergy = _energy.Current;
        var beforeShield = _shield.Current;
        var beforeDepleted = EnergyDepleted;
        var beforeMovement = MovementAvailable;
        var previousDeadline = ShieldDowntimeDeadline;
        Components.Power?.Advance(_energy, Stats.EnergyProduction, EnergyConsumption);
        RefreshPower();
        if (Components.Shield is { } shieldSystem)
            ChangeShield(shieldSystem.Advance(_shield, Stats, PoweredSystemsAvailable, tick, ShieldDowntimeDeadline));
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

    public bool IsAllyOf(ShipRuntime other) => ReferenceEquals(this, other) ||
        FactionId != 0 && FactionId == other.FactionId;

    public bool SetFaction(int factionId)
    {
        if (_disposed) return false;
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
        if (_disposed) return false;
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
        if (_disposed) return false;
        if (current == _shield.Current) return false;
        var revision = checked(Revision + 1);
        ChangeShield(current);
        Revision = revision;
        PublishChanges();
        return true;
    }

    private bool _committing;
    internal void PublishChanges() { if (!_committing && !_disposed) Changed?.Invoke(); }

    internal bool ApplyBehaviour(SupportedBehaviour definition, long tick, long instance, out bool created)
    {
        if (_disposed) { created = false; return false; }
        var nextRevision = checked(Revision + 1);
        if (!Effects.Apply(definition, tick, instance, out created)) return false;
        _shield.SetCapacity(Stats.ShieldCapacity);
        Revision = nextRevision;
        return true;
    }

    public bool Expire(long tick)
    {
        if (_disposed || tick < AuthoritativeTick) return false;
        AuthoritativeTick = tick;
        if (!Effects.Expire(tick)) return false;
        _shield.SetCapacity(Stats.ShieldCapacity);
        Revision = checked(Revision + 1);
        PublishChanges();
        return true;
    }

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours,
        int factionId, IReadOnlyList<string> conditions, ResolvedShipStats stats, double energyCurrent,
        bool energyDepleted, bool shieldProtectionAvailable, long shieldDowntimeDeadline, long authoritativeTick,
        long configurationRevision, bool afterburnerActive, ShipComponentKind components = ShipComponentKind.All)
    {
        if (_disposed || revision <= Revision || authoritativeTick < AuthoritativeTick || configurationRevision < ConfigurationRevision) return false;
        if ((components & ~ShipComponentKind.All) != 0 || !stats.IsValid || stats.ShieldFlatModifier != 0 || capacity != stats.ShieldCapacity || !double.IsFinite(capacity) || capacity < 0 || !double.IsFinite(current) || current < 0 || current > capacity ||
            !double.IsFinite(energyCurrent) || energyCurrent < 0 || energyCurrent > stats.EnergyStorage ||
            shieldDowntimeDeadline < 0 || authoritativeTick < 0 || configurationRevision < 1 ||
            behaviours.Count > MaxBehaviours || factionId < 0 || conditions.Count > MaxConditions ||
            (!stats.HasShieldGenerator && (capacity != 0 || current != 0)) ||
            afterburnerActive && (!stats.HasEngine || (components & ShipComponentKind.Movement) == 0) ||
            energyDepleted != (stats.EnergyProduction < stats.GetEnergyConsumption(afterburnerActive) && energyCurrent == 0) ||
            shieldProtectionAvailable != ((components & (ShipComponentKind.Power | ShipComponentKind.Shield)) == (ShipComponentKind.Power | ShipComponentKind.Shield) && stats.HasShieldGenerator && !energyDepleted && current > 0 && authoritativeTick >= shieldDowntimeDeadline))
            throw new ArgumentException("Invalid confirmed gameplay state.");
        var hasDepleted = false;
        for (var i = 0; i < conditions.Count; i++) if (conditions[i] == "energyDepleted") hasDepleted = true;
        if (hasDepleted != energyDepleted) throw new ArgumentException("Inconsistent energy condition.");
        ValidateShieldRate(capacity, stats);
        _stats.Confirm(stats);
        Components.Install(components);
        _pendingComponents = null;
        _shield.Confirm(capacity, current);
        _energy.Confirm(stats.EnergyStorage, energyCurrent);
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

    public bool StageComponentChange(ShipComponentKind component, bool attached)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (component == ShipComponentKind.None || (component & ~ShipComponentKind.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(component));
        var current = _pendingComponents ?? Components.Presence;
        var proposed = attached ? current | component : current & ~component;
        if (current == proposed) return false;
        _stats.Preview(EquipmentInputs, (proposed & ShipComponentKind.Effects) != 0);
        _pendingComponents = proposed;
        return true;
    }

    public bool CommitConfiguration(long tick)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_disposed || tick < AuthoritativeTick) return false;
        var before = Revision;
        var movement = MovementAvailable;
        _committing = true;
        try
        {
            Expire(tick);
            if (_pendingComponents is { } proposed && proposed != Components.Presence)
            {
                var install = Components.Prepare(proposed);
                _ = checked(Revision + 1);
                _ = checked(ConfigurationRevision + 1);
                EffectProcessingSystem.ResolvePresence(_stats, (proposed & ShipComponentKind.Effects) != 0);
                install();
                _shield.SetCapacity(Stats.ShieldCapacity);
                if (Components.Movement == null) AfterburnerActive = false;
                RefreshPower();
                if (movement != MovementAvailable) ConfigurationRevision = checked(ConfigurationRevision + 1);
                Revision = checked(Revision + 1);
            }
            _pendingComponents = null;
        }
        finally { _committing = false; }
        if (before != Revision) PublishChanges();
        return before != Revision;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pendingComponents = null;
        Components.Dispose();
        _stats.Clear();
        _stats.Confirm(default);
        _shield.SetCapacity(0);
        _energy.SetCapacity(0);
        Effects.Clear();
        _conditions.Clear();
        Grants.Clear();
        Changed = null;
    }

    private void CopyConfirmedState(long revision, IReadOnlyList<BehaviourInstance> behaviours, int factionId, IReadOnlyList<string> conditions)
    {
        Effects.Clear();
        Effects.Confirm(behaviours);
        if (!ReferenceEquals(conditions, Conditions))
        {
            _conditions.Clear();
            for (var i = 0; i < conditions.Count; i++) _conditions.Add(conditions[i]);
        }
        FactionId = factionId;
        Revision = revision;
    }
}
