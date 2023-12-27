using Microsoft.Xna.Framework;
using MonoMod;

namespace TowerFall;

public class patch_EvilCrystal : EvilCrystal
{
    private Vector2[] nodes;

    public patch_EvilCrystal(Vector2 position, Facing facing, CrystalColors color, Vector2[] nodes) : base(position, facing, color, nodes)
    {
    }

    public extern void orig_ctor(Vector2 position, Facing facing, CrystalColors color, Vector2[] nodes);

    [MonoModConstructor]
    public void ctor(Vector2 position, Facing facing, CrystalColors color, Vector2[] nodes) 
    {
        orig_ctor(position, facing, color, nodes);
        if (this.nodes == null || this.nodes.Length == 0) 
        {
            this.nodes = new Vector2[4] 
            {
                new Vector2(position.X + 8, position.Y + 8),
                new Vector2(position.X - 8, position.Y - 8),
                new Vector2(position.X + 8, position.Y - 8),
                new Vector2(position.X - 8, position.Y + 8),
            };
        }
    }
}