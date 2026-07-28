namespace Genesis.Gameplay.Components.Visuals;

public struct SimpleAnimationComponent
{
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    
    //Offset wo der sprite beginnt zb wenn man 32*32 spritesheets hat aber nur 16 benutzt
    public int FrameWidthOffset { get; }
    
    public int FrameHeightOffset { get; }
    
    public float FrameDuration { get; } // In milliseconds
    
    // Character specific (Row-based logic)
    public int FramesInIdle { get; }
    public int FramesInWalk { get; }
    
    // Object specific (Doors, Traps -> Linear logic)
    public int FrameCount { get; } 
    public int FramesPerRow { get; }

    // Runtime Data
    public float FrameTimer { get; set; }
    public int CurrentFrame { get; set; }
    public bool IsFinished { get; set; }
    public bool IsLooping { get; }

    // CONSTRUCTOR 1: For Characters (Player, Enemies)
    public SimpleAnimationComponent(int frameWidth, int frameHeight, float frameDuration, int idleFrames, int walkFrames,int frameHeightOffset=0, int frameWidthOffset=0)
    {
        FrameHeightOffset = frameHeightOffset;
        FrameWidthOffset =  frameWidthOffset;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameDuration = frameDuration;
        FramesInIdle = idleFrames;
        FramesInWalk = walkFrames;
        FrameCount = walkFrames; // Fallback
        FramesPerRow = 9999;
        
        FrameTimer = 0f;
        CurrentFrame = 0;
        IsFinished = false;
        IsLooping = true; // Characters usually loop
    }

    // CONSTRUCTOR 2: For Objects (Doors, Traps, Effects)
    public SimpleAnimationComponent(int frameWidth, int frameHeight, int frameCount, int framesPerRow, float frameDuration, bool isLooping)
    {
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCount = frameCount;
        FramesPerRow = framesPerRow;
        FrameDuration = frameDuration;
        IsLooping = isLooping;
        
        FramesPerRow = framesPerRow > 0 ? framesPerRow : FrameCount;

        // Dummy values for character logic to prevent crashes if StateComponent is accidentally added
        FramesInIdle = frameCount;
        FramesInWalk = frameCount;
        
        FrameTimer = 0f;
        CurrentFrame = 0;
        IsFinished = false;
    }
}