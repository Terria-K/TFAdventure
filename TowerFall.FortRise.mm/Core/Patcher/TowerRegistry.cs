using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Monocle;
using TowerFall;

namespace FortRise.Adventure;

public static class TowerRegistry 
{
    public static Dictionary<string, List<AdventureWorldTowerData>> DarkWorldTowerSets = new();
    public static List<string> DarkWorldLevelSets = new();

    public static Dictionary<string, List<AdventureQuestTowerData>> QuestTowerSets = new();
    public static List<string> QuestLevelSets = new();

    public static Dictionary<string, List<AdventureVersusTowerData>> VersusTowerSets = new();
    public static List<string> VersusLevelSets = new();

    public static Dictionary<string, List<AdventureTrialsTowerData[]>> TrialsTowerSets = new();
    public static List<string> TrialsLevelSet = new();

    public static void DarkWorldAdd(string levelSet, AdventureWorldTowerData data) 
    {
        if (levelSet == string.Empty)
            levelSet = "UNCATEGORIZED";
        if (DarkWorldTowerSets.TryGetValue(levelSet, out var val)) 
        {
            data.ID.X = val.Count;
            val.Add(data);
            return;
        }
        DarkWorldLevelSets.Add(levelSet);
        var list = new List<AdventureWorldTowerData>();
        data.ID.X = 0;
        list.Add(data);
        DarkWorldTowerSets[levelSet] = list; 
    }

    public static AdventureWorldTowerData DarkWorldGet(string levelSet, int levelID) 
    {
        return DarkWorldTowerSets[levelSet][levelID];
    }

    public static bool TryDarkWorldGet(string levelSet, int levelID, out AdventureWorldTowerData data) 
    {
        if (DarkWorldTowerSets.TryGetValue(levelSet, out var arr)) 
        {
            if (levelID < arr.Count && levelID > -1)
            {
                data = arr[levelID];
                return true;
            }
        }
        data = null;
        return false;
    }

    public static bool TryDarkWorldGet(string levelSet, string levelID, out AdventureWorldTowerData data) 
    {
        if (DarkWorldTowerSets.TryGetValue(levelSet, out var arr)) 
        {
            foreach (var level in arr) 
            {
                if (level.GetLevelID() == levelID) 
                {
                    data = level;
                    return true;
                }
            }
        }
        data = null;
        return false;
    }

    public static AdventureWorldTowerData DarkWorldGet(string levelSet, string levelID) 
    {
        var darkWorldLevel = DarkWorldTowerSets[levelSet];
        foreach (var level in darkWorldLevel) 
        {
            if (level.GetLevelID() == levelID) 
            {
                return level;
            }
        }
        return null;
    }
    public static bool TryDarkWorldGet(string levelSet, int levelID, out AdventureQuestTowerData data) 
    {
        if (QuestTowerSets.TryGetValue(levelSet, out var arr)) 
        {
            if (levelID < arr.Count && levelID > -1)
            {
                data = arr[levelID];
                return true;
            }
        }
        data = null;
        return false;
    }

    public static bool TryDarkWorldGet(string levelSet, string levelID, out AdventureQuestTowerData data) 
    {
        if (QuestTowerSets.TryGetValue(levelSet, out var arr)) 
        {
            foreach (var level in arr) 
            {
                if (level.GetLevelID() == levelID) 
                {
                    data = level;
                    return true;
                }
            }
        }
        data = null;
        return false;
    }


    public static void QuestAdd(string levelSet, AdventureQuestTowerData data) 
    {
        if (QuestTowerSets.TryGetValue(levelSet, out var val)) 
        {
            data.ID.X = val.Count;
            val.Add(data);
            return;
        }
        QuestLevelSets.Add(levelSet);
        var list = new List<AdventureQuestTowerData>();
        data.ID.X = 0;
        list.Add(data);
        QuestTowerSets[levelSet] = list; 
    }

    public static AdventureQuestTowerData QuestGet(string levelSet, int levelID) 
    {
        return QuestTowerSets[levelSet][levelID];
    }

    public static AdventureQuestTowerData QuestGet(string levelSet, string levelID) 
    {
        var questLevel = QuestTowerSets[levelSet];
        foreach (var level in questLevel) 
        {
            if (level.GetLevelID() == levelID) 
            {
                return level;
            }
        }
        return null;
    }

    public static void VersusAdd(string levelSet, AdventureVersusTowerData data) 
    {
        if (VersusTowerSets.TryGetValue(levelSet, out var val)) 
        {
            data.ID.X = val.Count;
            val.Add(data);
            return;
        }
        VersusLevelSets.Add(levelSet);
        var list = new List<AdventureVersusTowerData>();
        data.ID.X = 0;
        list.Add(data);
        VersusTowerSets[levelSet] = list; 
    }


