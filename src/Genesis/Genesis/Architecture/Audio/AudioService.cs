using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Genesis.Architecture.Audio;

/// <summary>
/// Manages the loading, caching, and playback of global Audio.
/// Handles the distinction between Background Music and
/// Sound Effects.
/// </summary>
public class AudioService(ContentManager content)
{
    /// <summary>
    /// The minimum volume treshhold.
    /// Anything quiter is considered silent to avoid unneccessary play calls.
    /// </summary>
    private const float MinVolumeTreshhold = 0.001f;

    /// <summary>
    /// Maximum number of simultaneously active SoundEffectInstances.
    /// Protects against reaching the hardware limit.
    /// </summary>
    private const int MaxActiveSounds = 50;

    // Starting with default
    public AudioSettings Settings { get; set; } = new();

    // Caches to prevent reloading assets during gameplay
    private readonly Dictionary<string, SoundEffect> mSfxCache = new();
    
    private readonly List<double> mOccupiedSlots = [];
    private double mCurrentGameTime;
    
    // List over active sound effect handles for cleanup
    private readonly List<SoundEffectInstance> mActiveInstances = [];
    public int ActiveCount => mOccupiedSlots.Count + mActiveInstances.Count;

    // Music management using SoundEffectInstance
    private SoundEffectInstance mCurrentSongInstance;
    private string mCurrentSongName;

    // Flag to allow callers to suppress the default UI confirm sound for the next click.
    // If set to true, the next time a Button triggers its default confirmation sound,
    // the sound will be skipped and the flag will be cleared.
    public bool SuppressNextConfirmSound { get; set; } = false;


    /// <summary>
    /// Remove sounds, that have finished playing or that are disposed
    /// </summary>
    public void Update(GameTime gameTime)
    {
        mCurrentGameTime = gameTime.TotalGameTime.TotalMilliseconds;

        for (var i = mOccupiedSlots.Count - 1; i >= 0; i--)
        {
            if (mOccupiedSlots[i] <= mCurrentGameTime)
            {
                mOccupiedSlots.RemoveAt(i);
            }
        }
        
        for (var i = mActiveInstances.Count - 1; i >= 0; i--)
        {
            var sfx = mActiveInstances[i];
            switch (sfx.IsDisposed)
            {
                case false when sfx.State != SoundState.Stopped:
                    continue;
                case false:
                    sfx.Dispose();
                    break;
            }

            mActiveInstances.RemoveAt(i);
        }
    }

    // --- SOUND EFFECTS ---

    /// <summary>
    /// Plays a sound effect once.
    /// </summary>
    /// <param name="name">The content path.</param>
    /// <param name="pitch">Pitch adjustment ranging from -1.0 (down to an octave) to 1.0 (up an octave).</param>
    /// <param name="pan">Panning, ranging from -1.0 (left speaker) to 1.0 (right speaker).</param>
    public void PlaySfx(string name, float pitch = 0.0f, float pan = 0.0f)
    {
        if (ActiveCount >= MaxActiveSounds) { return; }
        var volume = Settings.GetEffectiveSfxVol();
        if (volume <= MinVolumeTreshhold) { return; }
        
        mOccupiedSlots.Add(mCurrentGameTime + 3000);

        var finalPan = Settings.EnableSpatialPanning switch
        {
            false => 0.0f,
            true => pan * Settings.StereoWidth,
        };

        var sfx = GetOrLoadSfx(name);
        try
        {
            sfx?.Play(volume, pitch, finalPan);
        }
        catch (InstancePlayLimitException) { }
    }



    /// <summary>
    /// Creates an instance for advanced control (e.g. looping)
    /// </summary>
    /// <param name="name">The content path.</param>
    /// <returns>The SFX instance.</returns>
    public SoundEffectInstance PlaySfxInstance(string name, bool isLooped = false)
    {
        CleanupInstances();

        // Check Limit
        if (ActiveCount >= MaxActiveSounds)
        {
            System.Diagnostics.Debug.WriteLine("[WARN] Audio Limit reached. Cannot play " + name);
            return null;
        }
        var volume = Settings.GetEffectiveSfxVol();
        if (volume <= MinVolumeTreshhold) { return null; }

        // Create instance
        var instance = CreateSfxInstance(name);
        if (instance == null) { return null; }

        instance.IsLooped = isLooped;
        instance.Play();

        // Add to list
        mActiveInstances.Add(instance);

        return instance;
    }

