#pragma warning disable CS0612
#pragma warning disable CS0618
using Monocle;
using TowerFall;

namespace FortRise;

// This is a legacy gamemode.
// Intended used for backward compatibility with CustomRoundLogic
// DO NOT USE THIS AS AN EXAMPLE
internal class LegacyCustomGamemode : CustomGameMode
{
    private RoundLogicType Type;
    internal RoundLogicLoader LegacyLoader;

    public LegacyCustomGamemode(string name, RoundLogicInfo info) : base()
    {
        Name = info.Name;
        Icon = info.Icon;
        ModeType = GameModeType.Versus;

        Type = info.RoundType;
        ID = name;
        TeamMode = info.RoundType == RoundLogicType.TeamDeatchmatch;
    }

    public override RoundLogic CreateRoundLogic(Session session)
    {
        if (LegacyLoader != null) 
        {
            return LegacyLoader.Invoke((patch_Session)session, true);
        }
        return Type switch
        {
            RoundLogicType.FFA => new LastManStandingRoundLogic(session),
            RoundLogicType.HeadHunters => new HeadhuntersRoundLogic(session),
            RoundLogicType.TeamDeatchmatch => new TeamDeathmatchRoundLogic(session),
            _ => new LastManStandingRoundLogic(session)
        };
    }

    public override LevelSystem GetLevelSystem(LevelData levelData)
    {
        return base.GetLevelSystem(levelData);
    }

    public override void Initialize() {}

    public override Sprite<int> CoinSprite()
    {
        if (Type == RoundLogicType.HeadHunters)
            return DeathSkull.GetSprite();
        return base.CoinSprite();
    }

    public override void InitializeSounds()
    {
        if (Type == RoundLogicType.HeadHunters) 
        {
            EarnedCoinSound = Sounds.sfx_multiSkullEarned; 
            CoinOffset = 12;
        }
    }
}