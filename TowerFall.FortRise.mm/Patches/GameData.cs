using FortRise;
using FortRise.Adventure;

namespace TowerFall;

public static class patch_GameData 
{
    public static extern void orig_Load();

    public static void Load() 
    {
        // Should be safe to restart here
        RiseCore.CantRestart = false;
        RiseCore.Events.Invoke_OnBeforeDataLoad();
        orig_Load();
        ExtendedGameData.Load();

        // Assign its LevelID
        foreach (var questTowers in GameData.QuestLevels) 
        {
            var name = questTowers.Theme.Name;
            var correctName = StringUtils.ToTitleCase(name);
            questTowers.SetLevelID(correctName);
        }

        foreach (var versusTowers in GameData.VersusTowers) 
        {
            var name = versusTowers.Theme.Name;
            var correctName = StringUtils.ToTitleCase(name);
            versusTowers.SetLevelID(correctName);
        }

        foreach (var darkWorldTowers in GameData.DarkWorldTowers) 
        {
            var name = darkWorldTowers.Theme.Name;
            var correctName = StringUtils.ToTitleCase(name);
            darkWorldTowers.SetLevelID(correctName);
        }

        foreach (var trialTowers in GameData.TrialsLevels) 
        {
            var name = trialTowers.Theme.Name;
            var id = trialTowers.ID.Y switch {
                0 => "I",
                1 => "II",
                _ => "III"
            };
            var correctName = StringUtils.ToTitleCase(name) + " " + id;
            trialTowers.SetLevelID(correctName);
        }

        TFGame.WriteLineToLoadLog("Loading Adventure World Tower Data...");

        TowerRegistry.LoadQuest();
        TowerRegistry.LoadDarkWorld();
        TowerRegistry.LoadVersus();
        TowerRegistry.LoadTrials();

        TowerPatchRegistry.Initialize();

        patch_MapScene.FixedStatic();
        RiseCore.Events.Invoke_OnAfterDataLoad();
    }
}

