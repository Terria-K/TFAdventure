using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace FortRise;

public static class ShaderManager 
{
    public static Dictionary<string, int> ShadersLookup = new();
    public static List<ShaderResource> Shaders = new();

    public static T AddShader<T>(RiseCore.Resource fxRes, string passName, out int id) 
    where T : ShaderResource, new()
    {
        using var fxStream = fxRes.Stream;
        using var memStream = new MemoryStream();
        fxStream.CopyTo(memStream);
        var effect = new Effect(Engine.Instance.GraphicsDevice, memStream.ToArray());
        var shaderResource = new T();
        shaderResource.Init(effect, passName);
        id = Shaders.Count;
        Shaders.Add(shaderResource);
        ShadersLookup[fxRes.RootPath] = id;
        return shaderResource;
    }
}