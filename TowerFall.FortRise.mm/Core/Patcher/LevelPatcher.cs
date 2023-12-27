#pragma warning disable CS0618
using TowerFall;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FortRise;

/// <summary>
/// Patch any towers by deriving from this class.
/// Name your class with the tower name you want to patch, must not have any spaces.
/// Make sure that you properly spelled it and it is case sensitive.
/// </summary>
public abstract class TowerPatch
{
    public virtual void VersusPatch(VersusTowerPatchContext ctx) { }
    public virtual void DarkWorldPatch(DarkWorldTowerPatchContext ctx) { }

    public virtual TowerTheme ThemeOverride() 
    {
        return null;
    }
}

public static class TowerPatchRegistry 
{
    // garbage
    private static List<Type> toPatch = new();

    private static VersusTowerPatchContext versusCtx = new();
    private static DarkWorldTowerPatchContext dwCtx = new();

    public static void Initialize() 
    {
        foreach (var type in toPatch) 
        {
            var instance = Activator.CreateInstance(type) as TowerPatch;
            if (instance == null)
                continue;

            Versus(type, instance);
        }
        toPatch = null;
    }

    public static void Register<T>(FortModule module) 
    where T : TowerPatch
    {
        Register(typeof(T), module);
    }

    public static void Register(Type type, FortModule module) 
    {
        if (type.IsSubclassOf(typeof(TowerPatch))) 
        {
            toPatch.Add(type);
        }
    }

    private static void Versus(Type levelPatchType, TowerPatch patcher) 
    {
        var tower = GameData.VersusTowers
            .Where(x => levelPatchType.Name
                .StartsWith(x.GetLevelID().Replace("'", "").Replace(" ", "")))
            .FirstOrDefault();
        if (tower == null) 
        {
            if (levelPatchType.Name.StartsWith("Global")) 
            {
                foreach (var tow in GameData.VersusTowers) 
                {
                    versusCtx.Patch(tow);
                    patcher.VersusPatch(versusCtx);
                    tow.ApplyPatch(versusCtx);
                }
            }
            return;
        }
        versusCtx.Patch(tower);
        patcher.VersusPatch(versusCtx);
        tower.ApplyPatch(versusCtx);
    }

    private static void DarkWorld(Type levelPatchType, TowerPatch patcher) 
    {
        var tower = GameData.DarkWorldTowers
            .Where(x => levelPatchType.Name
                .StartsWith(x.GetLevelID().Replace("'", "").Replace(" ", "")))
            .FirstOrDefault();
        if (tower == null) 
        {
            if (levelPatchType.Name.StartsWith("Global")) 
            {
                foreach (var tow in GameData.DarkWorldTowers) 
                {
                    dwCtx.Patch(tow);
                    patcher.VersusPatch(versusCtx);
                    // tow.ApplyPatch(versusCtx);
                }
            }
            return;
        }
        dwCtx.Patch(tower);
        patcher.DarkWorldPatch(dwCtx);
        // tower.ApplyPatch(dwCtx);
    }
}

public interface ITowerPatchContext
{
    public LevelData Data { get; }
}

public class VersusTowerPatchContext : ITowerPatchContext
{

    public LevelData Data => data;

    private VersusTowerData data;

    private Dictionary<string, int[]> modifiedTreasureMasks = new();

    internal VersusTowerPatchContext() {}


    public void Patch(VersusTowerData data)
    {
        this.data = data;
        if (!modifiedTreasureMasks.ContainsKey(data.GetLevelID())) 
        {
            var origTreasureMask = data.TreasureMask;
            var moddedTreasureMask = new int[origTreasureMask.Length];
            Array.Copy(origTreasureMask, moddedTreasureMask, origTreasureMask.Length);
            modifiedTreasureMasks.Add(data.GetLevelID(), moddedTreasureMask);
        }
    }

