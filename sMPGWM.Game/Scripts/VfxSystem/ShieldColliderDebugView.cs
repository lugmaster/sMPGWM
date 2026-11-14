using Godot; // TEST-SUPPORT: SHIELD-DEBUG-001 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: SHIELD-DEBUG-002 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

namespace sMPGWM.Scripts.VfxSystem; // TEST-SUPPORT: SHIELD-DEBUG-004 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

public partial class ShieldColliderDebugView : Node2D // TEST-SUPPORT: SHIELD-DEBUG-006 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
{ // TEST-SUPPORT: SHIELD-DEBUG-007 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    private ShipController _ship = null!; // TEST-SUPPORT: SHIELD-DEBUG-008 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    private Vector2[] _outline = []; // TEST-SUPPORT: SHIELD-DEBUG-009 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    private float _radius; // TEST-SUPPORT: SHIELD-DEBUG-010 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    private bool _enabled; // TEST-SUPPORT: SHIELD-DEBUG-011 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    public bool Enabled // TEST-SUPPORT: SHIELD-DEBUG-012 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    { // TEST-SUPPORT: SHIELD-DEBUG-013 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        get => _enabled; // TEST-SUPPORT: SHIELD-DEBUG-014 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        set { _enabled = value; Visible = value; } // TEST-SUPPORT: SHIELD-DEBUG-015 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    } // TEST-SUPPORT: SHIELD-DEBUG-016 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

    public void Initialize(ShipController ship, ShieldGeometry geometry) // TEST-SUPPORT: SHIELD-DEBUG-018 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    { // TEST-SUPPORT: SHIELD-DEBUG-019 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        _ship = ship; // TEST-SUPPORT: SHIELD-DEBUG-020 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        Position = geometry.Center; // TEST-SUPPORT: SHIELD-DEBUG-021 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        RotationDegrees = geometry.RotationDegrees; // TEST-SUPPORT: SHIELD-DEBUG-022 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        ZIndex = 2; // TEST-SUPPORT: SHIELD-DEBUG-023 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        var shape = ship.GetNode<CollisionShape2D>("ShieldCollider").Shape; // TEST-SUPPORT: SHIELD-DEBUG-024 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        if (shape is CircleShape2D circle) _radius = circle.Radius; // TEST-SUPPORT: SHIELD-DEBUG-025 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        else if (shape is ConvexPolygonShape2D polygon) // TEST-SUPPORT: SHIELD-DEBUG-026 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        { // TEST-SUPPORT: SHIELD-DEBUG-027 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            var points = polygon.Points; // TEST-SUPPORT: SHIELD-DEBUG-028 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            _outline = new Vector2[points.Length + 1]; // TEST-SUPPORT: SHIELD-DEBUG-029 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            points.CopyTo(_outline, 0); // TEST-SUPPORT: SHIELD-DEBUG-030 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            _outline[^1] = points[0]; // TEST-SUPPORT: SHIELD-DEBUG-031 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        } // TEST-SUPPORT: SHIELD-DEBUG-032 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        Visible = Enabled; // TEST-SUPPORT: SHIELD-DEBUG-033 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        QueueRedraw(); // TEST-SUPPORT: SHIELD-DEBUG-034 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        _Process(0); // TEST-SUPPORT: SHIELD-DEBUG-035 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    } // TEST-SUPPORT: SHIELD-DEBUG-036 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

    public override void _UnhandledKeyInput(InputEvent @event) // TEST-SUPPORT: SHIELD-DEBUG-038 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    { // TEST-SUPPORT: SHIELD-DEBUG-039 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        if (OS.IsDebugBuild() && @event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F8 }) // TEST-SUPPORT: SHIELD-DEBUG-040 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            Enabled = !Enabled; // TEST-SUPPORT: SHIELD-DEBUG-041 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    } // TEST-SUPPORT: SHIELD-DEBUG-042 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

    public override void _Process(double delta) // TEST-SUPPORT: SHIELD-DEBUG-044 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    { // TEST-SUPPORT: SHIELD-DEBUG-045 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        if (_ship == null) return; // TEST-SUPPORT: SHIELD-DEBUG-046 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        Modulate = _ship.ShieldColliderActive ? new Color(0.15f, 1, 0.85f, .8f) : new Color(.55f, .6f, .65f, .4f); // TEST-SUPPORT: SHIELD-DEBUG-047 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    } // TEST-SUPPORT: SHIELD-DEBUG-048 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

    public override void _Draw() // TEST-SUPPORT: SHIELD-DEBUG-050 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    { // TEST-SUPPORT: SHIELD-DEBUG-051 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        if (_radius > 0) DrawArc(Vector2.Zero, _radius, 0, Mathf.Tau, 128, Colors.White, 1, true); // TEST-SUPPORT: SHIELD-DEBUG-052 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
        else if (_outline.Length > 1) DrawPolyline(_outline, Colors.White, 1, true); // TEST-SUPPORT: SHIELD-DEBUG-053 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
    } // TEST-SUPPORT: SHIELD-DEBUG-054 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
} // TEST-SUPPORT: SHIELD-DEBUG-055 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape

