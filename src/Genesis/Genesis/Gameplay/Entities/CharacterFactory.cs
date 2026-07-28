using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.Visuals;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Gameplay.Entities;

/// <summary>
/// Base factory class for creating character entities (Enemies, Companions) with shared components.
/// </summary>
public abstract class CharacterFactory(ContentManager content)
{
    private const int FrameSize = 32;

    protected CommonCharacterComponents CreateCommonComponents(EnemyDefinition def)
    {
        var spriteSheet = content.Load<Texture2D>(def.SpritePathIdle);
        var frameSize = (def.Framesize != 0) ? def.Framesize : FrameSize;

        // Source Rect for the first frame
        var sourceRect = new Rectangle(0, 0, frameSize, frameSize);
    
        // Sprite Component
        var spriteComponent = new SpriteComponent(
            spriteSheet,
            sourceRect,
            def.LayerDepth,
            def.Scale // Keep scale from definition
        )
        {
            Origin = new Vector2(frameSize / 2, frameSize / 2) + frameSize * def.SpriteOriginOffsetFactor
        };

        // Animation Component (Constructor 1 for characters)
        var animComponent = new SimpleAnimationComponent(
            frameWidth: frameSize,
            frameHeight: frameSize,
            frameDuration: def.FrameDuration,
            idleFrames: def.IdleFrames, 
            walkFrames: def.WalkFrames
        );

        // Collider & Hitbox calculation (adjusted to frameSize)
        var colliderSize = frameSize * def.Scale * def.ColliderSizeFactor;
        var hitboxSize = new Vector2(colliderSize.X, frameSize * def.Scale * def.HitboxHeightFactor);
        var hitboxOffset = new Vector2(0, (colliderSize.Y - hitboxSize.Y) / 2f);
        
        var meleeLoadout = def.MeleeWeapon.HasValue ? ItemDefinitions.Get(def.MeleeWeapon.Value) : null;
        var rangedLoadout = def.RangedWeapon.HasValue ? ItemDefinitions.Get(def.RangedWeapon.Value) : null;

        return new CommonCharacterComponents
        {
            Velocity = new VelocityComponent(Vector2.Zero, def.Speed),
            State = new StateComponent(),
            Sprite = spriteComponent,
            Animation = animComponent,
            Collider = new ColliderComponent(colliderSize),
            HitBox = new HitBoxComponent(hitboxSize, hitboxOffset),
            Loadout = new LoadoutComponent(meleeLoadout, rangedLoadout),
            HitSound = new HitSoundComponent("Sounds/Attack/GegnerHitTestSound"),
            AttackCooldown = new AttackCooldownComponent(),
            Status = new StatusComponent([])
        };
    }

    protected void AddOptionalBehaviors(World world, Entity entity, EnemyDefinition def)
    {
        if (def.CanDuck)
        {
            world.Add(entity, new DuckBehaviorComponent(def.DuckProbability, def.DuckDuration));
        }
        
        if (def.CanTakeCover)
        {
            world.Add(entity, new CoverBehaviorComponent(def.CoverSearchRadius, def.CoverPreference));
        }
    }

    protected readonly struct CommonCharacterComponents
    {
        public VelocityComponent Velocity { get; init; }
        public StateComponent State { get; init; }
        public SpriteComponent Sprite { get; init; }
        public SimpleAnimationComponent Animation { get; init; }
        public ColliderComponent Collider { get; init; }
        public HitBoxComponent HitBox { get; init; }
        public LoadoutComponent Loadout { get; init; }
        public HitSoundComponent HitSound { get; init; }
        public AttackCooldownComponent AttackCooldown { get; init; }
        public StatusComponent Status { get; init; }
    }
}