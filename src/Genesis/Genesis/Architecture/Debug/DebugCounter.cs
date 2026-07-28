using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace Genesis.Architecture.Debug;

public class DebugCounter(World world, AudioService audio)
{
    private static int sCompanionCount;
    private static int sEnemyCount;
    
    public void UpdateDebugCounter()
    {
        var enemyQuery = new QueryDescription().WithAll<EnemyComponent>();
        var companionQuery = new QueryDescription().WithAll<CompanionComponent>();
        sEnemyCount = world.CountEntities(in enemyQuery);
        sCompanionCount = world.CountEntities(in companionQuery);
    }

    public void DrawDebugCounter(GameTime gameTime)
    {
        ImGui.SetWindowSize("Counter",new Vector2(400,200));
        ImGui.Begin("Counter");
        
        ImGui.Text($"Companions: {sCompanionCount}");
        ImGui.Text($"Enemies: {sEnemyCount}");
        var time = gameTime.ElapsedGameTime.Milliseconds;
        double fps = 0;
        if (time != 0)
        {
            fps = 1000 / gameTime.ElapsedGameTime.Milliseconds;
        }
        ImGui.Text($"Fps: {fps}");
        ImGui.Text($"Active sound instances: {audio.ActiveCount}");
        ImGui.End();
    }
}