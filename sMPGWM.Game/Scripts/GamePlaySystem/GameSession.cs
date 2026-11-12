using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed record GameSession(
    PlayerController Player,
    MessageController Messages,
    NetworkManager Network,
    IGameFixture Fixture)
{
    public ItemHotbarController Hotbar { get; init; } = null!;
    public RestoredWorldState CaptureState() =>
        Network.Simulation.CaptureState(Network.Synchronization!.CompletedTick, Hotbar.CaptureState());
}
