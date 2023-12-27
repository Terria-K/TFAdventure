using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using TeuJson;
using TeuJson.Attributes;

namespace FortRise;

/// <summary>
/// A utility class that loads and convert Ogmo Editor 3 level Json format to Ogmo Editor level OEL/XML format.
/// </summary>
public static class Ogmo3ToOel 
{
    public delegate void ReadLayerHandler(OgmoLayer layer, XmlElement xmlLevel);
    public static event ReadLayerHandler ReadLayer;
    /// <summary>
    /// Loads a Ogmo Editor 3 *.json level file.
    /// </summary>
    /// <param name="path">A path to the *.json file</param>
    /// <returns>An Ogmo Editor 3 level data</returns>
    public static OgmoLevelData LoadOgmo(string path) 
    {
        return JsonConvert.DeserializeFromFile<OgmoLevelData>(path);
    }

    /// <summary>
    /// Loads a Ogmo Editor 3 *.json level file.
    /// </summary>
    /// <param name="stream">A stream containing the *.json file</param>
    /// <returns>An Ogmo Editor 3 level data</returns>
    public static OgmoLevelData LoadOgmo(Stream stream) 
    {
        return JsonConvert.DeserializeFromStream<OgmoLevelData>(stream);
    }

    /// <summary>
    /// Convert an Ogmo Editor 3 level data into Ogmo Editor XML/OEL level data.
    /// </summary>
    /// <param name="levelData">An Ogmo Editor 3 level Json format to convert</param>
    /// <returns>XmlDocument that represent Ogmo Editor XML/OEL level data</returns>
    public static XmlDocument OgmoToOel(OgmoLevelData levelData) 
    {
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml("<level></level>");
        XmlElement level = xmlDocument.DocumentElement;
        level.SetAttribute("width", levelData.Width.ToString());
        level.SetAttribute("height", levelData.Height.ToString());
        level.SetAttribute("Darkness", levelData.GetValueFloat("Darkness").ToString());
        level.SetAttribute("CanUnlockMoonstone", "False");

        XmlElement bg = xmlDocument.CreateElement("BG");
        bg.SetAttribute("exportMode", "Bitstring");

        XmlElement bgTiles = xmlDocument.CreateElement("BGTiles");
        bgTiles.SetAttribute("tileset", levelData.GetValueString("TilesetBG"));
        bgTiles.SetAttribute("exportMode", "TrimmedCSV");

        XmlElement solids = xmlDocument.CreateElement("Solids");
        solids.SetAttribute("exportMode", "Bitstring");

        XmlElement solidTiles = xmlDocument.CreateElement("SolidTiles");
        solidTiles.SetAttribute("tileset", levelData.GetValueString("Tileset"));
        solidTiles.SetAttribute("exportMode", "TrimmedCSV");

        XmlElement entities = xmlDocument.CreateElement("Entities");

        foreach (var layer in levelData.Layers) 
        {
            switch (layer.Name) 
            {
            case "BG": 
                bg.InnerText = Array2DToBitString(layer.Grid2D);
                break;
            case "BGTiles": 
                bgTiles.InnerText = Array2DToCSV(layer.Data);
                break;
            case "Solids":
                solids.InnerText = Array2DToBitString(layer.Grid2D);
                break;
            case "SolidTiles":
                solidTiles.InnerText = Array2DToCSV(layer.Data);
                break;
            case "Entities":
                foreach (var entity in layer.Entities) 
                {
                    var element = xmlDocument.CreateElement(entity.Name);
                    element.SetAttribute("id", entity.ID.ToString());
                    element.SetAttribute("x", entity.X.ToString());
                    element.SetAttribute("y", entity.Y.ToString());
                    element.SetAttribute("width", entity.Width.ToString());
                    element.SetAttribute("height", entity.Height.ToString());

                    if (entity.Nodes != null) 
                    {
                        foreach (var node in entity.Nodes) 
                        {
                            var nodeElement = xmlDocument.CreateElement("node");
                            nodeElement.SetAttribute("x", node.X.ToString());
                            nodeElement.SetAttribute("y", node.Y.ToString());
                            element.AppendChild(nodeElement);
                        }
                    }

                    foreach (var values in entity.Values?.Pairs) 
                    {
                        var attrib = xmlDocument.CreateAttribute(values.Key);
                        if (values.Value.IsBoolean) 
                        {
                            attrib.Value = values.Value.AsBoolean ? "True" : "False";
                        }
                        else 
                        {
                            attrib.Value = values.Value.ToString();
                        }
                        element.Attributes.Append(attrib);
                    }
                    entities.AppendChild(element);
                }
                break;
            default:
                ReadLayer?.Invoke(layer, level);
                break;
            }
        }

        level.AppendChild(bg);
        level.AppendChild(bgTiles);
        level.AppendChild(solids);
        level.AppendChild(solidTiles);
        level.AppendChild(entities);

        return xmlDocument;
    }

