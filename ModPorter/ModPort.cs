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

    public static void AddModules(PortModule port) 
    {
        Porters.Add(port);
    }

    public static void ClearContext() 
    {
        Porters.Clear();
    }

    public static void FNAShortcut(string path, string output = null) 
    {
        Porters.Add(new FNAPort());
        StartPorting(path, output ?? path, null);
    }

    public static void NetCoreShortcut(string path, string output = null, bool inline = true) 
    {
        Porters.Add(new CorePort());
        StartPorting(path, output ?? path, inline);
    }

    public static void StartPorting(string path, string output = null, IAssemblyResolver resolver = null)  
    {
        StartPorting(path, output, true, resolver);
    }

    public static void StartPorting(string path, string output = null, bool noInlining = true, IAssemblyResolver resolver = null) 
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

            StartPorting(moduleDefinition, noInlining, resolver);

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
        StartPorting(mod, true, resolver);
    }

    public static void StartPorting(ModuleDefinition mod, bool noInlining = true, IAssemblyResolver resolver = null) 
    {
        StartPorting(mod, noInlining, false, resolver);
    }

    public static void StartPorting(ModuleDefinition mod, bool noInlining = true, bool sharedDeps = false, IAssemblyResolver resolver = null) 
    {
        foreach (var modifier in Porters) 
        {
            modifier.PrePatch(mod);
        }
        foreach (var modifier in Porters) 
        {
            if (modifier.CanPort(mod)) 
            {
                using var modder = new PortMonoModder(modifier) 
                {
                    Module = mod,
                    MissingDependencyThrow = false,
                    AssemblyResolver = resolver,
                    NoInlining = noInlining,
                    PrivateSystemLinkRelinker = modifier.PrivateSystemLibsRelink,
                    SharedDeps = sharedDeps
                };


                modder.MapDependencies();
                modder.AutoPatch();
                modifier.PostPatch(mod);
            }
        }
    }
}


public class PortMonoModder : MonoModder 
{
    private static ModuleDefinition ModPorter;

    private PortModule CurrentPortModule;

    public readonly HashSet<string> PrivateSystemLibs = new HashSet<string>() { "System.Private.CoreLib" };
    public bool NoInlining;
    public bool PrivateSystemLinkRelinker;
    public bool SharedDeps;

    public PortMonoModder(PortModule portModules) 
    {
        CurrentPortModule = portModules;
    }

    public override void Dispose() 
    {
        Module = null;
        if (SharedDeps) 
        {
            DependencyMap.Clear();
            DependencyCache.Clear();
        }
        ModPorter?.Dispose();

        base.Dispose();
    }

    public void AddReference(AssemblyName asmName) {
        if (!Module.AssemblyReferences.Any(asmRef => asmRef.Name == asmName.Name)) 
            Module.AssemblyReferences.Add(AssemblyNameReference.Parse(asmName.FullName));
    }

    public void AddReference(string name) => AddReference(Assembly.GetExecutingAssembly().GetReferencedAssemblies().First(asmName => asmName.Name == name));

    public override void MapDependencies()
    {
        CurrentPortModule.MapDependecies(this);
        AddReference(Assembly.GetExecutingAssembly().GetName());

        ModPorter ??= ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);
        DependencyCache[Assembly.GetExecutingAssembly().FullName] = ModPorter;
        base.MapDependencies();
    }

    public override IMetadataTokenProvider Relinker(IMetadataTokenProvider mtp, IGenericParameterProvider context)
    {
        IMetadataTokenProvider relinkedMetadataTokenProvider = base.Relinker(mtp, context);
        if (PrivateSystemLinkRelinker && relinkedMetadataTokenProvider is TypeReference typeRef && PrivateSystemLibs.Contains(typeRef.Scope.Name))
            return Module.ImportReference(FindType(typeRef.FullName));
        return relinkedMetadataTokenProvider;
    }


    public override void PatchRefs(ModuleDefinition mod)
    {
        base.PatchRefs(mod);

        CurrentPortModule.StartPort(this);
    }

    public override void AutoPatch()
    {
        CurrentPortModule.AutoPatch(this);
        base.AutoPatch();
    }

    public override void PatchRefsInMethod(MethodDefinition method)
    {
        base.PatchRefsInMethod(method);
        CurrentPortModule.PatchMethod(this, method);
    }
}