using System;
using System.Collections.Generic;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoMod;

namespace Monocle;

[MonoModPatch("MInput")]
[MonoModIfFlag("OS:Windows")]
public static class DInputRemoval 
{
    [MonoModRemove]
    private static object DirectInput;
    [MonoModRemove]
    public static List<MInput.JoystickData> Joysticks;

    [MonoModReplace]
    public static string[] LogJoysticks()
    {
        return Array.Empty<string>();
    }

    [MonoModPatch("JoystickData")]
    [MonoModRemove]
    public class JoystickData {}
} 

public static class patch_MInput 
{
    public static patch_KeyboardData Keyboard;
    public static patch_MouseData Mouse;
    public static List<patch_XGamepadData> XGamepads;

    [MonoModReplace]
    internal static void Initialize() 
    {
        Keyboard = new patch_KeyboardData();
        Mouse = new patch_MouseData();
        XGamepads = new List<patch_XGamepadData>();
        UpdateJoysticks();
        foreach (var gamepad in MInput.XGamepads) 
        {
            gamepad.StopRumble();
        }
    }

    [MonoModReplace]
    internal static void Update() 
    {
        if (Engine.Instance.IsActive) 
        {
            if (Engine.Instance.Commands.Open)
                Keyboard.UpdateNull();
            else
                Keyboard.Update();
            
            Mouse.Update();
            if (MInput.UpdateXInput) 
            {
                foreach (var gamepadData in XGamepads) 
                {
                    gamepadData.Update(true);
                }
            }
        }
        else 
        {
            Keyboard.UpdateNull();
            if (MInput.UpdateXInput) 
            {
                foreach (var gamepad in XGamepads) 
                {
                    gamepad.Update(false);
                }
            }
        }
        MInput.GamepadsChanged = false;
    }

    [MonoModReplace]
    internal static void UpdateJoysticks()
    {
        for (int i = 0; i < XGamepads.Count; i++)
        {
            var xGamepadData = XGamepads[i];
            if (!xGamepadData.Attached)
            {
                Logger.Info("Removed XGamepad: " + xGamepadData);
                MInput.GamepadsChanged = true;
                XGamepads[i].Dispose();
                MInput.XGamepads.RemoveAt(i);
                i--;
            }
        }
        if (MInput.XGamepads.Count >= 4)
            return;
        
        for (int i = MInput.XGamepads.Count; i < 4; i++)
        {
            var playerIndex = (PlayerIndex)i;
            if (GamePad.GetState(playerIndex).IsConnected)
            {
                var gamepadData = new MInput.XGamepadData(playerIndex);
                MInput.XGamepads.Add(gamepadData);
                Logger.Info("Add XGamepad: " + gamepadData);
                MInput.GamepadsChanged = true;
            }
        }
    }

    [MonoModReplace]
    internal static void Shutdown() 
    {
        foreach (var gamepad in MInput.XGamepads) 
        {
            gamepad.StopRumble();
        }
        Keyboard.Dispose();
    }

    public class patch_XGamepadData
    {
        public bool Attached
        {
            [MonoModIgnore]
            get => false;
        }
        [MonoModIgnore]
        internal extern void Update(bool focus);

        [MonoModIgnore]
        internal extern void Dispose();
    }

    public class patch_MouseData 
    {
        [MonoModIgnore]
        internal extern void UpdateNull();

        [MonoModIgnore]
        internal extern void Update();

        [MonoModIgnore]
        internal extern void Dispose();
    }

    public class patch_KeyboardData 
    {
        [MonoModIgnore]
        internal extern void UpdateNull();

        [MonoModIgnore]
        internal extern void Update();

        [MonoModIgnore]
        internal extern void Dispose();
    }
}