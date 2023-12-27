using System;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_VersusModeButton : VersusModeButton
{
    public static event Action ModeSwitch;
    private Wiggler iconWiggler;
    private static int currentIndex;
    public patch_VersusModeButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom)
    {
    }

    [MonoModLinkTo("TowerFall.BorderButton", "Update")]
    public void base_Update() 
    {
        base.Update();
    }

    [MonoModLinkTo("TowerFall.BorderButton", "OnConfirm")]
    protected void base_OnConfirm() 
    {
        base.OnConfirm();
    }

    public override void Update()
    {
        const int BuiltInModeCount = 3;
        base_Update();

        string currentModeName = patch_MainMenu.VersusMatchSettings.CurrentModeName;
        if (!Selected)
            return;
        
        if (currentIndex < GameModeRegistry.VersusGameModes.Count + BuiltInModeCount - 1 && MenuInput.Right)
        {
            currentIndex++;
            if (currentIndex < 3) 
            {
                patch_MainMenu.VersusMatchSettings.IsCustom = false;
                MainMenu.VersusMatchSettings.Mode = (Modes)currentIndex + BuiltInModeCount;
            }
            else 
            {
                patch_MainMenu.VersusMatchSettings.IsCustom = true;
                var gameMode = GameModeRegistry.VersusGameModes[currentIndex - BuiltInModeCount];
                MainMenu.VersusMatchSettings.Mode = gameMode.GameModeInternal;
                patch_MainMenu.VersusMatchSettings.CurrentModeName = gameMode.ID;
            }

            ModeSwitch?.Invoke();
            Sounds.ui_move2.Play(160f, 1f);
            iconWiggler.Start();
            base_OnConfirm();
            UpdateSides();
        }
        else if (currentIndex > 0 && MenuInput.Left)
        {
            currentIndex--;
            if (currentIndex < 3) 
            {
                patch_MainMenu.VersusMatchSettings.IsCustom = false;
                MainMenu.VersusMatchSettings.Mode = (Modes)currentIndex + BuiltInModeCount;
            }
            else 
            {
                patch_MainMenu.VersusMatchSettings.IsCustom = true;
                var gameMode = GameModeRegistry.VersusGameModes[currentIndex - BuiltInModeCount];
                MainMenu.VersusMatchSettings.Mode = gameMode.GameModeInternal;
                patch_MainMenu.VersusMatchSettings.CurrentModeName = gameMode.ID;
            }

            ModeSwitch?.Invoke();
            Sounds.ui_move2.Play(160f, 1f);
            iconWiggler.Start();
            base_OnConfirm();
            UpdateSides();
        }
    }

    private extern void orig_UpdateSides();

    private void UpdateSides() 
    {
        orig_UpdateSides();
        DrawRight = (currentIndex < GameModeRegistry.VersusGameModes.Count + 3 - 1);
        DrawLeft = currentIndex != 0;
    }

    public extern static Subtexture orig_GetModeIcon(Modes mode);

    public static Subtexture GetModeIcon(Modes mode)
    {
        if (patch_MainMenu.VersusMatchSettings.IsCustom && GameModeRegistry.TryGetGameMode(patch_MainMenu.VersusMatchSettings.CurrentModeName, out var gameMode)) 
        {
            return gameMode.Icon;
        }
        return orig_GetModeIcon(mode);
    }

    public extern static string orig_GetModeName(Modes mode);

    public static string GetModeName(Modes mode)
    {
        if (patch_MainMenu.VersusMatchSettings.IsCustom && GameModeRegistry.TryGetGameMode(patch_MainMenu.VersusMatchSettings.CurrentModeName, out var gameMode)) 
        {
            return gameMode.Name.ToUpperInvariant();
        }
        return orig_GetModeName(mode);
    }
}
