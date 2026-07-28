using System.Collections.Generic;
using Genesis.Architecture.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture;

/// <summary>
/// Manages a stack of <see cref="IGameState"/> objects (e.g. Pause > Ingame).
/// </summary>
/// <remarks>
/// This class is the bridge of the game's flow. It is owned by Game1.cs
/// and delegates all <see cref="Update"/> and <see cref="Draw"/> calls
/// to the *currently active* state on its stack.
/// </remarks>
public class GameStateManager(Game game, GameServices services, ScreenService screen, CameraService cameraService, AudioService sound)
{
    // The Stack of our GameStates. Only the top on is active.
    // That way the IngameState is not lost when going into a PauseState
    private readonly Stack<IGameState> mStates = new();

    #region Stack-Management (Push, Pop, Change)
    
    /// <summary>
    /// Pushes a new state onto the stack, pausing the current state.
    /// (e.g. IngameState -> PauseState)
    /// </summary>
    /// <param name="state">The new state to make active.</param>
    public void PushState(IGameState state)
    {
        // Pause the previous state
        if (mStates.Count > 0)
        {
            mStates.Peek().Pause();
        }
        
        // Initialize the _new_ state
        state.Initialize(this, services, screen, sound);
        
        // Optional Injection
        if (state is ICameraUser cameraUser)
        {
            cameraUser.SetCameraService(cameraService);
            screen.Adapter.Reset();
        }
        
        mStates.Push(state);
        state.Enter();
    }

    /// <summary>
    /// Pops the current state from the stack, resuming the one below it.
    /// (e.g. PauseState -> IngameState)
    /// </summary>
    public void PopState()
    {
        if (mStates.Count == 0)
        {
            return;
        }

        // Properly exit the state
        var removedState = mStates.Pop();
        removedState.Exit();

        // Resume the underlying state if it exists
        if (mStates.Count > 0)
        {
            mStates.Peek().Resume();
        }
    }

    /// <summary>
    /// Pops *all* states and pushes a new one as the only state.
    /// (e.g. PauseState -> MainMenuState)
    /// </summary>
    /// <param name="state">The new base state to start.</param>
    public void ChangeState(IGameState state)
    {
        // Exit all previous states
        while (mStates.Count > 0)
        {
            mStates.Pop().Exit();
        }
        
        PushState(state);
    }
    
    /// <summary>
    /// Retrieves the <see cref="IGameState"/> that is currently second from the top of the stack.
    /// This state is typically the one that is paused (e.g., IngameState under PauseMenuState).
    /// </summary>
    /// <returns>The <see cref="IGameState"/> below the top, or null if there are fewer than two states.</returns>
    public IGameState GetBelowTopState()
    {
        var statesArray = mStates.ToArray();
        return statesArray.Length >= 2 ? statesArray[1] : null;
    }
    
    /// <summary>
    /// Checks if the currently active state (the top of the stack) is of the specified type T.
    /// </summary>
    /// <typeparam name="T">The expected type of the IGameState (e.g., InventoryState).</typeparam>
    /// <returns>True if the top state matches the type T; otherwise, False.</returns>
    public bool IsTopState<T>() where T : IGameState
    {
        if (mStates.Count == 0)
        {
            return false;
        }
        return mStates.Peek() is T;
    }

    #endregion

    #region Game Loop Delegation

    /// <summary>
    /// Handle the input of the currently active state.
    /// </summary>
    public void HandleInput(InputService input)
    {
        if (mStates.Count > 0)
        {
            mStates.Peek().HandleInput(input);
        }
    }

    /// <summary>
    /// Delegates <see cref="Game.Update"/> call to the active state.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (mStates.Count > 0)
        {
            mStates.Peek().Update(gameTime);
        }
    }

    /// <summary>
    /// Delegates the <see cref="Game.Draw"/> call to the active state.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (mStates.Count > 0)
        {
            mStates.Peek().Draw(gameTime, spriteBatch);
        }
    }
    public void DrawBelowTop(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var arr = mStates.ToArray();
        if (arr.Length > 1)
        {
            arr[1].Draw(gameTime, spriteBatch);
        }
    }

    #endregion

    #region Game Control

    /// <summary>
    /// Exits the game by calling the MonoGame Game.Exit() method.
    /// </summary>
    public void ExitGame()
    {
        game.Exit();
    }

    #endregion
}