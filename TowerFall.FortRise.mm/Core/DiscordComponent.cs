using System;
using System.Collections;
using System.Collections.Generic;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using TowerFall;

namespace FortRise;

public class DiscordComponent : GameComponent
{
    public static DiscordComponent Instance;
    private Discord.Discord DiscordInstance;
    private Discord.Activity NextPresence;
    private bool dirty;

    private const string FortRiseIcon = "https://i.imgur.com/jMyu0Hl.png";

    public DiscordComponent(Game game) : base(game)
    {
        UpdateOrder = -50000;

        Logger.Verbose("Initializing Discord game SDK");
        try 
        {
            DiscordInstance = new Discord.Discord(1139944748220690532L, (ulong)Discord.CreateFlags.NoRequireDiscord);
        }
        catch (Exception e) 
        {
            Logger.Error("Could not initialize discord game sdk");
            Logger.Error(e.ToString());
            return;
        }

        DiscordInstance.SetLogHook(Discord.LogLevel.Debug, (level, mess) => {
            Logger.Log("[DISCORD] " + mess);
        });

        RiseCore.Events.OnLevelLoaded += OnLevelLoaded;
        RiseCore.Events.OnMainBegin += OnMainBegin;
        RiseCore.Events.OnMapBegin += OnMapBegin;
        RiseCore.Events.OnQuestSpawnWave += OnQuestSpawnWave;
        RiseCore.Events.OnQuestRegisterEnemyKills += OnQuestRegisterEnemyKills;
        TFGame.Instance.Exiting += OnGameExit;

        TFGame.Instance.Components.Add(this);

        Logger.Info("[DISCORD] Discord Game SDK initialized");
    }

    protected override void Dispose(bool disposing)
    {
        if (Instance == null)
            return;

        base.Dispose(disposing);

        DiscordInstance?.Dispose();

        RiseCore.Events.OnLevelLoaded -= OnLevelLoaded;
        RiseCore.Events.OnMainBegin -= OnMainBegin;
        RiseCore.Events.OnMapBegin -= OnMapBegin;
        RiseCore.Events.OnQuestSpawnWave -= OnQuestSpawnWave;
        RiseCore.Events.OnQuestRegisterEnemyKills -= OnQuestRegisterEnemyKills;
        TFGame.Instance.Exiting -= OnGameExit;

        Instance = null;
        TFGame.Instance.Components.Remove(this);


        Logger.Info("[DISCORD] Discord Game SDK disposed");
    }

    private void OnQuestRegisterEnemyKills(QuestRoundLogic logic, Vector2 vector, int arg3, int arg4)
    {
        if (logic.GauntletCounter == null)
            return;
        var session = logic.Session;
        var levelID = (session.MatchSettings.LevelSystem as QuestLevelSystem).QuestTowerData.GetLevelID() ?? "Official Level";
        var index = levelID.IndexOf("/");
        if (index != -1) 
        {
            levelID = levelID.Substring(index + 1);
        }

        NextPresence = new Discord.Activity {
            Details = "Playing " + levelID,
            State = "Enemies: " + logic.GauntletCounter.Amount + " | " + "Players: " + TFGame.PlayerAmount
        };
        NextPresence.Assets.LargeText = "FortRise";
        NextPresence.Assets.LargeImage = FortRiseIcon;
        NextPresence.Assets.SmallImage = GetQuestDifficulty(logic.Session.MatchSettings);
        NextPresence.Assets.SmallText = "TowerFall";

        dirty = true;
    }

    private void OnQuestSpawnWave(QuestControl control, int waveNum, List<IEnumerator> groups, int[] floors, bool dark, bool slow, bool scroll)
    {
        var session = control.Level.Session;
        var levelID = (session.MatchSettings.LevelSystem as QuestLevelSystem).QuestTowerData.GetLevelID() ?? "Official Level";
        var index = levelID.IndexOf("/");
        if (index != -1) 
        {
            levelID = levelID.Substring(index + 1);
        }

        NextPresence = new Discord.Activity {
            Details = "Playing " + levelID + " | " + "Wave: " + (waveNum + 1),
            State = "Players: " + TFGame.PlayerAmount
        };
        NextPresence.Assets.LargeText = "FortRise";
        NextPresence.Assets.LargeImage = FortRiseIcon;
        NextPresence.Assets.SmallImage = GetQuestDifficulty(control.Level.Session.MatchSettings);
        NextPresence.Assets.SmallText = "Quest";

        dirty = true;
    }

