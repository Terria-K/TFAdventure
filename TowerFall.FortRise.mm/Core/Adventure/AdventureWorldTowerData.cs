using System.Collections.Generic;
using System.Xml;
using Monocle;
using MonoMod;
using TeuJson;
using TowerFall;

namespace FortRise.Adventure;

public class AdventureWorldTowerData : patch_DarkWorldTowerData 
{
    public string Author;
    public bool Procedural;
    public int StartingLives = -1;
    public int[] MaxContinues = new int[3] { -1, -1, -1 };
    public string RequiredMods;
    public AdventureWorldTowerStats Stats;


    public void BuildIcon(RiseCore.Resource icon) 
    {
        using var stream = icon.Stream;
        var json = JsonConvert.DeserializeFromStream(stream);
        var layers = json["layers"].AsJsonArray;
        var solids = layers[0];
        var grid2D = solids["grid2D"].ConvertToArrayString2D();
        var bitString = Ogmo3ToOel.Array2DToStraightBitString(grid2D);
        var x = grid2D.GetLength(1);
        var y = grid2D.GetLength(0);
        if (x != 16 || y != 16) 
        {
            Logger.Error($"[Adventure] {icon.FullPath}: Invalid icon size, it must be 16x16 dimension or 160x160 in level dimension");
            return;
        }
        Theme.Icon = new Subtexture(new Monocle.Texture(TowerMapData.BuildIcon(bitString, Theme.TowerType)));
    }

    public void LoadExtraData(XmlElement xmlElement) 
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

    [MonoModIgnore]
    private extern List<DarkWorldTowerData.LevelData> LoadLevelSet(XmlElement xml);
}