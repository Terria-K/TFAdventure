using FortRise;
using MonoMod;

namespace TowerFall;

public class patch_VersusMapButton : VersusMapButton
{
    public patch_VersusMapButton(VersusTowerData tower) : base(tower)
    {
    }
}

