using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;

namespace ModPorter;

public static class ModPort 
{
    private static List<PortModule> Porters = new();
    private static List<AssemblyModifier> Modifiers = new();

    public static void AddModules(PortModule port) 
    {
        Porters.Add(port);
    }

    public static void AddAssemblyModifier(AssemblyModifier modifier) 
    {
        Modifiers.Add(modifier);
    }

    public static void StartPorting(string path, string output = null, IAssemblyResolver resolver = null) 
    {
        ModuleDefinition moduleDefinition = null;
        try 
        {
            ReaderParameters readerParams = new ReaderParameters()  { ReadSymbols = false };
            try 
            {
                moduleDefinition = ModuleDefinition.ReadModule(path, readerParams);
            }
            catch (SymbolsNotFoundException)
            {
                readerParams.ReadSymbols = false;
                moduleDefinition = ModuleDefinition.ReadModule(path, readerParams);
            }
            catch (SymbolsNotMatchingException)
            {
                readerParams.ReadSymbols = false;
                moduleDefinition = ModuleDefinition.ReadModule(path, readerParams);
            }

            StartPorting(moduleDefinition, resolver);

            using var stream = File.Create(output ?? path);

            moduleDefinition.Write(stream, new WriterParameters() { WriteSymbols = readerParams.ReadSymbols });
        }
        finally 
        {
            moduleDefinition?.Dispose();
        }
    }

    public static void StartPorting(ModuleDefinition mod, IAssemblyResolver resolver = null) 
    {
        foreach (var modifier in Modifiers) 
        {
            modifier.ModifyAssembly(mod);
        }
        using var modder = new PortMonoModder(Porters) 
        {
            Module = mod,
            MissingDependencyThrow = false,
            AssemblyResolver = resolver
        };

        modder.PatchRefs(mod);
        modder.MapDependencies();
        modder.AutoPatch();
    }
}

public abstract class AssemblyModifier 
{
    public abstract void ModifyAssembly(ModuleDefinition mod);
}

public class Assembly32BitNotRequired : AssemblyModifier
{
    public override void ModifyAssembly(ModuleDefinition mod)
    {
        mod.Attributes &= ~(ModuleAttributes.Required32Bit | ModuleAttributes.Preferred32Bit);
    }
}

public class PortMonoModder : MonoModder 
{
    private static List<PortModule> Porters = new();
    private static ModuleDefinition ModPorter;

    public PortMonoModder(List<PortModule> portModules) 
    {
        Porters = portModules;
    }

    public override void Dispose() 
    {
        Module = null;
        ModPorter?.Dispose();

        base.Dispose();
    }

    private void AddReference(string name) {
        var asmName = Assembly.GetExecutingAssembly().GetReferencedAssemblies().First(asmName => asmName.Name == name);
        if (!Module.AssemblyReferences.Any(asmRef => asmRef.Name == asmName.Name)) {
            Module.AssemblyReferences.Add(AssemblyNameReference.Parse(asmName.FullName));
        }
    }

    public override void MapDependencies()
    {
        ModPorter ??= ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);
        DependencyCache[Assembly.GetExecutingAssembly().FullName] = ModPorter;
        base.MapDependencies();
    }


    public override void PatchRefs(ModuleDefinition mod)
    {
        base.PatchRefs(mod);

        foreach (var port in Porters) 
        {
            port.StartPort(this);
        }
    }
}