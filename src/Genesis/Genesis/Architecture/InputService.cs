using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Genesis.Architecture;

/// <summary>
/// Manages raw hardware input (Keyboard, Mouse) and maps it to
/// abstract <see cref="InputAction"/> values.
/// </summary>
/// <remarks>
/// This service provides a central place for ECS systems to
/// fetch intended actions. Systems and states should never read
/// hardware input themselves.
/// </remarks>
public class InputService
{
    private KeyboardState mPrevKbdState, mCurrentKbdState;
    private MouseState mPrevMouseState, mCurrentMouseState;
    private int mScrolldelta = 0;

    private bool IsWindowFocused { get; set; } = true;

    //damit nicht item benutzt wird wenn man mit einem Klick aus dem Shop oder Menu geht.
    public double pausetime = 0;

    /// <summary>
    /// Binds keys to actions.
    /// </summary>
    private Dictionary<InputAction, List<Keys>> mKeyBindings  = new()
    {
        { InputAction.MoveUp, [Keys.W, Keys.Up] },
        { InputAction.MoveDown, [Keys.S, Keys.Down] },
        { InputAction.MoveLeft, [Keys.A, Keys.Left] },
        { InputAction.MoveRight, [Keys.D, Keys.Right] },
        { InputAction.Sprint, [Keys.LeftShift] },
        { InputAction.Pause, [Keys.Escape] },
        { InputAction.ToggleDebug, [Keys.F12] },
        { InputAction.ToggleDebugCounter, [Keys.F11] },
        { InputAction.Interact, [Keys.E] },
        { InputAction.OpenInventory, [Keys.I] },
        { InputAction.Duck, [Keys.LeftControl, Keys.RightControl]},
        { InputAction.ActivateSlot0, [Keys.D1] },
        { InputAction.ActivateSlot1, [Keys.D2] },
        { InputAction.ActivateSlot2, [Keys.D3] },
        { InputAction.ActivateSlot3, [Keys.D4] },
        { InputAction.ActivateSlot4, [Keys.D5] },
        { InputAction.SecondaryItemAction, [Keys.F] },
        { InputAction.ScrollUp, [Keys.Up] },
        { InputAction.ScrollDown, [Keys.Down] },
    };
    
    /// <summary>
    /// Default Bindings.
    /// </summary>
    private readonly Dictionary<InputAction, List<Keys>> mDefaultKeyBindings  = new()
    {
        { InputAction.MoveUp, [Keys.W, Keys.Up] },
        { InputAction.MoveDown, [Keys.S, Keys.Down] },
        { InputAction.MoveLeft, [Keys.A, Keys.Left] },
        { InputAction.MoveRight, [Keys.D, Keys.Right] },
        { InputAction.Sprint, [Keys.LeftShift] },
        { InputAction.Pause, [Keys.Escape] },
        { InputAction.ToggleDebug, [Keys.F12] },
        { InputAction.ToggleDebugCounter, [Keys.F11] },
        { InputAction.Interact, [Keys.E] },
        { InputAction.OpenInventory, [Keys.I] },
        { InputAction.Duck, [Keys.LeftControl, Keys.RightControl]},
        { InputAction.ActivateSlot0, [Keys.D1] },
        { InputAction.ActivateSlot1, [Keys.D2] },
        { InputAction.ActivateSlot2, [Keys.D3] },
        { InputAction.ActivateSlot3, [Keys.D4] },
        { InputAction.ActivateSlot4, [Keys.D5] },
        { InputAction.SecondaryItemAction, [Keys.F] },
        { InputAction.ScrollUp, [Keys.Up] },
        { InputAction.ScrollDown, [Keys.Down] },
    };
    
    /// <summary>
    /// Maps Bindings to Names for Display in settings.
    /// </summary>
    private readonly Dictionary<InputAction, string> mBindingNames  = new()
    {
        { InputAction.MoveUp, "Move up" },
        { InputAction.MoveDown, "Move down" },
        { InputAction.MoveLeft, "Move left" },
        { InputAction.MoveRight, "Move right" },
        { InputAction.Save, "Save" },
        { InputAction.Load, "Load" },
        { InputAction.Sprint, "Sprint" },
        { InputAction.Pause, "Pause" },
        { InputAction.ToggleDebug, "Toggle debug mode" },
        { InputAction.ToggleDebugCounter, "Toggle FPS" },
        { InputAction.Interact, "Interact" },
        { InputAction.OpenInventory, "Inventory" },
        { InputAction.Duck, "Duck" },
        { InputAction.ActivateSlot0, "Slot 0" },
        { InputAction.ActivateSlot1, "Slot 1" },
        { InputAction.ActivateSlot2, "Slot 2" },
        { InputAction.ActivateSlot3, "Slot 3" },
        { InputAction.ActivateSlot4, "Slot 4" },
        { InputAction.SecondaryItemAction, "Secondary item action" },
        { InputAction.ScrollUp, "Scroll up" },
        { InputAction.ScrollDown, "Scroll down" },
    };

    //get Keybindings for settings
    public Dictionary<InputAction, List<Keys>> GetKeyBindings()
    {
        return mKeyBindings;
    }
    //get Keybindings for settings
    public Dictionary<InputAction, string> GetKeyBindingNames()
    {
        return mBindingNames;
    }
    

