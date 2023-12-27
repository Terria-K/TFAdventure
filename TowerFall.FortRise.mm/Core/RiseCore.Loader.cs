using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

namespace FortRise;

public static partial class RiseCore 
{
    public static class Loader 
    {
        internal static HashSet<string> BlacklistedMods;
        private static List<ModuleMetadata> DelayedMods = new();
        internal static List<string> CantLoad = new();

        internal static void InitializeMods() 
        {
            var directory = Directory.GetDirectories("Mods");
            foreach (var dir in directory) 
            {
                if (dir.Contains("_RelinkerCache"))
                    continue;    
                var dirInfo = new DirectoryInfo(dir);
                if (BlacklistedMods != null && BlacklistedMods.Contains(dirInfo.Name))  
                {
                    Logger.Verbose($"[Loader] Ignored {dir} as it's blacklisted");
                    continue;
                }
                LoadDir(dir);
            }

            var files = Directory.GetFiles("Mods");
            foreach (var file in files) 
            {
                if (!file.EndsWith("zip"))
                    continue;
                var fileName = Path.GetFileName(file);
                if (BlacklistedMods != null && BlacklistedMods.Contains(Path.GetFileName(fileName))) 
                {
                    Logger.Verbose($"[Loader] Ignored {file} as it's blacklisted");
                    continue;
                }
                LoadZip(file);
            }

            foreach (var delayMod in DelayedMods) 
            {
                LoadMod(delayMod, true);
            }
            DelayedMods.Clear();
        }

        public static void LoadDir(string dir) 
        {
            var metaPath = Path.Combine(dir, "meta.json");
            ModuleMetadata moduleMetadata = null;
            if (!File.Exists(metaPath))
                metaPath = Path.Combine(dir, "meta.hjson");
            if (!File.Exists(metaPath))
                return;
            
            moduleMetadata = ParseMetadata(dir, metaPath);
            Loader.LoadMod(moduleMetadata, false);
        }

        public static void LoadZip(string file)
        {
            using var zipFile = ZipFile.Read(file);

            ModuleMetadata moduleMetadata = null;
            string metaFile = "meta.json";
            if (!zipFile.ContainsEntry(metaFile))
                metaFile = "meta.hjson";
             if (!zipFile.ContainsEntry(metaFile))
                return;
            
            var entry = zipFile[metaFile];
            using var memStream = entry.ExtractStream();

            if (Path.GetExtension(metaFile) == ".hjson")
                moduleMetadata = ParseMetadataWithHJson(file, memStream, true);
            else
                moduleMetadata = ParseMetadataWithJson(file, memStream, true);
            Loader.LoadMod(moduleMetadata, false);
        }

        public static bool CheckDependencies(ModuleMetadata metadata, bool isDelay) 
        {
            bool notFound = false;
            if (metadata.Dependencies != null) 
            {
                foreach (var dep in metadata.Dependencies) 
                {
                    if (RiseCore.InternalModuleMetadatas.Contains(dep))
                        continue;

                    if (!isDelay) 
                    {
                        DelayedMods.Add(metadata);
                        return false;
                    }

                    Logger.Error($"[Loader] [{metadata.Name}] Dependency {dep} not found!");
                    notFound = true;
                }
            }
            return !notFound;
        }

