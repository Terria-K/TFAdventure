using System.Text;
using System.IO;
using System;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace FortRise.Installer;

public class Installer 
{
    public static Assembly AsmMonoMod;
    public static Assembly AsmHookGen;
    public static Assembly AsmModPorter;


    public static readonly string[] NetCoreSystemLibs = new string[] {
        "System.Drawing.Common.dll", "System.Security.Permissions.dll", "System.Windows.Extensions.dll"
    };


    private static readonly string[] fileDependencies = {
        "FNA.dll", "FNA.dll.config", "FNA.pdb",
        "FNA.xml", "MonoMod.RuntimeDetour.HookGen.dll",
        "MonoMod.Patcher.dll", "ModPorter.dll",
        "MonoMod.xml", "0Harmony.dll",
        "MonoMod.Utils.dll", "MonoMod.Utils.xml", 
        "MonoMod.RuntimeDetour.HookGen.xml",
        "Microsoft.Win32.SystemEvents.dll",
        "System.Drawing.Common.dll", "System.Security.Permissions.dll", "System.Windows.Extensions.dll",
        "MonoMod.ILHelpers.dll", "MonoMod.Backports.dll",
        "TowerFall.FortRise.mm.xml",
        "TowerFall.FortRise.mm.pdb",
        "MonoMod.Core.dll", "MonoMod.Core.xml", "MonoMod.Iced.dll", "MonoMod.Iced.xml",
        "MonoMod.RuntimeDetour.dll", "MonoMod.RuntimeDetour.xml",
        "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll",
        "TeuJson.dll", "Ionic.Zip.Reduced.dll", "NLua.dll", "KeraLua.dll",
        "MonoMod.ILHelpers.dll", "MonoMod.Backports.dll", "Hjson.dll",
        "DiscordGameSdk.dll", "DiscordGameSdk.pdb", "Fortrise.targets"
    };

    private static string[] fnaLibs; 
    private static readonly string modFile = "TowerFall.FortRise.mm.dll";

