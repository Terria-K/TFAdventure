using Mono.Cecil;

namespace ModPorter;

internal class Program 
{
    private static void Main(string[] args) 
    {
        ModPort.AddModules(new FNAPort());
        ModPort.AddModules(new CorePort());
        string output = null;
        bool inline = false;
        if (args.Length > 1) 
        {
            output = args[1];
        }
        if (args.Length > 2) 
        {
            if (args[2] == "--inline") 
            {
                inline = true;
            }
        }
        ModPort.StartPorting(args[0], output, inline);
        NetCoreUtils.GenerateRuntimeConfig(output ?? args[0]);
    }
}