    public int[] ReceivePatch(string levelID) 
    {
        return modifiedTreasureMasks[levelID];
    }

#region TreasureRates
    /// <summary>
    /// Increase the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">An enum pickup to add or increase</param>
    /// <param name="rate">How many pickups in this tower will appear, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext IncreaseTreasureRates(Pickups pickup, int rate = 1) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        var currentRate = moddedTreasureMask[(int)pickup];
        moddedTreasureMask[(int)pickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Increase the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">A string pickup to add or increase</param>
    /// <param name="rate">How many pickups in this tower will appear, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext IncreaseTreasureRates(string pickup, int rate = 1) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        var currentRate = moddedTreasureMask[(int)enumPickup];
        moddedTreasureMask[(int)enumPickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Decrease the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">A string pickup to decrease or completely removed if reached to 0</param>
    /// <param name="rate">How many rates of the pickup in this tower will be decrease, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext DecreaseTreasureRates(string pickup, int rate = 1) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        var currentRate = moddedTreasureMask[(int)enumPickup];
        if (currentRate == 0)
            return this;
        moddedTreasureMask[(int)enumPickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Decrease the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">An enum pickup to decrease or completely removed if reached to 0</param>
    /// <param name="rate">How many rates of the pickup in this tower will be decrease, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext DecreaseTreasureRates(Pickups pickup, int rate = 1) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        var currentRate = moddedTreasureMask[(int)pickup];
        if (currentRate == 0)
            return this;
        moddedTreasureMask[(int)pickup] = currentRate - rate;
        return this;
    }

    /// <summary>
    /// Remove the treasure rate of a pickup in this tower. (Note that this will not be completely removed,
    /// if there is any mods that adding the same pickup as you want to remove them).
    /// </summary>
    /// <param name="pickup">An enum pickup to remove in this tower</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext RemoveTreasureRates(Pickups pickup) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        moddedTreasureMask[(int)pickup] = 0;
        return this;
    }

    /// <summary>
    /// Remove the treasure rate of a pickup in this tower. (Note that this will not be completely removed,
    /// if there is any mods that adding the same pickup as you want to remove them).
    /// </summary>
    /// <param name="pickup">A string pickup to remove in this tower</param>
    /// <returns>A context of this struct</returns>
    public VersusTowerPatchContext RemoveTreasureRates(string pickup) 
    {
        var moddedTreasureMask = modifiedTreasureMasks[data.GetLevelID()];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        moddedTreasureMask[(int)enumPickup] = 0;
        return this;
    }

    #endregion
}

public class DarkWorldTowerPatchContext : ITowerPatchContext
{
    public LevelData Data => data;

    private DarkWorldTowerData data;

    internal DarkWorldTowerPatchContext() 
    {
    }

    public void Patch(DarkWorldTowerData data)
    {
        this.data = data;
    }


    public void AddTreasure(Pickups pickup, DarkWorldDifficulties difficulty, int level) 
    {
        for (int i = 0; i < 4; i++) 
        {
            AddTreasure(pickup, i, difficulty, level);
        }
    }

    public void AddTreasure(string pickup, DarkWorldDifficulties difficulty, int level) 
    {
        var realPickup = RiseCore.PickupRegistry[pickup].ID;
        AddTreasure(realPickup, difficulty, level);
    }

    public void AddTreasure(
        string pickup, int playerAmount,
        DarkWorldDifficulties difficulty,
        int level) 
    {
        var realPickup = RiseCore.PickupRegistry[pickup].ID;
        AddTreasure(realPickup, playerAmount, difficulty, level);
    }

    public void AddTreasure(
        Pickups pickup, int playerAmount, 
        DarkWorldDifficulties difficulty,
        int level) 
    {
        var diff = difficulty switch 
        {
            DarkWorldDifficulties.Normal => data.Normal,
            DarkWorldDifficulties.Hardcore => data.Hardcore,
            _ => data.Legendary
        };
        var treasureData = diff[level].TreasureData;
        treasureData[playerAmount].Add(pickup);
    }
}

/// <summary>
/// Patch Vanilla versus towers with this struct. 
/// If you want to patch outside of <see cref="ITowerPatcher.PatchTower(FortRise.OnTower)"/>,
/// you just create this struct and pass around your Fort Module.
/// </summary>
[Obsolete("Create a class with the tower name you want to patch and extends it from FortRise.TowerPatch")]
public struct OnTower 
{
    public OnTower(FortModule module) 
    {
        // Future use...
    }

#region VersusPatch
    public readonly VersusPatch VERSUS_SacredGround => new VersusPatch(0);
    public readonly VersusPatch VERSUS_TwilightSpire => new VersusPatch(1);
    public readonly VersusPatch VERSUS_Backfire => new VersusPatch(2);
    public readonly VersusPatch VERSUS_Flight => new VersusPatch(3);
    public readonly VersusPatch VERSUS_Mirage => new VersusPatch(4);
    public readonly VersusPatch VERSUS_Thornwood => new VersusPatch(5);
    public readonly VersusPatch VERSUS_FrostfangKeep => new VersusPatch(6);
    public readonly VersusPatch VERSUS_KingsCourt => new VersusPatch(7);
    public readonly VersusPatch VERSUS_SunkenCity => new VersusPatch(8);
    public readonly VersusPatch VERSUS_Moonstone => new VersusPatch(9);
    public readonly VersusPatch VERSUS_Towerforge => new VersusPatch(10);
    public readonly VersusPatch VERSUS_Ascension => new VersusPatch(11);
    public readonly VersusPatch VERSUS_TheAmaranth => new VersusPatch(12);
    public readonly VersusPatch VERSUS_Dreadwood => new VersusPatch(13);
    public readonly VersusPatch VERSUS_Darkfang => new VersusPatch(14);
    public readonly VersusPatch VERSUS_Cataclysm => new VersusPatch(15);

