using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace TowerFall;

public class patch_JumpPad : JumpPad
{
    private Entity[] surfaceEntities;
    private Image[] images;
    public patch_JumpPad(Vector2 position, int width) : base(position, width)
    {
    }

    // If JumpPad is spawned after the level loaded, it will throw a NullReferenceException at Update.
    // which is a problem when hot reloading the level.
    // To prevent that, we initialized the surfaceEntities.
    public override void Added()
    {
        base.Added();
        if (Scene.HasBegun && surfaceEntities == null) 
        {
            var x = 0f;
			Func<SnowClump, bool> findSnowClump = s => s.X == x && s.Y == Y;
			surfaceEntities = new Entity[images.Length];
			for (int i = 0; i < surfaceEntities.Length; i++)
			{
				x = X + (float)(i * 10);
				surfaceEntities[i] = Scene.Layers[LayerIndex].FindFirst<SnowClump>(findSnowClump);
			}
        }
    }
}