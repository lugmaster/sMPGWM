namespace sMPGWM.Scripts.CollisionSystem;

public static class CollisionLayers
{
    public const uint Player = 1u << 0;
    public const uint Enemy = 1u << 1;
    public const uint Structure = 1u << 3;
    public const uint Asteroid = 1u << 4;
    public const uint Hitscan = Player | Enemy | Structure | Asteroid;
}
