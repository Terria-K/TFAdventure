using System;
using FortRise;
using MonoMod;

namespace TowerFall;

// Some users might have a itch, Humble or GOG version of the game.
[MonoModIfFlag("Steamworks")]
public class patch_CustomLevelSystem : CustomLevelSystem
{
    private int[] treasureMask;
    public patch_CustomLevelSystem(string file) : base(file)
    {
    }
    public extern void orig_ctor(string file);

    [MonoModConstructor]
    public void ctor(string file) 
    {
        orig_ctor(file);
        // Resize so we don't get any error
        Array.Resize<int>(ref treasureMask, treasureMask.Length + RiseCore.PickupRegistry.Count + 1);
    }
}