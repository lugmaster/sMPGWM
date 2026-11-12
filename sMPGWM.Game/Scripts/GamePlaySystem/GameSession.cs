using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed record GameSession(
    PlayerControllerSystem.PlayerController Player,
    MessageController Messages,
    NetworkManager Network,
    IGameFixture Fixture);
