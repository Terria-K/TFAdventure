using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using TeuJson;

namespace TowerFall;

public class patch_TowerTheme : TowerTheme 
{
    [MonoModConstructor]
    public void ctor(JsonValue value) 
    {
        Name = value.GetJsonValueOrNull("Name") ?? "";
        Icon = TFGame.MenuAtlas["towerIcons/" + value.GetJsonValueOrNull("Icon") ?? "sacredGround"];
        if (Enum.TryParse<MapButton.TowerType>(value.GetJsonValueOrNull("TowerType") ?? "Normal" , out var result)) 
        {
            TowerType = result;
        }
        var jsonPosition = value.GetJsonValueOrNull("MapPosition");
        MapPosition = jsonPosition == null ? Vector2.Zero : jsonPosition.Position();
        Music = value.GetJsonValueOrNull("Music") ?? "SacredGround";
        DarknessColor = Calc.HexToColor(value.GetJsonValueOrNull("DarknessColor") ?? "000000");
        DarknessOpacity = value.GetJsonValueOrNull("DarknessOpacity") ?? 0f;
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
        BackgroundID = value["Background"];
        BackgroundData = GameData.BGs[BackgroundID]["Background"];
        ForegroundData = GameData.BGs[BackgroundID]["Foreground"];
        if (value.Contains("PlayerInvisibility")) 
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
}