using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using TeuJson;

namespace ModPorter;

public static class NetCoreUtils 
{
    public static void GenerateRuntimeConfig(string asmInput, string[] additionalDeps = null) 
    {
        additionalDeps ??= Array.Empty<string>();
        Console.WriteLine($"Generating Runtime Config for {asmInput}");

        var framework = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
        
        var netVer = framework.Substring(21);

        using var runtimeConfigFs = File.OpenWrite(Path.ChangeExtension(asmInput, ".runtimeconfig.json"));
        var runtimeConfigObj = new JsonObject() 
        {
            ["runtimeOptions"] = new JsonObject
            {
                ["configProperties"] = new JsonObject 
                {
                    ["NetBeautyLibsDir"] = ".;libraries",
                    ["NetBeautySharedRuntimeMode"] = "no",
                    ["STARTUP_HOOKS"] = "nbloader"
                },
                ["tfm"] = $"net{netVer}",
                ["framework"] = new JsonObject 
                {
                    ["name"] = "Microsoft.NETCore.App",
                    ["version"] = $"{netVer}.0"
                }
            }
        };
        JsonTextWriter.WriteToStream(runtimeConfigFs, runtimeConfigObj);

        var assemblies = new Dictionary<string, Dictionary<string, string>>();

        void AddAssembly(string asm) 
        {
            if (assemblies.ContainsKey(asm))
                return;
            var deps = GetPEAssemblyReferences(asm);
            assemblies.Add(asm, deps);

            foreach (var (name, _) in deps) 
            {
                var depPath = Path.Combine(Path.GetDirectoryName(asm), $"{name}.dll");
                Console.WriteLine(depPath);
                Console.WriteLine(File.Exists(depPath));
                if (File.Exists(depPath))
                    AddAssembly(depPath);
            }
        }
        AddAssembly(asmInput);
        foreach (var addDep in additionalDeps)
            AddAssembly(addDep);

        using var depsFs = File.OpenWrite(Path.ChangeExtension(asmInput, ".deps.json"));
        var frameworkObj = new JsonObject();
        foreach (var (path, deps) in assemblies) 
        {
            var runtimeObj = new JsonObject() 
            {
                ["runtime"] = new JsonObject() 
                {
                    [Path.GetFileName(path)] = new JsonObject()
                }
            };
            if (deps.Count > 0) 
            {
                var depObj = new JsonObject();
                foreach (var dep in deps) 
                {
                    depObj[dep.Key] = dep.Value;
                }
                runtimeObj["dependencies"] = depObj;
            }
            frameworkObj[$"{Path.GetFileNameWithoutExtension(path)}/{GetPEAssemblyVersion(path)}"] = runtimeObj;
        }

        var librariesObj = new JsonObject();
        foreach (var (path, deps) in assemblies) 
        {
            librariesObj[$"{Path.GetFileNameWithoutExtension(path)}/{GetPEAssemblyVersion(path)}"] = new JsonObject 
            {
                ["type"] = (path == asmInput) ? "project" : "reference",
                ["servicable"] = false,
                ["sha512"] = ""
            };   
        }

        var depsObj = new JsonObject
        {
            ["runtimeTarget"] = new JsonObject 
            {
                ["name"] = framework,
                ["signature"] = ""
            },
            ["compilationOptions"] = new JsonObject(),
            ["targets"] = new JsonObject 
            {
                [framework] = frameworkObj
            },
            ["libraries"] = librariesObj
        };
        JsonTextWriter.WriteToStream(depsFs, depsObj);
    }

    private static Version GetPEAssemblyVersion(string path) 
    {
        using var fs = File.OpenRead(path);
        using var peReader = new PEReader(fs);
        return peReader.GetMetadataReader().GetAssemblyDefinition().Version;
    }

    private static Dictionary<string, string> GetPEAssemblyReferences(string path) 
    {
        using var fs = File.OpenRead(path);
        using var peReader = new PEReader(fs);

        MetadataReader meta = peReader.GetMetadataReader();
        var deps = new Dictionary<string, string>();
        foreach (var asmRef in meta.AssemblyReferences.Select(meta.GetAssemblyReference))
            deps.TryAdd(meta.GetString(asmRef.Name), asmRef.Version.ToString());
        return deps;
    }
}