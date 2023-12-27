using FortRise;

namespace TowerFall;

public class patch_VersusTowerData : VersusTowerData 
{
    public float[] TreasureChances;
    public bool NoPatching;
    internal VersusTowerPatchContext InternalPatch;

    public int[] ModTreasureMask() 
    {
        if (!NoPatching && InternalPatch != null) 
        {
            return InternalPatch.ReceivePatch(this.GetLevelID());
        }
        return TreasureMask;
    }
}

public static class VersusTowerDataExt 
{
    public static void ApplyPatch(this VersusTowerData data, VersusTowerPatchContext ctx) 
    {
        ((patch_VersusTowerData)data).InternalPatch = ctx;
    }

    public static float[] GetTreasureChances(this VersusTowerData data) 
    {
        var cast = ((patch_VersusTowerData)data);
        return cast.TreasureChances ?? TreasureSpawner.DefaultTreasureChances;
    }

    public static void SetTreasureChances(this VersusTowerData data, float[] treasureChance) 
    {
        ((patch_VersusTowerData)data).TreasureChances = treasureChance;
    }
}