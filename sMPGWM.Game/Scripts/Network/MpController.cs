using Godot;

namespace sMPGWM.Scripts.Network;

public partial class MpController : CharacterBody2D
{
    public int ControllingPeerId { get; internal set; }
}