    public static AdventureVersusTowerData VersusGet(string levelSet, int levelID) 
    {
        return VersusTowerSets[levelSet][levelID];
    }

    public static AdventureVersusTowerData VersusGet(string levelSet, string levelID) 
    {
        var versusLevel = VersusTowerSets[levelSet];
        foreach (var level in versusLevel) 
        {
            if (level.GetLevelID() == levelID) 
            {
                return level;
            }
        }
        return null;
    }


    public static void TrialsAdd(AdventureTrialsTowerData[] datas) 
    {
        if (datas.Length == 0)
            return;    
        var levelSet = datas[0].GetLevelSet();
        if (TrialsTowerSets.TryGetValue(levelSet, out var val)) 
        {
            int i = 0;
            foreach (var data in datas) 
            {
                data.ID.X = val.Count;
                data.ID.Y = i;
                i++;
            }
            val.Add(datas);
            return;
        }
        TrialsLevelSet.Add(levelSet);
        var list = new List<AdventureTrialsTowerData[]>();
        datas[0].ID.X = 0;
        datas[1].ID.X = 0;
        datas[2].ID.X = 0;
        datas[0].ID.Y = 0;
        datas[1].ID.Y = 1;
        datas[2].ID.Y = 2;
        list.Add(datas);
        TrialsTowerSets[levelSet] = list; 
    }

    public static TrialsLevelData[] TrialsGet(string levelSet, int levelID) 
    {
        return TrialsTowerSets[levelSet][levelID];
    }

    internal static void LoadQuest() 
    {
        foreach (var map in RiseCore.ResourceTree.TreeMap.Values
            .Where(folder => folder.ResourceType == typeof(RiseCore.ResourceTypeQuestTowerFolder)))
        {
            var fullPath = map.FullPath;
            var path = fullPath.Substring(4).Replace("Content/Levels/Quest/", string.Empty);

            var levelData = new AdventureQuestTowerData();
            levelData.SetLevelID(path);
            levelData.SetLevelSet(path);
            levelData.Path = fullPath + "/" + "level.oel";
            levelData.DataPath = fullPath + "/" + "data.xml";

            RiseCore.Resource towerXmlResource = null;
            foreach (var child in map.Childrens) 
            {
                if (!child.Path.Contains("tower.xml")) 
                    continue;
                towerXmlResource = child;
                break;
            }
            if (towerXmlResource == null)
                continue;

            levelData.Stats = AdventureModule.SaveData.AdventureQuest.AddOrGet(levelData.GetLevelID());

            using var xmlStream = towerXmlResource.Stream;
            var xml = patch_Calc.LoadXML(xmlStream)["tower"];

            levelData.Author = xml.ChildText("author", string.Empty);
            levelData.Theme = LoadTheme(xml, map);

            if (xml.HasChild("required"))
                levelData.RequiredMods = xml["required"].InnerText;
            else
                levelData.RequiredMods = string.Empty;

            TowerRegistry.QuestAdd(levelData.GetLevelSet(), levelData);
            RiseCore.Events.Invoke_OnAdventureQuestTowerDataAdd(levelData.GetLevelSet(), levelData);
        }
    }

