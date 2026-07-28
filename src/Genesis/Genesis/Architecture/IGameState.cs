using Genesis.Architecture.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture;

/// <summary>
/// Defines the "contract" for a game state (e.g. MainMenu, Ingame, Pause, ...)
/// that can be managed by the <see cref="GameStateManager"/>.
/// </summary>
public interface IGameState
{
    /// <summary>
    /// Initializes the state and provides it with core dependencies.
    /// This is called by the GameStateManager *before* the state is pushed to the stack.
    /// </summary>
    /// <param name="manager">The <see cref="GameStateManager"/>, to allow this state to request changes.</param>
    /// <param name="services">The <see cref="GameServices"/> toolbox. Holds all necessary services.</param>
    /// <param name="window">The <see cref="GameWindow"/>.</param>
    /// <param name="sound">The <see cref="AudioService"/>, to allow the state to play sounds.</param>
    void Initialize(GameStateManager manager, GameServices services, ScreenService screen, AudioService sound);

    /// <summary>
    /// Called once when this state becomes the primary active state for the first time.
    /// This handles initial setup tasks that occur when control is gained.
    /// </summary>
    void Enter();
    
    /// <summary>
    /// Called once when this state is permanently terminated or completely removed from the stack.
    /// This releases all unmanaged or disposable resources (e.g., calling Dispose on the HudWorld).
    /// </summary>
    void Exit();
    
    /// <summary>
    /// Called when a new state is pushed on top of the current state.
    /// This temporarily halts continuous logic and sounds, but does not release resources.
    /// </summary>
    void Pause();

    /// <summary>
    /// Called when the state stacked above is removed (popped), making this state active again.
    /// This resumes continuous logic and sounds.
    /// </summary>
    void Resume();

    /// <summary>
    /// Translates user inputs into meta-actions (like switching game-states)
    /// or gameplay impacting input (like player movement)
    /// </summary>
    void HandleInput(InputService input);
    
    /// <summary>
    /// Executes the continuous game logic for this state. In particular,
    /// running ECS systems.
    /// </summary>
    /// <param name="gameTime">Delta-time information from MonoGame.</param>
    void Update(GameTime gameTime);
    
    /// <summary>
    /// Executes all rendering logic for this state.
    /// </summary>
    /// <param name="gameTime"></param>
    /// <param name="spriteBatch"></param>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}