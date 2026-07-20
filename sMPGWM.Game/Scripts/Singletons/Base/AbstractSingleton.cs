using System;
using Godot;
using Logger = sMPGWM.Scripts.Singletons.Autoload.Logger;

namespace sMPGWM.Scripts.Singletons.Base;

public abstract partial class AbstractSingleton<TSelf> : Node
    where TSelf : AbstractSingleton<TSelf>
{
    public static TSelf Instance { get; private set; } = null!;

    public sealed override void _EnterTree()
    {
        Logger.Trace($"{nameof(AbstractSingleton<TSelf>)}.{nameof(_EnterTree)} called for {typeof(TSelf).Name}.");

        if (Instance != null)
        {
            Logger.Error($"{nameof(AbstractSingleton<TSelf>)}.{nameof(_EnterTree)} reached duplicate singleton for {typeof(TSelf).Name}.");
            throw new InvalidOperationException($"{typeof(TSelf).Name} autoload already exists.");
        }

        Instance = (TSelf)this;
        OnEnterTree();
    }

    public sealed override void _Ready()
    {
        Logger.Trace($"{nameof(AbstractSingleton<TSelf>)}.{nameof(_Ready)} called for {typeof(TSelf).Name}.");
        OnReady();
    }

    public sealed override void _ExitTree()
    {
        Logger.Trace($"{nameof(AbstractSingleton<TSelf>)}.{nameof(_ExitTree)} called for {typeof(TSelf).Name}.");
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
