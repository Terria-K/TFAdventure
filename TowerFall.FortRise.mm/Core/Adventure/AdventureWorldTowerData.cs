using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod;
using TeuJson;
using TowerFall;

namespace FortRise.Adventure;

public class AdventureWorldTowerData : DarkWorldTowerData 
{
    public RiseCore.ModResource System;
    public string StoredDirectory;
    public string Author;
    public bool Procedural;
    public int StartingLives = -1;
    public int[] MaxContinues = new int[3] { -1, -1, -1 };
    public string RequiredMods;
    public AdventureWorldTowerStats Stats;

    public AdventureWorldTowerData(RiseCore.ModResource system, string path) 
    {
        System = system;
        System.Lookup();
    }

    public AdventureWorldTowerData(RiseCore.ModResource system) 
    {
        System = system;
    }

    private bool Lookup(string directory) 
    {
        bool customIcon = false;
        foreach (RiseCore.Resource resource in System.Resources.Values) 
        {
            var path = resource.Path;

            if (path.Contains("icon")) 
            {
                customIcon = true;
                continue;
            }
            if (path.EndsWith(".json") || path.EndsWith(".oel"))
                Levels.Add(path);
        }
        return customIcon;
    }

    private bool ModLookup(string directory) 
    {
        bool customIcon = false;
        foreach (RiseCore.Resource resource in System.Resources[directory].Childrens) 
        {
            var path = resource.Path;

            if (path.Contains("icon")) 
            {
                customIcon = true;
                continue;
            }
            if (path.EndsWith(".json") || path.EndsWith(".oel"))
                Levels.Add(path);
        }
        return customIcon;
    }

    private void BuildIcon(string path) 
    {
        var json = JsonConvert.DeserializeFromFile(path);
        var layers = json["layers"].AsJsonArray;
        var solids = layers[0];
        var grid2D = solids["grid2D"].ConvertToArrayString2D();
        var bitString = Ogmo3ToOel.Array2DToStraightBitString(grid2D);
        var x = grid2D.GetLength(1);
        var y = grid2D.GetLength(0);
        if (x != 16 || y != 16) 
        {
            Logger.Error($"[Adventure] {path}: Invalid icon size, it must be 16x16 dimension or 160x160 in level dimension");
            return;
        }
        Theme.Icon = new Subtexture(new Monocle.Texture(TowerMapData.BuildIcon(bitString, Theme.TowerType)));
    }

    internal bool AdventureLoad(int id, string levelDirectory) 
    {
        Levels = new List<string>();
        var customIcon = Lookup(levelDirectory);
        return InternalAdventureLoad(id, levelDirectory, string.Empty, customIcon);
    }

    internal bool ModAdventureLoad(int id, string levelDirectory, string levelPrefix) 
    {
        Levels = new List<string>();
        var customIcon = ModLookup(levelPrefix.Remove(levelPrefix.Length - 1));
        return InternalAdventureLoad(id, levelDirectory, levelPrefix, customIcon);
    }

    private void LoadExtraData(XmlElement xmlElement) 
    {
        if (xmlElement.HasChild("lives")) 
        {
            StartingLives = int.Parse(xmlElement["lives"].InnerText);
        }
        if (xmlElement.HasChild("procedural"))
            Procedural = bool.Parse(xmlElement["procedural"].InnerText);
        if (xmlElement.HasChild("continues")) 
        {
            var continues = xmlElement["continues"];
            if (continues.HasChild("normal"))
                MaxContinues[0] = int.Parse(continues["normal"].InnerText);
            if (continues.HasChild("hardcore"))
                MaxContinues[1] = int.Parse(continues["hardcore"].InnerText);
            if (continues.HasChild("legendary"))
                MaxContinues[2] = int.Parse(continues["legendary"].InnerText);
        }
    }

    internal bool InternalAdventureLoad(int id, string levelDirectory, string directoryPrefix, bool customIcons) 
    {
        if (this.Levels.Count <= 0) 
        {
            Logger.Error($"[Adventure] {levelDirectory} failed to load as there is no levels found.");
            return false;
        }

        IAdventureTowerLoader towerLoader = null;
        if (System.Resources.ContainsKey(directoryPrefix + "tower.xml")) 
        {
            towerLoader = new XmlAdventureTowerLoader(System, this);
        }
        else if (System.Resources.ContainsKey(directoryPrefix + "tower.lua")) 
        {
            towerLoader = new LuaAdventureLoader();
        }
        else 
        {
            return false;
        }

        using var fs = System.Resources[directoryPrefix + "tower." + towerLoader.FileExtension].Stream;
        var info = towerLoader.Load(id, fs, levelDirectory, directoryPrefix, customIcons);
        var guid = (info.Theme as patch_TowerTheme).GenerateThemeID();

        StoredDirectory = info.StoredDirectory;
        ID.X = info.ID;
        Theme = info.Theme;
        Author = info.Author;
        Stats = info.Stats;
        StartingLives = info.Extras.StartingLives;        
        MaxContinues[0] = info.Extras.NormalContinues; 
        MaxContinues[1] = info.Extras.HardcoreContinues;
        MaxContinues[2] = info.Extras.LegendaryContinues;
        Procedural = info.Extras.Procedural;
        RequiredMods = info.RequiredMods;

        TimeBase = info.TimeBase;
        TimeAdd = info.TimeAdd;
        EnemySets = info.EnemySets;
        Normal = info.Normal;
        Hardcore = info.Hardcore;
        Legendary = info.Legendary;

        var pathToIcon = Path.Combine(levelDirectory, "icon.json");
        if (!string.IsNullOrEmpty(pathToIcon) && customIcons)
            BuildIcon(pathToIcon);

        LoadCustomElements(info, guid, directoryPrefix);

        return true;
    }

