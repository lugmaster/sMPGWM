using System;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.HotbarSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.UiSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed record GameSession(
    PlayerController Player, ItemRequestService Items, ItemController ItemController,
    InventoryController Inventory, EquipmentController Equipment, ItemHotbarController Hotbar,
    MessageController Messages, NetworkManager Network, IGameFixture Fixture);

internal static class GameSessionFactory
{
    public static GameSession Create(SessionScope scope, Node parent, UiCanvas canvas, Node world,
        NetworkStartupData startup, SessionCoordinator coordinator, IGameFixture fixture)
    {
        var items = new ItemRequestService();
        var itemController = new ItemController(items);
        var inventory = scope.Own(new InventoryController(items));
        var equipment = scope.Own(new EquipmentController(items));
        var transfers = scope.Own(new InventoryEquipmentTransferService(inventory, equipment));
        var hotbar = scope.Own(new ItemHotbarController(inventory, equipment, transfers,
            error => Logger.Error($"Hotbar observer failed: {error}")));
        var player = new PlayerController { Name = "PlayerController" };
        scope.Defer(() => ReleaseNode(player));
        scope.Defer(player.Shutdown);
        var messages = scope.Own(new MessageController(canvas, player.InputPolicy,
            error => Logger.Error($"Message observer failed: {error}")));
        fixture.Populate(inventory, messages);
        scope.Defer(() =>
        {
            if (GodotObject.IsInstanceValid(canvas)) canvas.UnbindInventory();
        });
        canvas.BindInventory(inventory, equipment, transfers);
        player.Initialize(canvas, transfers, position => hotbar.Activate(position, fixture.ActivateAction,
            request => Logger.Info($"Item hotbar activation requested: {request}")), world);
        parent.AddChild(player);
        canvas.BindHotbar(hotbar, fixture.Actions);
        player.SetSessionActive(false);

        var participants = new MpManager { Name = nameof(MpManager), LocalPlayer = player };
        scope.Defer(() => ReleaseNode(participants));
        scope.Defer(participants.Clear);
        world.AddChild(participants);
        var network = new NetworkManager { Name = nameof(NetworkManager), Startup = startup, World = participants };
        scope.Defer(() => ReleaseNode(network));
        scope.Defer(network.StopSession);
        var notifications = new SessionNotifications(messages);
        network.Failed += coordinator.Fail;
        network.SessionReady += coordinator.CompleteLoading;
        network.MembershipChanged += notifications.MembershipChanged;
        network.LocalJoined += notifications.LocalJoined;
        parent.AddChild(network);
        return new GameSession(player, items, itemController, inventory, equipment, hotbar, messages, network, fixture);
    }

    private static void ReleaseNode(Node node)
    {
        if (!GodotObject.IsInstanceValid(node)) return;
        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        // May be called from an RPC or signal on this node. Free after its callback returns.
        if (node.IsInsideTree()) node.QueueFree();
        else node.Free();
    }
}
