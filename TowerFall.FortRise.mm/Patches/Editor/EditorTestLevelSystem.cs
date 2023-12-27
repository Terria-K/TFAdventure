using System;
using FortRise;
using MonoMod;

namespace TowerFall.Editor;

public class patch_EditorTestLevelSystem : EditorTestLevelSystem
{
    public patch_EditorTestLevelSystem(Tower tower, EditorScene testFrom = null) : base(tower, testFrom)
    {
    }

    [MonoModReplace]
    public override TreasureSpawner GetTreasureSpawner(Session session)
    {
        int[] treasureMask = new int[Tower.TreasureMask.Length];
        Array.Copy(Tower.TreasureMask, treasureMask, Tower.TreasureMask.Length);
        Array.Resize(ref treasureMask, treasureMask.Length + RiseCore.PickupRegistry.Count + 1);
        return new TreasureSpawner(session, treasureMask, Tower.TreasureArrowChance, false);
    }
}