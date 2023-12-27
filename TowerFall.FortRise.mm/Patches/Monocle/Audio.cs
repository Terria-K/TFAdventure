using System.Collections.Generic;
using System.IO;
using FortRise;
using Microsoft.Xna.Framework.Audio;

namespace Monocle;

public static class patch_Audio 
{
    // Compat together for 1.3.3.1
    public static string ORIGINAL_LOAD_PREFIX = Calc.LOADPATH + "SFX" + Path.DirectorySeparatorChar.ToString();
    public static Dictionary<string, IMusicSystem> AudioSystems = new();
    public static Dictionary<string, TrackInfo> TrackMap = new();
    private static IMusicSystem currentSystem;
    internal static List<SFX> loopList;

    internal static void InitMusicSystems() 
    {
        RegisterMusicSystem(new WavMusicSystem(), ".wav");
        RegisterMusicSystem(new OggMusicSystem(), ".ogg");
        RegisterMusicSystem(new VanillaMusicSystem(), ".vanilla");
    }

    public static bool TryGetTrackMap(string name, out TrackInfo info) 
    {
        if (TrackMap.TryGetValue(name, out info))
            return true;
        return false;
    }

    public static void PlayMusic(IMusicSystem system, string name) 
    {
        system.Play(name);
        currentSystem = system;
    }

    public static void PlayMusic(IMusicSystem system, TrackInfo info) 
    {
        system.Play(info);
        currentSystem = system;
    }

    public static void RegisterMusicSystem(IMusicSystem system, string associatedExtension) 
    {
        if (AudioSystems.ContainsKey(associatedExtension)) 
        {
            Logger.Warning($"[Audio System] Conflicting system extensions '{associatedExtension}'. Replacing what comes last.");
        }
        AudioSystems[associatedExtension] = system;
    }

    public static IMusicSystem GetMusicSystemFromExtension(string musicPath) 
    {
        var ext = Path.GetExtension(musicPath);
        if (string.IsNullOrEmpty(ext))
            return AudioSystems[".vanilla"];
        if (AudioSystems.TryGetValue(ext, out var audio)) 
        {
            return audio;
        }
        Logger.Warning($"There is no associated extension with this music path: '{musicPath}'. Can't find the AudioSystem.");
        return null;
    }

    public static void StopMusic(AudioStopOptions options) 
    {
        if (currentSystem != null) 
        {
            currentSystem.Stop(options);
            currentSystem = null;
        }
    }
}