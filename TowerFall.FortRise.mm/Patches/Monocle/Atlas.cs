using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using FortRise;
using FortRise.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;

namespace Monocle;

public class patch_Atlas : Atlas
{
    private string xmlPath;
    internal HashSet<string> injectedAtlas = new HashSet<string>();
    public string DataPath;

    public patch_Atlas(string xmlPath, string imagePath, bool load) : base(xmlPath, imagePath, load) {}

    public patch_Atlas() : base(null, null, false) {}

    public extern void orig_ctor(string xmlPath, string imagePath, bool load);

    [MonoModConstructor]
    public void ctor(string xmlPath, string imagePath, bool load) 
    {
        injectedAtlas = new HashSet<string>();
        orig_ctor(xmlPath, imagePath, load);

        TaggedSubTextures = new();
        DataPath = Path.Combine(Calc.LOADPATH, xmlPath)
            .Replace(".xml", "")
            .Replace('\\', '/')
            .Replace("/Atlas", "/VanillaAtlas");
        MapAllAssets(this, DataPath);
    }

    public static void MapAllAssets(patch_Atlas atlas, string path) 
    {
        if (path.StartsWith("Content/../DarkWorldContent/"))
            path = path.Replace("Content/../DarkWorldContent/", "Content/");
        foreach (var resource in RiseCore.ResourceTree.TreeMap.Values
            .Where(x => x.Path.Contains(path))) 
        {
            atlas.Digest(resource);
        }
    }

    public static void MergeAtlas(RiseCore.Resource resource, patch_Atlas source, Atlas destination, string prefix) 
    {
        foreach (var subTexture in source.SubTextures) 
        {
            destination.SubTextures.Add(prefix + subTexture.Key, subTexture.Value);
        }

        destination.GetAllInjectedAtlas().Add(resource.Root + resource.Path);
    }

    public static void MergeTexture(RiseCore.Resource resource, Subtexture source, Atlas destination, string prefix) 
    {
        var pngPath = resource.RootPath;
        int indexOfSlash = pngPath.IndexOf('/');
        var key = Path.ChangeExtension(pngPath.Substring(indexOfSlash + 1).Replace("Content/Atlas/atlas/", ""), null);
        destination.SubTextures.Add(prefix + key, source);
        destination.GetAllInjectedAtlas().Add(pngPath);
    }

    public void Digest(RiseCore.Resource resource) 
    {
        var pngPath = resource.Root + resource.Path;
        if (Path.GetExtension(pngPath) != ".png")
            return;
        var xmlPath = pngPath.Replace(".png", ".xml");
        if (!RiseCore.ResourceTree.TreeMap.ContainsKey(xmlPath)) 
            return;
        using var xmlStream = RiseCore.ResourceTree.TreeMap[xmlPath].Stream;
        using var imageStream = RiseCore.ResourceTree.TreeMap[pngPath].Stream;

        var baseTexture = new Texture(Texture2D.FromStream(Engine.Instance.GraphicsDevice, imageStream));
        
        var subTextures = patch_Calc.LoadXML(xmlStream)["TextureAtlas"].GetElementsByTagName("SubTexture");
        foreach (XmlElement subTexture in subTextures) 
        {
            var attrib = subTexture.Attributes;
            var x = Convert.ToInt32(attrib["x"].Value);
            var y = Convert.ToInt32(attrib["y"].Value);
            var width = Convert.ToInt32(attrib["width"].Value);
            var height = Convert.ToInt32(attrib["height"].Value);
            string name = subTexture.HasAttr("mapTo") ? attrib["mapTo"].Value : attrib["name"].Value;
            
            Subtexture subTex;
            if (!subTexture.HasAttr("tag")) 
            {
                subTex = new Subtexture(baseTexture, x, y, width, height);
                SubTextures[name] = subTex;
                continue;
            }
            var tagsCSV = attrib["tag"].Value;
            var tags = Calc.ReadCSV(tagsCSV);
            foreach (var tag in tags) 
            {
                subTex = new TaggedSubtexture(baseTexture, x, y, width, height, tags);
                if (TaggedSubTextures.TryGetValue(tag, out var textures)) 
                {
                    textures.Add(name, subTex);
                    continue;
                }
                var newTextures = new Dictionary<string, Subtexture>();
                newTextures.Add(name, subTex);
                TaggedSubTextures.Add(tag, newTextures);
            }
        }
    }

    public Dictionary<string, Subtexture> SubTextures { get; private set; }
    public Dictionary<string, Dictionary<string, Subtexture>> TaggedSubTextures { get; private set; }

