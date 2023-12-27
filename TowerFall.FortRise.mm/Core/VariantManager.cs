using System;
using System.Collections.Generic;
using System.Globalization;
using Monocle;
using TowerFall;

namespace FortRise;

/// <summary>
/// Use to manage variants on load.
/// </summary>
public class VariantManager : IDisposable 
{
    private string currentContext;
    /// <summary>
    /// The current name of the module registering their variants.
    /// </summary>
    public string CurrentContext => currentContext;
    private patch_MatchVariants main;
    /// <summary>
    /// <see cref="TowerFall.MatchVariants"/> is a TowerFall class for holding all the variants.
    /// </summary>
    public MatchVariants MatchVariants => main;
    public static List<(Variant, ArrowTypes)> StartWithVariants = new();

    internal Dictionary<string, List<Variant>> ToAdd = new();
    internal int TotalCustomVariantsAdded;
    internal List<Variant> CanRandoms = new();
    internal HashSet<string> Headers = new();


    internal VariantManager(patch_MatchVariants variant) 
    {
        main = variant;
    }

    internal void SetContext(ModuleMetadata context) 
    {
        var name = context.Name;
        currentContext = name;
    }

    /// <summary>
    /// Use to add variants to <see cref="TowerFall.MatchVariants"/>.
    /// </summary>
    /// <param name="info">A <see cref="FortRise.CustomVariantInfo"/> is needed to know what
    /// kind of variant is being passed to the holder.
    /// </param>
    /// <param name="noPerPlayer">A <see langwords="bool"/> value if you want the variant
    /// to have it on all players without excluding one of them.
    /// </param>
    /// <returns>A built <see cref="TowerFall.Variant"/> to be used for linking or just 
    /// ignore
    /// </returns>
    public Variant AddVariant(CustomVariantInfo info, bool noPerPlayer = false) 
    {
        var flags = info.Flags;
        Pickups[] itemExclusions = info.Exclusions;
        bool perPlayer = flags.HasFlag(CustomVariantFlags.PerPlayer) && !noPerPlayer;
        string description = info.Description;
        string header = (info.Header ?? currentContext).ToUpperInvariant();
        string useHeader = header;
        if (Headers.Contains(header)) 
            useHeader = null;
        else
            Headers.Add(header);
        bool scrollEffect = flags.HasFlag(CustomVariantFlags.ScrollEffect);
        bool hidden = flags.HasFlag(CustomVariantFlags.Hidden);
        bool flag = flags.HasFlag(CustomVariantFlags.CanRandom);
        bool tournamentRule1v = flags.HasFlag(CustomVariantFlags.TournamentRule1v1);
        bool tournamentRule2v = flags.HasFlag(CustomVariantFlags.TournamentRule2v2);
        bool unlisted = flags.HasFlag(CustomVariantFlags.Unlisted);
        bool darkWorldDLC = flags.HasFlag(CustomVariantFlags.DarkWorldDLC);
        int coopValue = 0;
        if (flags.HasFlag(CustomVariantFlags.CoopCurses)) 
        {
            coopValue = -1;
        }
        else if (flags.HasFlag(CustomVariantFlags.CoopBlessing))
        {
            coopValue = 1;
        }
        var title = GetCustomVariantTitle(info.Name);
        var icon = info.Icon ?? GetVariantIconFromName(info.Name);
        var variant = new Variant(icon, title, description, itemExclusions, perPlayer, 
            useHeader, null, scrollEffect, hidden, flag, tournamentRule1v, 
            tournamentRule2v, unlisted, darkWorldDLC, coopValue);
        if (flag)
            CanRandoms.Add(variant);
        
        main.InternalCustomVariants.Add(info.Name, variant);
        TotalCustomVariantsAdded++;

        if (!TempVariantHolder.TempCustom.ContainsKey(info.Name))
            TempVariantHolder.TempCustom.Add(info.Name, false);
        
        if (ToAdd.TryGetValue(header, out var toAdd)) 
        {
            toAdd.Add(variant);
            return variant;
        }
        var list = new List<Variant> { variant };
        ToAdd.Add(header, list);
        return variant;
    }

    public void AddArrowVariant(ArrowObject obj, Subtexture arrowVariantIcon, Subtexture arrowExcludeVariantIcon) 
    {
        AddArrowVariant(obj, arrowVariantIcon, arrowExcludeVariantIcon, currentContext);
    }

