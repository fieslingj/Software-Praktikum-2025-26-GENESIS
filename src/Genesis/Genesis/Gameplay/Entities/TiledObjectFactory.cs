using Arch.Core;
using Genesis.Gameplay.Components;
using Genesis.Gameplay.Extensions;
using Genesis.Gameplay.Navigation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;

namespace Genesis.Gameplay.Entities;

public static class TiledObjectFactory
{
    /// <summary>
    /// für tiles statt objects
    /// </summary>
    /// <param name="world"></param>
    /// <param name="mapObject"></param>
    /// <param name="spriteSheet"></param>
    /// <param name="TileLid"></param>
    /// <param name="TileColumns"></param>
    public static void Create(
        World world,
        TiledMapTile tile,
        TiledMapTileset tileset,
        Texture2D tileSetSpriteSheet,
        int tiledLocalid, 
        float layerdepth,
        bool withCollider = false)
    {
        //bei objekten übereinander die aus zwei tiles bestehen offset auf das obere, die localid kann man in tiled sehen
        Vector2 ysortoffset = Vector2.Zero;
        //für bett, leerentank, zweites bett
        if(tiledLocalid == 240 || tiledLocalid == 231 || tiledLocalid == 230){ysortoffset.Y = 32;}
        //für tiled chemietanks falls sie noch da sind.
        if(tiledLocalid >= 144 && tiledLocalid <= 147){ysortoffset.Y = 32;}
        if(tiledLocalid >= 192 && tiledLocalid <= 195){ysortoffset.Y = 32;}
        
        
        int tilewidth = tileset.TileWidth;
        int tileheight = tileset.TileHeight;
        var source = tileset.GetTileRegion(tiledLocalid);
        
        var tilepos = new Vector2(tile.X * tilewidth, tile.Y * tileheight);
        var offset = new Vector2(tilewidth/2f, tileheight - 10);
        var position = tilepos + offset;
        var spriteComponent = new SpriteComponent(tileSetSpriteSheet, source, layerdepth, scale: 1.0f, offset:ysortoffset)
        {
            Origin = offset
        };
        var entity = world.Create(
            new PositionComponent(position),
            spriteComponent
        );
        if (withCollider)
        {
            var size = new Vector2(tilewidth, tileheight);
            var collider = new ColliderComponent(size);
            var gridMap = world.GetResource<GridMap>();
            gridMap?.MarkColliderAsUnwalkable(position, collider);
            
            
            world.Add(entity, collider);
        }
    }
    
    /// <summary>
    /// Calculates the source rectangle within the tile sheet for a given Local Tile ID (LID).
    /// </summary>
    private static Rectangle GetSourceRect(int tileWidth, int tileHeight, int tileLid, int tileColumns)
    {
         int col = tileLid % tileColumns;
         int row = tileLid / tileColumns;
        return new Rectangle(
            col * tileWidth,
            row * tileHeight,
            tileWidth,
            tileHeight
        );
    }
}