        public static void LoadMod(ModuleMetadata metadata, bool isDelayed = false) 
        {
            if (metadata == null)
                return;

            if (!CheckDependencies(metadata, isDelayed)) 
            {
                if (!isDelayed)
                    return;
                string path; 
                if (!string.IsNullOrEmpty(metadata.PathZip))
                    path = metadata.PathZip.Replace("Mods/", "");
                else
                    path = metadata.PathDirectory.Replace("Mods\\", "");
                
                CantLoad.Add(path);
                return;
            }
            
            Assembly asm = null;
            LuaModule luaModule = null;
            ModResource modResource;
            if (!string.IsNullOrEmpty(metadata.PathZip)) 
            {
                modResource = new ZipModResource(metadata);

                RiseCore.ResourceTree.AddMod(metadata, modResource);

                if (!RiseCore.DisableFortMods) 
                {
                    using var zip = new ZipFile(metadata.PathZip);
                    var dllPath = metadata.DLL.Replace('\\', '/');
                    if (zip.ContainsEntry(dllPath)) 
                    {
                        using var dll = zip[dllPath].ExtractStream();
                        asm = Relinker.LoadModAssembly(metadata, metadata.DLL, dll);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(metadata.PathDirectory)) 
            {
                modResource = new FolderModResource(metadata);

                RiseCore.ResourceTree.AddMod(metadata, modResource);
                var fullDllPath = Path.Combine(metadata.PathDirectory, metadata.DLL);
                var fullLuaPath = Path.Combine(metadata.PathDirectory, "main.lua");

                if (!RiseCore.DisableFortMods) 
                {
                    if (File.Exists(fullDllPath)) 
                    {
                        using var stream = File.OpenRead(fullDllPath);
                        asm = Relinker.LoadModAssembly(metadata, metadata.DLL, stream);
                    }
                    else if (File.Exists(fullLuaPath)) 
                    {
                        using var mainLua = File.OpenRead(fullLuaPath);
                        luaModule = new LuaModule(metadata, mainLua);
                        var content = modResource.Content;
                        luaModule.Content = content;
                        ModuleGuids.Add(luaModule.ID);
                        luaModule.InternalLoad();
                        lock (InternalFortModules) 
                            InternalFortModules.Add(luaModule);
                        Logger.Info($"[Loader] {luaModule.ID}: {luaModule.Name} Lua Registered.");
                    }
                }
            }
            else 
            {
                Logger.Error($"[Loader] Mod {metadata.Name} not found");
                return;
            }

            if (asm != null) 
            {
                LoadAssembly(metadata, modResource, asm);
            }

            InternalMods.Add(modResource);
            RiseCore.InternalModuleMetadatas.Add(metadata);

            if (metadata.DLL == string.Empty) 
            {
                // Generate custom guids for DLL-Less mods
                string generatedGuid;
                if (string.IsNullOrEmpty(metadata.Author)) 
                {
                    Logger.Warning($"[Loader] [{metadata.Name}] Author is empty. Guids might conflict with other DLL-Less mods.");
                    generatedGuid = $"{metadata.Name}.{metadata.Version}";
                }
                else 
                    generatedGuid = $"{metadata.Name}.{metadata.Version}.{metadata.Author}";
                if (ModuleGuids.Contains(generatedGuid)) 
                {
                    Logger.Error($"[Loader] [{metadata.Name}] Guid conflict with {generatedGuid}");
                    return;
                }
                Logger.Verbose($"[Loader] [{metadata.Name}] Guid generated! {generatedGuid}");
                return;
            }
        }

        private static void LoadAssembly(ModuleMetadata metadata, ModResource resource, Assembly asm) 
        {
            foreach (var t in asm.GetTypes())
            {
                var customAttribute = t.GetCustomAttribute<FortAttribute>();
                if (customAttribute == null)
                    continue;
                
                FortModule module = Activator.CreateInstance(t) as FortModule;
                if (metadata.Name == string.Empty)
                {
                    metadata.Name = customAttribute.Name;
                }
                module.Meta = metadata;
                module.Name = customAttribute.Name;
                module.ID = customAttribute.GUID;
                var content = resource.Content;
                module.Content = content;
                module.ParseArgs(RiseCore.ApplicationArgs);
                module.InternalLoad();
                lock (InternalFortModules) 
                    InternalFortModules.Add(module);

                ModuleGuids.Add(module.ID);

                Logger.Info($"[Loader] {module.ID}: {module.Name} Loaded.");
                break;
            }
        }
    }
}