    private void OnMapBegin(MapScene scene)
    {
        NextPresence = new Discord.Activity {
            Details = "Selecting a Tower"
        };

        NextPresence.Assets.LargeText = "FortRise";
        NextPresence.Assets.LargeImage = FortRiseIcon;
        NextPresence.Assets.SmallImage = GetMap(scene.Mode);
        NextPresence.Assets.SmallText = scene.Mode.ToString();

        dirty = true;

        static string GetMap(MainMenu.RollcallModes mode) 
        {
            return mode switch 
            {
                MainMenu.RollcallModes.DarkWorld => "darkworldmap",
                MainMenu.RollcallModes.Quest => "questmap",
                MainMenu.RollcallModes.Versus => "versus",
                MainMenu.RollcallModes.Trials => "trials",
                _ => "hardcore"
            };
        }
    }

    private void OnMainBegin(MainMenu menu)
    {
        NextPresence = new Discord.Activity {
            Details = "On Lobby"
        };

        NextPresence.Assets.LargeText = "FortRise";
        NextPresence.Assets.LargeImage = FortRiseIcon;
        NextPresence.Assets.SmallImage = "versus";
        NextPresence.Assets.SmallText = "TowerFall";

        dirty = true;
    }

    public static void ChangePresence(Discord.Activity activity) 
    {
        if (Instance == null) 
        {
            Logger.Error("[Discord Presence] Discord Game SDK is not initialized yet.");
            return;
        }
        Instance.NextPresence = activity;
    }

    private void OnGameExit(object sender, EventArgs e)
    {
        Dispose();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (dirty) 
        {
            DiscordInstance.GetActivityManager().UpdateActivity(NextPresence, (result) => {
                if (result == Discord.Result.Ok) 
                    Logger.Verbose("Presence changed successfully!");
                else 
                    Logger.Warning($"Failed to change presence: {result}") ;
            });
            dirty = false;
        }

        try 
        {
            DiscordInstance.RunCallbacks();
        } 
        catch (Discord.ResultException e) 
        {
            if (e.Message == nameof(Discord.Result.NotRunning)) 
            {
                Logger.Warning("[DISCORD] Discord was shut down! Disposing Game SDK.");
                Dispose();
                return;
            }
            throw;
        }
    }

    public static DiscordComponent Create() 
    {
        if (Instance != null)
            return Instance;
        
        Logger.Info("Creating Discord SDK Instance");
        
        var component = new DiscordComponent(TFGame.Instance);
        if (component.DiscordInstance != null) 
        {
            Instance = component;
        }

        return Instance;
    }


