using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TowerFall;

namespace FortRise;

/// <summary>
/// A class that registers custom entity from types.
/// </summary>
public static class EntityRegistry 
{
    internal static void LoadAllBuiltinEnemies() 
    {
        AddEnemy<ShieldSlime>(RiseCore.AdventureModule, 
            "FortRise/SlimeS = SlimeS", 
            "FortRise/RedSlimeS = RedSlimeS",
            "FortRise/BlueSlimeS = BlueSlimeS"
        );
    }

    public static void AddEnemy<T>(this FortModule module, params string[] names) 
    {
        AddEnemy(module, typeof(T), names);
    }

    public static void AddEnemy(this FortModule module, Type type, params string[] names) 
    {
        foreach (var name in names) 
        {
            string id;
            string methodName = string.Empty;
            string[] split = name.Split('=');
            if (split.Length == 1) 
            {
                id = split[0];
            }
            else if (split.Length == 2) 
            {
                id = split[0];
                methodName = split[1];
            }
            else 
            {
                Logger.Error($"[Loader] [{module.Meta.Name}] Invalid syntax of custom entity ID: {name}, {type.FullName}");
                continue;
            }

            id = id.Trim();
            methodName = methodName?.Trim();
            ConstructorInfo ctor;
            MethodInfo info;
            EnemyLoader loader = null;

            info = type.GetMethod(methodName, new Type[] { typeof(Vector2), typeof(Facing) });
            if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(Enemy))) 
            {
                loader = (position, facing, _) => {
                    var invoked = (patch_Enemy)info.Invoke(null, new object[] {
                        position, facing
                    });
                    invoked.Load();
                    return invoked;
                };
                goto Loaded;
            }

            info = type.GetMethod(methodName, new Type[] { typeof(Vector2), typeof(Facing), typeof(Vector2[]) });
            if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(Enemy))) 
            {
                loader = (position, facing, nodes) => {
                    var invoked = (patch_Enemy)info.Invoke(null, new object[] {
                        position, facing, nodes
                    });
                    invoked.Load();
                    return invoked;
                };
                goto Loaded;
            }

            ctor = type.GetConstructor(
                new Type[] { typeof(Vector2), typeof(Facing) }
            );
            if (ctor != null) 
            {
                loader = (position, facing, _) => {
                    var invoked = (patch_Enemy)ctor.Invoke(new object[] {
                        position,
                        facing
                    });
                    invoked.Load();

                    return invoked;

                };
                goto Loaded;
            }

            ctor = type.GetConstructor(
                new Type[] { typeof(Vector2), typeof(Facing), typeof(Vector2[]) }
            );
            if (ctor != null) 
            {
                loader = (position, facing, nodes) => {
                    var invoked = (patch_Enemy)ctor.Invoke(new object[] {
                        position,
                        facing,
                        nodes
                    });
                    invoked.Load();

                    return invoked;

                };
                goto Loaded;
            }
            Loaded:
            RiseCore.EnemyLoader[id] = loader;
        }
    }
}