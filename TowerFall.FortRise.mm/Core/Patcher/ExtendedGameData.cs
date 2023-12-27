using System;
using System.Collections.Generic;
using FortRise.Adventure;

namespace FortRise;

public static class ExtendedGameData 
{
    public static IReadOnlyDictionary<string, MapRendererNode> MapRenderers => InternalMapRenderers;

    internal static Dictionary<string, MapRendererNode> InternalMapRenderers = new();

    private static Queue<Action> themesActions = new();
    private static Queue<Action> tilesetActions = new();

    public static void Load() 
    {
        int length = tilesetActions.Count;
        for (int i = 0; i < length; i++) 
        {
            tilesetActions.Dequeue()();
        }
        length = themesActions.Count;
        for (int i = 0; i < length; i++) 
        {
            themesActions.Dequeue()();
        }
    }

    public static void Defer(Action action, int index) 
    {
        switch (index) 
        {
        case 0:
            themesActions.Enqueue(action);
            break;
        case 1:
            tilesetActions.Enqueue(action);
            break;
        }
    }
}