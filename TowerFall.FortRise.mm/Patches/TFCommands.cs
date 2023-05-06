using System.Reflection;
using Monocle;
using FortRise;

namespace TowerFall;

public static class patch_TFCommands 
{
    public extern static void orig_Init();

    public static void Init() 
    {
        orig_Init();
        Commands commands = Engine.Instance.Commands;

        foreach (var module in FortRise.RiseCore.Modules) 
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