    public Subtexture this[string name]
    {
        [MonoModReplace]
        get
        {
            if (TaggedSubTextures == null)
                return this.SubTextures[name];

            var scene = Engine.Instance.Scene;

            if (scene?.GetSceneTags() != null) 
            {
                foreach (var tag in scene.GetSceneTags())
                {
                    if (TaggedSubTextures.TryGetValue(tag, out var tex))
                    {
                        if (tex.TryGetValue(name, out var val))
                            return val;
                    }
                }
            }

            return this.SubTextures[name];
        }
    }

    internal void SetXMLPath(string xmlPath) 
    {
        this.xmlPath = xmlPath;
    }

    internal void SetImagePath(string imagePath) 
    {
        this.ImagePath = imagePath;
    }

    internal void SetSubTextures(Dictionary<string, Subtexture> subTextures) 
    {
        this.SubTextures = subTextures;
    }
    

    [MonoModConstructor]
    internal void ctor() 
    {
        injectedAtlas = new HashSet<string>();
    }

    [MonoModIgnore]
    public extern bool Contains(string name);

    internal void LoadStream(Stream fs) 
    {
        Texture2D = Texture2D.FromStream(Engine.Instance.GraphicsDevice, fs);
        Rect = new Rectangle(0, 0, this.Texture2D.Width, this.Texture2D.Height);
    }
}

public static class AtlasExt 
{
    internal static patch_Atlas CreateAtlasFromEmbedded(string xmlPath, string imagePath) 
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream xmlStream = assembly.GetManifestResourceStream(xmlPath);
        using Stream imageStream = assembly.GetManifestResourceStream(imagePath);

        using var streamReader = new StreamReader(xmlStream);
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(streamReader.ReadToEnd());

        XmlNodeList elementsByTagName = xmlDocument["TextureAtlas"].GetElementsByTagName("SubTexture");
        var atlas = new patch_Atlas();

        atlas.SetXMLPath(xmlPath);
        atlas.SetImagePath(imagePath);
        atlas.SetSubTextures(new Dictionary<string, Subtexture>(elementsByTagName.Count));
        foreach (XmlElement item in elementsByTagName)
        {
            XmlAttributeCollection attributes = item.Attributes;
            atlas.SubTextures.Add(attributes["name"].Value, new Subtexture(atlas, Convert.ToInt32(attributes["x"].Value), Convert.ToInt32(attributes["y"].Value), Convert.ToInt32(attributes["width"].Value), Convert.ToInt32(attributes["height"].Value)));
        }
        atlas.LoadStream(imageStream);
        
        return atlas;
    }

    public static patch_Atlas CreateAtlas(string xmlPath, string imagePath) 
    {
        using var rootXmlStream = ModIO.OpenRead(xmlPath);
        using var rootImageStream = ModIO.OpenRead(imagePath);
        return AtlasExt.CreateAtlas(rootXmlStream, rootImageStream);       
    }

    public static patch_Atlas CreateAtlas(FortContent content, string xmlPath, string imagePath, ContentAccess access = ContentAccess.Root)
    {
        if (access == ContentAccess.Content) 
        {
            xmlPath = Calc.LOADPATH + xmlPath;
            imagePath =  Calc.LOADPATH + imagePath;
        }
        else if (content != null) 
        {
            xmlPath = Path.Combine(content.MetadataPath, xmlPath);
            imagePath = Path.Combine(content.MetadataPath, imagePath);
        }
        using var rootXmlStream = ModIO.OpenRead(xmlPath);
        using var rootImageStream = ModIO.OpenRead(imagePath);
        return AtlasExt.CreateAtlas(rootXmlStream, rootImageStream);
    }

    public static patch_Atlas CreateAtlas(Stream xmlStream, Stream imageStream)
    {
        patch_Atlas atlas = AtlasReader.Read(xmlStream, ".xml");
        atlas.LoadStream(imageStream);
        return atlas;
    }

    public static patch_Atlas CreateAtlas(Stream xmlStream, Stream imageStream, string ext)
    {
        patch_Atlas atlas = AtlasReader.Read(xmlStream, ext);
        atlas.LoadStream(imageStream);
        return atlas;
    }

    public static patch_Atlas CreateAtlasJson(Stream jsonStream, Stream imageStream)
    {
        patch_Atlas atlas = AtlasReader.Read(jsonStream, ".json");
        atlas.LoadStream(imageStream);
        return atlas;
    }

    internal static HashSet<string> GetAllInjectedAtlas(this Atlas atlas)
    {
        return ((patch_Atlas)atlas).injectedAtlas;
    }
}