    private void OnLevelLoaded(RoundLogic logic)
    {
        var session = logic.Session;
        var levelSystem = session.MatchSettings.LevelSystem;
        switch (levelSystem) 
        {
        case DarkWorldLevelSystem dwSystem:
        {
            var levelID = dwSystem.DarkWorldTowerData.GetLevelID() ?? "Official Level";
            var index = levelID.IndexOf("/");
            if (index != -1) 
            {
                levelID = levelID.Substring(index + 1);
            }
            var deaths = session.DarkWorldState.Deaths;
            int totalDeaths = 0;
            foreach (var death in deaths) 
            {
                totalDeaths += death;
            }

            var enemyKills = session.DarkWorldState.EnemyKills;
            var totalKills = 0;
            foreach (var enemyKill in enemyKills) 
            {
                totalKills += enemyKill;
            }
            NextPresence = new Discord.Activity {
                Details = "Playing " + levelID + " | " + "Level: " + (session.RoundIndex + 1),
                State = "Total Deaths: " + totalDeaths + " | " + "Total Kills: " + totalKills
            };

            NextPresence.Assets.SmallImage = GetDarkWorldDifficulty(session.MatchSettings.DarkWorldDifficulty);
            NextPresence.Assets.SmallText = "Dark World";
        }
            break;
        case VersusLevelSystem versus:
        {
            var levelID = versus.VersusTowerData.GetLevelID() ?? "Official Level";
            var index = levelID.IndexOf("/");
            if (index != -1) 
            {
                levelID = levelID.Substring(index + 1);
            }

            ulong totalKills = 0;
            ulong totalDeaths = 0;

            foreach (var stats in session.MatchStats) 
            {
                totalKills += stats.Kills.Kills;
                totalDeaths += stats.Deaths.Kills;
            }

            NextPresence = new Discord.Activity {
                Details = "Playing " + levelID + " | " + "Round: " + (session.RoundIndex + 1),
                State = "Players: " + TFGame.PlayerAmount + " Total Deaths: " + totalDeaths + " | " + "Total Kills: " + totalKills
            };

            NextPresence.Assets.SmallImage = "versus";
            NextPresence.Assets.SmallText = "Versus";
        }
            break;
        case QuestLevelSystem questSystem:
        {
            var levelID = questSystem.QuestTowerData.GetLevelID() ?? "Official Level";
            var index = levelID.IndexOf("/");
            if (index != -1) 
            {
                levelID = levelID.Substring(index + 1);
            }

            NextPresence = new Discord.Activity {
                Details = "Playing " + levelID,
                State = "Players: " + TFGame.PlayerAmount
            };
            NextPresence.Assets.SmallImage = GetQuestDifficulty(logic.Session.MatchSettings);
            NextPresence.Assets.SmallText = "Quest";
        }
            break;
        case TrialsLevelSystem trialsSystem:
        {
            var trialTowerData = trialsSystem.TrialsLevelData;
            int typeCompletion = 0;
            string bestTime = "";
            if (trialTowerData.IsOfficialLevelSet()) 
            {
                var data = SaveData.Instance.Trials.Levels[trialTowerData.ID.X][trialTowerData.ID.Y];
                if (data.UnlockedGold)
                    typeCompletion++;
                if (data.UnlockedDiamond)
                    typeCompletion++;
                if (data.UnlockedDevTime)
                    typeCompletion++;
                bestTime = TrialsResults.GetTimeString(TimeSpan.FromTicks(data.BestTime));
            }
            else 
            {
                var data = (trialTowerData as AdventureTrialsTowerData).Stats;
                if (data.UnlockedGold)
                    typeCompletion++;
                if (data.UnlockedDiamond)
                    typeCompletion++;
                if (data.UnlockedDevTime)
                    typeCompletion++;
                bestTime = TrialsResults.GetTimeString(TimeSpan.FromTicks(data.BestTime));
            }
            var levelID = trialTowerData.GetLevelID() ?? "Official Level";
            var index = levelID.IndexOf("/");
            if (index != -1) 
            {
                levelID = levelID.Substring(index + 1);
            }

            NextPresence = new Discord.Activity {
                Details = "Playing " + levelID,
                State = "Best Time: " + bestTime
            };
            NextPresence.Assets.SmallImage = GetTrialTime(typeCompletion);
            NextPresence.Assets.SmallText = "Trial";
        }
            break;
        }

        NextPresence.Assets.LargeText = "FortRise";
        NextPresence.Assets.LargeImage = FortRiseIcon;

        dirty = true;
    }

    private static string GetTrialTime(int typeCompletion) 
    {
        return typeCompletion switch 
        {
            3 => "trials_pearl",
            2 => "trials_diamond",
            1 => "trials_gold",
            _ => "trials_none"
        };
    }

    private static string GetQuestDifficulty(MatchSettings settings) 
    {
        if (settings.QuestHardcoreMode)
            return "hardcore";
        return "normal";
    }

    private static string GetDarkWorldDifficulty(DarkWorldDifficulties difficulty) 
    {
        return difficulty switch 
        {
            DarkWorldDifficulties.Normal => "normal",
            DarkWorldDifficulties.Hardcore => "hardcore",
            DarkWorldDifficulties.Legendary => "legendary",
            _ => "normal"
        };
    }
}