using System;
using System.IO;
using MonoMod;
using SDL2;
using Steamworks;
using TowerFall;

namespace FortRise;

[MonoModIfFlag("Steamworks")]
public static class SteamClient 
{
    public static bool Init() 
    {
        Logger.Info("Initializing Steam...");
        Logger.Info("Setting up steam app ID to " + TFGame.STEAM_ID);
        SetAppID(TFGame.STEAM_ID);
        bool steamInit = SteamAPI.Init();
        if (steamInit) 
        {
            SteamUserStats.RequestCurrentStats();
            SteamUserStats.RequestGlobalStats(7);
        }
        else 
        {
			SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "SAVE THIS MESSAGE!", "Couldn't find Steam!", IntPtr.Zero);
        }
        return steamInit;
    }

    private static void SetAppID(AppId_t appId) 
    {
        File.WriteAllText("steam_appid.txt", appId.ToString());
    }
}