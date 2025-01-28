using System;
using System.IO;
using XNAGraphics = Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Generic;
using System.Linq;
using TowerFall;
using FortRise.Adventure;
using System.Xml;
using FortRise.IO;
using System.Diagnostics;
using MonoMod;
using System.Text.Json;

namespace FortRise;

/// <summary>
/// A class that holds the whole mod's content and provides an API to create a lookup stream.
/// </summary>
/// <remarks>
/// This class is only use for getting a mod content inside of a module.
/// This will not interact the filesystem outside of the mod content.
/// </remarks>
public class FortContent
{
    public delegate void DataLoaderHandler(RiseCore.Resource resource, string rootPath);
    public static event DataLoaderHandler DataLoader;
    /// <summary>
    /// A property where your default Content lookup is being used. Some methods are relying on this.
    /// </summary>
    /// <value>The <c>ContentPath</c> property represents your Content path.</value>
    public string ContentPath
    {
        get => contentPath;
        set => contentPath = value;
    }
    public readonly RiseCore.ModResource ResourceSystem;
    private string contentPath = "Content";

    public Dictionary<string, RiseCore.Resource> MapResource => ResourceSystem.Resources;
    public IReadOnlyDictionary<string, patch_Atlas> Atlases => atlases;
    private Dictionary<string, patch_Atlas> atlases = new();

    public IReadOnlyDictionary<string, patch_SFX> SFX => sfxes;
    private Dictionary<string, patch_SFX> sfxes = new();

    public IReadOnlyDictionary<string, patch_SpriteData> SpriteDatas => spriteDatas;
    private Dictionary<string, patch_SpriteData> spriteDatas = new();

    private FileSystemWatcher watcher;
    private Dictionary<string, WatchTexture> watchingTexture = new Dictionary<string, WatchTexture>();
    private string requestedPath;
    private bool requestReload;

    public string MetadataPath
    {
        get
        {
            if (ResourceSystem.Metadata == null)
            {
                return "mod:::global:";
            }
            return $"mod:{ResourceSystem.Metadata.Name}";
        }
    }


    public RiseCore.Resource this[string path]
    {
        get
        {
            path = path.Replace('\\', '/');
            var root = $"mod:{ResourceSystem.Metadata.Name}/";
            return RiseCore.ResourceTree.TreeMap[root + path];
        }
    }

    public FortContent(RiseCore.ModResource resource) : base()
    {
        ResourceSystem = resource;

        if (ResourceSystem.Metadata != null && ResourceSystem.Metadata.IsDirectory) 
        {
            var dir = Path.Combine(resource.Metadata.PathDirectory, contentPath);
            if (!Directory.Exists(dir)) 
            {
                return;
            }
            watcher = new FileSystemWatcher(dir);
            watcher.EnableRaisingEvents = true;
            watcher.Changed += OnReload;
            watcher.IncludeSubdirectories = true;
        }
    }

    private void OnReload(object sender, FileSystemEventArgs e)
    {
        if (requestReload) // prevents another call
        {
            return;
        }
        requestReload = true;
        RiseCore.ResourceReloader.ContentRequestedReload.Enqueue(this);
        requestedPath = e.FullPath;
    }

    internal void Unload(bool disposeTexture)
    {
        Dispose(disposeTexture);
    }

    internal void Reload() 
    {
        requestReload = false;

        if (!watchingTexture.TryGetValue(requestedPath.Replace('\\', '/'), out var watchTexture))
        {
            return;
        }

        if (watchTexture.Type == typeof(RiseCore.ResourceTypeAtlasPng) || 
            watchTexture.Type == typeof(RiseCore.ResourceTypeBGAtlasPng) ||
            watchTexture.Type == typeof(RiseCore.ResourceTypeBossAtlasPng) ||
            watchTexture.Type == typeof(RiseCore.ResourceTypeMenuAtlasPng) ||
            watchTexture.Type == typeof(RiseCore.ResourceTypeAtlas))
        {
            try 
            {
                using var resource = ModIO.OpenRead(requestedPath.Replace('\\', '/'));
                using CPUImage image = new CPUImage(resource);
                watchTexture.Texture.Texture2D.SetData(image.Pixels.ToArray());
            }
            catch (FailedToLoadImageException)
            {
                Logger.Error("Failed to load image error, try again!");
            }
        }
    }

