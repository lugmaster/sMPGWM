using Godot;
using System;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ParallaxSystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;
using ParallaxMotion = sMPGWM.Scripts.ParallaxSystem.ParallaxMotion;

namespace sMPGWM.Scripts.GamePlaySystem;

internal static class GameSessionFactory
{
    public static GameSession Create(SessionScope scope, Node parent, UiCanvas canvas, Node world,
        NetworkStartupData startup, SessionCoordinator coordinator, IGameFixture fixture)
    {
        var abilities = scope.Own(new AbilityRuntime(GameplayDefinitions.Current));
        var participants = new GameWorld
        {
            Name = nameof(GameWorld), AuthoritativeItems = startup.Role == NetworkRole.Host,
            InitializeLoadout = StarterLoadout.Equip
        };
        scope.Defer(() => ReleaseNode(participants));
        scope.Defer(participants.Clear);
        var inventory = scope.Own(new InventoryController(participants.Items));
        var equipment = scope.Own(new EquipmentController(participants.Items));
        var transfers = scope.Own(new InventoryEquipmentTransferService(inventory, equipment));
        var hotbar = scope.Own(new ItemHotbarController(inventory, equipment, transfers,
            error => Logger.Error($"Hotbar observer failed: {error}")) { SupportsAbility = id => abilities.Definitions.Abilities.IsSupported(id, out _) });
        var player = new PlayerController { Name = "PlayerController" };
        scope.Defer(() => ReleaseNode(player));
        scope.Defer(player.Shutdown);
        var messages = scope.Own(new MessageController(error => Logger.Error($"Message observer failed: {error}")));
        scope.Own(new MessagePresentationBinding(messages, canvas, player.InputPolicy));
        fixture.Populate(inventory, messages, startup.Role == NetworkRole.Host);
        participants.InitialInventory = inventory.Snapshot;
        participants.Items.EvictReplica(inventory.Container);
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

        participants.LocalPlayer = player;
        participants.AddChild(new ParallaxMotion { Name = nameof(ParallaxMotion) });
        world.AddChild(participants);
        var network = new NetworkManager { Name = nameof(NetworkManager), Startup = startup, World = participants, Abilities = abilities, Simulation = scope.Own(new GameplaySimulation(participants, abilities)) };
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
        scope.Own(new LocalPlayerBindings(player, network, canvas, transfers, hotbar, targeting, fixture, inventory, equipment));
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
