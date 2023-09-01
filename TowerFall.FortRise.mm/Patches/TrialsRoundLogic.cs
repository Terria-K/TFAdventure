using System.Collections.Generic;
using System.Xml;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_TrialsRoundLogic : TrialsRoundLogic
{
    public TrialsControl Control { [MonoModIgnore] get => null; [MonoModIgnore] private set => throw null; }
    

    public patch_TrialsRoundLogic(Session session) : base(session)
    {
    }

    [MonoModLinkTo("TowerFall.RoundLogic", "System.Void OnLevelLoadFinish()")]
    [MonoModIgnore]
    public void base_OnLevelLoadFinish() 
    {
        base.OnLevelLoadFinish();
    }

    [MonoModReplace]
    public override void OnLevelLoadFinish()
    {
        base_OnLevelLoadFinish();
        Point id = MainMenu.TrialsMatchSettings.LevelSystem.ID;
        base.Session.CurrentLevel.Add<TrialsStart>(new TrialsStart(base.Session));
        base.Session.CurrentLevel.Add<TrialsControl>(this.Control = new TrialsControl());
        base.Players = 1;
        int num = 0;
        while (num < 4 && !TFGame.Players[num])
        {
            num++;
        }
        List<Vector2> xmlpositions = base.Session.CurrentLevel.GetXMLPositions("PlayerSpawn");
        Player player = new Player(num, xmlpositions[0] + Vector2.UnitY * 2f, Allegiance.Neutral, Allegiance.Neutral, new PlayerInventory(GameData.TrialsLevels[id.X, id.Y]), Player.HatStates.Normal, true, true, true);
        base.Session.CurrentLevel.Add<Player>(player);
        foreach (XmlElement xmlElement in base.Session.CurrentLevel.GetXMLEntities("TreasureChest"))
        {
            base.Session.CurrentLevel.Add<TreasureChest>(new TreasureChest(
                xmlElement.Position(), 
                xmlElement.AttrEnum<TreasureChest.Types>("Type"), 
                xmlElement.AttrEnum<TreasureChest.AppearModes>("Mode"), 
                xmlElement.AttrEnum<Pickups>("Treasure")
            ));
        }
        var tower = (Session.MatchSettings.LevelSystem as TrialsLevelSystem).TrialsLevelData;
        if (tower.GetLevelSet() == "TowerFall") 
        {
            TrialsLevelStats[] array = SaveData.Instance.Trials.Levels[id.X];
            int y = id.Y;
            array[y].Attempts = array[y].Attempts + 1UL;
            return;
        }
        var stats = (tower as AdventureTrialsTowerData).Stats;
        stats.Attempts += 1;
    }
}