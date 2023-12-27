using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using NLua;

namespace FortRise;


// TODO Need more functionality, can be used for editor maybe?
public static partial class RiseCore
{
    public static class Lua 
    {
        public static NLua.Lua Context { get; private set; }
        public static Action<string> Require => require;
        private static Action<string> require;

        public static Action<string> LoadAssembly => loadAssembly;
        private static Action<string> loadAssembly;

        public static Action<string, string> AddToImport => addToImport;
        private static Action<string, string> addToImport;

        public static Func<string, string[]> ResourceTree => (name) => {
            if (string.IsNullOrEmpty(name))
                return null;
            
            name = name.Replace(".", "/");
            var path = name;
            if (Path.GetExtension(path) != ".lua")
                path += ".lua";

            if (RiseCore.ResourceTree.TryGetValue(path, out var res)) 
            {
                using var s = res.Stream;
                using TextReader sr = new StreamReader(s);
                var data = sr.ReadToEnd();

                return new string[] { path, data };
            }
            return null;
        };

        internal static void Initialize() 
        {
            try 
            {
                Context = new NLua.Lua();
            }
            catch (Exception e)
            {
                Logger.Error("[LUA] Failed to load Lua context. Lua mods might not work!");
                Logger.Error(e);
                return;
            }
            var assembly = typeof(Lua).Assembly;
            using Stream luaStream = assembly.GetManifestResourceStream("Content\\Scripts\\fort.lua");
            using TextReader sr = new StreamReader(luaStream);
            var texts = sr.ReadToEnd();
            var table = Context.DoString(texts, "fort");
            require = s => (table[0] as NLua.LuaFunction).Call(s);
            loadAssembly = s => (table[1] as NLua.LuaFunction).Call(s);
            addToImport = (s1, s2) => (table[2] as NLua.LuaFunction).Call(s1, s2);

            (table[3] as NLua.LuaFunction).Call(ResourceTree);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) 
            {
                AsmLoad(asm);
            }
        }

        public static void AsmLoad(Assembly asm) 
        {
            if (loadAssembly == null) { return; }

            try 
            {
                LoadAssembly(asm.FullName);
            }
            catch {}

            var caches = new HashSet<string>();

            foreach (var type in asm.GetTypes())
            {
                if (type.Namespace is null || caches.Contains(type.Namespace))
                    continue;

                string luanamespace;
                {
                    Span<char> nameSpan = stackalloc char[type.Namespace.Length + 1];
                    var ns = type.Namespace.AsSpan();
                    nameSpan[0] = '#';
                    for (int i = 0; i < ns.Length; i++) 
                    {
                        nameSpan[i + 1] = char.ToLowerInvariant(ns[i]);
                    }
                    luanamespace = nameSpan.ToString();
                }

                caches.Add(type.Namespace);
                addToImport(luanamespace, type.Namespace);
            }
        }

        public static NLua.LuaFunction LoadScript(string path) 
        {
            if (!File.Exists(path))
                return null;
            using var fs = File.OpenRead(path);
            return LoadScript(fs, Path.GetFileNameWithoutExtension(path));
        }

        public static NLua.LuaFunction LoadScript(Stream stream, string chunk) 
        {
            using TextReader sr = new StreamReader(stream);
            var texts = sr.ReadToEnd();
            return Context.LoadString(texts, chunk);
        }
    }
}

public static class LuaHelper 
{
    public static string Get(this LuaTable table, string name) 
    {
        return table[name] as string;
    }

    public static string Get(this LuaTable table, string name, string defaultValue) 
    {
        var value = table[name] as string;
        if (value == null)
            return defaultValue;
        return value;
    }

    public static bool TryGet(this LuaTable table, string name, out string result) 
    {
        var val = table[name] as string;
        if (val != null)
        {
            result = val;
            return true;
        }
        result = null;
        return false;
    }

    public static string[] GetStringArray(this LuaTable xml, string childName)
    {
        if (!xml.TryGet(childName, out var result))
        {
            return null;
        }
        string[] array = result.Split(new char[] { ',' });
        if (array == null)
        {
            return Array.Empty<string>();
        }
        string[] array2 = new string[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array2[i] = array[i];
        }
        return array2;
    }

    public static bool Contains(this LuaTable table, string name) 
    {
        return (table[name]) != null;
    }

    public static T GetEnum<T>(this LuaTable table, string childName) where T : struct
    {
        var value = table.Get(childName);
        if (System.Enum.IsDefined(typeof(T), value))
        {
            return (T)((object)System.Enum.Parse(typeof(T), value));
        }
        throw new System.Exception("The attribute value cannot be converted to the enum type.");
    }

    public static T GetEnum<T>(this LuaTable table, string childName, T defaultValue) where T : struct
    {
        var value = table.Get(childName);
        if (value == null)
        {
            return defaultValue;
        }
        if (System.Enum.IsDefined(typeof(T), value))
        {
            return (T)((object)System.Enum.Parse(typeof(T), value));
        }
        throw new System.Exception("The attribute value cannot be converted to the enum type.");
    }


    public static bool GetBool(this LuaTable table, string name) 
    {
        object val = table[name];
        if (val is bool b)
            return b;
        return false;
    }

    public static bool GetBool(this LuaTable table, string name, bool defaultValue) 
    {
        object val = table[name];
        if (val is bool b)
            return b;
        return defaultValue;
    }

    public static bool TryGetBool(this LuaTable table, string name, out bool result) 
    {
        var val = table[name];
        if (val is bool b)
        {
            result = b;
            return true;
        }
        result = false;
        return false;
    }

    public static int GetInt(this LuaTable table, string name) 
    {
        object val = table[name];
        if (val is long i)
            return (int)i;
        return 0;
    }

    public static int GetInt(this LuaTable table, string name, int defaultValue) 
    {
        object val = table[name];
        if (val is long i)
            return (int)i;
        return defaultValue;
    }

    public static bool TryGetInt(this LuaTable table, string name, out int result) 
    {
        var val = table[name];
        if (val is long i)
        {
            result = (int)i;
            return true;
        }
        result = 0;
        return false;
    }

    public static float GetFloat(this LuaTable table, string name) 
    {
        object val = table[name];
        if (val is double f)
            return (float)f;
        return 0f;
    }

    public static float GetFloat(this LuaTable table, string name, float defaultValue) 
    {
        object val = table[name];
        if (val is double f)
            return (float)f;
        return defaultValue;
    }

    public static LuaTable GetTable(this LuaTable table, string name) 
    {
        return table[name] as LuaTable;
    }


    public static bool TryGetTable(this LuaTable table, string name, out LuaTable result) 
    {
        var val = table[name] as LuaTable;
        if (val != null)
        {
            result = val;
            return true;
        }
        result = null;
        return false;
    }

    public static Vector2 Position(this LuaTable table) 
    {
        var x = (long)table["x"];
        var y = (long)table["y"];
        return new Vector2(x, y);
    }
}