    public readonly void VERSUS_All(Action<VersusPatch> patchAll) 
    {
        for (int i = 0; i < 16; i++) 
        {
            var versusPatch = new VersusPatch(i);
            patchAll(versusPatch);
        }
    }
#endregion

#region DarkWorldPatch
    public readonly DarkWorldPatch DARKWORLD_TheAmaranth => new(0);
    public readonly DarkWorldPatch DARKWORLD_Dreadwood => new(1);
    public readonly DarkWorldPatch DARKWORLD_Darkfang => new(2);
    public readonly DarkWorldPatch DARKWORLD_Cataclysm => new(3);
    public readonly DarkWorldPatch DARKWORLD_DarkGauntlet => new(4);
#endregion
}

/// <summary>
/// A struct that patch the dark world towers. Please use this <see cref="ITowerPatcher.PatchTower(FortRise.OnTower)"/> 
/// to handle the patch.
/// </summary>
public struct DarkWorldPatch 
{
    internal int LevelID;
    internal DarkWorldPatch(int id) 
    {
        if (id >= GameData.DarkWorldTowers.Count)
        {
            Logger.Warning("[Tower Patcher] Level ID does not exists!");
        }
        LevelID = id;
    }

    public readonly LevelSet Normal()
    {
        var tower = GameData.DarkWorldTowers[LevelID];
        return new LevelSet(this, tower.Normal);
    }

    public readonly LevelSet Hardcore()
    {
        var tower = GameData.DarkWorldTowers[LevelID];
        return new LevelSet(this, tower.Hardcore);
    }

    public readonly LevelSet Legendary()
    {
        var tower = GameData.DarkWorldTowers[LevelID];
        return new LevelSet(this, tower.Legendary);
    }


    public struct LevelSet 
    {
        private DarkWorldPatch patch;
        private List<DarkWorldTowerData.LevelData> setData;
        internal LevelSet(DarkWorldPatch patch, List<DarkWorldTowerData.LevelData> setData) 
        {
            this.patch = patch;
            this.setData = setData;
        }

        public readonly DarkWorldPatch Back() 
        {
            return patch;
        }

        public readonly FileLevel Level(int file) 
        {
            return new FileLevel(this, setData[file]);
        }

        public readonly LevelSet AllLevel(Action<FileLevel> levelAction) 
        {
            for (int i = 0; i < setData.Count; i++) 
            {
                levelAction(new FileLevel(this, setData[i]));
            }
            return this;
        }


        public struct FileLevel 
        {
            private DarkWorldTowerData.LevelData levelData;
            private LevelSet levelSet;
            internal FileLevel(LevelSet levelSet, DarkWorldTowerData.LevelData levelData) 
            {
                this.levelData = levelData;
                this.levelSet = levelSet;
            }

            public readonly LevelSet Back() 
            {
                return levelSet;
            }

            public readonly FileLevel AddTreasure(Pickups pickup) 
            {
                for (int i = 0; i < 4; i++) 
                {
                    AddTreasure(pickup, i);
                }
                return this;
            }

            public readonly FileLevel AddTreasure(string pickup) 
            {
                var realPickup = RiseCore.PickupRegistry[pickup].ID;
                return AddTreasure(realPickup);
            }

            public readonly FileLevel AddTreasure(string pickup, int playerAmount) 
            {
                var realPickup = RiseCore.PickupRegistry[pickup].ID;
                return AddTreasure(realPickup, playerAmount);
            }

            public readonly FileLevel AddTreasure(Pickups pickup, int playerAmount) 
            {
                levelData.TreasureData[playerAmount].Add(pickup);
                return this;
            }

            public readonly FileLevel RemoveTreasure(Pickups pickup) 
            {
                for (int i = 0; i < 4; i++) 
                {
                    RemoveTreasure(pickup, i);
                }
                return this;
            }

            public readonly FileLevel RemoveTreasure(string pickup) 
            {
                var realPickup = RiseCore.PickupRegistry[pickup].ID;
                return RemoveTreasure(realPickup);
            }

            public readonly FileLevel RemoveTreasure(string pickup, int playerAmount) 
            {
                var realPickup = RiseCore.PickupRegistry[pickup].ID;
                return RemoveTreasure(realPickup, playerAmount);
            }

