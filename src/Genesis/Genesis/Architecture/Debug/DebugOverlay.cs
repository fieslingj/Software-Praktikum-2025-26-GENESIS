using System;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Microsoft.Xna.Framework;

namespace Genesis.Architecture.Debug;

public class DebugOverlay(Game game, World world, AudioService audio)
{
    private readonly ImGuiRenderer mRenderer = new(game);
    private readonly EntityInspector mEntityInspector = new(world);
    private DebugCounter mDebugCounter = new (world, audio);
    // private readonly SystemInspector mSystemInspector = new(systems);

    // Settings
    public bool DebugEnabled { get; private set; } = false;
    //für nur Counteranzeigen
    public bool DebugCounterEnabled { get; private set; } = false;
    public bool ToggleDebug { get; set; } = false;

    public bool ToggleDebugCounter { get; set; } = false;
    public bool ShowColliders { get; set; } = true;
    public bool ShowHitboxes { get; set; } = true;
    public bool ShowAoe { get; set; } = true;
    public bool ShowInteractables { get; set; } = true;
    public bool ShowMeleeRanges { get; set; } = true;
    public bool ShowPathfinding { get; set; } = true;
    public bool ShowFlowField { get; set; } = true;
    public bool IsInsideTechDemo { get; set; } = false;
    public bool ShowTrapRanges { get; set; } = true;
    public bool ShowEntityInspector { get; set; } = true;
    public bool ShowSystemsInspector { get; set; } = true;

    public void Update()
    {
        if (ToggleDebug)
        {
            DebugEnabled = !DebugEnabled;
            if (DebugEnabled)
            {
                var player = world.GetFirstEntity(new QueryDescription().WithAll<PlayerTagComponent>());
                ref var health = ref world.Get<HealthComponent>(player);
                health.Current = float.Min(health.Current + 1000, float.MaxValue);
                health.Max = health.Current;
                Console.WriteLine($"health: {health.Current}");
            }
        }

        ToggleDebug = false;

        if (ToggleDebugCounter) { DebugCounterEnabled = !DebugCounterEnabled; }

        ToggleDebugCounter = false;

       if(DebugCounterEnabled) { mDebugCounter.UpdateDebugCounter(); }
    }

    public void Draw(GameTime gameTime)
    {
        //wenn man nur counter haben will
        if(DebugCounterEnabled) {mRenderer.BeginLayout(gameTime);
            mDebugCounter.DrawDebugCounter(gameTime);
            mRenderer.EndLayout();
        }


        if (!DebugEnabled) {return;}
        mRenderer.BeginLayout(gameTime);
        if (ShowEntityInspector) {mEntityInspector.Draw();}
        // if (ShowSystemsInspector) mSystemInspector.Draw();

        mRenderer.EndLayout();
    }

    public void RebuildFontAtlas() => mRenderer.RebuildFontAtlas();
}