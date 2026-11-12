using System.Collections.Generic;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UtilitySystem.Mocks;

internal sealed class DevelopmentGameFixture(PlayerMock player, bool populateInventory) : IGameFixture
{
    private double _seconds;
    private double _nextLog = 3;
    private bool _sent;
    public IReadOnlyList<IconDefinition> Actions => player.GetHotBarSkills();

    public void Populate(InventoryController inventory, MessageController messages, bool includeItems)
    {
        if (includeItems && populateInventory) player.PopulateInventory(inventory);
        MessageMock.PopulateWelcomeMessage(messages);
    }

    public void ActivateAction(string key) => player.OnHotbarSlotPressed(int.Parse(key));

    public void Tick(double delta, MessageController messages)
    {
        if (_sent) return;
        _seconds += delta;
        if (_seconds >= _nextLog)
        {
            Logger.Info($"Mock message timer: {_seconds:F1}/30 seconds of active gameplay.");
            _nextLog += 3;
        }
        if (_seconds < 30) return;
        _sent = true;
        Logger.Info("Mock message timer: sending acknowledgement message.");
        MessageMock.SendAcknowledgementMessage(messages);
        Logger.Info("Mock message timer: send completed.");
    }
}
