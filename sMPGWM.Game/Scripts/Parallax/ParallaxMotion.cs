using Godot;
using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Scripts.Parallax;

public partial class ParallaxMotion : Node2D
{
    public Texture2D NebulaBackground = null!;
    public Texture2D StarBackground = null!;
    private const int SpriteSize = 1024;
    private const int GridSize = 3;
    private const int MirrorSize = SpriteSize * GridSize;

    public override void _Ready()
    {
        ZIndex = -100;
        NebulaBackground = GamePlayProvider.CreateSpaceBackgroundNebula();
        StarBackground = GamePlayProvider.CreateSpaceBackgroundStars();
        AddChild(GenerateSpriteGrid("Nebula", NebulaBackground, 0.2f));
        AddChild(GenerateSpriteGrid("Stars", StarBackground, 1f));
    }

    private static Parallax2D GenerateSpriteGrid(string name, Texture2D texture, float scrollScale)
    {
        var parallaxLayer = new Parallax2D
        {
            Name = name,
            RepeatSize = new Vector2(MirrorSize, MirrorSize),
            ScrollScale = new Vector2(scrollScale, scrollScale)
        };

        for (var x = 0; x < GridSize; x++)
        {
            for (var y = 0; y < GridSize; y++)
            {
                // Fill the repeat rectangle starting at its top-left corner.
                parallaxLayer.AddChild(new Sprite2D
                {
                    Texture = texture,
                    Centered = false,
                    Position = new Vector2(x * SpriteSize, y * SpriteSize)
                });
            }
        }

        // Godot frees these children when the session's world is freed.
        return parallaxLayer;
    }
}