    public void Install(string path) 
    {
        // Let's try to not do it at compile-time.. It's really hard to maintain that way
        string FNAPath;
        Action<string> FNACopy;
        switch (Environment.OSVersion.Platform) 
        {
        case PlatformID.MacOSX:
            FNAPath = "lib64-osx";
            FNACopy = CopyFNAFiles_MacOS;
            fnaLibs = new string[] {
                "libFAudio.0.dylib", "libFNA3D.0.dylib", "liblua53.dylib",
                "libMoltenVK.dylib", "libSDL2-2.0.0.dylib", "libtheorafile.dylib",
                "libvulkan.1.dylib", "discord_game_sdk.dylib"
            };
            break;
        case PlatformID.Unix:
            FNAPath = "lib64-linux";
            FNACopy = CopyFNAFiles_Linux;
            fnaLibs = new string[] {
                "libFAudio.so.0", "libFNA3D.so.0", "liblua53.so",
                "libSDL2-2.0.so.0", "libtheorafile.so", "discord_game_sdk.so"
            };
            break;
        default:
            fnaLibs = new string[8];
            if (Environment.Is64BitOperatingSystem) 
            {
                FNAPath = "lib64-win-x86";
                FNACopy = CopyFNAFiles_WindowsX64;
                fnaLibs[7] = "steam_api64.dll";
            }
            else 
            {
                FNAPath = "lib64-win-x86";
                FNACopy = CopyFNAFiles_WindowsX86;
                fnaLibs[7] = "steam_api.dll";
            }
            fnaLibs[0] = "FAudio.dll";
            fnaLibs[1] = "FNA3D.dll";
            fnaLibs[2] = "lua53.dll";
            fnaLibs[3] = "CSteamworks.dll";
            fnaLibs[4] = "SDL2.dll";
            fnaLibs[5] = "libtheorafile.dll";
            fnaLibs[6] = "discord_game_sdk.dll";
            break;
        }
        
        var fortOrigPath = Path.Combine(path, "fortOrig");

        if (File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe"))) 
        {
            File.Copy(Path.Combine(fortOrigPath, "TowerFall.exe"), Path.Combine(path, "TowerFall.exe"), true);
        }

        Underline("Moving original TowerFall into fortOrig folder");
        if (!Directory.Exists(fortOrigPath))
            Directory.CreateDirectory(fortOrigPath);
        
        if (!File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe")))
            File.Copy(Path.Combine(path, "TowerFall.exe"), Path.Combine(fortOrigPath, "TowerFall.exe"));

        if (!File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe")))
        {
            ThrowError("Copying failed");           
            return;
        }

        Underline("Moving original FNA.dll and FNA.dll.config into fortOrig folder if have one");
        if (File.Exists(Path.Combine(path, "FNA.dll")) && !File.Exists(Path.Combine(fortOrigPath, "FNA.dll")))
        {
            File.Copy(Path.Combine(path, "FNA.dll"), Path.Combine(fortOrigPath, "FNA.dll"));
        }
        if (File.Exists(Path.Combine(path, "FNA.dll.config")) && !File.Exists(Path.Combine(fortOrigPath, "FNA.dll.config")))
        {
            File.Copy(Path.Combine(path, "FNA.dll.config"), Path.Combine(fortOrigPath, "FNA.dll.config"));
        }

        var libPath = "";

        if (Environment.OSVersion.Platform == PlatformID.Win32NT) 
        {
            Underline("Supporting DInput and other SDL controllers");
            if (!File.Exists(Path.Combine(path, "gamecontrollerdb.txt")))
                File.Copy(Path.Combine(libPath, "gamecontrollerdb.txt"), Path.Combine(path, "gamecontrollerdb.txt"), true);
        }

        Underline("Moving the mod into TowerFall directory");

        var fortRiseDll = Path.Combine(libPath, modFile);
        if (!File.Exists(fortRiseDll)) 
        {
            ThrowError("TowerFall.FortRise.mm.dll mod file not found!");
            return;
        }
        File.Copy(fortRiseDll, Path.Combine(path, "TowerFall.FortRise.mm.dll"), true);

        Underline("Moving all of the lib files");
        foreach (var file in fileDependencies) 
        {
            var lib = Path.Combine(libPath, file);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"{lib} file not found!");
                continue;
            }

            File.Copy(lib, Path.Combine(path, Path.GetFileName(lib)), true);
        }

        Underline($"Moving all of the FNA files on {FNAPath}");
        FNACopy(path);


        Underline("Generating XML Document");
        GenerateDOC(Path.Combine(libPath, "TowerFall.FortRise.mm.xml"), Path.Combine(path, "TowerFall.xml"));


        Underline("Patching TowerFall");
        LoadAssembly(Path.Combine(path, "Mono.Cecil.dll"));
        LoadAssembly(Path.Combine(path, "Mono.Cecil.Pdb.dll"));
        LoadAssembly(Path.Combine(path, "Mono.Cecil.Mdb.dll"));
        LoadAssembly(Path.Combine(path, "MonoMod.Utils.dll"));
        LoadAssembly(Path.Combine(path, "MonoMod.RuntimeDetour.dll"));

        AsmMonoMod = LoadAssembly(Path.Combine(path, "MonoMod.Patcher.dll"));
        AsmModPorter = LoadAssembly(Path.Combine(path, "ModPorter.dll"));
        AsmHookGen = LoadAssembly(Path.Combine(path, "MonoMod.RuntimeDetour.HookGen.dll"));

        var towerFallExe = Path.Combine(path, "TowerFall.dll");
        var towerFallPdb = Path.Combine(path, "TowerFall.pdb");

        PortToFNA(Path.Combine(path, "TowerFall.exe"), Path.Combine(path, "FNA-TowerFall.exe"));
        File.Move(Path.Combine(path, "FNA-TowerFall.exe"), Path.Combine(path, "TowerFall.exe"), true);

        ConvertToNETCore(
            Path.Combine(path, "TowerFall.exe"), 
            Path.Combine(path, "TowerFall.dll"));

        Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "0");
        int returnCode = (int) AsmMonoMod.EntryPoint.Invoke(null, new object[] { 
            new string[] { Path.Combine(path, "TowerFall.dll"), Path.Combine(path, "MONOMODDED_TowerFall.dll") } });
        if (returnCode != 0) 
        {
            ThrowError("MonoMod failed to patch the assembly");
            UnderlineInfo("Note that the TowerFall might be patched from other modloader");
            return;
        }

        Underline("Renaming the output");

        if (File.Exists(towerFallExe)) 
        {
            File.Delete(towerFallExe);
        }
        if (File.Exists(towerFallPdb)) 
        {
            File.Delete(towerFallPdb);
        }

        var moddedOutputExe = Path.Combine(path, "MONOMODDED_TowerFall.dll");
        var moddedOutputPdb = Path.Combine(path, "MONOMODDED_TowerFall.pdb");
        File.Move(moddedOutputExe, towerFallExe);
        File.Move(moddedOutputPdb, towerFallPdb);

        Yellow("Generating HookGen");

        Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "0");
        AsmHookGen.EntryPoint.Invoke(null, new object[] { new string[] { "--private", Path.Combine(path, "TowerFall.dll"), Path.Combine(path, "MMHOOK_TowerFall.dll") } });


