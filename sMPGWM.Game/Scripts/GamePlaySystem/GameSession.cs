using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

internal sealed record GameSession(
    PlayerController Player,
    MessageController Messages,
    NetworkManager Network,
    IGameFixture Fixture);
