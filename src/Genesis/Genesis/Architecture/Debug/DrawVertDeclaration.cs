using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;

namespace Genesis.Architecture.Debug;

public static class DrawVertDeclaration
{
    public static readonly VertexDeclaration sDeclaration;

    public static readonly int sSize;

    static DrawVertDeclaration()
    {
        unsafe { sSize = sizeof(ImDrawVert); }

        sDeclaration = new VertexDeclaration(
            sSize,

            // Position
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

            // UV
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

            // Color
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
    }
}