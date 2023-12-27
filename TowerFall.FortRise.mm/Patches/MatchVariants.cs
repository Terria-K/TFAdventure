#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using FortRise;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_MatchVariants : MatchVariants 
{
    internal Dictionary<string, Variant> InternalCustomVariants;
    public IReadOnlyDictionary<string, Variant> CustomVariants => InternalCustomVariants;


    private List<Variant> canRandoms;
    [MonoModIgnore]
    public static int Count { get; private set; }
    internal VariantManager manager;

    public void orig_ctor(bool noPerPlayer = false) {}

    [MonoModConstructor]
    public void ctor(bool noPerPlayer = false) 
    {
        InternalCustomVariants = new();

        orig_ctor(noPerPlayer);

        TempVariantHolder.TempCustom = new Dictionary<string, bool>();
        manager = new VariantManager(this);
        foreach (var mod in RiseCore.InternalFortModules) 
        {
            manager.SetContext(mod.Meta);
            mod.OnVariantsRegister(manager, noPerPlayer);
            mod.OnVariantsRegister(this, noPerPlayer);
        }


        int oldLength = Variants.Length;
        Array.Resize(ref Variants, manager.TotalCustomVariantsAdded + Variants.Length);
        int count = 0;
        foreach (var key in manager.ToAdd.Keys) 
        {
            var list = manager.ToAdd[key]; 
            for (int i = oldLength; i < oldLength + list.Count; i++) 
            {
                Variants[i] = list[count];
                count++;
            }
            oldLength += count;
            count = 0;
        }

        foreach (var random in manager.CanRandoms)
            canRandoms.Add(random);
        Count = Variants.Length;
        manager.Dispose();
    }

    [Obsolete("Use FortRise.VariantManager.AddVariant")]
    public Variant AddVariant(string variantName, VariantInfo info, VariantFlags flags, bool noPerPlayer) 
    {
        return AddVariant(
            variantName, 
            VariantManager.GetVariantIconFromName(variantName, info.VariantAtlas), 
            info,
            flags,
            noPerPlayer
        );
    }


    [Obsolete("Use FortRise.VariantManager.AddVariant")]
    public Variant AddVariant(string variantName, Subtexture variantIcon, VariantInfo info, VariantFlags flags, bool noPerPlayer) 
    {
         var fortRiseInfo = new FortRise.CustomVariantInfo(
            variantName, 
            variantIcon,
            info.Header ?? manager.CurrentContext,
            (FortRise.CustomVariantFlags)(int)flags,
            info.Exclusions) 
        {
            Description = info.Description
        };
        return manager.AddVariant(fortRiseInfo, noPerPlayer);
    }

    [Obsolete("Use VariantManager.GetVariantIconFromName")]
    public static Subtexture GetVariantIconFromName(string variantName, Atlas atlas)
    {
        return VariantManager.GetVariantIconFromName(variantName, atlas);
    }

    [Obsolete("Use VariantManager.CreateLinks")]
    public void CreateCustomLinks(params Variant[] variants)
    {
        manager.CreateLinks(variants);
    }

    public Variant GetCustomVariant(string name) 
    {
        return InternalCustomVariants[name];
    }

    public extern ArrowTypes orig_GetStartArrowType(int playerIndex, ArrowTypes randomType);

    public ArrowTypes GetStartArrowType(int playerIndex, ArrowTypes randomType) 
    {
        foreach (var (arrowVariant, arrowTypes) in VariantManager.StartWithVariants)
        {
            if (!arrowVariant[playerIndex])
                continue;
            return arrowTypes;
        }
        return orig_GetStartArrowType(playerIndex, randomType);
    }

    public extern List<Pickups> orig_GetItemExclusions(bool customTower);

    public List<Pickups> GetItemExclusions(bool customTower)
    {
        var list = orig_GetItemExclusions(customTower);
        foreach (var customVariant in InternalCustomVariants.Values) 
        {
            if (customVariant.Value && customVariant.ItemExclusions != null) 
            {
                list.AddRange(customVariant.ItemExclusions);
            }
        }
        return list;
    }
}

[Obsolete("Use FortRise.CustomVariantInfo")]
public struct VariantInfo 
{
    public Atlas VariantAtlas;
    public string Header = "";
    public string Description = "";
    public Version NewInVersion;
    public Pickups[] Exclusions;

    public static readonly VariantInfo Empty = new VariantInfo();

    public VariantInfo(Atlas variantAtlas)
    {
        VariantAtlas = variantAtlas;
        Header = "";
        Description = "";
        Exclusions = null;
        NewInVersion = null;
    }

    public VariantInfo(string header, Atlas variantAtlas)
    {
        VariantAtlas = variantAtlas;
        Header = header;
        Description = "";
        Exclusions = null;
        NewInVersion = null;
    }

    public VariantInfo(string header, Atlas variantAtlas, params Pickups[] exclusion)
    {
        VariantAtlas = variantAtlas;
        Header = header;
        Description = "";
        Exclusions = null;
        NewInVersion = null;
    }

    public VariantInfo(string header, string description, Atlas variantAtlas, params Pickups[] exclusion)
    {
        VariantAtlas = variantAtlas;
        Header = header;
        Description = "";
        Exclusions = null;
        NewInVersion = null;
    }
}


[Flags]
[Obsolete("Use FortRise.CustomVariantFlags")]
public enum VariantFlags
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