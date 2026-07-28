using Genesis.Architecture.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Arch.Core;
using Genesis.Gameplay.Components;
using System;
using Genesis.Architecture;
using Genesis.Architecture.Debug;
using Genesis.Gameplay.Components.Inventory;
using Genesis.Gameplay.Components.UI;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Extensions;
using MonoGame.Extended;

namespace Genesis.Gameplay.Systems;

public class DrawSystem(ScreenService screen, SpriteFont debugFont, DebugOverlay debug) : IDrawSystem
{
    public DebugOverlay DebugOverlay { get; private set; } = debug;

    // TODO only for debugging, remove later!
    public float PlayerDamage { get; set; }
    public float EnemyDamage { get; set; }

    private static readonly QueryDescription sSpriteQuery = new QueryDescription()
        .WithAll<PositionComponent, SpriteComponent, IsVisibleComponent>();

    private static readonly QueryDescription sTextQuery = new QueryDescription()
        .WithAll<TextComponent, PositionComponent>();

    private static readonly QueryDescription sStaminaQuery = new QueryDescription()
        .WithAll<StaminaComponent, PlayerTagComponent>();

    private static readonly QueryDescription sHealthQuery = new QueryDescription()
        .WithAll<HealthComponent, PlayerTagComponent>();

    private static readonly QueryDescription sLineQuery = new QueryDescription()
        .WithAll<LinePositionComponent>();

    private static readonly QueryDescription sProgressBarQuery = new QueryDescription()
        .WithAll<PositionComponent, ProgressBarComponent>();

    private static readonly QueryDescription sSliderQuery = new QueryDescription()
        .WithAll<PositionComponent, UiSliderComponent>();

    private static readonly QueryDescription sPieChartQuery = new QueryDescription()
        .WithAll<PositionComponent, ProgressPieChartComponent>();

    private BasicEffect mPrimitiveEffect;

    public void Draw(World world, SpriteBatch spriteBatch, bool ySorting = false)
    {
        //positionen durch maximum teilen um in range von 0 bis 1 zu bekommen in selber reihenfolge
        //aber durch 10000 teilen ist warscheinlich schneller, wird aber nach y==10000 position nicht gerendert

        /*float max = 1;
        world.Query(in sSpriteQuery,
            (Entity entity, ref PositionComponent pos, ref SpriteComponent sprite) =>
            {
                if (pos.mPosition.Y > max) max = pos.mPosition.Y;
            });*/

        world.Query(in sSpriteQuery,
            (Entity entity, ref PositionComponent pos, ref SpriteComponent sprite) =>
            {
                var layerdepth = sprite.LayerDepth;;
                if (ySorting)
                {
                    layerdepth = (pos.Value.Y + sprite.mOffset.Y) / 10000f;
                    layerdepth += sprite.LayerDepth;
                }


                //if an Effect is not active it is not shown
                if(world.Has<EffectComponent>(entity))
                {
                    if (world.Get<EffectComponent>(entity).Active == false) {return;}
                }

                SpriteEffects effects = SpriteEffects.None;

                if (world.Has<VelocityComponent>(entity))
                {
                    var vel = world.Get<VelocityComponent>(entity);
                    // Wenn er nach links läuft (X < 0)
                    if (vel.Direction.X < -0.1f)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                    }

                }

                if (world.Has<FaceComponent>(entity))
                {
                    effects = Flipsprite(world.Get<FaceComponent>(entity).FaceDirection);
                }


                bool isButton = world.Has<ButtonComponent>(entity);

                float scaleX = sprite.mScale;
                float scaleY = sprite.mScale;
                Vector2 origin = sprite.Origin;

                if (isButton)
                {
                    ref var button = ref world.Get<ButtonComponent>(entity);

                    scaleX = button.Bounds.Width / (float)sprite.SourceRect.Width;
                    scaleY = button.Bounds.Height / (float)sprite.SourceRect.Height;

                    origin = new Vector2(
                        sprite.SourceRect.Width / 2f,
                        sprite.SourceRect.Height / 2f
                    );
                }
                if (world.Has<TextComponent>(entity) && !isButton) { layerdepth = 0.99f; }

                if (sprite.SpriteSheet == null) {return;}
                // If the entity has ProximityLightComponent and the light is on,
                // draw a scaled (glow color) version of the sprite behind the actual sprite to create an outline effect.
                if (world.Has<InteractableComponent>(entity))
                {
                    ref var interactable = ref world.Get<InteractableComponent>(entity);

                    bool shouldDrawGlow = interactable.LightOn;
                    
                    if (world.Has<DoorComponent>(entity))
                    {
                        var doorState = world.Get<DoorComponent>(entity).State;
                        if (doorState == DoorState.Opening || doorState == DoorState.Open)
                        {
                            shouldDrawGlow = false;
                        }
                    }

                    if (shouldDrawGlow)
                    {
                        float glowScale = 1.3f;
                        Color glowColor = new Color(Color.White, 0.1f);

                        spriteBatch.Draw(
                            sprite.SpriteSheet,
                            pos.Value,
                            sprite.SourceRect,
                            glowColor,
                            0f,
                            origin,
                            new Vector2(scaleX * glowScale, scaleY * glowScale),
                            SpriteEffects.None,
                            sprite.LayerDepth - 0.001f
                        );
                    }
                }
                if (world.Has<StateComponent>(entity))
                {
                    ref var state = ref world.Get<StateComponent>(entity);


                    //draw glow when hit
                    if (state.Current == ActorState.Hit)
                    {
                        ref var vel = ref world.Get<VelocityComponent>(entity);
                        var angle = float.Atan2(vel.Direction.Y,vel.Direction.X) - 90;

                        float glowScale = 1.2f;
                        Color glowColor = new Color(Color.Chartreuse, 0.6f);

                        spriteBatch.Draw(
                            sprite.SpriteSheet,
                            pos.Value,
                            sprite.SourceRect,
                            glowColor,
                            0,
                            origin,
                            new Vector2(scaleX * glowScale, scaleY * glowScale),
                            SpriteEffects.None, layerdepth + 0.001f
                        );
                    }
                }

                spriteBatch.Draw(
                    sprite.SpriteSheet,
                    pos.Value,
                    sprite.SourceRect,
                    sprite.mColor,
                    sprite.Rotation,
                    origin,
                    new Vector2(scaleX, scaleY),
                    effects,
                    layerdepth
                );
            });

