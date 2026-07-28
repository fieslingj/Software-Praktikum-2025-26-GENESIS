using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Genesis.Architecture.ECS;

public interface ISystem {}

public interface IUpdateSystem : ISystem
{
    void Update(World world, GameTime gameTime);
}

public interface IDrawSystem : ISystem
{
    void Draw(World world, SpriteBatch spriteBatch, bool ySorting=false);
}

public interface IInputSystem : ISystem
{
    void HandleInput(World world, InputService input);
}