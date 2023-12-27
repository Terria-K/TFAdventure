using System.Xml;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_LevelTiles : LevelTiles
{
    private bool[,] bitData;
    private int[,] overwriteData;
    private Tilemap tilemap;

    public Grid Grid { [MonoModIgnore] get => null; [MonoModIgnore] private set => throw null; }

    public patch_LevelTiles(XmlElement xml, bool[,] solidsBitData) : base(xml, solidsBitData)
    {
    }

    public void Replace(bool[,] solidsBitData, int[,] overwriteData) 
    {
        bitData = solidsBitData;
        this.overwriteData = overwriteData;
        base.Collider = (this.Grid = new Grid(10f, 10f, this.bitData));
    }

    public void ReloadTiles() 
    {
        LoadTiles(tilemap);
    }
}