using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TowerFall;

namespace FortRise;

public static class TextureUtils 
{
    public static unsafe Texture2D GetTexture2DFromSubtexture(this Subtexture texture) 
    {
        Color[] tex2Ddata = new Color[texture.Texture2D.Width * texture.Texture2D.Height];
        texture.Texture.Texture2D.GetData(tex2Ddata);
        Color[] subTexData = new Color[texture.Width * texture.Height];

        var subX = texture.Rect.X;
        var subY = texture.Rect.Y;
        var width = texture.Rect.Width;
        var height = texture.Rect.Height;
        var texWidth = texture.Texture2D.Width;
        var subTexWidth = texture.Width;

        fixed (Color* rawTex = tex2Ddata) 
        fixed (Color* rawSubTex = subTexData) 
        {
            for (int y = height - 1; y > -1; y--) 
            {
                for (int x = width - 1; x > -1; x--) 
                {
                    rawSubTex[y * subTexWidth + x] = rawTex[(subY + y) * texWidth + subX + x];
                }
            }
        }
        
        var output = new Texture2D(TFGame.Instance.GraphicsDevice, texture.Width, texture.Height);
        output.SetData(subTexData);
        return output;
    }
}