    internal static void LoadVersus() 
    {
        foreach (var map in RiseCore.ResourceTree.TreeMap.Values
            .Where(folder => folder.ResourceType == typeof(RiseCore.ResourceTypeVersusTowerFolder))) 
        {
            var path = map.FullPath.Substring(4).Replace("Content/Levels/Versus/", string.Empty);

            var levelData = new AdventureVersusTowerData();
            levelData.SetLevelID(path);
            levelData.SetLevelSet(path);
            levelData.Levels = new();

            RiseCore.Resource xmlResource = null;
            foreach (var child in map.Childrens) 
            {
                if ((child.ResourceType == typeof(RiseCore.ResourceTypeOel) || 
                child.ResourceType == typeof(RiseCore.ResourceTypeJson)) &&
                !child.Path.StartsWith("icon"))
                {
                    levelData.Levels.Add(AdventureVersusLevelData.CreateFromAdventure(child.Root + child.Path));
                    continue;
                }

                if (child.Path.Contains("tower.xml")) 
                {
                    xmlResource = child;
                }
            }
            if (xmlResource == null)
                continue;

            using var xmlStream = xmlResource.Stream;
            var xml = patch_Calc.LoadXML(xmlStream)["tower"];
            levelData.Author = xml.ChildText("author", string.Empty);
            levelData.Theme = LoadTheme(xml, map);
            levelData.FixedFirst = xml.HasChild("fixedFirst");
            levelData.Procedural = xml.HasChild("procedural");
            if (!xml.HasChild("treasure")) 
            {
                levelData.TreasureMask = TreasureSpawner.FullTreasureMask;
                levelData.ArrowShuffle = false;
                levelData.SpecialArrowRate = 0.6f;
            }
            else 
            {
                var array = xml.ChildText("treasure").Split(',');
                levelData.TreasureMask = new int[TreasureSpawner.FullTreasureMask.Length];
                levelData.SetTreasureChances((float[])TreasureSpawner.DefaultTreasureChances.Clone());
                for (int i = 0; i < array.Length; i++)
                {
                    ReadOnlySpan<char> treasure = array[i];
                    ParseTreasure(treasure.Trim(), out string resTreasure, out int chance, out int rate);
                    if (!patch_Calc.TryStringToEnum<Pickups>(resTreasure, out var pickups)) 
                    {
                        Logger.Error($"[ADVENTURE][VERSUS] The pickup name '{resTreasure}' cannot be found.");
                        continue;
                    }
                    
                    if (rate == -1)
                        levelData.TreasureMask[(int)pickups]++;
                    else
                        levelData.TreasureMask[(int)pickups] = levelData.TreasureMask[(int)pickups] + rate;

                    if (chance != -1)
                        levelData.GetTreasureChances()[(int)pickups] = (chance / 100);
                }

                levelData.ArrowShuffle = xml["treasure"].AttrBool("arrowShuffle", false);
                levelData.SpecialArrowRate = xml["treasure"].AttrFloat("arrows", 0.6f);
            }

            if (xml.HasChild("required"))
                levelData.RequiredMods = xml["required"].InnerText;
            else
                levelData.RequiredMods = string.Empty;
            
            TowerRegistry.VersusAdd(levelData.GetLevelSet(), levelData);
            RiseCore.Events.Invoke_OnAdventureVersusTowerDataAdd(levelData.GetLevelSet(), levelData);
        }
    }

    private static void ParseTreasure(ReadOnlySpan<char> treasure, out string resultTreasure, out int chance, out int rate) 
    {
        if (treasure.IndexOf('[') != 0)
        {
            chance = -1;
            rate = -1;
            resultTreasure = treasure.ToString();
            return;
        }

        var text = treasure.Slice(1, treasure.IndexOf(']') - 1);
        resultTreasure = treasure.Slice(treasure.IndexOf(']') + 1).ToString();
        var split = text.SplitLines('*');
        chance = -1;
        rate = -1;
        foreach (var sp in split) 
        {
            var numText = sp.Line;
            if (numText.Contains("%".AsSpan(), StringComparison.InvariantCulture)) 
            {
                var chanceSlice = numText.Slice(0, numText.IndexOf('%'));
                chance = int.Parse(new string(chanceSlice));
                continue;
            }
            rate = int.Parse(new string(numText));
        }
    }

    internal static void LoadDarkWorld() 
    {
        foreach (var map in RiseCore.ResourceTree.TreeMap.Values
            .Where(folder => folder.ResourceType == typeof(RiseCore.ResourceTypeDarkWorldTowerFolder)))
        {
            var path = map.FullPath.Substring(4).Replace("Content/Levels/DarkWorld/", string.Empty);

            var levelData = new AdventureWorldTowerData();
            levelData.SetLevelID(path);
            levelData.SetLevelSet(path);
            levelData.Levels = new();

            RiseCore.Resource xmlResource = null;
            RiseCore.Resource iconResource = null;
            foreach (var child in map.Childrens) 
            {
                if ((child.ResourceType == typeof(RiseCore.ResourceTypeOel) || 
                child.ResourceType == typeof(RiseCore.ResourceTypeJson)) &&
                !child.Path.Contains("icon.json"))
                {
                    levelData.Levels.Add(child.Root + child.Path);
                    continue;
                }

                if (child.Path.Contains("icon.json")) 
                {
                    iconResource = child;
                    continue;
                }

                if (child.Path.Contains("tower.xml")) 
                {
                    xmlResource = child;
                }
            }
            if (xmlResource == null)
                continue;

            levelData.Stats = AdventureModule.SaveData.AdventureWorld.AddOrGet(levelData.GetLevelID());

            using var xmlStream = xmlResource.Stream;
            var xml = patch_Calc.LoadXML(xmlStream)["tower"];
            levelData.LoadExtraData(xml);
            levelData.Author = xml.ChildText("author", string.Empty);
            levelData.Theme = LoadTheme(xml, map);
            if (iconResource != null)
                levelData.BuildIcon(iconResource);
			
			levelData.TimeBase = xml["time"].ChildInt("base", 300);
			levelData.TimeAdd = xml["time"].ChildInt("add", 40);
			levelData.EnemySets = new Dictionary<string, List<DarkWorldTowerData.EnemyData>>();

			foreach (XmlElement xmlElement2 in xml["enemies"].GetElementsByTagName("set"))
			{
				string text3 = xmlElement2.Attr("id");
				List<DarkWorldTowerData.EnemyData> list = new List<DarkWorldTowerData.EnemyData>();
				foreach (XmlElement xmlElement3 in xmlElement2.GetElementsByTagName("spawn"))
				{
					list.Add(new DarkWorldTowerData.EnemyData(xmlElement3));
				}
				levelData.EnemySets.Add(text3, list);
			}

			levelData.Normal = levelData.LoadLevelSet_Public(xml["normal"]);
			levelData.Hardcore = levelData.LoadLevelSet_Public(xml["hardcore"]);
			levelData.Legendary = levelData.LoadLevelSet_Public(xml["legendary"]);

            if (xml.HasChild("required"))
                levelData.RequiredMods = xml["required"].InnerText;
            else
                levelData.RequiredMods = string.Empty;

            TowerRegistry.DarkWorldAdd(levelData.GetLevelSet(), levelData);
            RiseCore.Events.Invoke_OnAdventureDarkWorldTowerDataAdd(levelData.GetLevelSet(), levelData);
        }
    }