    private void LoadCustomElements(AdventureTowerInfo info, Guid guid, string prefix) 
    {
        var fgTileset = info.Theme.Tileset.AsSpan();
        var bgTileset = info.Theme.BGTileset.AsSpan();
        var background = info.Theme.BackgroundID.AsSpan();

        if (fgTileset.StartsWith("custom:".AsSpan())) 
        {
            var sliced = fgTileset.Slice(7).ToString();
            var id = Path.Combine(StoredDirectory, sliced);
            var resource = System.Resources[prefix + sliced];
            using var path = resource.Stream;
            var loadedXML = patch_Calc.LoadXML(path)["Tileset"];
            using var tilesetPath = System.Resources[loadedXML.Attr("image")].Stream;
            patch_GameData.CustomTilesets.Add(id, patch_TilesetData.Create(loadedXML, tilesetPath));
            Theme.Tileset = id;
        }
        if (bgTileset.StartsWith("custom:".AsSpan())) 
        {
            var sliced = bgTileset.Slice(7).ToString();
            var id = Path.Combine(StoredDirectory, sliced);
            var resource = System.Resources[prefix + sliced];
            using var path = resource.Stream;
            var loadedXML = patch_Calc.LoadXML(path)["Tileset"];
            using var tilesetPath = System.Resources[prefix + loadedXML.Attr("image")].Stream;
            patch_GameData.CustomTilesets.Add(id, patch_TilesetData.Create(loadedXML, tilesetPath));
            Theme.BGTileset = id;
        }
        if (background.StartsWith("custom:".AsSpan())) 
        {
            var sliced = background.Slice(7).ToString();
            Theme.BackgroundID = sliced;
            LoadBG(sliced);
        }

        void LoadBG(string background) 
        {
            var path = System.Resources[prefix + background].Stream;
            var loadedXML = patch_Calc.LoadXML(path)["BG"];

            // Old API
            if (loadedXML.HasChild("ImagePath")) 
            {
                var oldAPIPath = loadedXML.InnerText;
                Logger.Warning("[Background] Use of deprecated APIs should no longer be used");

                if (!string.IsNullOrEmpty(oldAPIPath)) 
                {
                    using var fs = System.Resources[prefix + oldAPIPath].Stream;
                    var texture2D = Texture2D.FromStream(Engine.Instance.GraphicsDevice, fs);
                    var old_api_atlas = new patch_Atlas();
                    old_api_atlas.SetSubTextures(new Dictionary<string, Subtexture>() {{oldAPIPath, new Subtexture(new Monocle.Texture(texture2D)) }});
                    patch_GameData.CustomBGAtlas.Add(guid, new CustomBGStorage(old_api_atlas, null));
                }
                return;
            }

            // New API

            var customBGAtlas = loadedXML.Attr("atlas", null);
            var customSpriteDataAtlas = loadedXML.Attr("spriteData", null);
            
            patch_Atlas atlas = null;
            patch_SpriteData spriteData = null;
            if (customBGAtlas != null) 
            {
                var xml = System.Resources[prefix + customBGAtlas + ".xml"].Stream;
                var png = System.Resources[prefix + customBGAtlas + ".png"].Stream;
                atlas = AtlasExt.CreateAtlas(null, xml, png);
            }

            if (customSpriteDataAtlas != null) 
            {
                using var spriteTexture = System.Resources[prefix + customSpriteDataAtlas + ".xml"].Stream;
                spriteData = SpriteDataExt.CreateSpriteData(null, spriteTexture, atlas);
            }

            var storage = new CustomBGStorage(atlas, spriteData);
            patch_GameData.CustomBGAtlas.Add(guid, storage);
            
            Theme.ForegroundData = loadedXML["Foreground"];
            Theme.BackgroundData = loadedXML["Background"];
        }
    }

    [MonoModIgnore]
    private extern List<DarkWorldTowerData.LevelData> LoadLevelSet(XmlElement xml);
}