        AsmModPorter.GetType("ModPorter.NetCoreUtils")
            .GetMethod("GenerateRuntimeConfig", BindingFlags.Static | BindingFlags.Public, null, new Type[] { 
                typeof(string), typeof(string[])}, null)
            .Invoke(null, new object[] { Path.Combine(path, "TowerFall.dll"), new string[] {
                Path.Combine(path, "MMHOOK_TowerFall.dll")
            } });

        var patchVersion = Path.Combine(path, "PatchVersion.txt");

        Underline("Writing the version file");

        var sb = new StringBuilder();
        sb.AppendLine("Installer Version: " + "4.7.0");

        var text = sb.ToString();

        File.WriteAllText(Path.Combine(path, "PatchVersion.txt"), sb.ToString());

        Yellow("Cleaning up");
        Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "");

        Succeed("Installed");
    }

    private static void PortToFNA(string asmFrom, string asmTo = null) 
    {
        asmTo ??= asmFrom;

        AsmModPorter.GetType("ModPorter.ModPort")
            .GetMethod("FNAShortcut", BindingFlags.Static | BindingFlags.Public, null, new Type[] { 
                typeof(string), typeof(string)}, null)
            .Invoke(null, new object[] { asmFrom, asmTo});

        AsmModPorter.GetType("ModPorter.ModPort")
            .GetMethod("ClearContext", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, Array.Empty<object>());
    }

    private static void ConvertToNETCore(string asmFrom, string asmTo = null) 
    {
        asmTo ??= asmFrom;
        string[] deps = GetPEAssemblyReferences(asmFrom).Keys.ToArray();

        if (!asmFrom.Contains("TowerFall.exe") && deps.Contains("ModPorter"))
            return;
        
        foreach (var dep in deps) 
        {
            if (dep.Contains("ModPorter"))
                continue;
            var srcPath = Path.Combine(Path.GetDirectoryName(asmFrom), $"{dep}.dll");
            var dstPath = Path.Combine(Path.GetDirectoryName(asmTo), $"CORE-{dep}.dll");
            if (File.Exists(srcPath) && !IsSystemLibrary(srcPath))
            {
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(asmFrom), $"fortOrig/{dep}.dll")))
                    File.Copy(Path.Combine(Path.GetDirectoryName(asmFrom), $"{dep}.dll"), 
                        Path.Combine(Path.GetDirectoryName(asmFrom), $"fortOrig/{dep}.dll"));
                ConvertToNETCore(srcPath, dstPath);

                if (!File.Exists(dstPath))
                    continue;
                File.Move(Path.Combine(Path.GetDirectoryName(asmTo), $"CORE-{dep}.dll"), Path.Combine(Path.GetDirectoryName(asmTo), $"{dep}.dll"), true);
            }
            else if (File.Exists(dstPath) && !IsSystemLibrary(srcPath))
            {
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(asmFrom), $"fortOrig/{dep}.dll")))
                    File.Copy(Path.Combine(Path.GetDirectoryName(asmFrom), $"{dep}.dll"), 
                        Path.Combine(Path.GetDirectoryName(asmFrom), $"fortOrig/{dep}.dll"));
                ConvertToNETCore(dstPath);

                File.Move(Path.Combine(Path.GetDirectoryName(asmTo), $"CORE-{dep}.dll"), Path.Combine(Path.GetDirectoryName(asmTo), $"{dep}.dll"), true);
            }
        }

        Console.WriteLine($"Converting {asmFrom} to .NET 7");

        AsmModPorter.GetType("ModPorter.ModPort")
            .GetMethod("NetCoreShortcut", BindingFlags.Static | BindingFlags.Public, null, new Type[] { 
                typeof(string), typeof(string), typeof(bool)}, null)
            .Invoke(null, new object[] { asmFrom, asmTo, true });
    }

    private static bool IsSystemLibrary(string file) {
        if (Path.GetExtension(file) != ".dll")
            return false;

        if (Path.GetFileName(file).StartsWith("System.") && !NetCoreSystemLibs.Contains(Path.GetFileName(file)))
            return true;

        return new string[] {
            "mscorlib.dll",
            "Mono.Posix.dll",
            "Mono.Security.dll"
        }.Any(name => Path.GetFileName(file).Equals(name, StringComparison.OrdinalIgnoreCase));
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

    public void Uninstall(string path) 
    {
        var patchVersion = Path.Combine(path, "PatchVersion.txt");
        bool shouldProceed = false;
        if (File.Exists(patchVersion)) 
        {
            shouldProceed = true;
        }
        if (!shouldProceed) 
        {
            ThrowError("This TowerFall has not been patched yet.");
            return;
        }
        var fortOrigPath = Path.Combine(path, "fortOrig", "TowerFall.exe");
        Underline("Copying original TowerFall into TowerFall root folder");
        File.Copy(fortOrigPath, Path.Combine(path, "TowerFall.exe"), true);

        Underline("Deleting the libraries from the TowerFall root folder");

        foreach (var file in fileDependencies) 
        {
            var lib = Path.Combine(path, file);
            if (!File.Exists(lib)) 
            {
                continue;
            }

            File.Delete(lib);
        }

        Underline("Deleting the mod");

        var fortRiseDll = Path.Combine(path, modFile);
        if (File.Exists(fortRiseDll)) 
        {
            File.Delete(fortRiseDll);
        }

        Underline("Deleting the hooks");

        var hookDll = Path.Combine(path, "MMHOOK_TowerFall.dll");
        if (File.Exists(hookDll)) 
        {
            File.Delete(hookDll);
        }

        Underline("Deleting the PatchVersion text file");
        
        File.Delete(Path.Combine(path, "PatchVersion.txt"));

        Succeed("Unpatched");
    }

    private static void GenerateDOC(string docXML, string toPath) 
    {
        var xmlDocument = new XmlDocument();
        try 
        {
            xmlDocument.Load(docXML);
        }
        catch 
        {
            ThrowErrorContinous("Failed to generate doc xml");
            return;
        }
        var xmlName = xmlDocument["doc"]?["assembly"]?["name"];
        if (xmlName == null) 
        {
            ThrowErrorContinous("Failed to generate doc xml");
            return;
        }
        xmlName.InnerText = "TowerFall";
        xmlDocument.Save(toPath);
    }

    private static void CopyFNAFiles_WindowsX86(string path) 
    {
        Console.WriteLine("CopyFNAFiles_WindowsX86 is called");
        foreach (var fnaLib in fnaLibs) 
        {
            var lib = Path.Combine("lib64-win-x86", fnaLib);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"{lib} file not found!");
                continue;
            }   
            var x86Path = Path.Combine(path, "lib64-win-x86");
            if (!Directory.Exists(x86Path)) 
                Directory.CreateDirectory(x86Path);
            File.Copy(lib, Path.Combine(x86Path, Path.GetFileName(lib)), true);
        }
    }

    private static void CopyFNAFiles_WindowsX64(string path) 
    {
        Console.WriteLine("CopyFNAFiles_Windows64 is called");
        foreach (var fnaLib in fnaLibs) 
        {
            var lib = Path.Combine("lib64-win-x64", fnaLib);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"{lib} file not found!");
                continue;
            }   
            var x86Path = Path.Combine(path, "lib64-win-x64");
            if (!Directory.Exists(x86Path)) 
                Directory.CreateDirectory(x86Path);
            File.Copy(lib, Path.Combine(x86Path, Path.GetFileName(lib)), true);
        }
    }

    private static void CopyFNAFiles_Linux(string path) 
    {
        Console.WriteLine("CopyFNAFiles_Linux is called");
        foreach (var fnaLib in fnaLibs) 
        {
            var origPath = Path.Combine(path, "lib64-linux/orig");

            var lib64Path = Path.Combine(path, "lib64-linux");
            if (!Directory.Exists(origPath)) 
                Directory.CreateDirectory(origPath);
            
            if (File.Exists(Path.Combine(lib64Path, Path.GetFileName(fnaLib))) && !File.Exists(Path.Combine(origPath, Path.GetFileName(fnaLib))))
                File.Copy(Path.Combine(lib64Path, Path.GetFileName(fnaLib)), origPath, true);
            
            var lib = Path.Combine("lib64-linux", fnaLib);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"{lib} file not found!");
                continue;
            }   
            File.Copy(lib, Path.Combine(lib64Path, Path.GetFileName(lib)), true);
        }
    }

    private static void CopyFNAFiles_MacOS(string path) 
    {
        Console.WriteLine("CopyFNAFiles_MACOS is called");
        foreach (var fnaLib in fnaLibs) 
        {
            var osxPath = Path.Combine(path, "lib64-osx");
            var origPath = Path.Combine(osxPath, "orig");
            if (!Directory.Exists(origPath)) 
                Directory.CreateDirectory(origPath);

            if (File.Exists(Path.Combine(osxPath, Path.GetFileName(fnaLib))) && !File.Exists(Path.Combine(origPath, Path.GetFileName(fnaLib))))
                File.Copy(Path.Combine(osxPath, Path.GetFileName(fnaLib)), origPath, true);
            
            var lib = Path.Combine("lib64-osx", fnaLib);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"{lib} file not found!");
                continue;
            }   
            File.Copy(lib, Path.Combine(osxPath, Path.GetFileName(lib)), true);
        }
    }


    private static void Yellow(string text) 
    {
        Console.WriteLine(text);
    }

    private static void UnderlineInfo(string text) 
    {
        Console.WriteLine(text);
    }

    private static void Underline(string text) 
    {
        Console.WriteLine(text);
    }

    private static void Succeed(string text) 
    {
        Console.WriteLine(text);
    }


    private static void ThrowErrorContinous(string error) 
    {
        Console.WriteLine(error);
    }

    private static void ThrowError(string error) 
    {
        Console.WriteLine(error);
    }

    private static Assembly LoadAssembly(string path) 
    {
        ResolveEventHandler tmpResolver = (s, e) => {
            string asmPath = Path.Combine(Path.GetDirectoryName(path), new AssemblyName(e.Name).Name + ".dll");
            if (!File.Exists(asmPath))
                return null;
            return Assembly.LoadFrom(asmPath);
        };
        AppDomain.CurrentDomain.AssemblyResolve += tmpResolver;
        // Assembly asm = Assembly.Load(Path.GetFileNameWithoutExtension(path));
        Assembly asm = Assembly.Load(Path.GetFileNameWithoutExtension(path));
        AppDomain.CurrentDomain.AssemblyResolve -= tmpResolver;
        AppDomain.CurrentDomain.TypeResolve += (s, e) => {
            return asm.GetType(e.Name) != null ? asm : null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            return e.Name == asm.FullName || e.Name == asm.GetName().Name ? asm : null;
        };
        return asm;
    }
}