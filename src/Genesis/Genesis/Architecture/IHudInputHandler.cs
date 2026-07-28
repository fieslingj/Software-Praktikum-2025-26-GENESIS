using Microsoft.Xna.Framework;

namespace Genesis.Architecture;

public interface IHudController
{
    void HandleHudInput(InputService input);
    void UpdateHud(GameTime gameTime);
}