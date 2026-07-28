using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ViewportAdapters;

namespace Genesis.Architecture;

public class ScreenService
{
    private readonly GraphicsDeviceManager mGraphicsManager;
    
    // --- State Memory ---
    private int mLastWindowWidth;
    private int mLastWindowHeight;
    private bool mHasSavedWindowSize;

    // --- Constants ---
    public const int VirtualWidth = 32 * 32;
    public const int VirtualHeight = 18 * 32;
    
    // --- Properties ---
    public GraphicsDevice Graphics => mGraphicsManager.GraphicsDevice;
    public GameWindow Window { get; }
    public BoxingViewportAdapter Adapter { get; }
 
    public bool IsFullScreen => mGraphicsManager.IsFullScreen;

    // Event, which is triggered when display mode changes
    public event Action DisplayChanged;

    public ScreenService(GraphicsDeviceManager graphicsManager, GameWindow window)
    {
        mGraphicsManager = graphicsManager;
        Window = window;
        
        Adapter = new BoxingViewportAdapter(Window, Graphics, VirtualWidth, VirtualHeight);
        
        // Subscribe to window resize events to keep our Adapter in sync
        Window.ClientSizeChanged += OnDisplayChanged;
        Window.AllowUserResizing = true;
    }

    // --- Public API ---
    public void SetFullscreen(bool enableFullScreen)
    {
        if (enableFullScreen == IsFullScreen) {return;}

        if (enableFullScreen)
        {
            UpdateSavedWindowSize();
            var (targetWidth, targetHeight) = GetMonitorResolution();
            mGraphicsManager.PreferredBackBufferWidth = targetWidth;
            mGraphicsManager.PreferredBackBufferHeight = targetHeight;
            mGraphicsManager.IsFullScreen = true;
        }
        else
        {
            mGraphicsManager.IsFullScreen = false;
            var (targetWidth, targetHeight) = GetRestoredWindowSize();
            mGraphicsManager.PreferredBackBufferWidth = targetWidth;
            mGraphicsManager.PreferredBackBufferHeight = targetHeight;
        }

        mGraphicsManager.ApplyChanges();
        HandleWindowResize();
    }

    public void ToggleFullscreen()
    {
        SetFullscreen(!IsFullScreen);
    }

    public void SetResolution(int width, int height)
    {
        if (IsFullScreen) {return;}
        
        mGraphicsManager.PreferredBackBufferWidth = width;
        mGraphicsManager.PreferredBackBufferHeight = height;
        mGraphicsManager.ApplyChanges();
        
        HandleWindowResize();
    }
    
    public float GetUiScale()
    {
        var vp = Graphics.Viewport;
        var scaleX = vp.Width / (float)VirtualWidth;
        var scaleY = vp.Height / (float)VirtualHeight;

        return Math.Min(scaleX, scaleY);
    }
    
    // --- Private Helpers ---
    
    // Gets the current viewport
    private void UpdateSavedWindowSize()
    {
        if (mGraphicsManager.IsFullScreen) { return; }
        mLastWindowWidth = mGraphicsManager.PreferredBackBufferWidth;
        mLastWindowHeight = mGraphicsManager.PreferredBackBufferHeight;
        mHasSavedWindowSize = true;
    }
    
    private void HandleWindowResize()
    {
        UpdateSavedWindowSize();
        Adapter.Reset();
        DisplayChanged?.Invoke();
    }

    private void OnDisplayChanged(object sender, EventArgs e)
    {
        HandleWindowResize();
    }
    
    private (int width, int height) GetMonitorResolution()
    {
        var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        return (dm.Width, dm.Height);
    }

    /// <summary>
    /// Return saved user size OR default design size
    /// </summary>
    private (int width, int height) GetRestoredWindowSize()
    {
        return mHasSavedWindowSize
            ? (mLastWindowWidth, mLastWindowHeight)
            : (VirtualWidth, VirtualHeight);
    }
}