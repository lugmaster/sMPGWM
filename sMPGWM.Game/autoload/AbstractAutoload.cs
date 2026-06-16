using System;
using Godot;

namespace sMPGWM.autoload;

public abstract partial class AbstractAutoload<TSelf> : Node, IAutoloaded
    where TSelf : AbstractAutoload<TSelf>
{
    public static TSelf Instance { get; private set; } = null!;

    public sealed override void _EnterTree()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{typeof(TSelf).Name} autoload already exists.");

        Instance = (TSelf)this;
        OnEnterTree();
    }

    public sealed override void _Ready()
    {
        OnReady();
    }

    public sealed override void _ExitTree()
    {
        OnExitTree();

        if (Instance == this)
            Instance = null!;
    }

    protected virtual void OnEnterTree()
    {
    }

    protected virtual void OnReady()
    {
    }

    protected virtual void OnExitTree()
    {
    }
}
