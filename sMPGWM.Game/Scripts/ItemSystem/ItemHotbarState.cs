#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.ItemSystem;

public abstract record HotbarBinding;
public sealed record ConsumableHotbarBinding(ItemDefinitionId DefinitionId) : HotbarBinding;
public sealed record WeaponHotbarBinding(string SlotKey) : HotbarBinding;

public enum HotbarResult
{
    Success,
    NoChange,
    InvalidSession,
    StaleRevision,
    InvalidPosition,
    InvalidLocation,
    IneligibleSource,
    Unavailable,
    PublicationInProgress,
    Disposed
}

public static class ItemHotbarPolicy
{
    public static bool IsConsumable(ItemCategoryId category) => category == ItemCategories.Consumable;
    public static bool IsWeapon(ItemCategoryId category) => category == ItemCategories.LightWeapon || category == ItemCategories.HeavyWeapon;
}

public sealed class ItemHotbarState : IDisposable
{
    public const int PositionCount = 10;
    private Guid _session;
    private bool _publishing;
    private bool _disposed;
    private readonly Action<Exception>? _reportError;
    public IReadOnlyList<HotbarBinding?> Entries { get; private set; }
    public long Revision { get; private set; }
    public event Action? Changed;

    public ItemHotbarState(Action<Exception>? reportError = null) : this(Array.Empty<HotbarBinding?>(), reportError) { }

    internal ItemHotbarState(IEnumerable<HotbarBinding?> initialBindings, Action<Exception>? reportError = null)
    {
        var initial = initialBindings.ToArray();
        if (initial.Length > PositionCount)
            throw new ArgumentException("Initial bindings must fit the hotbar.", nameof(initialBindings));
        var entries = new HotbarBinding?[PositionCount];
        Array.Copy(initial, entries, initial.Length);
        Entries = Array.AsReadOnly(entries);
        _reportError = reportError;
    }

    public void SetEditSession(Guid session) => _session = _disposed ? Guid.Empty : session;

    public HotbarResult ValidateEdit(Guid session, long revision, int position)
    {
        if (_disposed) return HotbarResult.Disposed;
        if (_publishing) return HotbarResult.PublicationInProgress;
        if (_session == Guid.Empty || session != _session) return HotbarResult.InvalidSession;
        if (revision != Revision) return HotbarResult.StaleRevision;
        return position is >= 0 and < PositionCount ? HotbarResult.Success : HotbarResult.InvalidPosition;
    }

    internal HotbarResult ValidateAssignment(Guid session, long revision, int position, HotbarBinding binding)
    {
        var result = ValidateEdit(session, revision, position);
        if (result != HotbarResult.Success) return result;
        return Entries[position] == binding ? HotbarResult.NoChange : HotbarResult.Success;
    }

    internal HotbarResult Assign(Guid session, long revision, int position, HotbarBinding binding)
    {
        var result = ValidateAssignment(session, revision, position, binding);
        if (result != HotbarResult.Success) return result;
        var next = Entries.ToArray();
        next[position] = binding;
        return Publish(next);
    }

    public HotbarResult ValidateMove(Guid session, long revision, int source, int destination)
    {
        var result = ValidateEdit(session, revision, source);
        if (result != HotbarResult.Success) return result;
        result = ValidateEdit(session, revision, destination);
        if (result != HotbarResult.Success) return result;
        if (source == destination || Entries[source] == null || Entries[source] == Entries[destination]) return HotbarResult.NoChange;
        return HotbarResult.Success;
    }

    public HotbarResult Move(Guid session, long revision, int source, int destination)
    {
        var result = ValidateMove(session, revision, source, destination);
        if (result != HotbarResult.Success) return result;
        var next = Entries.ToArray();
        (next[source], next[destination]) = (next[destination], next[source]);
        return Publish(next);
    }

    public HotbarResult Clear(Guid session, long revision, int position)
    {
        var result = ValidateEdit(session, revision, position);
        if (result != HotbarResult.Success) return result;
        if (Entries[position] == null) return HotbarResult.NoChange;
        var next = Entries.ToArray();
        next[position] = null;
        return Publish(next);
    }

    private HotbarResult Publish(HotbarBinding?[] next)
    {
        var snapshot = Array.AsReadOnly(next);
        var revision = checked(Revision + 1);
        Entries = snapshot;
        Revision = revision;
        _publishing = true;
        try
        {
            if (Changed is { } handlers)
                foreach (Action observer in handlers.GetInvocationList())
                    try { observer(); }
                    catch (Exception exception)
                    {
                        // TODO: Add subscriber recovery through its owner (rebuild or disable a failed view).
                        // Preserve committed state and remaining notifications; also surface error-reporter failures.
                        try { _reportError?.Invoke(exception); }
                        catch (Exception) { }
                    }
        }
        finally { _publishing = false; }
        return HotbarResult.Success;
    }

    public void Dispose()
    {
        _disposed = true;
        _session = Guid.Empty;
        Changed = null;
    }
}
