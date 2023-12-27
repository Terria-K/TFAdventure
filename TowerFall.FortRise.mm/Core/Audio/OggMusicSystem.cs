using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace FortRise;


public class OggMusicSystem : IMusicSystem
{
    private AudioTrack current;

    public void Pause()
    {
        current?.Pause();
    }

    public void Play(string name)
    {
        Stop(AudioStopOptions.Immediate);

        if (patch_Audio.TryGetTrackMap(name, out var info)) 
        {
            current = info.Create();
            current.Looping = true;
            current.Play();
            return;
        }
        Logger.Error($"[OGG Music System] No audio file named '{name}' exists on the Music Folder"); 
    }

    public void Play(TrackInfo trackInfo)
    {
        current = trackInfo.Create();
        current.Looping = true;
        current.Play();
    }

    public void Resume()
    {
        current?.Resume();
    }

    public void Seek(uint seekFrames)
    {
        current?.Seek(seekFrames);
    }

    public void Stop(AudioStopOptions options)
    {
        if (current != null) 
        {
            current.Stop(true);
            current.Dispose();
            current = null;
        }
    }
}