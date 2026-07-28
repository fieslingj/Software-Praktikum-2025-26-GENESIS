using Arch.Core;
using Microsoft.Xna.Framework;

namespace Genesis.Gameplay.Navigation;

public readonly struct SpatialEntry
{
    public readonly Entity mEntity;
    public readonly Rectangle mAabb;
    public readonly Vector2 mPosition;
    public readonly SpatialFlags mFlags;

    public SpatialEntry(Entity entity, Rectangle aabb, Vector2 position, SpatialFlags flags)
    {
        mEntity = entity;
        mAabb = aabb;
        mPosition =  position;
        mFlags = flags;
    }
}