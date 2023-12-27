using Microsoft.Xna.Framework.Graphics;

namespace FortRise;

public static class SpriteBatchExtension 
{
    public static void BeginShaderRegion(this SpriteBatch spriteBatch) 
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
    }

    public static void BeginShaderRegion(this SpriteBatch spriteBatch, ShaderResource shader) 
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, shader.Shader);
    }

    public static void EndShaderRegion(this SpriteBatch spriteBatch) 
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
    }
}