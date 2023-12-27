using System;
using System.Collections.Generic;
using System.Xml;
using FortRise;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_VersusLevelSystem : VersusLevelSystem
{
    private List<string> levels;
    private string lastLevel;
    public string LastLevel => lastLevel;

    [MonoModIgnore]
    public VersusTowerData VersusTowerData { get; private set; }
    


    public patch_VersusLevelSystem(VersusTowerData tower) : base(tower)
    {
    }

    [MonoModConstructor]
    [MonoModReplace]
    public void ctor(VersusTowerData tower)
    {
        this.VersusTowerData = tower;
        base.ID = tower.ID;
        base.Theme = tower.Theme;
        base.ShowControls = base.ID.X == 0 && tower.IsOfficialLevelSet();
        base.ShowTriggerControls = base.ID.X == 14 && tower.IsOfficialLevelSet();
        this.levels = new List<string>();
    }

    public extern XmlElement orig_GetNextRoundLevel(MatchSettings matchSettings, int roundIndex, out int randomSeed);

    public override XmlElement GetNextRoundLevel(MatchSettings matchSettings, int roundIndex, out int randomSeed)
    {
        try 
        {
            if (VersusTowerData.GetLevelSet() != "TowerFall") 
            {
                if (levels.Count == 0)
                {
                    GenLevels(matchSettings);
                }
                lastLevel = this.levels[0];
                levels.RemoveAt(0);
                randomSeed = 0;
                foreach (char c in lastLevel)
                {
                    randomSeed += c;
                }
                if (RiseCore.ResourceTree.TreeMap.TryGetValue(lastLevel, out var res)) 
                {
                    using var lastLevelStream = res.Stream;
                    return patch_Calc.LoadXML(lastLevelStream)["level"];
                }
                Logger.Error($"[VERSUSLEVELSYSTEM][{lastLevel}] Path not found!");
            }
            return orig_GetNextRoundLevel(matchSettings, roundIndex, out randomSeed);
        }
        catch (Exception e)
        {
            ErrorHelper.StoreException("Missing Level", e);
            randomSeed = 0;
            return null;
        }
    }

    [MonoModIgnore]
    private extern void GenLevels(MatchSettings matchSettings);
}

public static class VersusLevelSystemExt 
{
    public static string GetLastLevel(this VersusLevelSystem system) 
    {
        return ((patch_VersusLevelSystem)system).LastLevel;
    }
}