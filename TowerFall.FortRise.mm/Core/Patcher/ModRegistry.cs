using TowerFall;

namespace FortRise;

public static class ModRegisters
{
    public static TowerFall.Modes GameModeType<T>() 
    where T : CustomGameMode
    {
        if (GameModeRegistry.GameModeTypes.TryGetValue(typeof(T), out var idx))
            return GameModeRegistry.VersusGameModes[idx].GameModeInternal;
        return TowerFall.Modes.LastManStanding;
    }

    public static ArrowTypes ArrowType<T>() 
    where T : Arrow
    {
        if (ArrowsRegistry.Types.TryGetValue(typeof(T), out var val))
            return val;
        return ArrowTypes.Normal;
    }

    public static Pickups PickupType<T>() 
    where T : Pickup
    {
        if (PickupsRegistry.Types.TryGetValue(typeof(T), out var val))
            return val;
        return Pickups.Arrows;
    }
}