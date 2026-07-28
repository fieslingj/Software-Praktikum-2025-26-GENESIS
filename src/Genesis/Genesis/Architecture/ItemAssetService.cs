using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Genesis.Gameplay.Definitions;

namespace Genesis.Architecture;

public class ItemAssetService
{
    private readonly Dictionary<ItemType, Texture2D> mItemIcons = new();
    private readonly ContentManager mContent;

    public ItemAssetService(ContentManager content)
    {
        mContent = content;
        LoadAssets();
    }

    public void LoadAssets()
    {
        mItemIcons.Clear();

        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            if (type == ItemType.None) {continue;}
            
            var definition = ItemDefinitions.Get(type);
            var path = definition.IconPath;

            try
            {
                mItemIcons[type] = mContent.Load<Texture2D>(path);
            }
            catch (ContentLoadException)
            {
                Console.WriteLine($"[Warning] Failed to load icon for {type} at '{path}'. Using default.");
                mItemIcons[type] = mContent.Load<Texture2D>("Sprites/Icons/Default");
            }
        }
    }

    public Texture2D GetIcon(ItemType type)
    {
        return mItemIcons.GetValueOrDefault(type);
    }
}