using System.Collections.Generic;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Network;

public partial class MpManager : AbstractSingleton<MpManager>
{
    private readonly Dictionary<int, MpController> _controllers = new();
    private readonly Dictionary<int, PlayerStartupData> _startupData = new();
    public IReadOnlyDictionary<int, MpController> MpControllers => _controllers;
    public IReadOnlyDictionary<int, PlayerStartupData> StartupData => _startupData;

    public void Spawn(int peerId, PlayerStartupData data)
    {
        if (_controllers.ContainsKey(peerId)) return;
        var controller = new MpController { Name = $"Player_{peerId}" };
        AddChild(controller);
        _controllers.Add(peerId, controller);
        _startupData.Add(peerId, data);
        Logger.Info($"NET spawn peer={peerId} message='{data.Message}'");
    }

    public void Despawn(int peerId)
    {
        if (_controllers.Remove(peerId, out var controller))
        {
            RemoveChild(controller);
            controller.QueueFree();
        }
        _startupData.Remove(peerId);
    }

    public void Clear()
    {
        foreach (var peerId in new List<int>(_controllers.Keys)) Despawn(peerId);
    }

    protected override void OnExitTree() => Clear();
}
