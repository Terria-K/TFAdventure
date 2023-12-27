using System;
using System.Xml;
using System.Reflection;
using MonoMod;
using FortRise;
using TowerFall;
using System.IO;

namespace Monocle;

public static class patch_Calc 
{
    public static string LOADPATH;
    public static string DW_LOADPATH;
    public static XmlDocument LoadXML(Stream stream) 
    {
        using var textReader = new StreamReader(stream);
        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(textReader.ReadToEnd());
        return xmlDocument;
    }

    [MonoModReplace]
    public static T StringToEnum<T>(string str) where T : struct, Enum 
    {
        if (Enum.IsDefined(typeof(T), str)) 
        {
            return (T)Enum.Parse(typeof(T), str);
        }
        // Try to get the pickup value
        else if (RiseCore.PickupRegistry.TryGetValue(str, out var s) && s.ID is T custmPickup) 
        {
            return custmPickup;
        }
        Logger.Error("[Calc][StringToEnum] The string cannot be converted to the enum type");
        return default;
    }

    public static bool TryStringToEnum<T>(string str, out T result) where T : struct 
    {
        if (Enum.IsDefined(typeof(T), str)) 
        {
            result = (T)Enum.Parse(typeof(T), str);
            return true;
        }
        // Try to get the pickup value
        else if (RiseCore.PickupRegistry.TryGetValue(str, out var s) && s.ID is T custmPickup) 
        {
            result = custmPickup;
            return true;
        }
        result = default;
        return false;
    }

    [MonoModReplace]
    public static Delegate GetMethod<T>(object obj, string method) where T : class 
    {
        if (obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
        {
            return Delegate.CreateDelegate(typeof(T), obj, method);
        }
        if (obj.GetType().BaseType
            .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
        {
            return Delegate.CreateDelegate(typeof(T), obj, method);
        }
        return null;
    }

    public static T[] ChildEnumArray<T>(this XmlElement xml, string childName)
    where T : struct
    {
        var childs = xml[childName].InnerText.Split(',');
        if (childs == null)
            return Array.Empty<T>();
        var array = new T[childs.Length];
        int i = 0;
        foreach (var child in childs) 
        {
            if (System.Enum.TryParse<T>(child, out T result)) 
            {
                array[i] = result;
            }
            i++;
        }
        return array;
    }

    public static string[] ChildStringArray(this XmlElement xml, string childName) 
    {
        if (!xml.HasChild(childName))
            return null;
        var childs = xml[childName].InnerText.Split(',');
        if (childs == null)
            return Array.Empty<string>();
        
        var array = new string[childs.Length];
        for (int i = 0; i < childs.Length; i++) 
        {
            array[i] = childs[i];
        }
        return array;
    }

    public static void IncompatibleWith(this Variant variant, Variant targetVariant) 
    {
        variant.Links.Add(targetVariant);
        targetVariant.Links.Add(variant);
    }

    public static void IncompatibleWith(this Variant variant, params Variant[] variants) 
    {
        foreach (var varia in variants) 
        {
            variant.Links.Add(varia);
            varia.Links.Add(variant);
        }
    }

    [MonoModReplace]
    public static void Log(params object[] obj) 
    {
        if (!RiseCore.DebugMode)
            return;

        foreach (object obj2 in obj)
        {
            Logger.Log(obj2);
        }
    }

    public static float Clamp(float value, float min, float max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}