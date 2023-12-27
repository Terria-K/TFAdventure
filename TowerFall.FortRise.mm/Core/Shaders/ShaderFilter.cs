using Microsoft.Xna.Framework.Graphics;
using TowerFall;

namespace FortRise;

public abstract class ShaderFilter 
{
    public RenderTarget2D ForegroundRenderTarget;
    public Level Level;
    public LevelTiles SolidTiles;
    public LevelBGTiles BGTiles;


    public abstract void BeforeRender(RenderTarget2D canvas);
    public abstract void Render(RenderTarget2D canvas);
    public abstract void AfterRender(RenderTarget2D canvas);

    public virtual void Activated(LevelRenderData data) 
    {
        ForegroundRenderTarget = data.ForegroundRenderTarget;
        Level = data.Level;
        SolidTiles = data.SolidTiles;
        BGTiles = data.BGTiles;
    }

    public virtual void Deactivated() {}

    public struct LevelRenderData 
    {
        public Level Level;
        public RenderTarget2D ForegroundRenderTarget;
        public LevelTiles SolidTiles;
        public LevelBGTiles BGTiles;
    }
}