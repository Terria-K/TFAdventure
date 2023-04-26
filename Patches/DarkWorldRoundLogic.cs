#pragma warning disable CS0626
#pragma warning disable CS0108
using Microsoft.Xna.Framework;
using MonoMod;

namespace TowerFall;

public class patch_DarkWorldRoundLogic : RoundLogic
{
    public patch_DarkWorldRoundLogic(Session session) : base(session, false)
    {
    }

    private float autoReviveCounter;
    // public patch_DarkWorldRoundLogic(Session session) : base(session, false)
    // {
    // }

    // public patch_Session Session { get; private set; }
    // public int Points 
    // {
    //     get => Session.Points;
    //     set => Session.Points = value;
    // }

    public DarkWorldControl Control { get; private set; }


    // public extern void orig_RegisterEnemyKill(Vector2 at, int killerIndex, int points);

    // public void RegisterEnemyKill(Vector2 at, int killerIndex, int points) 
    // {
    //     orig_RegisterEnemyKill(at, killerIndex, points);
    //     Points += points;
    // }

    public override void OnPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex, DeathCause cause, Vector2 position, int killerIndex)
    {
        base.OnPlayerDeath(player, corpse, playerIndex, cause, position, killerIndex);
        if (!patch_SaveData.AdventureActive)
            SaveData.Instance.DarkWorld.Towers[base.Session.MatchSettings.LevelSystem.ID.X].Deaths += 1UL;
        base.Session.DarkWorldState.OnPlayerDeath(player);
        if (!this.Control.PlayerEnteredPortal && !base.Session.CurrentLevel.Ending && this.CoOpCheckForAllDead())
        {
            if (base.Session.DarkWorldState.ExtraLives > 0)
            {
                this.autoReviveCounter = 60f;
                return;
            }
            if (base.Session.DarkWorldState.ContinuesRemaining == 0)
            {
                base.FinalKillNoSpotlight();
            }
            else
            {
                base.FinalKillNoSpotlightOrMusicStop();
            }
            base.Session.CurrentLevel.Ending = true;
            base.Session.CurrentLevel.Add<patch_DarkWorldGameOver>(new patch_DarkWorldGameOver(this));
        }
    }
}