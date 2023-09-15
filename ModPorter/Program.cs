using Mono.Cecil;

namespace ModPorter;

internal class Program 
{
    private static void Main(string[] args) 
    {
        ModPort.AddModules(new FNAPort());
        string output = null;
        if (args.Length > 1) 
        {
            output = args[1];
        }
        ModPort.StartPorting(args[0], output);
    }
}