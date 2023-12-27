using Microsoft.Xna.Framework;

namespace TowerFall;

public abstract class patch_DarkWorldBoss : DarkWorldBoss
{
    public patch_DarkWorldBoss(Vector2 position, Facing facing, int states, int health, int difficulty) : base(position, facing, states, health, difficulty)
    {
    }

    public virtual string BossMusic => "DarkBoss";
}