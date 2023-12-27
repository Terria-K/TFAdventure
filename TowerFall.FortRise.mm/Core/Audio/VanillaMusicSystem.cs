using System.IO;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace FortRise;

public class VanillaMusicSystem : IMusicSystem
{
    private SoundBank soundBank;
    private AudioCategory audioCategory;
    public VanillaMusicSystem() 
    {
        soundBank = MusicExt.GetSoundBank();
        audioCategory = MusicExt.GetAudioCategory();
    }

    public void Add(string name, Stream stream) {}

    public void Pause()
    {
        audioCategory.Pause();
    }

    public void Play(string name)
    {
        if (MusicExt.GetAudioEngine() == null)
            return;
        soundBank.PlayCue(name);
    }

    public void Play(TrackInfo trackInfo)
    {
        Logger.Log("[FACT Music System] Playing a trackInfo type music is not supported!");
    }

    public void Resume()
    {
        audioCategory.Resume();
    }

    public void Seek(uint seekFrames)
    {
        Logger.Log("[FACT Music System] Seek is not supported!");
    }

    public void Stop(AudioStopOptions options)
    {
        if (MusicExt.GetAudioEngine() == null)
            return;
        audioCategory.Stop(options);
    }
}