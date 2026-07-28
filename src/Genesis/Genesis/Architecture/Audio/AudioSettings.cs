using System;
using Microsoft.Xna.Framework;

namespace Genesis.Architecture.Audio;

[Serializable]
public class AudioSettings
{
    private float mMasterVolume = 1.0f;
    private float mMusicVolume = 1.0f;
    private float mSfxVolume = 1.0f;

    public float MasterVolume
    {
        get => mMasterVolume;
        set => mMasterVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
    }

    public float MusicVolume
    {
        get => mMusicVolume;
        set => mMusicVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
    }

    public float SfxVolume
    {
        get => mSfxVolume;
        set => mSfxVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
    }

    public bool IsMasterMuted { get; set; } = false;
    public bool IsMusicMuted { get; set; } = false;
    public bool IsSfxMuted { get; set; } = false;
    
    public bool EnableSpatialPanning { get; set; } = true;

    private float mStereoWidth = 1.0f;
    public float StereoWidth
    {
        get => mStereoWidth;
        set => mStereoWidth = MathHelper.Clamp(value, 0.0f, 1.0f);
    }
    
    public float GetEffectiveMusicVol() => (IsMasterMuted || IsMusicMuted) ? 0.0f : mMusicVolume * mMasterVolume;
    public float GetEffectiveSfxVol() => (IsMasterMuted || IsSfxMuted) ? 0.0f : mSfxVolume * mMasterVolume;
}