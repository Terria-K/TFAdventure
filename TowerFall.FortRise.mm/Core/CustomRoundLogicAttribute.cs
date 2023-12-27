using System;

namespace TowerFall;

[Obsolete("Use FortRise.GameMode instead")]
public sealed class CustomRoundLogicAttribute : Attribute 
{
    public string Name;
    public string OverrideFunction;

    public CustomRoundLogicAttribute(string name, string overriden = null) 
    {
        Name = name;
        OverrideFunction = overriden;
    }
}