using System;
using System.Collections.Generic;
using TowerFall;

namespace FortRise;

public static class GameModeRegistry 
{
    internal static int ModesCount = 10;
    public static Dictionary<string, Type> GameModesMap = new();
    public static Dictionary<string, int> LegacyGameModesMap = new();
    public static Dictionary<Type, int> GameModeTypes = new();
    public static List<CustomGameMode> VersusGameModes = new();

    public static bool TryGetGameMode(string name, out CustomGameMode mode) 
    {
        if (LegacyGameModesMap.TryGetValue(name, out var legIdx)) 
        {
            mode = VersusGameModes[legIdx];
            return true;
        }
        if (GameModesMap.TryGetValue(name, out var type)) 
        {
            if (GameModeTypes.TryGetValue(type, out var idx)) 
            {
                mode = VersusGameModes[idx];
                return true;
            }
        }
        mode = null;
        return false;
    }

    public static void Register<T>(FortModule module) 
    {
        Register(typeof(T), module);
    }

    public static void Register(Type type, FortModule module) 
    {
        if (type.IsSubclassOf(typeof(CustomGameMode)) && type.IsPublic) 
        {
            var instance = Activator.CreateInstance(type) as CustomGameMode;
            instance.Initialize();
            instance.coinSprite = instance.CoinSprite();
            AddToVersus(type, instance);
        }
    }

    internal static void AddToLegacyVersus(string name, CustomGameMode gameMode) 
    {
        LegacyGameModesMap[name] = VersusGameModes.Count;
        VersusGameModes.Add(gameMode);
        gameMode.GameModeInternal = (Modes)ModesCount++;
    }

    internal static void AddToVersus(Type type, CustomGameMode gameMode) 
    {
        GameModesMap[gameMode.ID] = type;
        GameModeTypes[type] = VersusGameModes.Count;
        VersusGameModes.Add(gameMode);
        gameMode.GameModeInternal = (Modes)ModesCount++;
    }
}