using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Core;
using Genesis.Simulation.LoadingTasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Systems;

public class LayerTransitionSystem(GameStateManager gameStateManager, GraphicsDevice graphics) : IUpdateSystem
{
    private readonly QueryDescription mPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent, ColliderComponent>();

    private readonly QueryDescription mTriggerQuery = new QueryDescription()
        .WithAll<ElevatorTriggerComponent, PositionComponent, ColliderComponent>();

    public void Update(World world, GameTime gameTime)
    {
        var playerEntity = world.GetFirstEntity(in mPlayerQuery);
        if (playerEntity == Entity.Null)
        {
            return;
        }

        var playerPos = world.Get<PositionComponent>(playerEntity);
        var playerCollider = world.Get<ColliderComponent>(playerEntity);
        var playerRect = playerCollider.GetAabb(playerPos.Value);

        world.Query(in mTriggerQuery,
            (Entity entity, ref ElevatorTriggerComponent trigger, ref PositionComponent tPos,
                ref ColliderComponent tCol) =>
            {
                var triggerRect = tCol.GetAabb(tPos.Value + new Vector2(0, 4));
                if (playerRect.Intersects(triggerRect))
                {
                    gameStateManager.ChangeState(new LoadingState(new SwitchLayerTask(), graphics));
                }
            });
    }
}
