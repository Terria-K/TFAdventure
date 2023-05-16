using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace FortRise.Installer;

public static class Installer
{
    public const string TowerFallVersion = "1.3.3.3";
    public const string InstallerVersion = "2.1.3";

    private static readonly string[] fileDependencies = {
        "MonoMod.exe", "MonoMod.xml", "0Harmony.dll",
        "MonoMod.Utils.dll", "MonoMod.Utils.xml", 
        "MonoMod.RuntimeDetour.HookGen.exe", "MonoMod.RuntimeDetour.HookGen.xml",
        "MonoMod.RuntimeDetour.dll", "MonoMod.RuntimeDetour.xml",
        "Mono.Cecil.dll", "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll",
        "TeuJson.dll", "MonoMod.ILHelpers.dll", "MonoMod.Backports.dll"
    };

    private static readonly string modFile = "TowerFall.FortRise.mm.dll";

    public static async Task Install(string path) 
    {
        var patchVersion = Path.Combine(path, "PatchVersion.txt");
        bool shouldProceed = true;
        if (File.Exists(patchVersion)) 
        {
            using var fs = File.OpenRead(patchVersion);
            using var sr = new StreamReader(fs);
            var tfVersion = sr.ReadLine()?.Split(':')[1].Trim();
            var pVersion = sr.ReadLine()?.Split(':')[1].Trim();
            if (pVersion == InstallerVersion) 
            {
                shouldProceed = false;
            }
        }
        if (!shouldProceed) 
        {
            if (!AnsiConsole.Confirm("[green]TowerFall has already been patched[/], [underline]do you want to patch again?[/]", false))
                return;
        }

        var fortOrigPath = Path.Combine(path, "fortOrig");

        if (File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe"))) 
        {
            File.Copy(Path.Combine(fortOrigPath, "TowerFall.exe"), Path.Combine(path, "TowerFall.exe"), true);
        }

        AnsiConsole.MarkupLine("[underline]Moving original TowerFall into fortOrig Folder[/]");
        if (!Directory.Exists(fortOrigPath))
            Directory.CreateDirectory(fortOrigPath);
        
        if (!File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe")))
            File.Copy(Path.Combine(path, "TowerFall.exe"), Path.Combine(fortOrigPath, "TowerFall.exe"));

        if (!File.Exists(Path.Combine(fortOrigPath, "TowerFall.exe")))
        {
            ThrowError("[underline][red]Copying failed[/][/]");           
            return;
        }
        var libPath = "lib";
        AnsiConsole.MarkupLine("[underline]Moving the mod into TowerFall directory[/]");

        var fortRiseDll = Path.Combine(libPath, modFile);
        if (!File.Exists(fortRiseDll)) 
        {
            ThrowError("[underline][red]TowerFall.FortRise.mm.dll mod file not found![/][/]");
            return;
        }
        File.Copy(fortRiseDll, Path.Combine(path, "TowerFall.FortRise.mm.dll"), true);


        AnsiConsole.MarkupLine("[underline]Moving all of the lib files[/]");
        foreach (var file in fileDependencies) 
        {
            var filePath = Path.Combine(libPath, path);
            var lib = Path.Combine(libPath, file);
            if (!File.Exists(lib)) 
            {
                ThrowErrorContinous($"[underline][red]{lib} file not found![/][/]");
                continue;
            }

            File.Copy(lib, Path.Combine(path, Path.GetFileName(lib)), true);
        }

        AnsiConsole.MarkupLine("[underline]Patching TowerFall[/]");

        var monoModPath = Path.Combine(libPath, "MonoMod.exe");
        if (!File.Exists(monoModPath))
        {
            ThrowError("No MonoMod executable found in the lib path");
            return;
        }
        var process = Process.Start(monoModPath, $"\"{Path.Combine(path, "TowerFall.exe")}\"");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) 
        {
            ThrowError("[underline][red]MonoMod failed to patch the assembly[/][/]");
            AnsiConsole.MarkupLine("[underline][gray]Note that the TowerFall might be patched from other modloader[/][/]");
            await Task.Delay(1000);
            return;
        }

        AnsiConsole.MarkupLine("[underline]Running HookGen[/]");
        var hookGenPath = Path.Combine(libPath, "MonoMod.RuntimeDetour.HookGen.exe");
        if (!File.Exists(hookGenPath))
        {
            ThrowError("No MonoMod.RuntimeDetour.HookGen executable found in the lib path");
            return;
        }
        process = Process.Start(hookGenPath, $"\"{Path.Combine(path, "TowerFall.exe")}\"");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) 
        {
            ThrowError("[underline][red]HookGen failed to generate hooks[/][/]");
        }

        AnsiConsole.MarkupLine("[yellow]Finalizing[/]");

        AnsiConsole.MarkupLine("[underline]Renaming the output[/]");
        File.Copy(Path.Combine(path, "MONOMODDED_TowerFall.exe"), Path.Combine(path, "TowerFall.exe"), true);
        File.Copy(Path.Combine(path, "MONOMODDED_TowerFall.pdb"), Path.Combine(path, "TowerFall.pdb"), true);
        File.Delete(Path.Combine(path, "MONOMODDED_TowerFall.exe"));
        File.Delete(Path.Combine(path, "MONOMODDED_TowerFall.pdb"));

        AnsiConsole.MarkupLine("[underline]Writing the version file[/]");

        var debugMode = AnsiConsole.Confirm("Do you want to run in debug mode?", false);

        var sb = new StringBuilder();
        sb.AppendLine("TF Version: " + TowerFallVersion);
        sb.AppendLine("Installer Version: " + InstallerVersion);
        sb.AppendLine("Debug Mode: " + debugMode);
        var text = sb.ToString();
        await File.WriteAllTextAsync(Path.Combine(path, "PatchVersion.txt"), sb.ToString());

        AnsiConsole.MarkupLine("[green]Installed![/]");
    }

    public static async Task Uninstall(string path) 
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
        AnsiConsole.MarkupLine("[underline]Copying original TowerFall into TowerFall root folder[/]");
        File.Copy(fortOrigPath, Path.Combine(path, "TowerFall.exe"), true);


        AnsiConsole.MarkupLine("[underline]Deleting libraries from the TowerFall root folder[/]");
        foreach (var file in fileDependencies) 
        {
            var lib = Path.Combine(path, file);
            if (!File.Exists(lib)) 
            {
                continue;
            }

            File.Delete(lib);
        }

        AnsiConsole.MarkupLine("[underline]Deleting the mod[/]");

        var fortRiseDll = Path.Combine(path, modFile);
        if (File.Exists(fortRiseDll)) 
        {
            File.Delete(fortRiseDll);
        }

        AnsiConsole.MarkupLine("[underline]Deleting the hooks[/]");

        var hookDll = Path.Combine(path, "MMHOOK_TowerFall.dll");
        if (File.Exists(hookDll)) 
        {
            File.Delete(hookDll);
        }

        AnsiConsole.MarkupLine("[underline]Deleting the PatchVersion text file[/]");

        
        File.Delete(Path.Combine(path, "PatchVersion.txt"));
        await Task.Delay(1000);

        AnsiConsole.MarkupLine("[underline][green]Unpatched[/][/]");
    }


    private static void ThrowErrorContinous(string error) 
    {
        AnsiConsole.MarkupLine(error);
    }

    private static void ThrowError(string error) 
    {
        AnsiConsole.Prompt(new TextPrompt<string>(error).AllowEmpty());
    }
}