using Microsoft.Xna.Framework.Audio;

namespace FortRise;

public interface IMusicSystem 
{
    void Play(string name);
    void Play(TrackInfo trackInfo);
    void Resume();
    void Pause();
    void Stop(AudioStopOptions options);
    void Seek(uint seekFrames);
}