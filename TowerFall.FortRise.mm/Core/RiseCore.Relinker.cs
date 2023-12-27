using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Ionic.Zip;
using ModPorter;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using MonoMod;
using TowerFall;

namespace FortRise;

// https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Everest/Everest.Relinker.cs

public static partial class RiseCore 
{

    /// <summary>
    /// A TowerFall checksum executable.
    /// </summary>
    public static string GameChecksum { get; internal set; }

    public static class Relinker 
    {
        private static bool temporaryASM;
        private static ModuleMetadata currentMetaRelinking;
        public static List<Assembly> RelinkedAssemblies = new();
        private static Dictionary<string, ModuleDefinition> relinkedModules = new ();

        private static Dictionary<string, ModuleDefinition> sharedRelinkModuleMap;
        public static Dictionary<string, ModuleDefinition> SharedRelinkModuleMap
        {
            get 
            {
                if (sharedRelinkModuleMap != null)
                    return sharedRelinkModuleMap;
                
                sharedRelinkModuleMap = new Dictionary<string, ModuleDefinition>();
                foreach (var path in Directory.GetFiles(GameRootPath))
                {
                    var name = Path.GetFileName(path);
                    if (name.EndsWith(".mm.dll")) 
                    {
                        string modName = Path.GetFileNameWithoutExtension(path);
                        string relinkedPath;
                        if (name == "TowerFall.FortRise.mm.dll") 
                        {
                            relinkedPath = typeof(TFGame).Assembly.Location;
                        }
                        else 
                        {
                            Logger.Warning($"[Relinker] Found unknown mod assembly {name}");
                            relinkedPath = name.Substring(0, modName.Length - 3);
                            var pathRelinked = Path.Combine(GameRootPath, relinkedPath + ".dll");
                            if (File.Exists(pathRelinked))
                                Logger.Info($"[RELINKER] Remapping to {Path.GetFileName(pathRelinked)}");
                            else {
                                Logger.Info($"[RELINKER] Couldn't remap, ignoring...");
                                continue;
                            }
                        }
                        sharedRelinkModuleMap[modName] = ModuleDefinition.ReadModule(relinkedPath, new ReaderParameters(ReadingMode.Immediate));
                    }
                }
                return sharedRelinkModuleMap;
            }
        }


        private static Dictionary<string, object> sharedRelinkMap;
        public static Dictionary<string, object> SharedRelinkMap 
        {
            get 
            {
                if (sharedRelinkMap != null)
                    return sharedRelinkMap;
                
                sharedRelinkMap = new Dictionary<string, object>();

                return sharedRelinkMap;
            }
        }

        private static MonoModder modder;
        public static MonoModder Modder {
            get {
                if (modder != null)
                    return modder;

                modder = new MonoModder() {
                    CleanupEnabled = false,
                    RelinkModuleMap = SharedRelinkModuleMap,
                    RelinkMap = SharedRelinkMap,
                    DependencyDirs = {
                        GameRootPath
                    },
                    ReaderParameters = {
                        SymbolReaderProvider = new RelinkerSymbolReaderProvider()
                    }
                };

                return modder;
            }
            set {
                modder = value;
            }
        }

        private static ModuleDefinition runtimeRulesModule;

