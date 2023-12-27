using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace FortRise;

public static class PickupsRegistry 
{
    public static Dictionary<string, PickupObject> RegisteredPickups => RiseCore.PickupRegistry;
    public static Dictionary<Type, Pickups> Types = new();


    public static void Register<T>(FortModule module) 
    {
        Register(typeof(T), module);
    }

    public static void Register(Type type, FortModule module) 
    {
        foreach (var pickup in type.GetCustomAttributes<CustomPickupAttribute>()) 
        {
            if (pickup is null)
                continue;
            var pickupName = pickup.Name ?? $"{type.Namespace}.{type.Name}";
            var stride = RiseCore.PickupLoaderCount;
            ConstructorInfo ctor = type.GetConstructor(new Type[2] { typeof(Vector2), typeof(Vector2) });
            PickupLoader loader = null;

            if (ctor != null) 
            {
                loader = (pos, targetPos, idx) => 
                {
                    var custom = (Pickup)ctor.Invoke(new object[2] { pos, targetPos });
                    if (custom is CustomOrbPickup customOrb) 
                    {
                        var info = customOrb.CreateInfo();
                        customOrb.Sprite = info.Sprite;
                        customOrb.LightColor = info.Color.Invert();
                        customOrb.Collider = info.Hitbox;
                        customOrb.Border.Color = info.Color;
                        customOrb.Sprite.Play(0);
                        customOrb.Add(customOrb.Sprite);
                    }

                    return custom;
                };
            }

            var pickupObject = new PickupObject() 
            {
                Name = pickupName,
                ID = (Pickups)RiseCore.PickupLoaderCount,
                Chance = pickup.Chance
            };

            RiseCore.PickupRegistry[pickupName] = pickupObject;
            RiseCore.PickupLoader[(Pickups)RiseCore.PickupLoaderCount] = loader;
            RiseCore.PickupLoaderCount++;
        }
    }
}