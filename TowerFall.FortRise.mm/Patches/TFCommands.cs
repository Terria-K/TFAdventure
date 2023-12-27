using System.Reflection;
using Monocle;
using FortRise;

namespace TowerFall;

public static partial class patch_TFCommands 
{
    public extern static void orig_Init();

    public static void Init() 
    {
        orig_Init();
        Commands commands = Engine.Instance.Commands;
        commands.RegisterCommand("detours", args => 
        {
            if (!RiseCore.DebugMode) 
            {
                commands.Log("Command only available in Debug Mode");
                return;
            }
            RiseCore.LogDetours(Logger.LogLevel.Info);
        });
        commands.RegisterCommand("hitbox", args => 
        {
            if (!RiseCore.DebugMode) 
            {
                commands.Log("Command only available in Debug Mode");
                return;
            }
            if (Engine.Instance.Scene is not Level) 
            {
				commands.Log("Command can only be used during gameplay!");
                return;
            }
            patch_Level.DebugMode = !patch_Level.DebugMode;
        });
        commands.RegisterCommand("summon", args => 
        {
            if (!RiseCore.DebugMode) 
            {
                commands.Log("Command only available in Debug Mode");
                return;
            }
            if (Engine.Instance.Scene is not patch_Level level) 
            {
				commands.Log("Command can only be used during gameplay!");
                return;
            }
            var portals = level.Layers[0].GetList<QuestSpawnPortal>();
            portals.Shuffle<QuestSpawnPortal>(); 
            portals[0].AppearAndSpawn(args[0]);
        });
        commands.RegisterCommand("dump_oel", args => 
        {
            if (Engine.Instance.Scene is not patch_Level level) 
            {
				commands.Log("Command can only be used during gameplay!");
                return;
            }
            string name = "dumplevel.oel";
            if (args.Length > 0)
                name = args[0] + ".oel";
            level.XML.OwnerDocument.Save("DUMP/" + name);
        });
        commands.RegisterCommand("logtags", args => 
        {
            Engine.Instance.Scene.LogTags();
        });

        foreach (var module in FortRise.RiseCore.InternalFortModules) 
        {
            var types = module.GetType().Assembly.GetTypes();
            foreach (var type in types) 
            {
                if (!type.IsAbstract || !type.IsSealed) 
                    continue;
                
                foreach (var method in type.GetMethods()) 
                {
                    var customAttribute = method.GetCustomAttribute<CommandAttribute>();
                    if (customAttribute == null)
                        continue;
                    
                    commands.RegisterCommand(customAttribute.CommandName, args => {
                        // Don't be so confused about the parameters:
                        // method.Invoke(null, args); 
                        // and the current one are the not the same!
                        method.Invoke(null, new object[] { args });
                    });
                }
            }
        }
    }
}