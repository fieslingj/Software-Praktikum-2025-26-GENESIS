using Arch.Core;
using Genesis.Gameplay.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Genesis.Architecture.Audio;
using Microsoft.Xna.Framework.Audio;

namespace Genesis.Gameplay.Entities
{
    public static class ButtonEntity
    {
        /// <summary>
        /// Creates a button entity in the given world at the specified position.
        /// Returns the created Entity.
        /// </summary>
        public static Entity Create(
            World world,
            Vector2 position,
            string text,
            Rectangle bounds,
            Texture2D texture,
            Action onClickAction,
            SpriteFont buttonFont,
            AudioService audio)
        {
            const float layerDepth = 0.5f;
            const float scale = 1.0f;

            var entity = world.Create(
                new ButtonComponent(bounds, () =>
                {
                    // Invoke the user-provided action first. It may set
                    // `audio.SuppressNextConfirmSound` and/or play an error sound.
                    onClickAction.Invoke();

                    // Only play the default confirmation sound if it wasn't suppressed
                    // by the caller during the click action.
                    if (!audio.SuppressNextConfirmSound)
                    {
                        audio.PlaySfx("Sounds/UI/ButtonConfirm");
                    }
                    else
                    {
                        // consume the flag so it only affects the next click
                        audio.SuppressNextConfirmSound = false;
                    }
                }),
                new PositionComponent(position),
                new SpriteComponent(
                    spriteSheet: texture,
                    sourceRect: new Rectangle(0, 0, texture.Width, texture.Height),
                    layerDepth: layerDepth,
                    scale: scale
                ),
                new TextComponent(
                    text: text,
                    font: buttonFont,
                    color: new Color(40, 40, 40),
                    alignment: TextAlignment.MiddleCenter
                )
            );

            return entity;
        }
    }
}