    public void AddArrowVariant(
        ArrowObject obj, Subtexture arrowVariantIcon, Subtexture arrowExcludeVariantIcon, string header)  
    {
        var customArrowInfo = obj.InfoLoader();
        var name = $"{RemoveSlashes(customArrowInfo.Name)}";
        AddArrowVariant(obj, arrowVariantIcon, arrowExcludeVariantIcon, name, header);
    }

    public void AddArrowVariant(
        ArrowObject obj, Subtexture arrowVariantIcon, Subtexture arrowExcludeVariantIcon, string name, string header) 
    {
        string variantStartWithName = $"StartWith{name}";
        var variantInfo = new CustomVariantInfo(variantStartWithName, arrowVariantIcon, 
            CustomVariantFlags.PerPlayer | CustomVariantFlags.CanRandom) { Header = header };
        var variant = AddVariant(variantInfo);
        string variantPickupName = $"No{name}";
        if (!variantPickupName.EndsWith("Arrows"))
            variantPickupName += " Arrows";
        AddPickupVariant(obj.PickupType, arrowExcludeVariantIcon, variantPickupName, header);

        CreateLinks(main.StartWithBoltArrows, variant);
        CreateLinks(main.StartWithBombArrows, variant);
        CreateLinks(main.StartWithDrillArrows, variant);
        CreateLinks(main.StartWithBrambleArrows, variant);
        CreateLinks(main.StartWithFeatherArrows, variant);
        CreateLinks(main.StartWithLaserArrows, variant);
        CreateLinks(main.StartWithPrismArrows, variant);
        CreateLinks(main.StartWithRandomArrows, variant);
        CreateLinks(main.StartWithTriggerArrows, variant);
        CreateLinks(main.StartWithSuperBombArrows, variant);
        CreateLinks(main.StartWithToyArrows, variant);
        foreach (var (other, _) in StartWithVariants) 
        {
            CreateLinks(other, variant);
        }

        StartWithVariants.Add((variant, obj.Types));
    }

    public void AddPickupVariant(PickupObject obj, Subtexture pickupExcludeVariantIcon) 
    {
        AddPickupVariant(obj, pickupExcludeVariantIcon, currentContext);
    }

    public void AddPickupVariant(PickupObject obj, Subtexture pickupExcludeVariantIcon, string header) 
    {
        AddPickupVariant(obj, pickupExcludeVariantIcon, $"No{RemoveSlashes(obj.Name)}", header);
    }

    public void AddPickupVariant(PickupObject obj, Subtexture pickupExcludeVariantIcon, string name, string header) 
    {
        var variantInfo = new CustomVariantInfo(name, pickupExcludeVariantIcon, 
            CustomVariantFlags.PerPlayer | CustomVariantFlags.CanRandom, obj.ID) { Header = header };
        AddVariant(variantInfo);
    }

    private static string RemoveSlashes(string name) 
    {
        var idx = name.IndexOf('/');
        if (idx != -1) 
        {
            return name.Substring(idx + 1);
        }
        return name;
    }

    /// <summary>
    /// Get a variant icon from name with an atlas pass around. Note: It is recommended to
    /// just pass a <see cref="Monocle.Subtexture"/> Icon to the <see cref="FortRise.CustomVariantInfo"/> instead.
    /// </summary>
    /// <param name="variantName">A name of the variant</param>
    /// <param name="atlas">An atlas that includes the variants folder and 
    /// an exact name of the variant</param>
    /// <returns>A <see cref="Monocle.Subtexture"/> from the atlas</returns>
    public static Subtexture GetVariantIconFromName(string variantName, Atlas atlas)
    {
        return atlas["variants/" + variantName[0].ToString().ToLowerInvariant() + variantName.Substring(1)];
    }

    /// <summary>
    /// Get a variant icon from name within the context. 
    /// Note that you should only use this on the 
    ///<see cref="FortRise.FortModule.OnVariantsRegister(VariantManager, bool)"/>, or else
    /// you will get a possible error.
    /// </summary>
    /// <param name="variantName">A name of the variant</param>
    /// <returns>A <see cref="Monocle.Subtexture"/> from the atlas</returns>
    public Subtexture GetVariantIconFromName(string variantName)
    {
        return TFGame.Atlas[CurrentContext + "/" + 
            "variants/" + variantName[0].ToString().ToLowerInvariant() + variantName.Substring(1)];
    }

    /// <summary>
    /// Use to link the variants and you won't be able to select both of these variants anymore.
    /// Only one of the variants can be selected.
    /// </summary>
    /// <param name="variants">A variants that you want to link together</param>
    public void CreateLinks(params Variant[] variants)
    {
        for (int i = 0; i < variants.Length; i++)
        {
            variants[i].AddLinks(variants);
        }
    }

