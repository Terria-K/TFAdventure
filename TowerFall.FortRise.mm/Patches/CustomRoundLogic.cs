using System;
using System.Collections.Generic;
using Monocle;

namespace TowerFall;

[Obsolete("Turn this into RoundLogic and use FortRise.GameMode instead")]
public abstract class CustomVersusRoundLogic : RoundLogic
{
    protected CustomVersusRoundLogic(Session session, bool canHaveMiasma) : base(session, canHaveMiasma)
    {
    }
}

[Obsolete("Use FortRise.GameMode instead")]
public struct RoundLogicInfo 
{
    public string Name;
    public Subtexture Icon;
    public RoundLogicType RoundType;
}

[Obsolete("Use FortRise.GameMode instead")]
public enum RoundLogicType 
{
    FFA,
    HeadHunters,
    TeamDeatchmatch
}