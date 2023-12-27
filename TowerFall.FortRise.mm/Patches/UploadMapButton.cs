using System;
using System.Collections.Generic;
using System.IO;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using Monocle;

namespace TowerFall;


[Obsolete("Will be removed in FortRise 5")]
public sealed class UploadMapButton : patch_MapButton
{
    public UploadMapButton() : base("UPLOAD LEVELS")
    {
    }

    public override void OnConfirm()
    {
        LoadTower();
        Map.Selection = null;
        OnDeselect();
        Map.GotoAdventure(Map.CurrentAdventureType);
        Map.MatchStarting = false;
    }

    private void LoadTower() 
    {
        if (XNAFileDialog.ShowDialogSynchronous("Load DarkWorld Tower .xml file") && !string.IsNullOrEmpty(XNAFileDialog.Path)) 
        {
            Load(Path.GetDirectoryName(XNAFileDialog.Path));
        }
    }

    private void Load(string path) 
    {
        // var selectedPath = path.Replace("\\", "/");
        // var loader = AdventureModule.SaveData.LevelLocations;
        // if (!loader.Contains(selectedPath) && 
        //     patch_GameData.LoadAdventureTowers(selectedPath, null))
        // {
        //     loader.Add(selectedPath);
        //     SaveLoaded();
        // }
    }

    internal void SaveLoaded() 
    {
        var saver = new Saver(true);
        Scene.Add(saver);
    }

    protected override List<Image> InitImages()
    {
        Image image = new Image(TFGame.MenuAtlas["randomLevelBlock"], null);
        image.CenterOrigin();
        Image image2 = new Image(TFGame.MenuAtlas["towerIcons/discovery"], null);
        image2.CenterOrigin();
        image2.Color = Color.Gray;
        return new List<Image>
        {
            image,
            image2
        };
    }
}