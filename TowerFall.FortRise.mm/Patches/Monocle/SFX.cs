using System.IO;
using FortRise;
using Microsoft.Xna.Framework.Audio;
using MonoMod;

namespace Monocle;

public class patch_SFX : SFX
{
    public SoundEffect Data
    {
        [MonoModIgnore] get => throw null;
        [MonoModIgnore] private set => throw null;
    }
    public bool ObeysMasterPitch
    {
        [MonoModIgnore] get => throw null;
        [MonoModIgnore] private set => throw null;
    }
    internal patch_SFX(bool obeysMasterPitch) :base("", false) {}
    public patch_SFX(string filename, bool obeysMasterPitch = true) : base(filename, obeysMasterPitch)
    {
    }
    public patch_SFX(Stream filename, bool obeysMasterPitch = true) : base("", obeysMasterPitch)
    {
    }

    [MonoModLinkTo("Monocle.SFX", "System.Void .ctor(System.Boolean)")]
    [MonoModIgnore]
    public void thisctor(bool obeysMasterPitch) {}

    [MonoModConstructor]
    public void ctor(Stream stream, bool obeysMasterPitch = true) 
    {
        thisctor(obeysMasterPitch);
        try
        {
            Data = SoundEffect.FromStream(stream);
        }
        catch (NoAudioHardwareException)
        {
            Data = null;
        }
    }

    [MonoModReplace]
    public override void Play(float panX = 160f, float volume = 1f)
    {
        if (Data != null && Audio.MasterVolume > 0f)
        {
            AddToPlayedList(panX, volume);
            volume *= Audio.MasterVolume;
            var instance = Data.CreateInstance();
            instance.Volume = volume;
            instance.Pitch = ObeysMasterPitch ? Audio.MasterPitch : 0f;
            instance.Pan = SFX.CalculatePan(panX);
            SoundEffectTracker.Track(instance);
            instance.Play();
        }
    }
}


public static class SFXExt 
{
    public static SFX CreateSFX(this FortContent content, string filename, bool obeysMasterPitch = true, ContentAccess contentAccess = ContentAccess.Root) 
    {
        if (Path.GetExtension(filename) == string.Empty)
            filename += ".wav";
        switch (contentAccess) 
        {
        case ContentAccess.Content: 
            filename = Calc.LOADPATH + filename;
            break;
        case ContentAccess.ModContent:
            {
                if (content == null) 
                {
                    Logger.Error("[Atlas] You cannot use SFXExt.CreateSFX while FortContent is null");
                    return null;
                }
                using var stream = content[filename].Stream;
                return CreateSFX(content, stream, obeysMasterPitch);
            }
        }
        using var fileStream = new FileStream(filename, FileMode.Open);
        return CreateSFX(content, fileStream, obeysMasterPitch);
    }

    public static SFX CreateSFX(this FortContent content, Stream stream, bool obeysMasterPitch = true) 
    {
        return new patch_SFX(stream, obeysMasterPitch);
    }
}