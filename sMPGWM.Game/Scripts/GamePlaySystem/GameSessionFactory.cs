using Godot;
using System;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.Util.Mocks;
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
        var abilities = scope.Own(new AbilityRuntime(GameplayDefinitions.Current));
        var items = scope.Own(new PlayerItemState());
        var inventory = items.Inventory;
        var equipment = items.Equipment;
        var transfers = scope.Own(new InventoryEquipmentTransferService(inventory, equipment));
        var hotbar = scope.Own(new ItemHotbarController(inventory, equipment, transfers,
            error => Logger.Error($"Hotbar observer failed: {error}")) { SupportsAbility = id => abilities.IsSupported(id, out _) });
        var player = new PlayerController { Name = "PlayerController" };
        scope.Defer(() => ReleaseNode(player));
        scope.Defer(player.Shutdown);
        var messages = scope.Own(new MessageController(canvas, player.InputPolicy,
            error => Logger.Error($"Message observer failed: {error}")));
        fixture.Populate(inventory, messages, startup.Role == NetworkRole.Host);
        var initialItems = inventory.Snapshot;
        scope.Defer(() =>
        {
            if (GodotObject.IsInstanceValid(canvas)) canvas.UnbindInventory();
        });
        canvas.BindInventory(inventory, equipment, transfers);
        AbilityTargetingController? targeting = null;
        player.Initialize(canvas, transfers, position => hotbar.Activate(position, fixture.ActivateAction,
            request =>
            {
                if (request.Binding is EquipmentAbilityHotbarBinding binding) targeting?.Begin(binding);
                else Logger.Info($"Item hotbar activation requested: {request}");
            }), world);
        parent.AddChild(player);
        canvas.BindHotbar(hotbar, fixture.Actions);
        player.SetSessionActive(false);

        var participants = new GameWorld { Name = nameof(GameWorld), LocalPlayer = player };
        scope.Defer(() => ReleaseNode(participants));
        scope.Defer(participants.Clear);
        participants.AddChild(new ParallaxMotion { Name = nameof(ParallaxMotion) });
        world.AddChild(participants);
        var network = new NetworkManager { Name = nameof(NetworkManager), Startup = startup, World = participants, Abilities = abilities, LocalItems = items, InitialItems = initialItems };
        scope.Defer(() => ReleaseNode(network));
        scope.Defer(network.StopSession);
        var synchronization = new SynchronizationCoordinator
        {
            Name = nameof(SynchronizationCoordinator), Player = player, World = participants, Network = network
        };
        scope.Defer(() => ReleaseNode(synchronization));
        scope.Defer(synchronization.Stop);
        transfers.SetAuthority(network.RequestItemTransfer);
        targeting = new AbilityTargetingController(network, player, canvas) { Name = "AbilityTargeting" };
        world.AddChild(targeting);
        canvas.CancelGameplayAction = () =>
        {
            if (!targeting.IsTargeting) return false;
            targeting.Cancel();
            return true;
        };
        scope.Defer(() => canvas.CancelGameplayAction = null);
        scope.Defer(() => ReleaseNode(targeting));
        scope.Defer(targeting.Cancel);
        player.Input.SetPointerInputFilter(() => canvas.IsPointerOverInteractiveUi() || targeting.BlocksPointerActions);
        network.AbilityFeedback += result => canvas.ShowGameplayFeedback(AbilityActivationResultText.Format(result));
        network.ItemFeedback += canvas.ShowGameplayFeedback;
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
