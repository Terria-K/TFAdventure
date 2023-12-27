using System.Xml;
using FortRise;
using MonoMod;

namespace TowerFall;

public class patch_LevelSystem : LevelSystem
{
    [MonoModIgnore]
    public extern override XmlElement GetNextRoundLevel(MatchSettings matchSettings, int roundIndex, out int randomSeed);

    [MonoModReplace]
    public override Background GetBackground(Level level)
    {
        return new Background(level, this.Theme.BackgroundData);
    }

    [MonoModReplace]
    public override Background GetForeground(Level level)
    {
        if (this.Theme.ForegroundData == null)
        {
            return null;
        }
        
        return new Background(level, this.Theme.ForegroundData);
    }
}