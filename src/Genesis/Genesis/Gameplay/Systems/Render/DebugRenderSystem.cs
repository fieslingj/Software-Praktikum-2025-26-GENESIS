using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.Debug;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Genesis.Gameplay.Systems.Render;

public class DebugRenderSystem(DebugOverlay debug, Texture2D debugTexture, SpriteFont font) : IInputSystem, IDrawSystem
{
    private const float LayerDepth = 0.9f;
    
    private FlowField mFlowField;

    private static readonly QueryDescription sColliderQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent>();

    private static readonly QueryDescription sHitboxQuery = new QueryDescription()
        .WithAll<PositionComponent, HitBoxComponent>();

    private static readonly QueryDescription sAoeQuery = new QueryDescription()
        .WithAll<PositionComponent, AreaOfEffectComponent>();

    private static readonly QueryDescription sInteractablesQuery = new QueryDescription()
        .WithAll<PositionComponent, InteractableComponent>();

    private static readonly QueryDescription sPathfindingQuery = new QueryDescription()
        .WithAll<PositionComponent, PathComponent>();

    private static readonly QueryDescription sTrapQuery = new QueryDescription()
        .WithAll<PositionComponent, TrapComponent>();

    public void SetFlowField(FlowField flowField)
    {
        mFlowField = flowField;
    }

    public void HandleInput(World world, InputService input)
    {
        if (input.IsActionPressed(InputAction.ToggleDebug)) { debug.ToggleDebug = true; }
        if (input.IsActionPressed(InputAction.ToggleDebugCounter)) { debug.ToggleDebugCounter = true; }
    }

    public void Draw(World world, SpriteBatch spriteBatch, bool ySorting=false)
    {
        if (!debug.DebugEnabled) { return; }
        DrawColliders(world, spriteBatch);
        DrawHitBoxes(world, spriteBatch);
        DrawAoe(world, spriteBatch);
        DrawInteractables(world, spriteBatch);
        DrawPathfinding(world, spriteBatch);
        DrawTrapRanges(world, spriteBatch);

        if (mFlowField != null)
        {
            DrawFlowField(spriteBatch, mFlowField, font);
        }
    }

    private void DrawHitBoxes(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowHitboxes) { return; }

        var color = new Color(Color.Red, 0.3f);
        world.Query(in sHitboxQuery, (ref PositionComponent pos, ref HitBoxComponent hitbox) =>
        {
            var bounds = hitbox.GetBounds(pos.Value);
            spriteBatch.Draw(
                texture: debugTexture,
                destinationRectangle: bounds,
                color: color,
                rotation: 0f,
                layerDepth: LayerDepth,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                sourceRectangle: null
            );
        });
    }

    private void DrawColliders(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowColliders) { return; }

        var color = new Color(Color.Blue, 0.3f);
        world.Query(in sColliderQuery, (ref PositionComponent pos, ref ColliderComponent collider) =>
        {
            var colliderRect = collider.GetAabb(pos.Value);
            spriteBatch.Draw(
                texture: debugTexture,
                destinationRectangle: colliderRect,
                color: color,
                rotation: 0f,
                layerDepth: LayerDepth,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                sourceRectangle: null
            );
        });
    }

    private void DrawAoe(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowAoe) { return; }

        var color = new Color(Color.Green, 0.3f);
        world.Query(in sAoeQuery, (ref PositionComponent pos, ref AreaOfEffectComponent aoe) =>
        {
            spriteBatch.DrawCircle(
                center: pos.Value,
                radius: aoe.Radius,
                sides: 32,
                color: color,
                thickness: 1f,
                layerDepth: LayerDepth
            );
        });
    }

    private void DrawInteractables(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowInteractables) { return; }

        var color = new Color(Color.LightBlue, 0.3f);
        world.Query(in sInteractablesQuery, (ref PositionComponent pos, ref InteractableComponent interactable) =>
        {
            spriteBatch.DrawCircle(
                center: pos.Value,
                radius: interactable.Radius,
                sides: 32,
                color: color,
                thickness: .8f,
                layerDepth: LayerDepth
            );
        });
    }

    private void DrawPathfinding(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowPathfinding) { return; }

        var color = new Color(Color.Green, 0.3f);
        world.Query(in sPathfindingQuery, (ref PositionComponent pos, ref PathComponent path) =>
        {
            if (path.Waypoints == null || path.Waypoints.Count == 0) { return; }

            var waypoints = path.Waypoints;
            var last = pos.Value;
            for (var i = path.CurrentWaypointIndex; i < waypoints.Count; i++)
            {
                var target = waypoints[i];
                spriteBatch.DrawPoint(target, color, 3f, layerDepth: LayerDepth);
                spriteBatch.DrawLine(last, target, color, .8f, layerDepth: LayerDepth);
                last = target;
            }
        });
    }

    /// <summary>
    /// Visualize the FlowField: the costs (string) for each cell aswell as the vector (represented by a line).
    /// </summary>
    private void DrawFlowField(SpriteBatch spriteBatch, FlowField flowField, SpriteFont font)
    {
        if (!debug.ShowFlowField) { return; }

        for (var x = 0; x < flowField.Width; x++)
        {
            for (var y = 0; y < flowField.Height; y++)
            {
                var worldPos = flowField.Grid.GridToWorld(new Point(x, y));

                var cost = flowField.IntegrationField[x, y];
                if (cost != int.MaxValue)
                {
                    spriteBatch.DrawString(font, cost.ToString(), worldPos - new Vector2(10, 10),
                    Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
                }

                var dir = flowField.VectorField[x, y];
                if (dir != Vector2.Zero)
                {
                    spriteBatch.DrawLine(
                        worldPos, 
                        worldPos + (dir * 15f),
                        Color.Yellow,
                        thickness: 1f,
                        layerDepth: LayerDepth
                    );
                }
            }
        }
    }

    private void DrawTrapRanges(World world, SpriteBatch spriteBatch)
    {
        if (!debug.ShowTrapRanges) { return; }

        var color = new Color(Color.OrangeRed, 0.3f);
        world.Query(in sTrapQuery, (ref PositionComponent pos, ref TrapComponent trap) =>
        {
            spriteBatch.DrawCircle(
                center: pos.Value,
                radius: trap.Radius,
                sides: 32,
                color: color,
                thickness: 1f,
                layerDepth: LayerDepth
            );
        });
    }
}