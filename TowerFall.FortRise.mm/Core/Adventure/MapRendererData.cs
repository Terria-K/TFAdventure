using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace FortRise.Adventure;

public class MapRendererNode : CompositeComponent
{
    public XmlElement Xml;
    public RiseCore.Resource Mod;
    public Subtexture Water;
    public Subtexture Land;
    public bool HideVanilla;

    public Dictionary<string, AnimatedTower> AnimatedTowers = new();
    public AnimatedTower CurrentTower;

    public MapRendererNode(XmlElement xml, RiseCore.Resource mod) : base(true, true)
    {
        Xml = xml;
        Mod = mod;

        var atlasPath = xml.Attr("atlas", "Atlas/atlas");
        var spriteDataPath = xml.Attr("spriteData", "Atlas/SpriteData/mapSpriteData");
        if (!mod.Source.Content.Atlases.TryGetValue(atlasPath, out var atlas)) 
        {
            Logger.Error($"[MAP RENDERER][{mod.Root}] Atlas path {atlasPath} not found!");
            return;
        }
        if (!mod.Source.Content.SpriteDatas.TryGetValue(spriteDataPath, out var spriteData)) 
        {
            Logger.Warning($"[MAP RENDERER][{mod.Root}] SpriteData path {spriteDataPath} not found!");
        }
        foreach (XmlElement element in xml["elements"]) 
        {
            switch (element.Name) 
            {
            case "hideVanilla":
                HideVanilla = true;
                break;
            case "landImage":
                var landImageName = element.Attr("image", "mapLand");
                Land = atlas[landImageName];
                Add(new Image(Land));
                break;
            case "waterImage":
                var waterImageName = element.Attr("image", "mapWater");
                Water = atlas[waterImageName];
                break;
            case "mapImage":
                var mapImageName = element.Attr("image");
                var x = element.AttrInt("x");
                var y = element.AttrInt("y");
                patch_Atlas atlasPack;
                if (mapImageName.StartsWith("TFGame.MenuAtlas::")) 
                {
                    atlasPack = (patch_Atlas)TFGame.MenuAtlas;
                    mapImageName = mapImageName.Replace("TFGame.MenuAtlas::", "");
                }
                else
                    atlasPack = atlas;
                
                Add(new Image(atlas[mapImageName]));
                break;
            case "mapAnimated":
                var mapAnimated = element.Attr("name");

                var containsVanillaSpriteData = mapAnimated.StartsWith("TFGame.MenuSpriteData::");
                if (spriteData == null && !containsVanillaSpriteData) 
                {
                    Logger.Error($"[MAP RENDERER][{mod.Root}] You cannot use mapAnimated when SpriteData is not present");
                    return;
                }
                
                var mapX = element.AttrInt("x");
                var mapY = element.AttrInt("y");
                var inAnimation = element.ChildText("in", "in");
                var outAnimation = element.ChildText("out", "out");
                var notSelected = element.ChildText("notSelected", "notSelected");
                var selected = element.ChildText("selected", "selected");
                Sprite<string> animation;
                if (containsVanillaSpriteData) 
                {
                    animation = TFGame.MenuSpriteData.GetSpriteString(mapAnimated.Substring("TFGame.MenuSpriteData::".Length));
                }
                else 
                {
                    animation = spriteData.GetSpriteString(mapAnimated);
                }
                var tower = new AnimatedTower(inAnimation, outAnimation, selected, notSelected, animation);

                animation.Position = new Vector2(mapX, mapY);
                
                animation.Play(notSelected);
                animation.OnAnimationComplete = (s) => {
                    if (s.CurrentAnimID == outAnimation)
                        s.Play(notSelected);
                    if (s.CurrentAnimID == inAnimation)
                        s.Play(selected);
                };

                AddAnimatedTowers(element.ChildText("towerName", ""), tower);
                break;
            }
        }
    }

    public void StartSelection(string towerName) 
    {
        if (AnimatedTowers.TryGetValue(towerName, out var value)) 
        {
            if (CurrentTower == value)
                return;
            
            value.Select();
            CurrentTower?.DeSelect();
            CurrentTower = value;
            return;
        }
        CurrentTower?.DeSelect();
        CurrentTower = null;
    }

    public void Deselection() 
    {
        CurrentTower?.DeSelect();
        CurrentTower = null;
    }

    private void AddAnimatedTowers(string towerTarget, AnimatedTower tower) 
    {
        if (AnimatedTowers.ContainsKey(towerTarget)) 
            return;

        AnimatedTowers[towerTarget] = tower;
        Add(tower.Sprite);
    }

    public class AnimatedTower 
    {
        public string In;
        public string Out;
        public string Selected;
        public string NotSelected;
        public Sprite<string> Sprite;

        public AnimatedTower(string ins, string outs, string selected, string notSelected, Sprite<string> sprite) 
        {
            In = ins;
            Out = outs;
            Selected = selected;
            NotSelected = notSelected;
            Sprite = sprite;
        }

        public void Select() 
        {
            Sprite.Play(In);
        }

        public void DeSelect() 
        {
            Sprite.Play(Out);
        }
    }
}