        DrawLines(world, spriteBatch);
        DrawText(world, spriteBatch);
        DrawProgressBars(world, spriteBatch);
        DrawProgressPieCharts(world, spriteBatch);
        DrawSliders(world, spriteBatch);

        if (DebugOverlay.DebugEnabled && debugFont != null)
        {
            DrawPlayerStaminaDebugText(world, spriteBatch);
            DrawPlayerHealthDebugText(world, spriteBatch);
            DrawDamageDebugText(world, spriteBatch);
        }
    }

    public SpriteEffects Flipsprite(FaceDirection direction)
    {
        if (direction == FaceDirection.North)
        {
            return SpriteEffects.FlipHorizontally;
        }
        else if (direction == FaceDirection.West)
        {
            return SpriteEffects.FlipHorizontally;
        }

        return SpriteEffects.None;
    }

    private void DrawText(World world, SpriteBatch spriteBatch)
    {
        world.Query(in sTextQuery,
            (ref TextComponent text, ref PositionComponent pos) =>
            {
                var textString = text.Text;
                var font = text.Font;

                if (string.IsNullOrEmpty(textString)) { return; }

                var fullSize = font.MeasureString(textString);

                // Values determined via debugging to fix font metrics.
                // Standard MeasureString includes too much whitespace
                const float cropTop = 15f;
                const float cropBottom = 7f;

                // 1. Calculate the visible height of the characters
                var visibleHeight = fullSize.Y - cropTop - cropBottom;

                // 2. Calculate the optical center Y
                // Origin Y = Top padding + half of the actual visible text
                var opticalCenterY = cropTop + (visibleHeight / 2f);

                Vector2 textOrigin;

                switch (text.Alignment)
                {
                    case TextAlignment.TopLeft:
                        textOrigin = new Vector2(0f, cropTop);
                        break;
                    case TextAlignment.TopCenter:
                        textOrigin = new Vector2(fullSize.X / 2f, cropTop);
                        break;
                    case TextAlignment.TopRight:
                        textOrigin = new Vector2(fullSize.X, cropTop);
                        break;
                    case TextAlignment.MiddleLeft:
                        textOrigin = new Vector2(0f, opticalCenterY);
                        break;
                    case TextAlignment.MiddleCenter:
                        textOrigin = new Vector2(fullSize.X / 2f, opticalCenterY);
                        break;
                    case TextAlignment.MiddleRight:
                        textOrigin = new Vector2(fullSize.X, opticalCenterY);
                        break;
                    case TextAlignment.BottomCenter:
                        textOrigin = new Vector2(fullSize.X / 2f, fullSize.Y - cropBottom);
                        break;

                    default:
                        textOrigin = new Vector2(0f, opticalCenterY);
                        break;
                }

                spriteBatch.DrawString(
                    spriteFont: font,
                    text: textString,
                    position: pos.Value,
                    color: text.Color,
                    rotation: 0f,
                    origin: textOrigin,
                    scale: 1f,
                    effects: SpriteEffects.None,
                    layerDepth: text.LayerDepth
                );
            });

        DrawWeaponRange(world, spriteBatch);
    }

    private void DrawPlayerStaminaDebugText(World world, SpriteBatch spriteBatch)
    {
        var currentStamina = -1f;

        world.Query(in sStaminaQuery,
            (ref StaminaComponent stamina) => { currentStamina = stamina.Current; });

        var debugText = $"Stamina: {currentStamina:F0}";

        var screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
        var textSize = debugFont.MeasureString(debugText);

        var position = new Vector2(
            screenWidth - textSize.X - 10,
            10
        );

        spriteBatch.DrawString(
            debugFont,
            debugText,
            position,
            Color.LightGreen
        );
    }

    private void DrawPlayerHealthDebugText(World world, SpriteBatch spriteBatch)
    {
        var currentHealth = -1f;

        world.Query(in sHealthQuery,
            (ref HealthComponent health) => { currentHealth = health.Current; });

        var debugText = $"Health: {currentHealth:F0}";

        var screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
        var textSize = debugFont.MeasureString(debugText);

        var position = new Vector2(
            screenWidth - textSize.X - 10,
            30
        );

        spriteBatch.DrawString(
            debugFont,
            debugText,
            position,
            Color.LightGreen
        );
    }

    private void DrawDamageDebugText(World world, SpriteBatch spriteBatch)
    {
        var playerText = $"Damage: {PlayerDamage:F0}";
        var enemyText = $"Damage: {EnemyDamage:F0}";

        var screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
        var textSizePlayer = debugFont.MeasureString(playerText);

        var textPositionPlayer = new Vector2(
            screenWidth - textSizePlayer.X - 10,
            50
        );

        var textPositionEnemy = new Vector2(
            10,
            50
        );

        spriteBatch.DrawString(
            debugFont,
            playerText,
            textPositionPlayer,
            Color.LightGreen
        );

        spriteBatch.DrawString(
            debugFont,
            enemyText,
            textPositionEnemy,
            Color.LightGreen
        );
    }

    private static void DrawLines(World world, SpriteBatch spriteBatch)
    {
        world.Query(in sLineQuery,
            (Entity lineEntity, ref LinePositionComponent line) =>
            {
                if (world.Has<LifeTimeComponent>(lineEntity))
                {
                    if (world.Get<LifeTimeComponent>(lineEntity).Active)
                    {
                        return;
                    }
                }
                DrawLine(spriteBatch, line.Start,
                    line.End, line.Color, line.Thickness);
            });
    }

    public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f)
    {
        var distance = Vector2.Distance(point1, point2);
        var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        DrawLine(spriteBatch, point1, distance, angle, color, thickness);
    }
    public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness = 1f)
    {

        var origin = new Vector2(0, 0);
        var scale = new Vector2(length, thickness);
        spriteBatch.Draw(GetTexture(spriteBatch), point, null, color, angle, origin, scale, SpriteEffects.None, 0);
    }
    public static Texture2D GetTexture(SpriteBatch spriteBatch)
    {
        Texture2D texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        texture.SetData(new[] {Color.White});
        return texture;
    }

    /// <summary>
    /// Draws all progress bars based on the ProgressBarComponent.
    /// </summary>
    private void DrawProgressBars(World world, SpriteBatch spriteBatch)
    {
        Color backgroundColor = Color.DarkGray;
        Texture2D pixel = GetTexture(spriteBatch);

        world.Query(in sProgressBarQuery,
            (ref PositionComponent pos, ref ProgressBarComponent bar) =>
            {
                if (!bar.IsActive) { return; }
                Rectangle bgRect = bar.BackgroundBounds;
                bgRect.X += (int)pos.Value.X;
                bgRect.Y += (int)pos.Value.Y;

                // Draw the background bar.
                spriteBatch.Draw(
                    pixel,
                    bgRect,
                    null,
                    backgroundColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.9f
                );

                // Draw the foreground bar.
                if (bar.Max > 0)
                {
                    // Calculate the ratio
                    float ratio = bar.Current / bar.Max;
                    int currentWidth = (int)(bgRect.Width * ratio);

                    // Create and draw the rectangle.
                    Rectangle fgRect = new Rectangle(
                        bgRect.X,
                        bgRect.Y,
                        currentWidth,
                        bgRect.Height
                    );

                    spriteBatch.Draw(
                        pixel,
                        fgRect,
                        null,
                        bar.ForegroundColor,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        1f
                    );
                }
            });
    }

    private void DrawSliders(World world, SpriteBatch spriteBatch)
    {
        world.Query(in sSliderQuery, (ref UiSliderComponent slider, ref PositionComponent pos) =>
        {
            var leftEdge = pos.Value.X - (slider.Bounds.Width / 2f);
            var centerY = pos.Value.Y;

            var range = slider.Max - slider.Min;
            var normalized = (range == 0) ? 0 : (slider.Value - slider.Min) / range;
            var thumbPosition = new Vector2(
                leftEdge + (slider.Bounds.Width * normalized),
                centerY
            );

            // Draw Rail
            var railDest = new Rectangle(
                (int)leftEdge - slider.Bounds.Height / 2,
                (int)(centerY - slider.Bounds.Height / 2f),
                slider.Bounds.Width + slider.Bounds.Height,
                slider.Bounds.Height
            );
            spriteBatch.DrawRectangle(railDest, Color.Gray, layerDepth: 1f);

            // Draw Thumb
            var color = slider.State switch
            {
                UiSliderState.Idle => Color.White,
                UiSliderState.Hover => Color.Yellow,
                UiSliderState.Dragging => Color.Red,
                _ => Color.Violet
            };

            spriteBatch.DrawCircle(thumbPosition, slider.Bounds.Height / 2f, 32, color);
        });
    }

    private static readonly QueryDescription sRangeQuery = new QueryDescription()
        .WithAll<PositionComponent, HotbarComponent>();
    private void DrawWeaponRange(World world, SpriteBatch spriteBatch)
    {
        world.Query(in sRangeQuery, (Entity player, ref HotbarComponent hotbar) =>
        {
            var itemEntity = hotbar.Slots[hotbar.ActiveSlot];
            if (itemEntity == Entity.Null || !world.IsAlive(itemEntity)) { return; }
            ref var id = ref world.Get<ItemIdentificationComponent>(itemEntity);
            var def = ItemDefinitions.Get(id.mType);

            if (def.AttackRange > 0)
            {
                spriteBatch.DrawCircle(
                    world.GetCenter(player),
                    def.AttackRange,
                    sides: 64,
                    color: Color.LightGray * 0.25f,
                    thickness: 1f,
                    layerDepth: 1f
                );
            }
        });
    }

    private void DrawProgressPieCharts(World world, SpriteBatch spriteBatch)
    {
        var overlayColor = new Color(0, 0, 0, 150);
        const float radius = 28f;
        const int segments = 40;
        const float startAngle = -MathHelper.PiOver2;
        var anyToDraw = false;
        world.Query(in sPieChartQuery, (ref PositionComponent p, ref ProgressPieChartComponent pie) => {
            if (pie is { mIsActive: true, mProgress: < 1.0f }) { anyToDraw = true; }
        });

        if (!anyToDraw) { return; }

        var uiScale = screen.GetUiScale();
        var uiMatrix = Matrix.CreateScale(uiScale, uiScale, 1.0f);

        spriteBatch.End();

        var gd = spriteBatch.GraphicsDevice;
        if (mPrimitiveEffect == null)
        {
            mPrimitiveEffect = new BasicEffect(gd);
            mPrimitiveEffect.VertexColorEnabled = true;
            mPrimitiveEffect.View = Matrix.Identity;
        }
        mPrimitiveEffect.Projection = Matrix.CreateOrthographicOffCenter(0, gd.Viewport.Width, gd.Viewport.Height, 0, 0, 1);
        mPrimitiveEffect.World = uiMatrix;

        foreach (var pass in mPrimitiveEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            world.Query(in sPieChartQuery, (ref PositionComponent pos, ref ProgressPieChartComponent pie) =>
            {
                if (!pie.mIsActive || pie.mProgress >= 1.0f) { return; }

                var remainingProgress = 1.0f - pie.mProgress;
                var currentMaxAngle = MathHelper.TwoPi * remainingProgress;
                var vertices = new VertexPositionColor[segments * 3];

                for (var i = 0; i < segments; i++)
                {
                    var angle1 = startAngle + (i / (float)segments) * currentMaxAngle;
                    var angle2 = startAngle + ((i + 1) / (float)segments) * currentMaxAngle;

                    var center = pos.Value;
                    var p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
                    var p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;

                    vertices[i * 3 + 0] = new VertexPositionColor(new Vector3(center, 0), overlayColor);
                    vertices[i * 3 + 1] = new VertexPositionColor(new Vector3(p1, 0), overlayColor);
                    vertices[i * 3 + 2] = new VertexPositionColor(new Vector3(p2, 0), overlayColor);
                }
                gd.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, segments);
            });
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(uiScale, uiScale, 1.0f),
            sortMode: SpriteSortMode.FrontToBack
        );
    }
}
