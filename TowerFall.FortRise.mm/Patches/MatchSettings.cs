using System;
using FortRise;
using MonoMod;

namespace TowerFall;

public class patch_MatchSettings : MatchSettings
{
    public bool IsCustom;
    public string CurrentModeName;
    public patch_Modes Mode;
    public patch_MatchSettings.patch_MatchLengths MatchLength;
    private static readonly float[] GoalMultiplier;
    public static int CustomGoal { get; set; } = 1;
    public FortRise.CustomGameMode CurrentCustomGameMode 
    {
        get 
        {
            if (GameModeRegistry.TryGetGameMode(CurrentModeName, out var mode)) 
            {
                return mode;
            }
            return null;
        }
    } 

    public extern int orig_get_GoalScore();

    public int get_GoalScore() 
    {
        if (IsCustom || MatchLength == patch_MatchLengths.Custom) 
        {
            var matchLength = (int)MatchLength;
            if (matchLength >= GoalMultiplier.Length) 
            {
                return patch_MatchSettings.CustomGoal;
            }
            int goals = (int)PlayerGoals(5, 8, 10);
            return (int)Math.Ceiling(((float)goals * GoalMultiplier[(int)MatchLength]));
        }
        return orig_get_GoalScore();
    }

    [MonoModReplace]
    public bool get_TeamMode() 
    {
        if (IsCustom) 
        {
            var gameMode = CurrentCustomGameMode;
            if (gameMode != null) 
            {
                return gameMode.TeamMode;
            }
        }
        return Mode == patch_Modes.TeamDeathmatch;
    }

    [MonoModIgnore]
    private extern int PlayerGoals(int p2goal, int p3goal, int p4goal);

    public patch_MatchSettings(LevelSystem levelSystem, Modes mode, MatchLengths matchLength) : base(levelSystem, mode, matchLength)
    {
    }

    public enum patch_MatchLengths { Instant, Quick, Standard, Epic, Custom }
}