using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Entities;

public class MinigunFactory(ContentManager content)
{
    private const int LifeTimeSeconds = 15;
    private const int StartingAmmo = 9999;
    private const float LayerDepth = 0.1f;
    private readonly Texture2D mTexture = content.Load<Texture2D>("Sprites/Weapons/Minigun");

    public void Create(World world, Vector2 position)
    {
        var minigunDef = ItemDefinitions.Get(ItemType.Minigun);
        
        world.Create(
            new MinigunTagComponent(),
            new PositionComponent(position),
            new SpriteComponent(mTexture, mTexture.Bounds, LayerDepth),
            new LifeTimeComponent(LifeTimeSeconds, true),
            new LoadoutComponent(melee: null, ranged: minigunDef),
            new AttackCooldownComponent(),
            new AmmoComponent(StartingAmmo)
        );
    }
}