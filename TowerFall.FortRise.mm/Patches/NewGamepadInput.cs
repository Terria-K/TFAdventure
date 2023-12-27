using MonoMod;

namespace TowerFall;

[MonoModRemove]
public class patch_NewGamepadInput : NewGamepadInput
{
    public patch_NewGamepadInput(int gamepadID) : base(gamepadID)
    {
    }
}