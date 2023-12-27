using System;

namespace FortRise;

public struct TrackInfo 
{
    public string Name;
    public string ResourcePath;
    public Type ResourceType;

    public TrackInfo(string name, string resourcePath, Type extension) 
    {
        Name = name;
        ResourcePath = resourcePath;
        ResourceType = extension;
    }

    public AudioTrack Create() 
    {
        var stream = RiseCore.ResourceTree.TreeMap[ResourcePath].Stream;
        if (ResourceType == typeof(RiseCore.ResourceTypeOggFile)) 
        {
            return new OggAudioTrack(stream);
        }
        if (ResourceType == typeof(RiseCore.ResourceTypeWavFile)) 
        {
            return new WavAudioTrack(stream);
        }

        Logger.Error("[TrackInfo] Unsupported Extension");
        return null;
    }
}