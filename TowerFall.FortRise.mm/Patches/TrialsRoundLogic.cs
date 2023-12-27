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
        Session.CurrentLevel.Add(new TrialsStart(Session));
        Session.CurrentLevel.Add(Control = new TrialsControl());
        Players = 1;
        int num = 0;
        while (num < 4 && !TFGame.Players[num])
        {
            num++;
        }
        List<Vector2> xmlpositions = Session.CurrentLevel.GetXMLPositions("PlayerSpawn");
        Player player = new Player(num, xmlpositions[0] + Vector2.UnitY * 2f, Allegiance.Neutral, Allegiance.Neutral, new PlayerInventory(GameData.TrialsLevels[id.X, id.Y]), Player.HatStates.Normal, true, true, true);
        Session.CurrentLevel.Add(player);
        foreach (XmlElement xmlElement in Session.CurrentLevel.GetXMLEntities("TreasureChest"))
        {
            Session.CurrentLevel.Add(new TreasureChest(
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