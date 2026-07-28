using Arch.Core;
using Genesis.Architecture;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Components.World;
using Genesis.Gameplay.Extensions;
using Genesis.GameStates.Core;
using Genesis.Simulation.LoadingTasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Gameplay.Systems;

public class RoomTransitionSystem(GameStateManager gameStateManager, GraphicsDevice graphics) : IUpdateSystem
{
    private readonly QueryDescription mPlayerQuery = new QueryDescription()
        .WithAll<PlayerTagComponent, PositionComponent, ColliderComponent>();

    private readonly QueryDescription mTriggerQuery = new QueryDescription()
        .WithAll<RoomTransitionTriggerComponent, PositionComponent, ColliderComponent>();

    public void Update(World world, GameTime gameTime)
    {
        var playerEntity = world.GetFirstEntity(in mPlayerQuery);
        if (playerEntity == Entity.Null) { return; }

        var playerPos = world.Get<PositionComponent>(playerEntity);
        var playerCollider = world.Get<ColliderComponent>(playerEntity);
        var playerRect = playerCollider.GetAabb(playerPos.Value);

        var transitionStarted = false;
        world.Query(in mTriggerQuery, (Entity entity, ref RoomTransitionTriggerComponent trigger, ref PositionComponent tPos, ref ColliderComponent tCol) =>
        {
            if (transitionStarted) { return; }
            var triggerRect = tCol.GetAabb(tPos.Value);
            if (playerRect.Intersects(triggerRect))
            {
                InitiateRoomChange(world, trigger.TargetDirection);
                transitionStarted = true;
            }
        });
    }

    private void InitiateRoomChange(World world, DoorDirection direction)
    {
        var floorLayoutEntity = world.GetFirstEntity(new QueryDescription().WithExclusive<FloorLayoutComponent>());
        var floorLayoutComponent = world.Get<FloorLayoutComponent>(floorLayoutEntity);

        var gridPosition = floorLayoutComponent.CurrentGridPosition;
        var targetGridPosition = direction switch
        {
            DoorDirection.North => gridPosition + new Point(0, -1),
            DoorDirection.East => gridPosition + new Point(1, 0),
            DoorDirection.South => gridPosition + new Point(0, 1),
            DoorDirection.West => gridPosition + new Point(-1, 0),
            _ => gridPosition
        };

        gameStateManager.ChangeState(new LoadingState(
            new SwitchRoomTask(targetGridPosition, direction),
            graphics
        ));
    }
}