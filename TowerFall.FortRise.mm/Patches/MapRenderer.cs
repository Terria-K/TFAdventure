using FortRise;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_MapRenderer : MapRenderer
{
    private MapRendererNode node;
    private Sprite<string> twilightSpire;
    private Sprite<string> sunkenCity;
    private Sprite<string> towerForge;
    private Sprite<string> ascension;
    private Sprite<string> theAmaranth;
    private Sprite<string> dreadwood;
    private Sprite<string> darkfang;
    private Sprite<string> cataclysm;

    // The devs does not make these properties instance instead of static.
    // Fine, I'll do it myself.
    public int GetInstanceWidth() 
    {
        if (node != null && node.Land != null) 
        {
            return node.Land.Width;
        }
        return MapRenderer.Width;
    }

    public int GetInstanceHeight() 
    {
        if (node != null && node.Land != null) 
        {
            return node.Land.Height;
        }
        return MapRenderer.Height;
    }

    public Vector2 GetInstanceCenter() 
    {
        if (node != null && node.Land != null) 
        {
            return node.Land.HalfSize;
        }
        return MapRenderer.Center;
    }


    public patch_MapRenderer(bool forceMoonstone) : base(forceMoonstone)
    {
    }

    public extern void orig_OnSelectionChange(string towerName);
    public extern void orig_OnStartSelection(string towerName);

    public void OnSelectionChange(string towerName) 
    {
        var scene = Scene as MapScene;
        var levelSet = scene.GetLevelSet();
        if (levelSet == "TowerFall") 
        {
            orig_OnSelectionChange(towerName);
            return;
        }
        if (ExtendedGameData.InternalMapRenderers.TryGetValue(levelSet, out var node)) 
        {
            node.StartSelection(towerName);
        }
    }
    public void OnStartSelection(string towerName)
    {
        orig_OnStartSelection(towerName);
    }

    [MonoModLinkTo("Monocle.CompositeComponent", "System.Void Render()")]
    [MonoModIgnore]
    public void base_Render() { base.Render(); }

    public extern void orig_Render();

    public override void Render()
    {
        if (node != null && node.Water != null) 
        {
            Draw.SineTextureV(node.Water, Entity.Position, new Vector2(5f, 0f), Vector2.One, 0f, Color.White, 
                SpriteEffects.None, Scene.FrameCounter * 0.03f, 2f, 1, 0.3926991f);
            base_Render();           
            return;
        }
        orig_Render();
    }

    public void ChangeLevelSet(string levelSet) 
    {
        if (node != null) 
        {
            node.Deselection();
            Remove(node);
            node = null;
        }
        if (levelSet == null || levelSet == "TowerFall") 
        {
            ToggleAllMainElements(true);
            return;
        }
        if (ExtendedGameData.InternalMapRenderers.TryGetValue(levelSet, out var val)) 
        {
            node = val;
            Add(node); 
            ToggleAllMainElements(false);
            return;
        }
        ToggleAllMainElements(true);
    }

    public void ToggleAllMainElements(bool toggle) 
    {
        twilightSpire.Visible = toggle;
        sunkenCity.Visible = toggle;
        towerForge.Visible = toggle;
        ascension.Visible = toggle;
        theAmaranth.Visible = toggle;
        dreadwood.Visible = toggle;
        darkfang.Visible = toggle;
        cataclysm.Visible = toggle;
    }
}