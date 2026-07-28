using Arch.Core;
using Genesis.Architecture.ECS;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Definitions;
using Microsoft.Xna.Framework;


namespace Genesis.Gameplay.Systems;

public class StatuseffectSystem : IUpdateSystem
{
    private static readonly QueryDescription sStatusQuery = new QueryDescription()
        .WithAll<StatusComponent, HealthComponent, StateComponent>();

    public void Update(World world,GameTime gameTime)
    {
        world.Query(in sStatusQuery, (Entity entity, ref StatusComponent status, 
            ref HealthComponent health, ref StateComponent state) =>
        {
            var isStunnedActive = false;
            
            for (var i = 0; i < status.Types.Count; i++)
            {
                var s = status.Types[i];
                s.Item2 += gameTime.ElapsedGameTime.TotalSeconds;
                status.Types[i] = s;
            }
            
            status.Types.RemoveAll(x => x.Item2 >= StatusTypeDefinitions.Get(x.Item1).TimeOfEffect);
            
            foreach (var (statusType,time) in status.Types)
            {
                switch (statusType)
                {
                    case StatusType.AcidSour:
                    {
                        var damagePerSecond = StatusTypeDefinitions.Get(StatusType.AcidSour).DamagePerSecond;
                        health.Current -= damagePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        break;
                    }
                    case StatusType.InAcid:
                    {
                        var damagePerSecond = StatusTypeDefinitions.Get(StatusType.InAcid).DamagePerSecond;
                        health.Current -= damagePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        break;
                    }
                    case StatusType.Stunned:
                        isStunnedActive = true;
                        break;
                }
            }
            
            if (isStunnedActive)
            {
                if (state.Current == ActorState.Stunned || state.Current == ActorState.Hit) { return; }

                state.Previous = state.Current;
                state.Current = ActorState.Stunned;
            }
            else if (state.Current == ActorState.Stunned)
            {
                state.Current = ActorState.Idle;
            }
        });
    }
}