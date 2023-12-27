using System;
using System.IO;
using NLua;

namespace FortRise;

public class LuaModule : FortModule
{
    private LuaTable table;

    public LuaModule(ModuleMetadata metadata, Stream stream) 
    {
        Meta = metadata;
        ID = "lua." + metadata.Author + "." + metadata.Name;
        Name = metadata.Name;
        var tableName = metadata.Name.Replace(" ", "_");
        table = RiseCore.Lua.LoadScript(stream, tableName).Call(Array.Empty<object>())[0] as LuaTable;
        table["meta"] = Meta;
        table["id"] = ID;
        table["name"] = Name;
    }

    public override void LoadContent()
    {
        table["content"] = Content;
        if (table.Contains("loadContent"))
        {
            (table["loadContent"] as LuaFunction).Call(table);
        }
    }

    public override void Load()
    {
        if (table.Contains("load"))
        {
            (table["load"] as LuaFunction).Call(table);
        }
    }

    public override void Unload()
    {
        if (table.Contains("unload"))
        {
            (table["unload"] as LuaFunction).Call(table);
        }
    }

    public override void OnVariantsRegister(VariantManager manager, bool noPerPlayer = false)
    {
        if (table.Contains("onVariantsRegister"))
        {
            (table["onVariantsRegister"] as LuaFunction).Call(table, manager, noPerPlayer);
        }
    }
}