using System;
using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

public sealed class UiWindowRegistry
{
    private readonly Dictionary<UiWindowId, bool> _windows = new();
    private readonly List<UiWindowId> _focusOrder = new();

    public event Action<bool>? BlockingChanged;

    public IReadOnlyList<UiWindowId> FocusOrder => _focusOrder.AsReadOnly();
    public UiWindowId? Topmost => _focusOrder.Count == 0 ? null : _focusOrder[^1];
    public bool HasBlockingWindow => _windows.Values.Any(blocks => blocks);

    public bool IsOpen(UiWindowId id) => _windows.ContainsKey(id);

    public bool Open(UiWindowId id, bool blocksPlayerControls = true)
    {
        if (IsOpen(id))
        {
            Focus(id);
            return false;
        }

        var wasBlocking = HasBlockingWindow;
        _windows.Add(id, blocksPlayerControls);
        _focusOrder.Add(id);
        NotifyBlockingChanged(wasBlocking);
        return true;
    }

    public void Focus(UiWindowId id)
    {
        if (!IsOpen(id)) return;
        _focusOrder.Remove(id);
        _focusOrder.Add(id);
    }

    public bool Close(UiWindowId id)
    {
        var wasBlocking = HasBlockingWindow;
        if (!_windows.Remove(id)) return false;
        _focusOrder.Remove(id);
        NotifyBlockingChanged(wasBlocking);
        return true;
    }

    public void CloseAll()
    {
        var wasBlocking = HasBlockingWindow;
        _windows.Clear();
        _focusOrder.Clear();
        NotifyBlockingChanged(wasBlocking);
    }

    private void NotifyBlockingChanged(bool wasBlocking)
    {
        if (wasBlocking != HasBlockingWindow)
            BlockingChanged?.Invoke(HasBlockingWindow);
    }
}
