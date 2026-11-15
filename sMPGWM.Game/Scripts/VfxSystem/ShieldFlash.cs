using Godot;

namespace sMPGWM.Scripts.VfxSystem;

public partial class ShieldFlash : Node2D
{
    public override void _Draw() => DrawArc(Vector2.Zero, 9, -1.2f, 1.2f, 12, new Color(.25f, .7f, 1f), 2, true);
}
