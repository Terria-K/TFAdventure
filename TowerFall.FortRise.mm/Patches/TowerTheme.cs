using System;
using System.Xml;
using FortRise;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using NLua;

namespace TowerFall;

public class patch_TowerTheme : TowerTheme 
{
    public RiseCore.Resource Mod;
    public patch_TowerTheme(XmlElement xml) {}
    public patch_TowerTheme(XmlElement xml, RiseCore.Resource mod, ThemeResource resource) {}
    public patch_TowerTheme(Hjson.JsonValue value, RiseCore.Resource mod, ThemeResource resource) {}
    public patch_TowerTheme(LuaTable value) {}


    [MonoModConstructor]
    public void ctor(XmlElement xml, RiseCore.Resource mod, ThemeResource resource) 
    {
        Mod = mod;
        var atlas = resource.Atlas != null ? resource.Atlas : TFGame.MenuAtlas;
        Name = xml.ChildText("Name").ToUpperInvariant();

        var icon = xml.ChildText("Icon", "sacredGround");
        if (atlas.Contains(icon)) 
            Icon = atlas[icon];
        else 
            Icon = TFGame.MenuAtlas["towerIcons/" + icon];

        TowerType = xml.ChildEnum<MapButton.TowerType>("TowerType", MapButton.TowerType.Normal);
        MapPosition = xml["MapPosition"].Position();
        Music = xml.ChildText("Music", "SacredGround");
        DarknessColor = xml.ChildHexColor("DarknessColor", Color.Black).Invert();
        DarknessOpacity = xml.ChildFloat("DarknessOpacity", 0.2f);
        Wind = xml.ChildInt("Wind", 0);
        Lanterns = xml.ChildEnum<TowerTheme.LanternTypes>("Lanterns", LanternTypes.CathedralTorch);
        World = xml.ChildEnum("World", TowerTheme.Worlds.Normal);
        Raining = xml.ChildBool("Raining", false);
        BackgroundID = xml.ChildText("Background", "SacredGround");
        if (GameData.BGs.ContainsKey(BackgroundID)) 
        {
            BackgroundData = GameData.BGs[this.BackgroundID]["Background"];
            ForegroundData = GameData.BGs[this.BackgroundID]["Foreground"];
        }

        DrillParticleColor = xml.ChildHexColor("DrillParticleColor", Color.Red);
        Cold = xml.ChildBool("Cold", false);
        CrackedBlockColor = xml.ChildHexColor("CrackedBlockColor", "4EB1E9");
        Tileset = xml.ChildText("Tileset", "SacredGround");
        BGTileset = xml.ChildText("BGTileset", "SacredGroundBG");
        Cataclysm = (xml.ChildText("Tileset") == "Cataclysm");

        if (xml.HasChild("PlayerInvisibility"))
        {
            this.InvisibleOpacities = new float[]
            {
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Green", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Blue", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Pink", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Orange", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("White", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Yellow", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Cyan", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Purple", 0) * 0.1f,
                0.2f + (float)xml["PlayerInvisibility"].ChildInt("Red", 0) * 0.1f
            };
            return;
        }

        InvisibleOpacities = new float[]
        {
            0.2f,
            0.2f,
            0.2f,
            0.2f,
            0.2f,
            0.2f,
            0.2f,
            0.2f,
            0.2f
        };
    }

    [MonoModConstructor]
    public void ctor(Hjson.JsonValue value, RiseCore.Resource mod, ThemeResource resource) 
    {
        Mod = mod;
        var atlas = resource.Atlas != null ? resource.Atlas : TFGame.MenuAtlas;
        Name = value.GetJsonValueOrNull("Name") ?? string.Empty;
        Name = Name.ToUpperInvariant();

        var icon = value.GetJsonValueOrNull("Icon") ?? "sacredGround";
        if (atlas.Contains(icon)) 
            Icon = atlas[icon];
        else 
            Icon = TFGame.MenuAtlas["towerIcons/" + icon];

        Icon = TFGame.MenuAtlas["towerIcons/" + value.GetJsonValueOrNull("Icon") ?? "sacredGround"];
        if (Enum.TryParse<MapButton.TowerType>(value.GetJsonValueOrNull("TowerType") ?? "Normal" , out var result)) 
        {
            TowerType = result;
        }
        var jsonPosition = value.GetJsonValueOrNull("MapPosition");
        MapPosition = jsonPosition == null ? Vector2.Zero : jsonPosition.Position();
        Music = value.GetJsonValueOrNull("Music") ?? "SacredGround";
        DarknessColor = Calc.HexToColor(value.GetJsonValueOrNull("DarknessColor") ?? "000000").Invert();
        DarknessOpacity = value.GetJsonValueOrNull("DarknessOpacity") ?? 0.2f;
        Wind = value.GetJsonValueOrNull("Wind") ?? 0;
        if (Enum.TryParse<TowerTheme.LanternTypes>(value.GetJsonValueOrNull("Lanterns") ?? "CathedralTorch", out var lanternResult)) 
        {
            Lanterns = lanternResult;
        }
        if (Enum.TryParse<TowerTheme.Worlds>(value.GetJsonValueOrNull("World") ?? "Normal", out var worldResult)) 
        {
            World = worldResult;
        }
        Raining = value.GetJsonValueOrNull("Raining") ?? false;
        BackgroundID = value.GetJsonValueOrNull("Background") ?? "SacredGround";
        
        if (GameData.BGs.ContainsKey(BackgroundID)) 
        {
            BackgroundData = GameData.BGs[this.BackgroundID]["Background"];
            ForegroundData = GameData.BGs[this.BackgroundID]["Foreground"];
        }
        if (value.ContainsKey("PlayerInvisibility")) 
        {
            var playerInvisibility = value["PlayerInvisibility"];
            InvisibleOpacities = new float[9]
            {
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Green") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Blue") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Pink") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Orange") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("White") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Yellow") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Cyan") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Purple") ?? 0f) * 0.1f,
                0.2f + (float)(playerInvisibility.GetJsonValueOrNull("Red") ?? 0f) * 0.1f,
            };
        }
        else 
        {
            InvisibleOpacities = new float[9] 
            {
                0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f
            };
        }
        DrillParticleColor = Calc.HexToColor(value.GetJsonValueOrNull("DrillParticleColor") ?? "ff0000");
        Cold = value.GetJsonValueOrNull("Cold") ?? false;
        CrackedBlockColor = Calc.HexToColor(value.GetJsonValueOrNull("CrackedBlockColor") ?? "4EB1E9");
        Tileset = value["Tileset"];
        BGTileset = value["BGTileset"];
        Cataclysm = value["Tileset"] == "Cataclysm";
    }

    [MonoModConstructor]
    public void ctor(LuaTable value) 
    {
        Name = (value.Get("name") ?? "").ToUpperInvariant();
        Icon = TFGame.MenuAtlas["towerIcons/" + value.Get("icon") ?? "sacredGround"];
        if (Enum.TryParse<MapButton.TowerType>(value.Get("towerType") ?? "Normal" , out var result)) 
        {
            TowerType = result;
        }
        var luaPos = value.GetTable("mapPosition");
        MapPosition = luaPos == null ? Vector2.Zero : luaPos.Position();
        Music = value.Get("music") ?? "SacredGround";
        DarknessColor = Calc.HexToColor(value.Get("darknessColor") ?? "000000").Invert();
        DarknessOpacity = value.GetFloat("darknessOpacity");
        Wind = value.GetInt("wind");
        if (Enum.TryParse<TowerTheme.LanternTypes>(value.Get("lanterns") ?? "CathedralTorch", out var lanternResult)) 
        {
            Lanterns = lanternResult;
        }
        if (Enum.TryParse<TowerTheme.Worlds>(value.Get("world") ?? "Normal", out var worldResult)) 
        {
            World = worldResult;
        }
        Raining = value.GetBool("raining");
        BackgroundID = value.Get("background");
        if (GameData.BGs.ContainsKey(BackgroundID)) 
        {
            BackgroundData = GameData.BGs[this.BackgroundID]["Background"];
            ForegroundData = GameData.BGs[this.BackgroundID]["Foreground"];
        }
        if (value.Contains("playerInvisibility")) 
        {
            var playerInvisibility = value.GetTable("playerInvisibility");
            InvisibleOpacities = new float[9]
            {
                0.2f + playerInvisibility.GetFloat("green") * 0.1f,
                0.2f + playerInvisibility.GetFloat("blue") * 0.1f,
                0.2f + playerInvisibility.GetFloat("pink") * 0.1f,
                0.2f + playerInvisibility.GetFloat("orange") * 0.1f,
                0.2f + playerInvisibility.GetFloat("white") * 0.1f,
                0.2f + playerInvisibility.GetFloat("yellow") * 0.1f,
                0.2f + playerInvisibility.GetFloat("cyan") * 0.1f,
                0.2f + playerInvisibility.GetFloat("purple") * 0.1f,
                0.2f + playerInvisibility.GetFloat("red") * 0.1f,
            };
        }
        else 
        {
            InvisibleOpacities = new float[9] 
            {
                0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f
            };
        }
        DrillParticleColor = Calc.HexToColor(value.Get("drillParticleColor") ?? "ff0000");
        Cold = value.GetBool("cold");
        CrackedBlockColor = Calc.HexToColor(value.Get("crackedBlockColor") ?? "4EB1E9");
        Tileset = value.Get("tileset");
        BGTileset = value.Get("bgTileset");
        Cataclysm = value.Get("tileset") == "Cataclysm";
    }

    public Guid GenerateThemeID() 
    {
        return Guid.Empty;
    }
}

public static class TowerThemeExt 
{
    public static bool TryGetMod(this TowerTheme theme, out RiseCore.Resource mod) 
    {
        var moddedTheme = ((patch_TowerTheme)theme);
        if (moddedTheme.Mod != null) 
        {
            mod = moddedTheme.Mod;
            return true;
        }
        mod = null;
        return false;
    }
}