    internal static void LoadTrials() 
    {
        foreach (var map in RiseCore.ResourceTree.TreeMap.Values
            .Where(folder => folder.ResourceType == typeof(RiseCore.ResourceTypeTrialsTowerFolder)))
        {
            var path = map.FullPath.Substring(4).Replace("Content/Levels/Trials/", string.Empty);
            RiseCore.Resource xmlResource = null;
            foreach (var child in map.Childrens) 
            {
                if (child.Path.Contains("tower.xml")) 
                {
                    xmlResource = child;
                }
            }
            if (xmlResource == null)
                continue;

            using var xmlStream = xmlResource.Stream;
            var xml = patch_Calc.LoadXML(xmlStream)["tower"];


            foreach (XmlElement tier in xml.GetElementsByTagName("tier")) 
            {
                int id = 0;
                var arr = new AdventureTrialsTowerData[3];
                foreach (XmlElement element in xml.GetElementsByTagName("level")) 
                {
                    var trialData = new AdventureTrialsTowerData();
                    trialData.SetLevelID(path + "-" + id);
                    trialData.Stats = AdventureModule.SaveData.AdventureTrials.AddOrGet(trialData.GetLevelID());
                    trialData.SetLevelSet(path);
                    trialData.Path = map.Root + map.Path + "/" + element.Attr("path");
                    trialData.Arrows = element.ChildInt("arrows", 3);
                    trialData.SwitchBlockTimer = element.ChildInt("switchTimer", 200);
                    trialData.Theme = LoadTheme(element, map);
                    trialData.Goals = new TimeSpan[3];
                    trialData.Goals[0] = TimeSpan.FromSeconds((double)element.ChildFloat("gold", 0.3f));
                    trialData.Goals[1] = TimeSpan.FromSeconds((double)element.ChildFloat("diamond", 0.2f));
                    trialData.Goals[2] = TimeSpan.FromSeconds((double)element.ChildFloat("dev", 0.1f));
                    arr[id] = trialData;
                    id++;

                    trialData.Author = xml.ChildText("author", string.Empty);

                    if (xml.HasChild("required"))
                        trialData.RequiredMods = xml["required"].InnerText;
                    else
                        trialData.RequiredMods = string.Empty;
                }
                TowerRegistry.TrialsAdd(arr);
                if (arr.Length > 0)
                    RiseCore.Events.Invoke_OnAdventureTrialsTowerDatasAdd(arr[0].GetLevelSet(), arr);
            }
        }
    }

    private static TowerTheme LoadTheme(XmlElement xml, RiseCore.Resource map) 
    {
        if (xml.HasChild("theme")) 
        {
            var xmlTheme = xml["theme"];
            if (xmlTheme.HasChild("Name")) 
            {
                var atlas = xml.Attr("atlas", "Atlas/atlas");
                var theme = ThemeResource.Create(atlas, map);
                return new patch_TowerTheme(xml["theme"], map, theme);
            }
            else if (RiseCore.GameData.Themes.TryGetValue(xml.ChildText("theme").Trim(), out var theme)) 
            {
                return theme;
            }
            else 
            {
                return GameData.Themes[xml.ChildText("theme")];
            }
        }
        return TowerTheme.GetDefault();
    }
}

public struct ThemeResource 
{
    public patch_Atlas Atlas;

    public static ThemeResource Create(string atlas, RiseCore.Resource map) 
    {
        ThemeResource theme = default;
        if (map.Source.Content.Atlases.TryGetValue(atlas, out var at)) 
        {
            theme = new ThemeResource() 
            {
                Atlas = at
            };
        }
        return theme;
    }
}