    /// <summary>
    /// Reads the current hardware state (Keyboard, Mouse) for
    /// this frame. This MUST be called exactly once at the beginning
    /// of the game's main Update loop.
    /// </summary>
    public void Update(bool isActive, GameTime gameTime)
    {
        if (pausetime > 0)
        {
            pausetime -= gameTime.ElapsedGameTime.Milliseconds;}
        
        mPrevKbdState = mCurrentKbdState;
        mPrevMouseState = mCurrentMouseState;
        
        IsWindowFocused = isActive;
        
        if (IsWindowFocused)
        {
            mCurrentKbdState = Keyboard.GetState();
            mCurrentMouseState = Mouse.GetState();
            
            mScrolldelta = mCurrentMouseState.ScrollWheelValue - mPrevMouseState.ScrollWheelValue;
        }
        else
        {
            mCurrentKbdState = new KeyboardState();
            var rawMouse = Mouse.GetState();
            mCurrentMouseState = new MouseState(
                rawMouse.X, rawMouse.Y, 
                rawMouse.ScrollWheelValue, 
                ButtonState.Released, ButtonState.Released, ButtonState.Released, 
                ButtonState.Released, ButtonState.Released
            );
        }
        
    }

    /// <summary>
    /// Checks if an action is *currently held down* in the current frame.
    /// </summary>
    /// <param name="inputAction">The abstract action to check (e.g. Sprint).</param>
    /// <returns>True if the action is held down, false otherwise.</returns>
    public bool IsActionDown(InputAction inputAction)
    {
        if (inputAction == InputAction.PrimaryItemAction)
        {
            return IsLeftMouseDown();
        }
        if (!IsWindowFocused) { return false; }
        
        if (mKeyBindings.TryGetValue(inputAction, out var keys))
        {
            foreach (var key in keys)
            {
                if (mCurrentKbdState.IsKeyDown(key))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if an action was *just pressed* in the current frame.
    /// (i.e., was Up last frame and is Down this frame).
    /// </summary>
    /// <param name="inputAction">The abstract action to check (e.g., Interact).</param>
    /// <returns>True if the action is held down, false otherwise.</returns>
    public bool IsActionPressed(InputAction inputAction)
    {
        if (!IsWindowFocused) { return false; }
        
        if (inputAction == InputAction.PrimaryItemAction) {return IsLeftMousePressed();}
        
        if (mKeyBindings.TryGetValue(inputAction, out var keys))
        {
            foreach (var key in keys)
            {
                if (mCurrentKbdState.IsKeyDown(key) && mPrevKbdState.IsKeyUp(key))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if the left mouse button is *currently held down* in the current frame.
    /// </summary>
    /// <returns>True if the left mouse button is held down, false otherwise.</returns>
    public bool IsLeftMouseDown()
    {
        if (pausetime > 0) return false;
        return mCurrentMouseState.LeftButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks if the left mouse button was *just pressed* in the current frame.
    /// (i.e., was Up last frame and is Down this frame).
    /// </summary>
    /// <returns>True if the left mouse button is held down, false otherwise.</returns>
    public bool IsLeftMousePressed()
    {
        return mCurrentMouseState.LeftButton == ButtonState.Pressed
            && mPrevMouseState.LeftButton == ButtonState.Released;
    }
    
    /// <summary>
    /// Checks if the right mouse button was *just pressed* in the current frame.
    /// (i.e., was Up last frame and is Down this frame).
    /// </summary>
    /// <returns>True if the right mouse button is held down, false otherwise.</returns>
    public bool IsRightMousePressed()
    {
        return mCurrentMouseState.RightButton == ButtonState.Pressed
               && mPrevMouseState.RightButton == ButtonState.Released;
    }

    /// <summary>
    /// Returns the current mouse state.
    /// </summary>
    /// <returns>The current mouse state.</returns>
    public MouseState GetCurrentMouseState()
    {
        return mCurrentMouseState;
    }

    /// <summary>
    /// Returns the previous mouse state.
    /// </summary>
    /// <returns>The previous mouse state.</returns>
    public MouseState GetPreviousMouseState()
    {
        return mPrevMouseState;
    }

    /// <summary>
    /// Returns the current mouse position.
    /// </summary>
    /// <returns>The current mouse position.</returns>
    public Point GetMousePosition()
    {
        return mCurrentMouseState.Position;
    }
    
    //for scrolling
    public int GetMouseScroll()
    {
        return  mScrolldelta;
    }

    public bool AnyKeypressed()
    {
        if (mCurrentKbdState.GetPressedKeyCount() > 0) {return true;}
        return false;
    }
    
    //for changing Keybinding
    public Keys GetPressedKey()
    {
        if (mCurrentKbdState.GetPressedKeys().Length < 1) {return Keys.None;}
        return mCurrentKbdState.GetPressedKeys().First();
    }

    public void ChangeKeyBinding(InputAction inputAction, Keys key)
    {
        mKeyBindings.Remove(inputAction);
        mKeyBindings.Add(inputAction, [key]);
    }
    public void ChangeKeyBindingDefault()
    {
        mKeyBindings = new Dictionary<InputAction, List<Keys>>(mDefaultKeyBindings);
    }
}