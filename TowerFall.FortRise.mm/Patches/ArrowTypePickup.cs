using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_ArrowTypePickup : ArrowTypePickup
{
    private GraphicsComponent graphic;
    private ArrowTypes arrowType;
    public patch_ArrowTypePickup(Vector2 position, Vector2 targetPosition, ArrowTypes type) : base(position, targetPosition, type)
    {
    }

    public extern void orig_ctor(Vector2 position, Vector2 targetPosition, ArrowTypes type);

    [MonoModConstructor]
    public void ctor(Vector2 position, Vector2 targetPosition, ArrowTypes type)
    {
        orig_ctor(position, targetPosition, type);
        if (type <= ArrowTypes.Prism)
            return;
        if (!RiseCore.ArrowNameMap.TryGetValue(type, out var id)) 
            return;
        
        var info = RiseCore.ArrowsRegistry[id].InfoLoader?.Invoke();
        if (info == null)
            return;
        var value = info.Value;
        
        if (value.Animated != null)
        {
            graphic = value.Animated;
            Add(graphic);
            return;
        }
        if (value.Simple != null)
        {
            graphic = value.Simple;
            Add(graphic);
        }
    }

    private void PlaySound()
    {
        switch (arrowType)
        {
        case ArrowTypes.Normal:
            Sounds.pu_plus2Arrows.Play(X, 1f);
            return;
        case ArrowTypes.Bomb:
            Sounds.pu_bombArrow.Play(X, 1f);
            return;
        case ArrowTypes.SuperBomb:
            Sounds.pu_superBomb.Play(X, 1f);
            return;
        case ArrowTypes.Laser:
            Sounds.pu_laserArrow.Play(X, 1f);
            return;
        case ArrowTypes.Bramble:
            Sounds.pu_brambleArrow.Play(X, 1f);
            return;
        case ArrowTypes.Drill:
            Sounds.pu_drill.Play(X, 1f);
            return;
        case ArrowTypes.Bolt:
            Sounds.pu_boltArrow.Play(X, 1f);
            return;
        case ArrowTypes.Feather:
            Sounds.pu_feather.Play(X, 1f);
            return;
        case ArrowTypes.Trigger:
            Sounds.pu_triggerArrow.Play(X, 1f);
            return;
        case ArrowTypes.Prism:
            Sounds.pu_prismArrow.Play(X, 1f);
            return;
        default:
            if (!RiseCore.ArrowNameMap.TryGetValue(arrowType, out var id)) 
                return;
            var info = RiseCore.ArrowsRegistry[id].InfoLoader?.Invoke();
            if (info == null)
                return;
            var value = info.Value;
            var sfx = value.PickupSound ?? Sounds.pu_plus2Arrows;
            sfx.Play(X, 1f);
            return;
        }
    }
}