    internal void LoadAudio()
    {
        foreach (var child in ResourceSystem.Resources.Values.Where(x =>
            x.ResourceType == typeof(RiseCore.ResourceTypeWavFile) || x.ResourceType == typeof(RiseCore.ResourceTypeOggFile)))
        {
            if (child.Path.StartsWith("Content/Music"))
            {
                var path = child.Path.Replace("Content/Music/", "");

                var indexOfSlash = child.Path.IndexOf('/');
                if (indexOfSlash != -1)
                {
                    using var stream = child.Stream;
                    path = child.Root.Substring(4) + path;
                    var trackInfo = new TrackInfo(path, child.RootPath, child.ResourceType);
                    patch_Audio.TrackMap[path] = trackInfo;
                    Logger.Verbose($"[MUSIC] [{child.Root}] Added '{path}' to TrackMap.");
                }
                continue;
            }
        }

        foreach (var soundRes in ResourceSystem.Resources.Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeXMLSoundBank)))
        {
            var child = soundRes.Value;

            XmlElement soundBank = ModIO.LoadXml(child)["SoundBank"];

            foreach (XmlElement sfx in soundBank)
            {
                var id = sfx.Attr("id");
                var path = Path.Combine(Path.GetDirectoryName(child.Path), id).Replace('\\', '/');
                var obeysMasterPitch = sfx.AttrBool("obeyMasterPitch", true);

                var checkPath = Path.Combine(child.Root, path);

                if (!ModIO.IsFileExists(checkPath + ".wav"))
                {
                    if (ModIO.IsFileExists(checkPath + ".ogg"))
                    {
                        path += ".ogg";
                    }
                    else 
                    {
                        Logger.Error($"This specific id: '{id}' cannot be mapped into any audio file. (.WAV or .OGG)");
                        continue;
                    }
                }
                else 
                {
                    path += ".wav";
                }

                path = path.Replace("Content/", "");

                switch (sfx.Name)
                {
                case "SFX":
                    var createdSFX = LoadSFX(path, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFX);
                    break;
                case "SFXInstanced":
                    var instances = sfx.AttrInt("instances", 2);
                    var createdSFXInstanced = LoadSFXInstance(path, instances, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXInstanced);
                    break;
                case "SFXVaried":
                    var amount = sfx.AttrInt("count");
                    var createdSFXVaried = LoadSFXVaried(path, amount, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXVaried);
                    break;
                case "SFXLooped":
                    var createdSFXLooped = LoadSFXLooped(path, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXLooped);
                    break;
                }
            }
        }

        foreach (var soundRes in ResourceSystem.Resources.Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeJSONSoundBank)))
        {
            var child = soundRes.Value;

            using var childStream = child.Stream;

            var dict = JsonSerializer.Deserialize<Dictionary<string, SFXSoundBank>>(childStream);

            foreach (var pair in dict)
            {
                var id = pair.Key;
                var path = Path.Combine(Path.GetDirectoryName(child.Path), id).Replace('\\', '/');
                var obeysMasterPitch = pair.Value.ObeysMasterPitch;

                var checkPath = Path.Combine(child.Root, path);

                if (!ModIO.IsFileExists(checkPath + ".wav"))
                {
                    if (ModIO.IsFileExists(checkPath + ".ogg"))
                    {
                        path += ".ogg";
                    }
                    else 
                    {
                        Logger.Error($"This specific id: '{id}' cannot be mapped into any audio file. (.WAV or .OGG)");
                        continue;
                    }
                }
                else 
                {
                    path += ".wav";
                }

                path = path.Replace("Content/", "");
                var type = pair.Value.Type.ToLowerInvariant();

                switch (pair.Value.Type)
                {
                case "sfx":
                    var createdSFX = LoadSFX(path, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFX);
                    break;
                case "instanced":
                    var instances = pair.Value.Instances;
                    var createdSFXInstanced = LoadSFXInstance(path, instances, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXInstanced);
                    break;
                case "varied":
                    var amount = pair.Value.Count;
                    var createdSFXVaried = LoadSFXVaried(path, amount, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXVaried);
                    break;
                case "looped":
                    var createdSFXLooped = LoadSFXLooped(path, obeysMasterPitch);
                    patch_Sounds.AddSFX(this, id, createdSFXLooped);
                    break;
                }
            }
        }
    }

    private struct PackerResource(RiseCore.Resource resource, CPUImage image)
    {
        public RiseCore.Resource Resource = resource;
        public CPUImage Image = image;
    }
    private static TexturePacker<PackerResource> texturePacker;

    internal void LoadResources() 
    {
        // Loads all atlas that has been crawled, this should only look for .png and .xml
        foreach (var atlasRes in ResourceSystem.Resources.Where(x =>
            x.Value.ResourceType == typeof(RiseCore.ResourceTypeAtlas)))
        {
            var child = atlasRes.Value;
            var png = child.RootPath;
            if (Path.GetExtension(png) != ".png") 
            {
                continue;
            }

            string data = null;
            foreach (var ext in AtlasReader.InternalReaders.Keys)
            {
                data = png.Replace(".png", ext);
                if (RiseCore.ResourceTree.TreeMap.ContainsKey(data))
                {
                    break;
                }
            }

            int indexOfSlash = png.IndexOf('/');

            png = png.Substring(indexOfSlash + 1).Replace("Content/", "");
            data = data.Substring(indexOfSlash + 1).Replace("Content/", "");

            var atlas = LoadAtlas(data, png);

            if (ResourceSystem.Metadata.IsDirectory)
            {
                // might be bad, but all we care about is the Texture2D to change data anyway.
                watchingTexture[child.FullPath] = new WatchTexture(child.ResourceType, new Subtexture(new Texture(atlas.Texture2D)));
            }

            string filename = Path.GetFileNameWithoutExtension(png);

            if (filename == "menuAtlas") 
            {
                patch_Atlas.MergeAtlas(child, atlas, TFGame.MenuAtlas, child.Root.Substring(4));
            }
            else if (filename == "bossAtlas") 
            {
                patch_Atlas.MergeAtlas(child, atlas, TFGame.BossAtlas, child.Root.Substring(4));
            }
            else if (filename == "bgAtlas") 
            {
                patch_Atlas.MergeAtlas(child, atlas, TFGame.BGAtlas, child.Root.Substring(4));
            }
            else 
            {
                patch_Atlas.MergeAtlas(child, atlas, TFGame.Atlas, child.Root.Substring(4));
            }
        }

        if (ResourceSystem.Metadata.IsZipped) 
        {
            Dictionary<Type, List<RiseCore.Resource>> resources = new Dictionary<Type, List<RiseCore.Resource>>() 
            {
                {typeof(RiseCore.ResourceTypeAtlasPng), new List<RiseCore.Resource>()},
                {typeof(RiseCore.ResourceTypeBGAtlasPng), new List<RiseCore.Resource>()},
                {typeof(RiseCore.ResourceTypeBossAtlasPng), new List<RiseCore.Resource>()},
                {typeof(RiseCore.ResourceTypeMenuAtlasPng), new List<RiseCore.Resource>()},
            };

            foreach (var atlasPng in ResourceSystem.Resources
                .Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeAtlasPng) || 
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeBGAtlasPng) ||
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeBossAtlasPng) ||
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeMenuAtlasPng))) 
            {
                var child = atlasPng.Value;

                if (resources.TryGetValue(child.ResourceType, out var list)) 
                {
                    list.Add(child);
                }
            }

            texturePacker = new TexturePacker<PackerResource>();
            foreach (var pair in resources) 
            {
                if (pair.Value.Count == 0) 
                {
                    continue;
                }
                foreach (var child in pair.Value) 
                {
                    var png = child.RootPath;

                    using var stream = child.Stream;

                    var image = new CPUImage(stream);
                    texturePacker.Add(new TexturePacker<PackerResource>.Item(new PackerResource(child, image), image.Width, image.Height));
                }

                if (texturePacker.Pack(out var items, out var size)) 
                {
                    using CPUImage image = new CPUImage(size.X, size.Y);
                    foreach (var item in items) 
                    {
                        image.CopyFrom(item.Data.Image, item.Rect.X, item.Rect.Y);
                        item.Data.Image.Dispose();
                    }
                    var texture = image.UploadAsTexture(Engine.Instance.GraphicsDevice);

                    foreach (var item in items) 
                    {
                        var child = item.Data.Resource;
                        var subtexture = new Subtexture(new Monocle.Texture(texture), item.Rect);

                        if (child.ResourceType == typeof(RiseCore.ResourceTypeAtlasPng)) 
                        {
                            patch_Atlas.MergeTexture(child, subtexture, TFGame.Atlas, child.Root.Substring(4));
                        }
                        else if (child.ResourceType == typeof(RiseCore.ResourceTypeMenuAtlasPng)) 
                        {
                            patch_Atlas.MergeTexture(child, subtexture, TFGame.MenuAtlas, child.Root.Substring(4));
                        }
                        else if (child.ResourceType == typeof(RiseCore.ResourceTypeBGAtlasPng)) 
                        {
                            patch_Atlas.MergeTexture(child, subtexture, TFGame.BGAtlas, child.Root.Substring(4));
                        }
                        else if (child.ResourceType == typeof(RiseCore.ResourceTypeBossAtlasPng)) 
                        {
                            patch_Atlas.MergeTexture(child, subtexture, TFGame.BossAtlas, child.Root.Substring(4));
                        }
                    }
                }
            }
        }
        else 
        {
            foreach (var atlasPng in ResourceSystem.Resources
                .Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeAtlasPng) || 
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeBGAtlasPng) ||
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeBossAtlasPng) ||
                            x.Value.ResourceType == typeof(RiseCore.ResourceTypeMenuAtlasPng))) 
            {
                var child = atlasPng.Value;
                using var stream = child.Stream;
                var texture = XNAGraphics.Texture2D.FromStream(TFGame.Instance.GraphicsDevice, stream);
                var subtexture = new Subtexture(new Monocle.Texture(texture));

                watchingTexture[child.FullPath] = new WatchTexture(child.ResourceType, subtexture);

                if (child.ResourceType == typeof(RiseCore.ResourceTypeAtlasPng)) 
                {
                    patch_Atlas.MergeTexture(child, subtexture, TFGame.Atlas, child.Root.Substring(4));
                }
                else if (child.ResourceType == typeof(RiseCore.ResourceTypeMenuAtlasPng)) 
                {
                    patch_Atlas.MergeTexture(child, subtexture, TFGame.MenuAtlas, child.Root.Substring(4));
                }
                else if (child.ResourceType == typeof(RiseCore.ResourceTypeBGAtlasPng)) 
                {
                    patch_Atlas.MergeTexture(child, subtexture, TFGame.BGAtlas, child.Root.Substring(4));
                }
                else if (child.ResourceType == typeof(RiseCore.ResourceTypeBossAtlasPng)) 
                {
                    patch_Atlas.MergeTexture(child, subtexture, TFGame.BossAtlas, child.Root.Substring(4));
                }
            }
        }

        foreach (var spriteDataRes in ResourceSystem.Resources
            .Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeSpriteData)))
        {
            var child = spriteDataRes.Value;
            var spriteData = child.RootPath;
            if (Path.GetExtension(spriteData) != ".xml")
            {
                continue;
            }

            int indexOfSlash = spriteData.IndexOf('/');

            spriteData = spriteData.Substring(indexOfSlash + 1).Replace("Content/", "");

            // Will just try to load the spriteData, some spriteData might not have a neeeded attribute.
            TryLoadSpriteData(spriteData, out var result);
        }

        foreach (var gameDataRes in ResourceSystem.Resources
            .Where(x => x.Value.ResourceType == typeof(RiseCore.ResourceTypeGameData)))
        {
            var child = gameDataRes.Value;
            var gameData = child.RootPath;
            if (Path.GetExtension(gameData) != ".xml") 
            {
                continue;
            }

            var filename = Path.GetFileName(gameData);

            switch (filename)
            {
            case "themeData.xml":
            {
                var xmlThemes = ModIO.LoadXml(child)["ThemeData"];
                foreach (XmlElement xmlTheme in xmlThemes)
                {
                    var atlas = xmlTheme.Attr("atlas", "Atlas/atlas");
                    var themeID = xmlTheme.Attr("id", xmlTheme.Name);
                    ExtendedGameData.Defer(() =>
                    {
                        var towerTheme = new patch_TowerTheme(xmlTheme, child);
                        TowerFall.GameData.Themes[child.Root.Substring(4) + themeID] = towerTheme;
                        Logger.Verbose("[TOWER THEME] Loaded: " + child.Root.Substring(4) + themeID);
                    }, 0);
                }
            }
                break;
            case "bgData.xml":
            {
                var xmlBGs = ModIO.LoadXml(child)["backgrounds"];
                foreach (XmlElement bgs in xmlBGs.GetElementsByTagName("BG"))
                {
                    var bgID = bgs.Attr("id");
                    ExtendedGameData.Defer(() =>
                    {
                        TowerFall.GameData.BGs[child.Root.Substring(4) + bgID] = bgs;
                    }, 1);
                }

                Logger.Verbose("[BG] Loaded: " + child.Root.Substring(4) + child.Path);
            }
                break;
            case "tilesetData.xml":
            {
                var xmlTilesets = ModIO.LoadXml(child)["TilesetData"];
                foreach (XmlElement tilesets in xmlTilesets.GetElementsByTagName("Tileset"))
                {
                    ExtendedGameData.Defer(() =>
                    {
                        var tilesetID = tilesets.Attr("id", tilesets.Name);
                        TowerFall.GameData.Tilesets[child.Root.Substring(4) + tilesetID] = new patch_TilesetData(tilesets);
                    }, 1);
                }

                Logger.Verbose("[Tileset] Loaded: " + child.Root.Substring(4) + child.Path);
            }
                break;
            case "mapData.xml":
            {
                var xmlMap = ModIO.LoadXml(child)["map"];
                var map = new MapRendererNode(xmlMap, child);
                ExtendedGameData.Defer(() => ExtendedGameData.InternalMapRenderers.Add(child.Root.Substring(4).Replace("/", ""), map), 1);
                Logger.Verbose("[MapData] Loaded: " + child.Root.Substring(4) + child.Path);
            }
                break;
            default:
                DataLoader?.Invoke(child, gameData);
                break;
            }
        }
    }

    public bool IsResourceExist(string path)
    {
        path = path.Replace('\\', '/');
        if (MapResource.ContainsKey(path))
            return true;
        return false;
    }

    public bool TryGetValue(string path, out RiseCore.Resource value)
    {
        path = path.Replace('\\', '/');
        return MapResource.TryGetValue(path, out value);
    }

    public RiseCore.Resource GetValue(string path)
    {
        path = path.Replace('\\', '/');
        return MapResource[path];
    }

    public IEnumerable<string> EnumerateFilesString(string path)
    {
        path = path.Replace('\\', '/');
        if (TryGetValue(path, out var folder))
        {
            for (int i = 0; i < folder.Childrens.Count; i++)
            {
                yield return folder.Childrens[i].Path;
            }
        }
    }

    public IEnumerable<RiseCore.Resource> EnumerateFiles(string path)
    {
        path = path.Replace('\\', '/');
        if (TryGetValue(path, out var folder))
        {
            return folder.Childrens;
        }
        return Enumerable.Empty<RiseCore.Resource>();
    }

    public IEnumerable<RiseCore.Resource> EnumerateFiles(string path, string filterExt)
    {
        path = path.Replace('\\', '/');
        if (TryGetValue(path, out var folder))
        {
            // FIXME Need improvements, this is not what I want!!!
            filterExt = filterExt.Replace("*", "");
            foreach (var child in folder.Childrens)
            {
                if (!child.Path.Contains(filterExt))
                    continue;
                yield return child;
            }
        }
    }

    public string[] GetResourcesPath(string path)
    {
        path = path.Replace('\\', '/');
        if (TryGetValue(path, out var folder))
        {
            string[] childrens = new string[folder.Childrens.Count];
            for (int i = 0; i < folder.Childrens.Count; i++)
            {
                childrens[i] = folder.Childrens[i].Path;
            }
            return childrens;
        }
        return Array.Empty<string>();
    }

    public T LoadShader<T>(string path, string passName, out int id)
    where T : ShaderResource, new()
    {
        path = path.Replace('\\', '/');
        var shaderPath = contentPath + "/" + path;
        return ShaderManager.AddShader<T>(this[shaderPath], passName, out id);
    }

    public TrackInfo LoadMusic(string path)
    {
        path = path.Replace('\\', '/');
        var musicPath = contentPath + "/" + path;
        var musicResource = this[musicPath];
        using var musicStream = musicResource.Stream;
        var resourceType = musicResource.ResourceType;
        var trackInfo = new TrackInfo(path, musicPath, resourceType);
        return trackInfo;
    }

    public RiseCore.Resource[] GetResources(string path)
    {
        path = path.Replace('\\', '/');
        if (TryGetValue(path, out var folder))
        {
            return folder.Childrens.ToArray();
        }
        return Array.Empty<RiseCore.Resource>();
    }

    /// <summary>
    /// Load Atlas from a mod Content path. This will use the <c>ContentPath</c> property, it's `Content/` by default.
    /// </summary>
    /// <param name="dataPath">A path to where the xml or json path is.</param>
    /// <param name="imagePath">A path to where the png path is.</param>
    /// <returns>An atlas of an image.</returns>
    public patch_Atlas LoadAtlas(string dataPath, string imagePath)
    {
        var ext = Path.GetExtension(dataPath);
        var atlasID = dataPath + imagePath;
        if (dataPath.Replace(ext, "") == imagePath.Replace(".png", ""))
        {
            atlasID = dataPath.Replace(ext, "");
        }
        if (atlases.TryGetValue(atlasID, out var atlasExisted))
            return atlasExisted;

        using var data = this[contentPath + "/" + dataPath].Stream;
        using var image = this[contentPath + "/" + imagePath].Stream;
        var atlas = AtlasExt.CreateAtlas(data, image, ext);
        atlases.Add(atlasID, atlas);
        Logger.Verbose("[ATLAS] Loaded: " + atlasID);
        return atlas;
    }

    /// <summary>
    /// Load Atlas from a mod Content path without extension. This will use the <c>ContentPath</c> property, it's `Content/` by default.
    /// </summary>
    /// <param name="path">A path to where an atlas data name and atlas png name can be match together</param>
    /// <returns>An atlas of an image.</returns>
    public patch_Atlas LoadAtlas(string path)
    {
        path = path.Replace('\\', '/');
        var ext = Path.GetExtension(path);
        var atlasID = path;
        if (!string.IsNullOrEmpty(ext))
        {
            var idx = path.IndexOf(".");
            path = path.Substring(0, idx);
        }

        if (atlases.TryGetValue(path, out var atlasExisted))
            return atlasExisted;

        RiseCore.Resource dataRes;
        if (this.MapResource.TryGetValue(contentPath + "/" + path + ".xml", out var res))
        {
            dataRes = res;
        }
        else if (this.MapResource.TryGetValue(contentPath + "/" + path + ".json", out var res2))
        {
            dataRes = res2;
        }
        else
        {
            Logger.Error($"[ATLAS] No data xml or json found in this path {path}");
            return null;
        }

        using var data = dataRes.Stream;
        using var image = this[contentPath + "/" + path + ".png"].Stream;
        var atlas = AtlasExt.CreateAtlas(data, image, ext);
        atlases.Add(atlasID, atlas);
        Logger.Verbose("[ATLAS] Loaded: " + atlasID);
        return atlas;
    }

    /// <summary>
    /// Load SpriteData from a mod Content path.
    /// This will use the <c>ContentPath</c> property, it's <c>Content</c> by default.
    /// </summary>
    /// <param name="path">A path to the SpriteData xml.</param>
    /// <param name="atlas">An atlas which the spriteData will use.</param>
    /// <returns>A SpriteData instance to be use for sprite</returns>
    public patch_SpriteData LoadSpriteData(string path, patch_Atlas atlas)
    {
        path = path.Replace('\\', '/');
        if (spriteDatas.TryGetValue(path, out var spriteDataExist))
            return spriteDataExist;

        using var xml = this[contentPath + "/" + path].Stream;
        var spriteData = SpriteDataExt.CreateSpriteData(xml, atlas);
        spriteDatas.Add(path, spriteData);
        return spriteData;
    }

    /// <summary>
    /// Try to load SpriteData from a mod Content path. If it succeed, it returns true if it succeed, else false.
    /// The SpriteData must have an attribute called "atlas" referencing the atlas path that exists to use to succeed.
    /// This will use the <c>ContentPath</c> property, it's <c>Content</c> by default.
    /// </summary>
    /// <param name="path">A path to the SpriteData xml.</param>
    /// <param name="result">A SpriteData instance to be use for sprite if it succeed, else null</param>
    /// <returns>true if it succeed, else false</returns>
    public bool TryLoadSpriteData(string path, out patch_SpriteData result)
    {
        path = path.Replace('\\', '/');
        var id = path.Replace(".xml", "");
        if (spriteDatas.TryGetValue(id, out var spriteDataExist))
        {
            result = spriteDataExist;
            return true;
        }

        var filename = Path.GetFileName(id);

        Atlas atlas = filename switch {
            "spriteData" => TFGame.Atlas,
            "menuSpriteData" => TFGame.MenuAtlas,
            "bossSpriteData" => TFGame.BossAtlas,
            "bgSpriteData" => TFGame.BGAtlas,
            _ => null
        };

        using var xml = this[contentPath + "/" + path].Stream;
        var data = SpriteDataExt.CreateSpriteDataFromAtlas(this, xml, (patch_Atlas)atlas);
        
        var spriteData = data;
        spriteDatas.Add(id, spriteData);
        result = spriteData;
        return true;
    }

    /// <summary>
    /// Load a stream from a mod Content path.
    /// This will use the <c>ContentPath</c> property, it's <c>Content</c> by default.
    /// </summary>
    /// <param name="path">A path to file.</param>
    /// <returns>A FileStream or ZipStream.</returns>
    public Stream LoadStream(string path)
    {
        path = path.Replace('\\', '/');
        return this[contentPath + "/" + path].Stream;
    }

    /// <summary>
    /// Load a raw Texture2D from an image from a mod Content path.
    /// This will use the <c>ContentPath</c> property, it's <c>Content</c> by default.
    /// </summary>
    /// <returns>A raw Texture2D that can be use for low-level texture access.</returns>
    public XNAGraphics::Texture2D LoadRawTexture2D(string path)
    {
        path = path.Replace('\\', '/');
        using var stream = LoadStream(path);
        var tex2D = XNAGraphics::Texture2D.FromStream(Engine.Instance.GraphicsDevice, stream);
        return tex2D;
    }

    /// <summary>
    /// Load a Texture from an image from a mod Content path.
    /// This will use the <c>ContentPath</c> property, it's <c>Content</c> by default.
    /// </summary>
    /// <returns>A Monocle Texture</returns>
    public Texture LoadTexture(string path)
    {
        path = path.Replace('\\', '/');
        var tex2D = LoadRawTexture2D(path);
        var tex = new Texture(tex2D);
        return tex;
    }

    /// <summary>
    /// Load text inside of a file
    /// </summary>
    /// <param name="path">A path to a file</param>
    /// <returns>A text inside of a file</returns>
    public string LoadText(string path)
    {
        path = path.Replace('\\', '/');
        using var stream = this[contentPath + "/" + path].Stream;
        using TextReader sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Load <see cref="System.Xml.XmlDocument"/> from a file.
    /// </summary>
    /// <param name="path">A path to a file</param>
    /// <returns>A <see cref="System.Xml.XmlDocument"/></returns>
    public XmlDocument LoadXML(string path)
    {
        path = path.Replace('\\', '/');
        using var stream = this[contentPath + "/" + path].Stream;
        return patch_Calc.LoadXML(stream);
    }

    public SFX LoadSFX(string path, bool obeysMasterPitch = true)
    {
        path = path.Replace('\\', '/');
        if (sfxes.TryGetValue(path, out var val))
            return val;
        using var stream = this[contentPath + "/" + path].Stream;
        return SFXExt.CreateSFX(this, stream, obeysMasterPitch);
    }

    public patch_SFXInstanced LoadSFXInstance(string path, int instances = 2, bool obeysMasterPitch = true)
    {
        path = path.Replace('\\', '/');
        if (sfxes.TryGetValue(path, out var val))
            return (patch_SFXInstanced)val;
        using var stream = this[contentPath + "/" + path].Stream;
        return SFXInstancedExt.CreateSFXInstanced(this, stream, instances, obeysMasterPitch);
    }

    public patch_SFXLooped LoadSFXLooped(string path, bool obeysMasterPitch = true)
    {
        path = path.Replace('\\', '/');
        if (sfxes.TryGetValue(path, out var val))
            return (patch_SFXLooped)val;
        using var stream = this[contentPath + "/" + path].Stream;
        return SFXLoopedExt.CreateSFXLooped(this, stream, obeysMasterPitch);
    }

    public patch_SFXVaried LoadSFXVaried(string path, int amount, bool obeysMasterPitch = true)
    {
        path = path.Replace('\\', '/');
        if (sfxes.TryGetValue(path, out var val))
            return (patch_SFXVaried)val;
        var currentExtension = Path.GetExtension(".wav");
        path = path.Replace(currentExtension, "");
        var stream = new Stream[amount];
        for (int i = 0; i < amount; i++)
        {
            stream[i] = this[contentPath + "/" + path + SFXVariedExt.GetSuffix(i + 1) + currentExtension].Stream;
        }
        return SFXVariedExt.CreateSFXVaried(this, stream, amount, obeysMasterPitch);
    }

    public void Dispose(bool disposeTexture)
    {
        ResourceSystem.Dispose();
        if (disposeTexture)
        {
            foreach (var atlas in atlases)
            {
                atlas.Value.Texture2D.Dispose();
            }
            atlases.Clear();
        }
    }

    private struct WatchTexture(Type type, Subtexture texture)
    {
        public Type Type = type;
        public Subtexture Texture = texture;
    }
}

public enum ContentAccess
{
    Root,
    Content,
    ModContent
}
