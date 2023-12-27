using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace FortRise;

public class ShaderResource 
{
    public Effect Shader => shader;
    private Effect shader;
    private EffectPass effectPass;

    private string passName;

    internal ShaderResource() 
    {

    }

    internal void Init(Effect shader, string passName) 
    {
        this.shader = shader;
        this.passName = passName;
        this.effectPass = shader.Techniques[0].Passes[passName];
    }

    public void SwapPass(string passName) 
    {
        this.passName = passName;
        effectPass = shader.Techniques[0].Passes[passName];
    }

    public virtual void Apply() 
    {
        effectPass.Apply();
    }
}

public class MiscShaderResource : ShaderResource
{
    private Vector4 uColor = Vector4.One;
    private Texture2D uTexture0;
    private Texture2D uTexture1;
    private Texture2D uTexture2;
    

    public override void Apply()
    {
        Shader.Parameters["uColor"].SetValue(uColor);
        Shader.Parameters["uDeltaTime"].SetValue(Engine.TimeMult);
        if (uTexture0 != null) 
        {
            Engine.Instance.GraphicsDevice.Textures[0] = uTexture0;
            Engine.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            Shader.Parameters["uTextureSize0"].SetValue(
                new Vector2(uTexture0.Width, uTexture0.Height)
            );
        }
        if (uTexture1 != null) 
        {
            Engine.Instance.GraphicsDevice.Textures[1] = uTexture1;
            Engine.Instance.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            Shader.Parameters["uTextureSize1"].SetValue(
                new Vector2(uTexture1.Width, uTexture1.Height)
            );
        }
        if (uTexture2 != null) 
        {
            Engine.Instance.GraphicsDevice.Textures[2] = uTexture2;
            Engine.Instance.GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
            Shader.Parameters["uTextureSize2"].SetValue(
                new Vector2(uTexture2.Width, uTexture2.Height)
            );
        }
        base.Apply();
    }

    public MiscShaderResource UseTexture0(Texture2D texture) 
    {
        uTexture0 = texture;
        return this;
    }

    public MiscShaderResource UseTexture1(Texture2D texture) 
    {
        uTexture1 = texture;
        return this;
    }

    public MiscShaderResource UseTexture2(Texture2D texture) 
    {
        uTexture2 = texture;
        return this;
    }
}