        public static Assembly LoadModAssembly(ModuleMetadata meta, string asmDLL, Stream stream) 
        {
            var asmName = Path.GetFileNameWithoutExtension(asmDLL);

            var dirPath = Path.Combine(GameRootPath, "Mods", "_RelinkerCache");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            if (Environment.Is64BitProcess && !string.IsNullOrEmpty(meta.NativePath)) 
            {
                var nativePath = Path.Combine(dirPath, "Natives", $"{asmName}.{meta.Name}");
                if (!Directory.Exists(nativePath))
                    Directory.CreateDirectory(nativePath);
                if (!string.IsNullOrEmpty(meta.PathZip)) 
                {
                    using var zipFile = ZipFile.Read(meta.PathZip);
                    try 
                    {
                        foreach (var entry in zipFile.Entries) 
                        {
                            if (entry.FileName.StartsWith(meta.NativePath)) 
                            {
                                entry.Extract(nativePath, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    }
                    catch (IOException) 
                    {
                        Logger.Error("[Relinker] Couldn't extract all of the native files as its in used by another process");
                    }
                }
                else if (!string.IsNullOrEmpty(meta.PathDirectory))
                {
                    try 
                    {
                        foreach (var files in Directory.GetFiles(Path.Combine(meta.PathDirectory, meta.NativePath))) 
                        {
                            File.Copy(files, Path.Combine(nativePath, Path.GetFileName(files)), true);
                        }
                    }
                    catch (IOException) 
                    {
                        Logger.Error("[Relinker] Couldn't copy all of the native files as its in used by another process");
                    }
                }
                else
                    Logger.Error($"[Relinker] Cannot find directory.");
                NativeMethods.AddDllDirectory(nativePath);
            }
            else if (!string.IsNullOrEmpty(meta.NativePathX86))
            {
                var nativePath = Path.Combine(dirPath, "NativesX86", $"{asmName}.{meta.Name}");
                if (!Directory.Exists(nativePath))
                    Directory.CreateDirectory(nativePath);
                if (!string.IsNullOrEmpty(meta.PathZip)) 
                {
                    using var zipFile = ZipFile.Read(meta.PathZip);
                    try 
                    {
                        foreach (var entry in zipFile.Entries) 
                        {
                            if (entry.FileName.StartsWith(meta.NativePathX86)) 
                            {
                                entry.Extract(nativePath, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    }
                    catch (IOException) 
                    {
                        Logger.Error("[Relinker] Couldn't extract all of the native files as its in used by another process");
                    }
                }
                else if (!string.IsNullOrEmpty(meta.PathDirectory))
                {
                    try 
                    {
                        foreach (var files in Directory.GetFiles(Path.Combine(meta.PathDirectory, meta.NativePathX86))) 
                        {
                            File.Copy(files, Path.Combine(nativePath, Path.GetFileName(files)), true);
                        }
                    }
                    catch (IOException) 
                    {
                        Logger.Error("[Relinker] Couldn't copy all of the native files as its in used by another process");
                    }
                }
                else
                    Logger.Error($"[Relinker] Cannot find directory.");
                NativeMethods.AddDllDirectory(nativePath);
            }
            return Relink(meta, asmName, stream);
        }

        public static Assembly Relink(
            ModuleMetadata meta, string asmName, Stream stream) 
        {
            ModuleDefinition module = null;
            asmName = asmName.Replace(" ", "_");

            var dirPath = Path.Combine(GameRootPath, "Mods", "_RelinkerCache");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var cachedPath = Path.Combine(dirPath, $"{asmName}.{meta.Name}.dll");
            var cachedChecksumPath = cachedPath.Substring(0, cachedPath.Length - 4) + ".sum";

            var checksums = new string[2];
            checksums[0] = GameChecksum;
            checksums[1] = RiseCore.GetChecksum(ref stream).ToHexadecimalString();
            

            if (File.Exists(cachedPath) && File.Exists(cachedChecksumPath) && 
                ChecksumsEqual(checksums, File.ReadAllLines(cachedChecksumPath))) 
            {
                Logger.Info($"[Relinker] Loading cached assembly for {meta} - {asmName}");

                var mod = ModuleDefinition.ReadModule(cachedPath);
                try 
                {
                    var asm = meta.AssemblyContext.LoadFromAssemblyPath(cachedPath);
                    RelinkedAssemblies.Add(asm);

                    if (!relinkedModules.ContainsKey(mod.Assembly.Name.Name)) 
                    {
                        relinkedModules.Add(mod.Assembly.Name.Name, mod);
                        mod = null;
                    }
                    else 
                    {
                        Logger.Warning($"[Relinker] Encountered a module name conflict loading cached assembly {meta} - {asmName} - {module.Assembly.Name}");
                    }
                    return asm;
                }
                catch (Exception e) 
                {
                    Logger.Error($"[Relinker] Failed loading {meta} - {asmName}");
                    Logger.Error(e.ToString());
                    return null;
                }
                finally 
                {
                    mod?.Dispose();
                }
            }

            var dependencyResolver = GenerateModDependencyResolver(meta);

            AssemblyResolveEventHandler resolver = (s, r) => 
            {
                var dep = dependencyResolver(Modder, Modder.Module, r.Name, r.FullName);
                if (dep != null) 
                {
                    return dep.Assembly;
                }
                
                if (r.FullName.ToLowerInvariant().Contains("fna") || r.FullName.ToLowerInvariant().Contains("xna")) 
                {
                    var asmRefs = typeof(TFGame).Assembly.GetReferencedAssemblies();
                    for (int i = 0; i < asmRefs.Length; i++) 
                    {
                        var asmRef = asmRefs[i];
                        if (!asmRef.FullName.ToLowerInvariant().Contains("xna") &&
                            !asmRef.FullName.ToLowerInvariant().Contains("fna") &&
                            !asmRef.FullName.ToLowerInvariant().Contains("monogame"))
                                continue;
                            
                        return ((DefaultAssemblyResolver)modder.AssemblyResolver).Resolve(AssemblyNameReference.Parse(asmRef.FullName));
                    }
                }
                return null;
            };

            try 
            {
                currentMetaRelinking = meta;
                MonoModder modder = Modder;
                modder.Input = stream;
                modder.OutputPath = cachedPath;
                modder.MissingDependencyResolver = dependencyResolver;

                var metaPath = meta.DLL.Substring(0, meta.DLL.Length - 4) + ".pdb";
                modder.ReaderParameters.SymbolStream = OpenSymbol(meta, metaPath);
                modder.ReaderParameters.ReadSymbols = modder.ReaderParameters.SymbolStream != null;

                ((DefaultAssemblyResolver)modder.AssemblyResolver).ResolveFailure += resolver;

                if (modder.ReaderParameters.SymbolReaderProvider != null && modder.ReaderParameters.SymbolReaderProvider is RelinkerSymbolReaderProvider) 
                {
                    ((RelinkerSymbolReaderProvider)modder.ReaderParameters.SymbolReaderProvider).Format = DebugSymbolFormat.PDB;
                }

                try 
                {
                    modder.ReaderParameters.ReadSymbols = true;
                    modder.Read();
                }
                catch 
                {
                    modder.ReaderParameters.SymbolStream?.Dispose();
                    modder.ReaderParameters.SymbolStream = null;
                    modder.ReaderParameters.ReadSymbols = false;
                    stream.Seek(0, SeekOrigin.Begin);
                    modder.Read();
                }

                if (modder.ReaderParameters.SymbolReaderProvider != null && modder.ReaderParameters.SymbolReaderProvider is RelinkerSymbolReaderProvider) 
                {
                    ((RelinkerSymbolReaderProvider)modder.ReaderParameters.SymbolReaderProvider).Format = DebugSymbolFormat.Auto;
                }

                if (runtimeRulesModule == null) 
                {
                    string rulesPath = Path.Combine(
                        Path.GetDirectoryName(typeof(TFGame).Assembly.Location),
                        Path.GetFileNameWithoutExtension(typeof(TFGame).Assembly.Location) + ".FortRise.mm.dll");
                    
                    if (!File.Exists(rulesPath)) 
                    {
                        rulesPath = Path.Combine(
                            Path.GetDirectoryName(typeof(TFGame).Assembly.Location),
                            "TowerFall.FortRise.mm.dll"
                        );
                    }
                    if (!File.Exists(rulesPath))
                        throw new InvalidOperationException($"[Relinker] Couldn't find runtime rules .FortRise.mm.dll");

                    if (File.Exists(rulesPath)) 
                    {
                        runtimeRulesModule = ModuleDefinition.ReadModule(rulesPath, new ReaderParameters(ReadingMode.Immediate));
                    }
                }

                modder.MapDependencies();
                modder.MapDependencies(runtimeRulesModule);
                var runtimeRulesType = runtimeRulesModule.GetType("MonoMod.MonoModRules");
                modder.ParseRules(runtimeRulesModule);
                if (runtimeRulesType != null)
                    runtimeRulesModule.Types.Add(runtimeRulesType);

                modder.ParseRules(modder.Module);
                modder.AutoPatch();

                ModPort.AddModules(new CorePort());
                ModPort.StartPorting(modder.Module, true, true);

                ISymbolWriterProvider symbolWriterProvider = modder.WriterParameters.SymbolWriterProvider;

                Retry:
                try 
                {
                    Logger.Info("[Relinker] Try writing the module with symbols");
                    modder.WriterParameters.SymbolWriterProvider = symbolWriterProvider;
                    modder.WriterParameters.WriteSymbols = true;
                    using var fs = File.Create(cachedPath);
                    modder.Write(fs);
                }
                catch 
                {
                    try 
                    {
                        Logger.Info("[Relinker] Writing the module without symbols");
                        modder.WriterParameters.SymbolWriterProvider = null;
                        modder.WriterParameters.WriteSymbols = false;
                        using var fs = File.Create(cachedPath);
                        modder.Write(fs);
                    }
                    catch when (!temporaryASM) 
                    {
                        Logger.Error($"[Relinker] {cachedPath} is currently in used.");
                        temporaryASM = true;
                        long stamp2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        cachedPath = Path.Combine(Path.GetTempPath(), $"FortRise.{asmName}.{Path.GetFileNameWithoutExtension(dirPath)}.{stamp2}.dll");
                        Logger.Info($"[Relinker] Moving to {cachedPath}");
                        modder.Module.Name += "." + stamp2;
                        modder.Module.Assembly.Name.Name += "." + stamp2;
                        modder.OutputPath = cachedPath;
                        goto Retry;
                    }
                }
                finally 
                {
                    modder.WriterParameters.SymbolWriterProvider = symbolWriterProvider;
                }

                module = modder.Module;
            }
            catch (Exception e) 
            {
                Logger.Error($"[Relinker] Failed Relinking {meta} - {asmName}");
                Logger.Error(e.ToString());
            }
            finally 
            {
                ((DefaultAssemblyResolver)Modder.AssemblyResolver).ResolveFailure -= resolver;
                Modder.ReaderParameters.SymbolStream?.Dispose();
                if (module != Modder.Module)
                    Modder.Module?.Dispose();
                Modder.Module = null;

                Modder.ClearCaches(moduleSpecific: true);
            }

            try 
            {
                if (File.Exists(cachedChecksumPath))
                    File.Delete(cachedChecksumPath);
                
                if (!temporaryASM) 
                {
                    File.WriteAllLines(cachedChecksumPath, checksums);
                }

                if (Modder != null) 
                {
                    foreach (AssemblyNameReference aref in module.AssemblyReferences) 
                    {
                        if (Modder.DependencyCache.ContainsKey(aref.FullName))
                            Logger.Verbose($"[Relinker] dep. {module.Name} -> (({aref.FullName}), ({aref.Name})) found");
                        else
                            Logger.Verbose($"[Relinker] dep. {module.Name} -> (({aref.FullName}), ({aref.Name})) NOT FOUND");
                    }
                }

                var asm = meta.AssemblyContext.LoadFromAssemblyPath(cachedPath);
                RelinkedAssemblies.Add(asm);
                if (!relinkedModules.ContainsKey(module.Assembly.Name.Name)) 
                {
                    relinkedModules.Add(module.Assembly.Name.Name, module);
                    module = null;
                }
                else 
                {
                    Logger.Log($"[Relinker] Encountered a module name conflict loading cached assembly {meta} - {asmName} - {module.Assembly.Name}", Logger.LogLevel.Warning);
                }
                return asm;
            }
            catch (Exception e) 
            {
                Logger.Error($"[Relinker] Failed Loading {meta} - {asmName}");
                Logger.Error(e.ToString());
                return null;
            }
            finally 
            {
                module?.Dispose();
            }
        }


        private static MissingDependencyResolver GenerateModDependencyResolver(ModuleMetadata meta) 
        {
            if (!string.IsNullOrEmpty(meta.PathZip)) 
            {
                return (mod, main, name, fullname) => 
                {
                    if (relinkedModules.TryGetValue(name, out ModuleDefinition def)) 
                    {
                        return def;
                    }

                    string path = name + ".dll";
                    if (!string.IsNullOrEmpty(meta.DLL))
                        path = Path.Combine(Path.GetDirectoryName(meta.DLL), path);
                    path = path.Replace('\\', '/');

                    using var zip = new ZipFile(meta.PathZip);
                    foreach (var entry in zip.Entries) 
                    {
                        if (entry.FileName != path)
                            continue;
                        using var memStream = entry.ExtractStream();
                        return ModuleDefinition.ReadModule(memStream, mod.GenReaderParameters(false));
                    }

                    Logger.Log($"[Relinker] Couldn't find the dependency {main.Name} -> {fullname}, ({name})");
                    return null;
                };
            }
            if (!string.IsNullOrEmpty(meta.PathDirectory)) 
            {
                return (mod, main, name, fullname) => 
                {
                    if (relinkedModules.TryGetValue(name, out ModuleDefinition def)) 
                    {
                        return def;
                    }

                    string path = name + ".dll";
                    path = Path.Combine(meta.PathDirectory, path);
                    if (File.Exists(path)) 
                    {
                        return ModuleDefinition.ReadModule(path, mod.GenReaderParameters(false, path));
                    }
                    
                    Logger.Log($"[Relinker] Couldn't find the dependency {main.Name} -> {fullname}, ({name})");
                    return null;
                };
            }
            return null;
        }

        private static Stream OpenSymbol(ModuleMetadata metadata, string name) 
        {
            if (!string.IsNullOrEmpty(metadata.PathZip)) 
            {
                using var zipFile = new ZipFile(metadata.PathZip);
                foreach (var entry in zipFile.Entries) 
                {
                    if (!name.Contains(entry.FileName))
                        continue;
                    return entry.ExtractStream();
                }
            }
            if (!string.IsNullOrEmpty(metadata.PathDirectory)) 
            {
                var pdbPath = Path.Combine(metadata.PathDirectory, name);
                if (File.Exists(pdbPath)) 
                {
                    return File.OpenRead(pdbPath);
                }
            }
            return null;
        }

        public static bool ChecksumsEqual(string[] a, string[] b) {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++) 
            {
                if (a[i].Trim() != b[i].Trim())
                    return false;
            }
            return true;
        }
    }
}

public class RelinkerSymbolReaderProvider : ISymbolReaderProvider {

    public DebugSymbolFormat Format;

    public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream) {
        switch (Format) {
            case DebugSymbolFormat.MDB:
                return new MdbReaderProvider().GetSymbolReader(module, symbolStream);

            case DebugSymbolFormat.PDB:
                if (IsPortablePdb(symbolStream))
                    return new PortablePdbReaderProvider().GetSymbolReader(module, symbolStream);
                return new NativePdbReaderProvider().GetSymbolReader(module, symbolStream);

            default:
                return null;
        }
    }

    public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName) {
        return null;
    }

    public static bool IsPortablePdb(Stream stream) {
        long start = stream.Position;
        if (stream.Length - start < 4)
            return false;
        try {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
                return reader.ReadUInt32() == 0x424a5342;
        } finally {
            stream.Seek(start, SeekOrigin.Begin);
        }
    }

}