            public readonly FileLevel RemoveTreasure(Pickups pickup, int playerAmount) 
            {
                levelData.TreasureData[playerAmount].Remove(pickup);
                return this;
            }

            public readonly FileLevel LogTotalTreasure(Pickups pickup) 
            {
                foreach (var treasure in levelData.TreasureData[0]) 
                {
                    Logger.Info("[LevelPatcher] Treasure: " + treasure);
                }
                return this;
            }
        }
    }
}

/// <summary>
/// An interface to directly inject the Tower Patcher code in Fort Modules.
/// </summary>
public interface ITowerPatcher 
{
    /// <summary>
    /// A method to perform the tower patching.
    /// </summary>
    /// <param name="tower">A struct containing the logic of tower patching</param>
    void PatchTower(OnTower tower);
}

/// <summary>
/// A struct that patch the versus towers. Please use this <see cref="ITowerPatcher.PatchTower(FortRise.OnTower)"/> 
/// to handle the patch.
/// </summary>
public struct VersusPatch 
{
    internal int LevelID;
    internal VersusPatch(int id) 
    {
        if (id >= GameData.VersusTowers.Count)
        {
            Logger.Warning("[Tower Patcher] Level ID does not exists!");
        }
        LevelID = id;
    }

    private bool IsSafe() 
    {
        if (LevelID >= GameData.VersusTowers.Count)
        {
            Logger.Error($"Cannot patch tower id: {LevelID} as it does not exists");
            return false;
        }
        return true;
    }

#region TreasureRates
    /// <summary>
    /// Increase the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">An enum pickup to add or increase</param>
    /// <param name="rate">How many pickups in this tower will appear, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch IncreaseTreasureRates(Pickups pickup, int rate = 1) 
    {
        if (!IsSafe()) return this;

        var tower = GameData.VersusTowers[LevelID];
        var currentRate = tower.TreasureMask[(int)pickup];
        tower.TreasureMask[(int)pickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Increase the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">A string pickup to add or increase</param>
    /// <param name="rate">How many pickups in this tower will appear, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch IncreaseTreasureRates(string pickup, int rate = 1) 
    {
        if (!IsSafe()) return this;

        var tower = GameData.VersusTowers[LevelID];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        var currentRate = tower.TreasureMask[(int)enumPickup];
        tower.TreasureMask[(int)enumPickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Decrease the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">A string pickup to decrease or completely removed if reached to 0</param>
    /// <param name="rate">How many rates of the pickup in this tower will be decrease, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch DecreaseTreasureRates(string pickup, int rate = 1) 
    {
        if (!IsSafe()) return this;
        var tower = GameData.VersusTowers[LevelID];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        var currentRate = tower.TreasureMask[(int)enumPickup];
        if (currentRate == 0)
            return this;
        tower.TreasureMask[(int)enumPickup] = currentRate + rate;
        return this;
    }

    /// <summary>
    /// Decrease the treasure rate of a pickup in this tower.
    /// </summary>
    /// <param name="pickup">An enum pickup to decrease or completely removed if reached to 0</param>
    /// <param name="rate">How many rates of the pickup in this tower will be decrease, default is 1</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch DecreaseTreasureRates(Pickups pickup, int rate = 1) 
    {
        if (!IsSafe()) return this;
        var tower = GameData.VersusTowers[LevelID];
        var currentRate = tower.TreasureMask[(int)pickup];
        if (currentRate == 0)
            return this;
        tower.TreasureMask[(int)pickup] = currentRate - rate;
        return this;
    }

    /// <summary>
    /// Remove the treasure rate of a pickup in this tower. (Note that this will not be completely removed,
    /// if there is any mods that adding the same pickup as you want to remove them).
    /// </summary>
    /// <param name="pickup">An enum pickup to remove in this tower</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch RemoveTreasureRates(Pickups pickup) 
    {
        if (!IsSafe()) return this;
        var tower = GameData.VersusTowers[LevelID];
        tower.TreasureMask[(int)pickup] = 0;
        return this;
    }

    /// <summary>
    /// Remove the treasure rate of a pickup in this tower. (Note that this will not be completely removed,
    /// if there is any mods that adding the same pickup as you want to remove them).
    /// </summary>
    /// <param name="pickup">A string pickup to remove in this tower</param>
    /// <returns>A context of this struct</returns>
    public VersusPatch RemoveTreasureRates(string pickup) 
    {
        if (!IsSafe()) return this;
        var tower = GameData.VersusTowers[LevelID];
        var enumPickup = Calc.StringToEnum<Pickups>(pickup);
        tower.TreasureMask[(int)enumPickup] = 0;
        return this;
    }
#endregion
}