    public SoundEffectInstance PlaySfxInstancelimited(string name, bool isLooped = false)
    {
        CleanupInstances();

        // Check Limit
        if (ActiveCount >= MaxActiveSounds)
        {
            System.Diagnostics.Debug.WriteLine("[WARN] Audio Limit reached. Cannot play " + name);
            return null;
        }
        var volume = Settings.GetEffectiveSfxVol();
        if (volume <= MinVolumeTreshhold) { return null; }

        // Create instance
        var instance = CreateSfxInstance(name);
        if (instance == null) { return null; }

        if (mActiveInstances.Exists(x => x.ToString() == instance.ToString())) return instance;

        instance.IsLooped = isLooped;
        instance.Play();



        // Add to list
        mActiveInstances.Add(instance);

        return instance;
    }


    public void StopSfxInstance(SoundEffectInstance sfx)
    {
        if (sfx != null && !sfx.IsDisposed && sfx.State != SoundState.Stopped)
        {
            sfx.Stop();
        }
    }

    private void CleanupInstances()
    {
        mActiveInstances.RemoveAll(s => s.IsDisposed || s.State == SoundState.Stopped);
    }

    private SoundEffectInstance CreateSfxInstance(string name)
    {
        var sfx = GetOrLoadSfx(name);
        if (sfx == null) { return null; }

        var instance = sfx.CreateInstance();
        instance.Volume = Settings.GetEffectiveSfxVol();
        return instance;
    }

    private SoundEffect GetOrLoadSfx(string name)
    {
        if (mSfxCache.TryGetValue(name, out var sfx)) {return sfx;}

        try
        {
            sfx = content.Load<SoundEffect>(name);
            mSfxCache[name] = sfx;
            return sfx;
        }
        catch (ContentLoadException)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Could not load Audio Asset: {name}");
            return null;
        }
    }


    // --- MUSIC ---

    /// <summary>
    /// Starts playing a background track.
    /// </summary>
    /// <param name="name">The content path.</param>
    /// <param name="isRepeating">Should the music loop?</param>
    public void PlayMusic(string name, bool isRepeating = true)
    {
        if (name == mCurrentSongName && mCurrentSongInstance?.State == SoundState.Playing) {return;}

        StopMusic();

        var sfx = GetOrLoadSfx(name);
        if (sfx is null) {return;}

        mCurrentSongInstance = sfx.CreateInstance();
        mCurrentSongInstance.IsLooped = isRepeating;
        mCurrentSongInstance.Volume = Settings.GetEffectiveMusicVol();
        mCurrentSongInstance.Play();

        mCurrentSongName = name;
    }

    /// <summary>
    /// Stop the current background track.
    /// </summary>
    public void StopMusic()
    {
        if (mCurrentSongInstance != null)
        {
            mCurrentSongInstance.Stop();
            mCurrentSongInstance.Dispose();
            mCurrentSongInstance = null;
        }
        mCurrentSongName = null;
    }

    /// <summary>
    /// Pause current backgroundtrack
    /// </summary>
    public void PauseMusic()
    {
        mCurrentSongInstance?.Pause();
    }

    /// <summary>
    /// Resume current backgroundtrack
    /// </summary>
    public void ResumeMusic()
    {
        mCurrentSongInstance?.Resume();
    }

    /// <summary>
    /// Is current backgroundtrack Paused
    /// </summary>
    public bool IsMusicPaused()
    {
        return mCurrentSongInstance?.State == SoundState.Paused;
    }

    public void UpdateMusicVolume()
    {
        if (mCurrentSongInstance != null && !mCurrentSongInstance.IsDisposed)
        {
            mCurrentSongInstance.Volume = Settings.GetEffectiveMusicVol();
        }
    }
}