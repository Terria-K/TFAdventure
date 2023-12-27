using Microsoft.Xna.Framework;
using MonoMod;

namespace TowerFall.Editor;

public class patch_OverlayTreasureDisplay : OverlayTreasureDisplay
{
    public patch_OverlayTreasureDisplay(Vector2 position) : base(position)
    {
    }

    [MonoModReplace]
    public float[] ApplyRates(int[] rates)
    {
        var array = new float[]
        {
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
            0.5f, 0.5f, 0.5f, 0.25f, 0.15f, 0.15f, 0.15f, 0.15f, 0.001f, 0.1f
        };
        for (int i = 0; i < rates.Length; i++)
        {
            array[i] *= rates[i];
        }
        TreasureSpawner.AdjustTreasureRatesForSpecialArrows(array, Editor.Tower.TreasureArrowChance);
        float num = array[2];
        for (int i = 3; i <= 6; i++)
        {
            array[i - 1] = array[i];
        }
        array[6] = num;
        return ConvertToSizes(array);
    }

    [MonoModIgnore]
    private extern float[] ConvertToSizes(float[] from);
}