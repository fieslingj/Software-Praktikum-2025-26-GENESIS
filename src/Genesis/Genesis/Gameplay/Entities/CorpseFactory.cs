using Arch.Core;
using Genesis.Architecture;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Navigation;
using Genesis.Persistence.Run;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Entities;

public class CorpseFactory(ContentManager content, GridMap grid)
{
    /// <summary>
    /// Creates a CorpseEntity in the given world at the specified position.
    /// </summary>
    public void Create(
        World world,
        Vector2 position,
        EnemyType enemyType,
        int ammo,
        RandomService rng
        )
    {
        var ammoAmount = (enemyType == EnemyType.Robot) ? rng.Next(1, 21) : ammo;
        var corpseData = new SavedCorpseData()
        {
            Type = new CorpseComponent(enemyType),
            Position = new PositionComponent(position),
            Ammo = new AmmoComponent(ammoAmount)
        };
        Create(world, corpseData);
    }

    public void Recreate(World world, SavedCorpseData corpseData) => Create(world, corpseData);

    private void Create(World world, SavedCorpseData corpse)
    {
        const float layerDepth = 0.09f;
        const float scale = 1.0f;
        
        var enemyType = corpse.Type.Type;
        var def = EnemyDefinitions.Get(enemyType);
        var spriteSheet = content.Load<Texture2D>(def.SpritePathCorpse);
        var sourceRect = spriteSheet.Bounds;

        var corpseEntity = world.Create(
            corpse.Type,
            corpse.Position,
            corpse.Ammo,
            new SpriteComponent(spriteSheet, sourceRect, layerDepth, scale),
            new InteractableComponent(50f, InteractionType.Corpse)
        );

        // Logic for robot corpse (corpse with collider)
        if (enemyType != EnemyType.Robot) return;
        var colliderSize = new Vector2(sourceRect.Width * scale * 0.8f, sourceRect.Height * scale * 0.3f);
        var colliderOffset = new Vector2(0, (sourceRect.Height * scale / 2f) - (colliderSize.Y / 2f));
        var collider = new ColliderComponent(colliderSize, colliderOffset);
        world.Add(corpseEntity, collider);
        grid.MarkColliderAsUnwalkable(corpse.Position.Value, collider);
    }
}