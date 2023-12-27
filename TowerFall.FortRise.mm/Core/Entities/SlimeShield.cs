using Microsoft.Xna.Framework;
using TowerFall;

namespace FortRise;

public class ShieldSlime : Slime
{
    private PlayerShield shield;
    public ShieldSlime(Vector2 position, Facing facing, SlimeColors slimeColors) : base(position, facing, slimeColors)
    {
        shield = new PlayerShield(this);
        Add(shield);
    }

    public override void Added() 
    {
        base.Added();
        if (shield)
            shield.Gain();
    }

    public static Enemy SlimeS(Vector2 position, Facing facing) 
    {
        return new ShieldSlime(position, facing, SlimeColors.Green);
    }

    public static Enemy RedSlimeS(Vector2 position, Facing facing) 
    {
        return new ShieldSlime(position, facing, SlimeColors.Red);
    }

    public static Enemy BlueSlimeS(Vector2 position, Facing facing) 
    {
        return new ShieldSlime(position, facing, SlimeColors.Blue);
    }

    public override void Hurt(Vector2 force, int damage, int killerIndex, Arrow arrow = null, Explosion explosion = null, ShockCircle shock = null)
    {
        if (this.shield != null) 
        {
            Speed = force;
            base.Flash(30, null);
            shield.Lose();
            Remove(shield);
            shield = null;
            if (arrow != null) 
            {
                arrow.EnterFallMode(true, false, true);
                return;
            }
            return;
        }
        base.Hurt(force, damage, killerIndex, arrow, explosion, shock);
    }
}