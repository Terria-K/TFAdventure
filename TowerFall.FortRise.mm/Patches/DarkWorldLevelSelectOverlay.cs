using FortRise.Adventure;
using MonoMod;

namespace TowerFall;

public class patch_DarkWorldLevelSelectOverlay : DarkWorldLevelSelectOverlay
{
    private int statsID;
    private MapScene map;

    private string levelAttemptsString;
    private string levelDeathsString;
    private string levelBestTimeString;
    private string levelCursesString;

    public patch_DarkWorldLevelSelectOverlay(MapScene map) : base(map)
    {
    }

    [MonoModIgnore]
    [MonoModConstructor]
    [PatchDarkWorldLevelSelectOverlayCtor]
    public extern void ctor(MapScene map);
    

    public extern void orig_Update();

    public override void Update()
    {
        if (map.Selection is DarkWorldMapButton or AdventureMapButton)
            orig_Update();
        else
            base_Update();
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Update()")]
    [MonoModIgnore]
    public void base_Update() 
    {
        base.Update();
    }

    private extern void orig_RefreshLevelStats();
    
    private void RefreshLevelStats() 
    {
        var levelSet = map.GetLevelSet();
        if (levelSet == "TowerFall")
        {
            orig_RefreshLevelStats();
            return;
        }
        var adventureWorldTowerStats = TowerRegistry.DarkWorldGet(levelSet, statsID).Stats;
        long bestTime;
        int mostCurses;
        switch (TFGame.PlayerAmount)
        {
        default:
            bestTime = adventureWorldTowerStats.Best1PTime;
            mostCurses = adventureWorldTowerStats.Most1PCurses;
            break;
        case 2:
            bestTime = adventureWorldTowerStats.Best2PTime;
            mostCurses = adventureWorldTowerStats.Most2PCurses;
            break;
        case 3:
            bestTime = adventureWorldTowerStats.Best3PTime;
            mostCurses = adventureWorldTowerStats.Most3PCurses;
            break;
        case 4:
            bestTime = adventureWorldTowerStats.Best4PTime;
            mostCurses = adventureWorldTowerStats.Most4PCurses;
            break;
        }
        if (bestTime <= 0L)
        {
            this.levelBestTimeString = "";
        }
        else
        {
            this.levelBestTimeString = TrialsResults.GetTimeString(bestTime);
        }
        if (mostCurses <= 0)
        {
            this.levelCursesString = "";
        }
        else if (mostCurses == 1)
        {
            this.levelCursesString = "1 CURSE";
        }
        else
        {
            this.levelCursesString = mostCurses.ToString() + " CURSES";
        }
        if (adventureWorldTowerStats.Deaths == 0UL)
        {
            this.levelDeathsString = "";
        }
        else
        {
            this.levelDeathsString = adventureWorldTowerStats.Deaths.ToString();
        }
        if (adventureWorldTowerStats.Attempts == 0UL)
        {
            this.levelAttemptsString = "";
            return;
        }
        this.levelAttemptsString = adventureWorldTowerStats.Attempts.ToString();
    }
}