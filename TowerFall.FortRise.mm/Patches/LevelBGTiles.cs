using System.Xml;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_LevelBGTiles : LevelBGTiles
{
    private bool[,] bgData;
    private bool[,] solidsData;
    private int[,] overwriteData;
    private Tilemap tilemap;

    public patch_LevelBGTiles(XmlElement xml, bool[,] data, bool[,] solids, int[,] overwriteData) : base(xml, data, solids, overwriteData)
    {
    }

    public void Replace(bool[,] bgData, bool[,] solidsData, int[,] overwriteData) 
    {
        this.bgData = bgData;
        this.solidsData = solidsData;
        this.overwriteData = overwriteData;
    }

    public void ReloadTiles() 
    {
        LoadTiles(tilemap);
    }

    [MonoModIgnore]
    private extern void LoadTiles(Tilemap tiles);
}