    /// <summary>
    /// Convert an Array2D to CSV format.
    /// </summary>
    /// <param name="array2D">An Array2D to be converted as CSV format</param>
    /// <returns>CSV String</returns>
    public static string Array2DToCSV(int[,] array2D) 
    {
        var sb = new StringBuilder();
        for (int x = 0; x < array2D.GetLength(0); x++) 
        {
            for (int y = 0; y < array2D.GetLength(1); y++) 
            {
                sb.Append(array2D[x, y]);
                if (x != (array2D.GetLength(1) - 1))
                    sb.Append(',');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Convert an Array2D to bit string format.
    /// </summary>
    /// <param name="array2D">An Array2D to be converted as bitstring format</param>
    /// <returns>Bit string</returns>
    public static string Array2DToBitString(string[,] array2D) 
    {
        var sb = new StringBuilder();
        for (int x = 0; x < array2D.GetLength(0); x++) 
        {
            for (int y = 0; y < array2D.GetLength(1); y++) 
            {
                sb.Append(array2D[x, y]);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Convert an Array2D to bit string format without a line break.
    /// </summary>
    /// <param name="array2D">An Array2D to be converted as bitstring format</param>
    /// <returns>Bit string</returns>
    public static string Array2DToStraightBitString(string[,] array2D) 
    {
        var sb = new StringBuilder();
        for (int x = 0; x < array2D.GetLength(0); x++) 
        {
            for (int y = 0; y < array2D.GetLength(1); y++) 
            {
                sb.Append(array2D[x, y]);
            }
        }
        return sb.ToString();
    }
}

/// <summary>
/// Ogmo Editor 3 level data.
/// </summary>
public sealed partial class OgmoLevelData : IDeserialize
{
    [Name("width")]
    public int Width { get; set; }
    [Name("height")]
    public int Height { get; set; }
    [Name("offsetX")]
    public int OffsetX { get; set; }
    [Name("offsetY")]
    public int OffsetY { get; set; }
    [Name("layers")]
    public OgmoLayer[] Layers { get; set; }
    [Name("values")]
    public JsonValue Values { get; set; }


    public int GetValueInt(string valueName) 
    {
        if (Values == null)
            return 0;
        return Values[valueName].AsInt32;
    }

    public bool GetValueBoolean(string valueName) 
    {
        if (Values == null)
            return false;
        return Values[valueName].AsBoolean;
    }

    public float GetValueFloat(string valueName) 
    {
        if (Values == null)
            return 0.0f;
        return Values[valueName].AsSingle;
    }

    public Vector2 GetValueVector2(string x, string y) 
    {
        if (Values == null)
            return Vector2.Zero;
        return new Vector2(Values[x].AsSingle, Values[y].AsSingle);
    }

    public string GetValueString(string valueName) 
    {
        if (Values == null)
            return string.Empty;
        return Values[valueName].AsString;
    }

    public T GetValueEnum<T>(string val) 
    where T : struct, System.Enum
    {
        if (Values == null)
            return default;
        if (System.Enum.TryParse<T>(Values[val].AsString, true, out T result)) 
        {
            return result;
        }
        return default;
    }

}

public sealed partial class OgmoLayer : IDeserialize
{
    [Name("name")]
    public string Name { get; set; } = "";
    [Name("offsetX")]
    public int OffsetX { get; set; }
    [Name("offsetY")]
    public int OffsetY { get; set; }
    [Name("gridCellWidth")]
    public int GridCellWidth { get; set; }
    [Name("gridCellHeight")]
    public int GridCellHeight { get; set; }
    [Name("gridCellsX")]
    public int GridCellsX { get; set; }
    [Name("gridCellsY")]
    public int GridCellsY { get; set; }
    [Name("tileset")]
    public string Tileset { get; set; }

    [Name("data2D")]
    public int[,] Data { get; set; }

    [Name("grid2D")]
    public string[,] Grid2D { get; set; }

    [Name("grid")]
    public string[] Grid { get; set; }

    [Name("entities")]
    public OgmoEntity[] Entities { get; set; }
}

public sealed partial class OgmoNode : IDeserialize
{
    [Name("x")]
    public float X { get; set; }
    [Name("y")]
    public float Y { get; set; }

    public Vector2 ToVector2() 
    {
        return new Vector2(X, Y);
    }
}

public sealed partial class OgmoEntity : IDeserialize
{
    [Name("name")]
    public string Name { get; set; }
    [Name("id")]
    public int ID { get; set; }
    [Name("x")]
    public int X { get; set; }
    [Name("y")]
    public int Y { get; set; }
    [Name("originX")]
    public int OriginX { get; set; }
    [Name("originY")]
    public int OriginY { get; set; }
    [Name("width")]
    public int Width { get; set; }
    [Name("height")]
    public int Height { get; set; }
    [Name("flippedX")]
    public bool FlippedX { get; set; }
    [Name("flippedY")]
    public bool FlippedY { get; set; }
    [Name("nodes")]
    public OgmoNode[] Nodes { get; set; }
    [Name("values")]
    public JsonValue Values { get; set; }
    [Ignore]
    public Vector2 Position => new(X, Y);
    [Ignore]
    public Rectangle Size => new(OriginX, OriginY, Width, Height);


    public int Int(string valueName) 
    {
        if (Values == null)
            return 0;
        return Values[valueName].AsInt32;
    }

    public bool Boolean(string valueName) 
    {
        if (Values == null)
            return false;
        return Values[valueName].AsBoolean;
    }

    public float Float(string valueName) 
    {
        if (Values == null)
            return 0.0f;
        return Values[valueName].AsSingle;
    }

    public string String(string valueName) 
    {
        if (Values == null)
            return string.Empty;
        return Values[valueName].AsString;
    }

    public Color Color(string r, string g, string b)
    {
        var red = Float(r);
        var green = Float(g);
        var blue = Float(b);
        return new Color(red, green, blue);
    }

    public Color Color(string r, string g, string b, string a)
    {
        var red = Float(r);
        var green = Float(g);
        var blue = Float(b);
        var alpha = Float(a);
        return new Color(red, green, blue, alpha);
    }
    private static char[] SplitChar = new char[1] { ',' };

    public Color Color(string value)
    {
        var val = String(value).Split(SplitChar);
        if (val.Length == 4) 
        {
            var red = Float(val[0]);
            var green = Float(val[1]);
            var blue = Float(val[2]);
            var alpha = Float(val[3]);
            return new Color(red, green, blue, alpha);
        }
        var r = Float(val[0]);
        var g = Float(val[1]);
        var b = Float(val[2]);
        return new Color(r, g, b);
    }

    public Vector2 Vector2(string x, string y) 
    {
        if (Values == null)
            return Microsoft.Xna.Framework.Vector2.Zero;
        return new Vector2(Values[x].AsSingle, Values[y].AsSingle);
    }

    public Point Point(string x, string y) 
    {
        if (Values == null)
            return Microsoft.Xna.Framework.Point.Zero;
        return new Point(Values[x].AsInt32, Values[y].AsInt32);
    }

    public T Enum<T>(string val) 
    where T : struct, System.Enum
    {
        if (Values == null)
            return default;
        if (System.Enum.TryParse<T>(Values[val].AsString, true, out T result)) 
        {
            return result;
        }
        return default;
    }
}