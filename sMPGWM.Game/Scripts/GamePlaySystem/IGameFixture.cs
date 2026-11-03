using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.GamePlaySystem;

internal interface IGameFixture
{
    IReadOnlyList<IconDefinition> Actions { get; }
    void Populate(InventoryController inventory, MessageController messages);
    void ActivateAction(string key);
    void Tick(double delta, MessageController messages);
}

internal sealed class EmptyGameFixture : IGameFixture
{
    public IReadOnlyList<IconDefinition> Actions => Array.Empty<IconDefinition>();
    public void Populate(InventoryController inventory, MessageController messages) { }
    public void ActivateAction(string key) { }
    public void Tick(double delta, MessageController messages) { }
}