    /// <summary>
    /// A static function that allows you to get a custom variant on the fly if you're in
    /// a level scene.
    /// </summary>
    /// <param name="variantName">A variant name</param>
    /// <returns>A variant use to check if it's activated or not</returns>
    public static Variant GetCustomVariant(string variantName) 
    {
        var scene = Engine.Instance.Scene;
        if (scene is not Level level)
        {
            Logger.Warning($"[Custom Variant] The scene instance is not a Level");
            return null;
        }
        return (level.Session.MatchSettings.Variants as patch_MatchVariants).GetCustomVariant(variantName);
    }

    private static string GetCustomVariantTitle(string name)
    {
        string text = name;
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                text = text.Substring(0, i) + " " + text.Substring(i);
                i++;
            }
        }
        return text.ToUpper(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Dispose all of the uses collection. 
    /// </summary>
    public void Dispose()
    {
        ToAdd = null;
        CanRandoms = null;
        Headers = null;
    }
}

/// <summary>
/// A struct that represents a information for variant.
/// </summary>
public struct CustomVariantInfo 
{
    /// <summary>
    /// Represents an name and id for the variant.
    /// </summary>
    public string Name;
    /// <summary>
    /// Adds a description to the given variant
    /// </summary>
    public string Description;
    /// <summary>
    /// A header to organize the variant in some way.
    /// </summary>
    public string Header;
    /// <summary>
    /// A flags on how the variants will behave in the list.
    /// </summary>
    public CustomVariantFlags Flags;
    /// <summary>
    /// An icon for the variant to show in the list. 
    /// </summary>
    public Subtexture Icon;
    /// <summary>
    /// What pickup to not appear when this variant is enabled.
    /// </summary>
    public Pickups[] Exclusions;

    public CustomVariantInfo(string name, CustomVariantFlags flag = CustomVariantFlags.None) 
    {
        Name = name;
        Icon = null;
        Flags = flag;
        Description = string.Empty;
        Exclusions = null;
        Header = null;
    }

    public CustomVariantInfo(string name, Subtexture texture, CustomVariantFlags flag = CustomVariantFlags.None) 
    {
        Name = name;
        Icon = texture;
        Flags = flag;
        Description = string.Empty;
        Exclusions = null;
        Header = null;
    }

    public CustomVariantInfo(string name, Subtexture icon, CustomVariantFlags flags = CustomVariantFlags.None, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = icon;
        Flags = flags;
        Description = string.Empty;
        Exclusions = exclusions;
        Header = null;
    }

    public CustomVariantInfo(string name, CustomVariantFlags flags = CustomVariantFlags.None, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = null;
        Flags = flags;
        Description = string.Empty;
        Exclusions = exclusions;
        Header = null;
    }

    public CustomVariantInfo(string name, Subtexture icon, string description, CustomVariantFlags flags = CustomVariantFlags.None) 
    {
        Name = name;
        Icon = icon;
        Flags = flags;
        Description = description;
        Exclusions = null;
        Header = null;
    }

    public CustomVariantInfo(string name, string description, CustomVariantFlags flags = CustomVariantFlags.None) 
    {
        Name = name;
        Icon = null;
        Flags = flags;
        Description = description;
        Exclusions = null;
        Header = null;
    }

    public CustomVariantInfo(string name, Subtexture icon, string description, CustomVariantFlags flags, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = icon;
        Flags = flags;
        Description = description;
        Exclusions = exclusions;
        Header = null;
    }

    public CustomVariantInfo(string name, string description, CustomVariantFlags flags, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = null;
        Flags = flags;
        Description = description;
        Exclusions = exclusions;
        Header = null;
    }

    public CustomVariantInfo(string name, Subtexture icon, string description, string header, CustomVariantFlags flags, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = icon;
        Flags = flags;
        Description = description;
        Exclusions = exclusions;
        Header = header;
    }

    public CustomVariantInfo(string name, string description, string header, CustomVariantFlags flags, params Pickups[] exclusions) 
    {
        Name = name;
        Icon = null;
        Flags = flags;
        Description = description;
        Exclusions = exclusions;
        Header = header;
    }
}

/// <summary>
/// A list of flags for variants behavior on the variant list.
/// </summary>
[Flags]
public enum CustomVariantFlags
{
    None,
    PerPlayer,
    CanRandom,
    ScrollEffect,
    CoopBlessing,
    CoopCurses,
    TournamentRule1v1,
    TournamentRule2v2,
    DarkWorldDLC,
    Hidden,
    Unlisted,
}