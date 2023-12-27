using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace FortRise;


[Obsolete("This will be removed in FortRise 5")]
public static class SoundHelper 
{
    public static Dictionary<string, SoundEffectInstance> StoredInstance = new();
    private static SoundEffectInstance currentMusicPlaying;

    public static SoundEffect PathToSoundWithPrefix(string path, string prefix, out SoundEffectInstance instance) 
    {
        var themeSpan = path.AsSpan().Slice(7);
        var localPath = themeSpan.ToString();
        return PathToSound(localPath, out instance);
    }

    public static SoundEffect PathToSoundWithPrefix(string path, string path2, string prefix, out SoundEffectInstance instance) 
    {
        var themeSpan = path.AsSpan().Slice(path2.Length);
        var localPath = themeSpan.ToString();
        var allPath = Path.Combine(path, path2);
        return PathToSound(allPath, out instance);
    }

    public static SoundEffect PathToSound(string path, out SoundEffectInstance instance) 
    {
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var effect = SoundEffect.FromStream(fs);
        fs.Close();
        instance = effect.CreateInstance();
        instance.IsLooped = true;
        instance.Volume = Music.MasterVolume;
        return effect;
    }

    public static void AddToPool(string name, SoundEffectInstance instance) 
    {
        StoredInstance.Add(name, instance);
    }

    public static void PlayMusic(SoundEffectInstance instance) 
    {
        Music.Stop();
        currentMusicPlaying = instance;
        instance.Volume = Music.MasterVolume;
        instance.Play();
    }

    public static void PlaySound(SoundEffectInstance instance) 
    {
        instance.Volume = Audio.MasterVolume;
        instance.Play();
    }

    public static void StopMusic() 
    {
        currentMusicPlaying?.Stop();
        currentMusicPlaying = null;
    }

    public static void StopMusicImmediate() 
    {
        currentMusicPlaying?.Stop(true);
        currentMusicPlaying = null;
    }

    public static void RemoveFromPool(string name) 
    {
         if (StoredInstance.TryGetValue(name, out var instance)) 
        {
            StoredInstance.Remove(name);
            instance.Dispose();
        }       
    }

    public static void Dispose(string name) 
    {
        if (StoredInstance.TryGetValue(name, out var instance)) 
        {
            instance.Dispose();
        }
    }

    public static void RemoveAll() 
    {
        foreach (var storedInstance in StoredInstance) 
        {
            storedInstance.Value.Dispose();
        }
        StoredInstance.Clear();
    }
}