using Genesis.Architecture.ECS;
using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Genesis.Gameplay.Definitions;
using Genesis.Gameplay.Navigation;

namespace Genesis.Gameplay.Systems;

/// <summary>
/// Deals damage over time to entities standing in acid hazards
/// and tints them green while they're affected.
/// </summary>
public class AcidHazardSystem(SpatialHash spatialHash) : IUpdateSystem
{
    // Query for acid hazards
    private static readonly QueryDescription sAcidQuery = new QueryDescription()
        .WithAll<PositionComponent, ColliderComponent, AcidHazardComponent>();

    private readonly List<(PositionComponent Pos, ColliderComponent Col)> mAcidHazards = new();
    private readonly List<SpatialEntry> mNearbyEntitiesTemp = new();
    private readonly HashSet<int> mProcessedThisAcid = new();

    public void Update(World world, GameTime gameTime)
    {
        // Collect all acid hazards
        mAcidHazards.Clear();
        world.Query(in sAcidQuery,
            (ref PositionComponent pos, ref ColliderComponent col) =>
            {
                mAcidHazards.Add((pos, col));
            });

        mProcessedThisAcid.Clear();

        // For each Acid Hazard go through the entities close by.
        foreach (var (acidPos, acidCol) in mAcidHazards)
        {

            Rectangle acidRect = acidCol.GetAabb(acidPos.Value);

            int cellSize = 64; // Same cellSize as the SpatialHash

            // Sample multiple points across the acid's area to catch entities in large pools.
            // This ensures we don't miss entities when the acid spans multiple spatial hash cells.
            for (float x = acidRect.Left; x <= acidRect.Right + cellSize; x += cellSize)
            {
                for (float y = acidRect.Top; y <= acidRect.Bottom + cellSize; y += cellSize)
                {
                    // Query spatial hash at this grid point (returns entity + 8 neighboring cells)
                    mNearbyEntitiesTemp.Clear();
                    spatialHash.GetEntitiesInRect(acidRect, mNearbyEntitiesTemp);

                    foreach (var entry in mNearbyEntitiesTemp)
                    {
                        var targetEntity = entry.mEntity;
                        
                        // Avios double counting of hazard damage when colliders overlap (chemical tank)
                        if (!mProcessedThisAcid.Add(targetEntity.Id)) { continue; }
                        
                        if (!world.IsAlive(targetEntity)) { continue; }
                        if (!acidRect.Intersects(entry.mAabb)) { continue; }
                        if (!world.Has<HealthComponent>(targetEntity)) { continue; }
                        if (world.Has<ChemicalTankComponent>(targetEntity)) { continue; }

                        var entity = entry.mEntity;
                        
                        if (!world.Has<StatusComponent>(entry.mEntity)) { world.Add(entity, new StatusComponent([])); }
                        ref var targetStatus = ref world.Get<StatusComponent>(entity);
                        targetStatus.Types.RemoveAll(status => status.Item1 == StatusType.InAcid);
                        targetStatus.Types.Add((StatusType.InAcid, 0.0));
                    }
                }
            }
        }
    }
}