namespace Genesis.Architecture;

/// <summary>
/// Defines abstract game actions that a player can perform.
/// </summary>
/// <remarks>
/// Systems (like PlayerInputSystem) should check for these abstract actions,
/// NOT for specific hardware keys (e.g. InputAction.Interact instead of Keys.E).
/// The <see cref="InputService"/> is responsible for mapping raw hardware input.
/// </remarks>
public enum InputAction
{
    // Movement-Axes
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    
    // Actions
    Sprint,
    Duck,
    UseMutation,
    PrimaryItemAction,
    SecondaryItemAction,
    Interact,
    OpenInventory,
    
    //Menu
    Pause,
    Save,
    Load,
    ScrollUp,
    ScrollDown,
    
    // Debug
    ToggleDebug,
    ToggleDebugCounter,
    
    // HUD
    ActivateSlot0,
    ActivateSlot1,
    ActivateSlot2,
    ActivateSlot3,
    ActivateSlot4,
}