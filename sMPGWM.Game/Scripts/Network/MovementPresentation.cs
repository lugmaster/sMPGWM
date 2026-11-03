using System;
using Godot;

namespace sMPGWM.Scripts.Network;

public static class MovementPresentation
{
    public static MovementSample Sample(long lowerTick, MovementSample lower, long upperTick, MovementSample upper, double cursor)
    {
        if (upperTick <= lowerTick || lower.Entity != upper.Entity ||
            lower.Generation != upper.Generation || lower.Revision != upper.Revision) return upper;
        float weight = (float)Math.Clamp((cursor - lowerTick) / (upperTick - lowerTick), 0, 1);
        var discrete = weight >= 1 ? upper : lower;
        return discrete with
        {
            Position = lower.Position.Lerp(upper.Position, weight),
            Rotation = Mathf.LerpAngle(lower.Rotation, upper.Rotation, weight)
        };
    }
}
