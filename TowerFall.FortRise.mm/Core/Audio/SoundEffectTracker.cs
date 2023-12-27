using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace FortRise;

public static class SoundEffectTracker 
{
    public static IReadOnlyList<SoundEffectInstance> TrackedSoundEffects => trackedSoundEffects;
    private static List<SoundEffectInstance> trackedSoundEffects = new(128);

    public static void Track(SoundEffectInstance instance) 
    {
        trackedSoundEffects.Add(instance);
    }

    public static void Update() 
    {
        for (int i = 0; i < trackedSoundEffects.Count; i++) 
        {
            var sound = trackedSoundEffects[i];
            if (sound == null) 
            {
                trackedSoundEffects.RemoveAt(i);
                i--;
                continue;
            }
            if (sound.State == SoundState.Stopped) 
            {
                sound.Dispose();
                trackedSoundEffects.RemoveAt(i);
                i--;
            }
        }
    }
}