#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Reflection;
using Monocle;
using TowerFall;

namespace FortRise;

public static class ArrowsRegistry
{
    public static Dictionary<string, ArrowObject> RegisteredArrows => RiseCore.ArrowsRegistry;
    public static Dictionary<Type, ArrowTypes> Types = new();


    public static void Register<T>(FortModule module) 
    {
        Register(typeof(T), module);
    }

    public static void Register(Type type, FortModule module) 
    {
        foreach (var arrow in type.GetCustomAttributes<CustomArrowsAttribute>()) 
        {
            const int offset = 11;
            if (type is null)
                return;
            var name = arrow.Name ?? $"{type.Namespace}.{type.Name}";
            var graphicFn = arrow.GraphicPickupInitializer ?? "CreateGraphicPickup";
            var stride = (ArrowTypes)offset + RiseCore.ArrowsRegistry.Count;
            MethodInfo graphic = type.GetMethod(graphicFn);

            ConstructorInfo ctor = type.GetConstructor(Array.Empty<Type>());
            ArrowLoader loader = null;
            if (ctor != null) 
            {
                loader = () => 
                {
                    var invoked = (patch_Arrow)ctor.Invoke(Array.Empty<object>());
                    invoked.ArrowType = stride;
                    return invoked;
                };
            }
            ArrowInfoLoader infoLoader = null;
            if (graphic == null || !graphic.IsStatic)
            {
                Logger.Log($"[Loader] [{module.Meta.Name}] No `static ArrowInfo CreateGraphicPickup()` method found on this Arrow {name}, falling back to normal arrow graphics.");
                infoLoader = () => {
                    return ArrowInfo.Create(new Image(TFGame.Atlas["arrows/arrow"]));
                };
            }
            else 
            {
                infoLoader = () => {
                    var identifier = (ArrowInfo)graphic.Invoke(null, Array.Empty<object>());
                    if (string.IsNullOrEmpty(identifier.Name))
                        identifier.Name = name;
                    return identifier;
                };
            }

            var pickupObject = new PickupObject() 
            {
                Name = name,
                ID = (Pickups)RiseCore.PickupLoaderCount,
                Chance = arrow.Chance
            };

            RiseCore.ArrowsID[name] = stride;
            RiseCore.ArrowsRegistry[name] = new ArrowObject() 
            {
                Types = stride,
                InfoLoader = infoLoader,
                PickupType = pickupObject
            };
            RiseCore.ArrowNameMap[stride] = name;
            RiseCore.Arrows[stride] = loader;
            RiseCore.PickupRegistry[name] = pickupObject;
            RiseCore.PickupLoader[(Pickups)RiseCore.PickupLoaderCount] 
                = (pos, targetPos, _) => new ArrowTypePickup(pos, targetPos, stride);
            RiseCore.PickupLoaderCount++;
            Types.Add(type, stride);
        }
    }
}