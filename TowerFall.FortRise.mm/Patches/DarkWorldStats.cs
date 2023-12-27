namespace TowerFall;

public class patch_DarkWorldStats : DarkWorldStats 
{
    public extern bool orig_ShouldRevealTower(int towerID);

    public bool ShouldRevealTower(int towerID) 
    {
        if (GameData.DarkWorldTowers[towerID].GetLevelSet() != "TowerFall")
            return false;
        return orig_ShouldRevealTower(towerID);
    }
}