using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Scripts.Parallax;

public partial class ParallaxMotion : ParallaxBackground
{
    private ParallaxLayer _parallaxLayerNebula;
    private ParallaxLayer _parallaxLayerStars;
    private readonly List<Sprite2D> _starsLayer = new();
    private readonly List<Sprite2D> _nebulaLayer = new();

    public Texture2D NebulaBackground;
    public Texture2D StarBackground;
    private const int SpriteSize = 1024;
    private const int GridSize = 3;
    private const int MirrorSize = SpriteSize * GridSize;

    public override void _Ready()
    {
        NebulaBackground = GamePlayProvider.CreateSpaceBackgroundNebula();
        StarBackground = GamePlayProvider.CreateSpaceBackgroundStars();
        _parallaxLayerNebula = GenerateSpriteGrid(_nebulaLayer, NebulaBackground);
        _parallaxLayerStars = GenerateSpriteGrid(_starsLayer, StarBackground);

        _parallaxLayerNebula.MotionMirroring = new Vector2(MirrorSize, MirrorSize);
        _parallaxLayerStars.MotionMirroring = new Vector2(MirrorSize, MirrorSize);

        _parallaxLayerNebula.MotionScale = new Vector2(0.2f, 0.2f);
        _parallaxLayerStars.MotionScale = new Vector2(1f, 1f);
    }

    private ParallaxLayer GenerateSpriteGrid(List<Sprite2D> spriteList, Texture2D texture)
    {
        var parallaxLayer = new ParallaxLayer();
        AddChild(parallaxLayer);
        const float halfGrid = (GridSize - 1) * 0.5f * SpriteSize;

        for (var x = 0; x < GridSize; x++)
        {
            for (var y = 0; y < GridSize; y++)
            {
                var sprite = new Sprite2D
                {
                    Texture = texture,
                    Position = new Vector2(x * SpriteSize - halfGrid, y * SpriteSize - halfGrid)
                };

                spriteList.Add(sprite);
                parallaxLayer.AddChild(sprite);
            }
        }

        return parallaxLayer;
    }
    
    public override void _ExitTree()
    {
        foreach (var sprite in _starsLayer)
        {
            sprite.QueueFree();
        }
        foreach (var sprite in _nebulaLayer)
        {
            sprite.QueueFree();
        }

        _starsLayer.Clear();
        _nebulaLayer.Clear();

        _parallaxLayerStars?.QueueFree();
        _parallaxLayerNebula?.QueueFree();
    }
}