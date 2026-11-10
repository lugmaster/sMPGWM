using Godot;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Hotbar;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.Parallax;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.World;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed record GameSession(
    PlayerController Player,
    MessageController Messages,
    NetworkManager Network,
    IGameFixture Fixture);

internal static class GameSessionFactory
{
    public static GameSession Create(SessionScope scope, Node parent, UiCanvas canvas, Node world,
        NetworkStartupData startup, SessionCoordinator coordinator, IGameFixture fixture)
    {
        var items = new ItemStore();
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
        NetworkManager? abilityNetwork = null;
        player.Initialize(canvas, transfers, position => hotbar.Activate(position, key =>
        {
            if (key == "0" && GameConfigSystem.GameConfig.Development) abilityNetwork?.ActivateAbility(GameplayDefinitions.Boost.AbilityId); // TEST-SUPPORT: SHIELD-LOADOUT-005 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            else fixture.ActivateAction(key);
        },
            request => Logger.Info($"Item hotbar activation requested: {request}")), world);
        parent.AddChild(player);
        if (GameConfigSystem.GameConfig.Development) // TEST-SUPPORT: SHIELD-LOADOUT-001 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        {
            hotbar.SetActionTitle("0", GameplayDefinitions.Boost.DisplayName); // TEST-SUPPORT: SHIELD-LOADOUT-002 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            hotbar.BindAction(0, "0"); // TEST-SUPPORT: SHIELD-LOADOUT-003 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            canvas.BindHotbar(hotbar, new[] { Util.UiComponents.Icons.Get(GameplayDefinitions.Boost.Icon) }); // TEST-SUPPORT: SHIELD-LOADOUT-004 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        }
        else canvas.BindHotbar(hotbar, fixture.Actions);
        player.SetSessionActive(false);

        var participants = new GameWorld { Name = nameof(GameWorld), LocalPlayer = player };
        scope.Defer(() => ReleaseNode(participants));
        scope.Defer(participants.Clear);
        participants.AddChild(new ParallaxMotion { Name = nameof(ParallaxMotion) });
        world.AddChild(participants);
        var network = new NetworkManager { Name = nameof(NetworkManager), Startup = startup, World = participants };
        scope.Defer(() => ReleaseNode(network));
        scope.Defer(network.StopSession);
        var synchronization = new SynchronizationCoordinator
        {
            Name = nameof(SynchronizationCoordinator), Player = player, World = participants, Network = network
        };
        scope.Defer(() => ReleaseNode(synchronization));
        scope.Defer(synchronization.Stop);
        abilityNetwork = network;
        canvas.BindGameplay(network);
        scope.Defer(() => canvas.BindGameplay(null));
        network.Synchronization = synchronization;
        network.SessionReady += synchronization.Activate;
        parent.AddChild(synchronization);
        var notifications = new SessionNotifications(messages);
        network.Failed += coordinator.Fail;
        network.SessionReady += coordinator.CompleteLoading;
        network.MembershipChanged += notifications.MembershipChanged;
        network.LocalJoined += notifications.LocalJoined;
        parent.AddChild(network);
        return new GameSession(player, messages, network, fixture);
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
