using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Enums.Game.Collision;
using sMPGWM.Scripts.Singletons.Autoload.Base;

namespace sMPGWM.Scripts.Gameplay.Collision;

public partial class CollisionManager : AbstractSingleton<CollisionManager>
{
    private static readonly Dictionary<CollisionTypes, (uint layer, uint mask)> CollisionPresets = new()
    {
        { CollisionTypes.Player, (1 << 0, (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5)) },
        { CollisionTypes.Enemy, (1 << 1, (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4)) },
        { CollisionTypes.Projectile, (1 << 2, (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4)) },
        { CollisionTypes.Structure, (1 << 3, (1 << 0) | (1 << 1) | (1 << 2) | (1 << 4)) },
        { CollisionTypes.Asteroid, (1 << 4, (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4)) },
        { CollisionTypes.Pickup, (1 << 5, (1 << 0) | (1 << 3) | (1 << 4)) },
        { CollisionTypes.Shield, (1 << 6, (1 << 7)) },
        { CollisionTypes.EnergyWeapon, (1 << 7, (1 << 6)) },
        { CollisionTypes.TargetScanner, (0, (1 << (int)CollisionTypes.Player) | (1 << (int)CollisionTypes.Enemy)) },
    };

    public static void SetupCollision(Node node, CollisionTypes type)
    {
        if (node is not CollisionObject2D colObj)
        {
            return;
        }

        if (!CollisionPresets.TryGetValue(type, out var settings))
        {
            return;
        }

        var (layer, mask) = settings;
        colObj.CollisionLayer = layer;
        colObj.CollisionMask = mask;
    }

    public static uint GetMask(params CollisionTypes[] types)
    {
        uint combinedMask = 0;

        foreach (var type in types)
        {
            if (CollisionPresets.TryGetValue(type, out var preset))
                combinedMask |= preset.layer;
        }

        return combinedMask;
    }
}