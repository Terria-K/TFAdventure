#pragma warning disable CS0618
using FortRise.Adventure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using MonoMod.Utils;
using TeuJson;
using TowerFall;
using System.Diagnostics;
using System.Text;
using YYProject.XXHash;
using Ionic.Zip;

namespace FortRise;

public delegate Enemy EnemyLoader(Vector2 position, Facing facing, Vector2[] nodes);
public delegate DarkWorldBoss DarkWorldBossLoader(int difficulty);
public delegate patch_Arrow ArrowLoader();
public delegate ArrowInfo ArrowInfoLoader();
public delegate Pickup PickupLoader(Vector2 position, Vector2 targetPosition, int playerIndex);

public delegate LevelEntity LevelEntityLoader(XmlElement x, Vector2 position, Vector2[] nodes);
/// <summary>
/// A loader delegate for custom RoundLogic.
/// </summary>
/// <param name="session">A game current session</param>
/// <param name="canHaveMiasma">If the RoundLogic can have miasma</param>
/// <returns></returns>
public delegate RoundLogic RoundLogicLoader(patch_Session session, bool canHaveMiasma = false);

/// <summary>
/// Core API of FortRise
/// </summary>
public static partial class RiseCore 
{
    /// <summary>
    /// A TowerFall root directory.
    /// </summary>
    public static string GameRootPath { get; internal set; }
    internal static long PickupLoaderCount = 21;
    public static Dictionary<string, EnemyLoader> EnemyLoader = new();
    public static Dictionary<string, DarkWorldBossLoader> DarkWorldBossLoader = new();
    public static Dictionary<string, LevelEntityLoader> LevelEntityLoader = new();



    // Extending enums this way. Please inform me if there's a better way to do this.
    public static Dictionary<Pickups, PickupLoader> PickupLoader = new();
    [Obsolete("Use RiseCore.ArrowsRegistry instead.")]
    public static Dictionary<string, ArrowTypes> ArrowsID = new();
    public static Dictionary<string, ArrowObject> ArrowsRegistry = new();
    public static Dictionary<string, PickupObject> PickupRegistry = new();
    public static Dictionary<ArrowTypes, ArrowLoader> Arrows = new();
    public static Dictionary<ArrowTypes, string> ArrowNameMap = new();

    /// <summary>
    /// Contains a read-only access to all of the Fort Modules.
    /// </summary>
    public static ReadOnlyCollection<FortModule> Modules => InternalFortModules.AsReadOnly();

    /// <summary>
    /// Contains a read-only access to all of the Mods' metadata and resource.
    /// </summary>
    public static ReadOnlyCollection<ModResource> Mods => InternalMods.AsReadOnly();
    internal static List<FortModule> InternalFortModules = new();
    internal static HashSet<string> ModuleGuids = new();
    internal static HashSet<ModuleMetadata> InternalModuleMetadatas = new();
    internal static List<ModResource> InternalMods = new();
    internal static FortModule AdventureModule;

    public static List<string> DetourLogs = new List<string>();

    /// <summary>
    /// A current version of FortRise.
    /// <note>This should be not used to check for Latest Version and Current Version, 
    /// this is an information for logging purposes.</note>
    /// </summary>
    public static Version FortRiseVersion;
    /// <summary>
    /// Checks if the OS that is currently running is Windows. 
    /// </summary>
    /// <value>true if the OS is running on Windows, otherwise false.</value>
    public static bool IsWindows { get; internal set; }

    /// <summary>
    /// Checks if the FortRise is currently running on Debug Mode.
    /// <note>It is better to use conditionals if the runtime debugging is not needed.</note>
    /// </summary>
    public static bool DebugMode;
    public static bool NoIntro { get; private set; }
    public static bool NoAutoPause { get; private set; }
    public static bool NoErrorScene { get; private set; }
    internal static bool NoRichPresence;
    internal static bool DumpResources;
    internal static bool DisableFortMods;
    internal static string[] ApplicationArgs;

    internal static bool CantRestart = true;

    /// <summary>
    /// Check if the game is about to restart.
    /// </summary>
    public static bool WillRestart = false;

    internal static HashSet<string> ReadBlacklistedMods(string blackListPath) 
    {
        try 
        {
            var json = JsonTextReader.FromFile(blackListPath).ConvertToArrayString();
            var blacklisted = new HashSet<string>();
            foreach (var j in json) 
            {
                blacklisted.Add(j);
            }
            return blacklisted;
        }
        catch 
        {
            return new HashSet<string>(0);
        }
    }

    internal static bool Start() 
    {
        GameRootPath = Path.GetDirectoryName(typeof(TFGame).Assembly.Location);

        Loader.BlacklistedMods = ReadBlacklistedMods("Mods/blacklist.txt");
        var directory = Directory.GetDirectories("Mods");
        foreach (var dir in directory) 
        {
            if (dir.Contains("_RelinkerCache"))
                continue;    
            var dirInfo = new DirectoryInfo(dir);
            if (Loader.BlacklistedMods != null && Loader.BlacklistedMods.Contains(dirInfo.Name))  
                continue;
            
            LoadDir(dir);
        }
        var files = Directory.GetFiles("Mods");
        foreach (var file in files) 
        {
            if (!file.EndsWith("zip"))
                continue;
            var fileName = Path.GetFileName(file);
            if (Loader.BlacklistedMods != null && Loader.BlacklistedMods.Contains(Path.GetFileName(fileName))) 
                continue;
            
            LoadZip(file);
        }
        static void LoadDir(string dir) 
        {
            var metaPath = Path.Combine(dir, "env.json");
            if (!File.Exists(metaPath))
                return;
            var value = JsonTextReader.FromFile(metaPath);    
            foreach (var val in value.Pairs) 
            {
                Environment.SetEnvironmentVariable(val.Key, val.Value);
            }
        }

        static void LoadZip(string file)
        {
            using var zipFile = ZipFile.Read(file);

            if (!zipFile.ContainsEntry("env.json"))
                return;
            
            var entry = zipFile["env.json"];
            using var memStream = entry.ExtractStream();
            var value = JsonTextReader.FromStream(memStream);
            foreach (var val in value.Pairs) 
            {
                Environment.SetEnvironmentVariable(val.Key, val.Value);
            }
        }

        return true;
    }

    internal static void ModuleStart() 
    {
        RiseCore.Flags();
        GameChecksum = GetChecksum(typeof(TFGame).Assembly.Location).ToHexadecimalString();
        if (!Directory.Exists("Mods"))
            Directory.CreateDirectory("Mods");

         new NoModule(new ModuleMetadata() {
            Name = "FortRise",
            Version = FortRiseVersion
        }).Register();

        AdventureModule = new AdventureModule();
        AdventureModule.InternalLoad();
        AdventureModule.Register();
        EntityRegistry.LoadAllBuiltinEnemies();
        InternalFortModules.Add(AdventureModule);

        AppDomain.CurrentDomain.AssemblyResolve += (asmSender, asmArgs) => {
            AssemblyName asmName = new AssemblyName(asmArgs.Name);
            foreach (Assembly asm in Relinker.RelinkedAssemblies) {
                if (asm.GetName().Name == asmName.Name)
                    return asm;
            }

            return null;
        };

        AppDomain.CurrentDomain.AssemblyResolve += (asmSender, asmArgs) => 
        {
            var name = asmArgs?.Name == null ? null : new AssemblyName(asmArgs.Name);

            if (string.IsNullOrEmpty(name?.Name))
                return null;

            foreach (var mod in RiseCore.ResourceTree.ModResources) 
            {
                var meta = mod.Metadata;

                if (meta is null)
                    continue;
                
                var path = name.Name + ".dll";
                var pathDirectory = string.IsNullOrEmpty(meta.PathZip) ? meta.PathDirectory : meta.PathZip;
                path = Path.Combine(pathDirectory, path).Replace('\\', '/');
                if (!string.IsNullOrEmpty(pathDirectory))
                    path = path.Substring(pathDirectory.Length + 1);

                if (mod.Resources.TryGetValue(path, out RiseCore.Resource res) && res.ResourceType == typeof(RiseCore.ResourceTypeAssembly)) 
                {
                    using var stream = res.Stream;
                    if (stream != null)
                        return Relinker.Relink(meta, name.Name, stream);
                }
            }
            return null;
        };

        Lua.Initialize();
        AtlasReader.Initialize();
        RiseCore.ResourceTree.AddMod(null, new AdventureGlobalLevelResource());
        Loader.InitializeMods();
        if (!NoRichPresence)
            DiscordComponent.Create();
    }

    public static void ParseArgs(string[] args) 
    {
        var compiledArgs = new List<string>(args);
        if (File.Exists("launch.txt")) 
        {
            using var fs = File.OpenText("launch.txt");
            string argPass;
            while ((argPass = fs.ReadLine()) != null) 
            {
                if (argPass.Trim().StartsWith(";") || string.IsNullOrEmpty(argPass))
                    continue;
                var splittedArgs = argPass.Trim().Split(' ');
                foreach (var splittedArg in splittedArgs) 
                {
                    if (!compiledArgs.Contains(splittedArg))
                        compiledArgs.Add(argPass.Trim());
                }
            }
        }
        else 
        {
            using var fs = File.CreateText("launch.txt");
            fs.WriteLine("; Add any of available launch arguments here.");
            fs.WriteLine("; Lines starting with ; are ignored, so all of the arguments are disabled.");
            fs.WriteLine("");
            fs.WriteLine(";--debug");
            fs.WriteLine(";--nointro");
            fs.WriteLine("; You can also change your graphics driver, it can be either OpenGL, DirectX, or Vulkan");
            fs.WriteLine("; The defualt may be depends on your platform and some platform doesn't support one of the other graphics driver.");
            fs.WriteLine(";--graphics OpenGL");
        }

        Logger.Verbosity = Logger.LogLevel.Error;
        int cursor = 0;
        while (cursor < compiledArgs.Count) 
        {
            var arg = compiledArgs[cursor];
            switch (arg) 
            {
            case "--debug":
                DebugMode = true;
                Logger.Verbosity = Logger.LogLevel.Debug;
                break;
            case "--verbose":
                Logger.Verbosity = Logger.LogLevel.Assert;
                break;
            case "--no-rich-presence":
                NoRichPresence = true;
                break;
            case "--no-auto-pause":
                NoAutoPause = true;
                break;
            case "--dump-resources":
                DumpResources = true;
                break;
            case "--use-scancodes":
                Environment.SetEnvironmentVariable("FNA_KEYBOARD_USE_SCANCODES", "1");
                break;
            case "--no-quit":
                MainMenu.NoQuit = true;
                break;
            case "--no-gamepads":
                MainMenu.NoGamepads = true;
                break;
            case "--no-gamepadsupdates":
                MainMenu.NoGamepadUpdates = true;
                break;
            case "--nointro":
                NoIntro = true;
                break;
            case "--no-error-scene":
                NoErrorScene = true;
                break;
            case "--loadlog":
                TFGame.StartLoadLog();
                break;
            case "--graphics":
                cursor++;
                arg = compiledArgs[cursor];
                Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", arg);
                break;
            case "--disable-fort-mods":
                DisableFortMods = true;
                break;
            case "--level-quick-start": 
                cursor++;
                arg = compiledArgs[cursor];
                try 
                {
                    var argSpan = arg.AsSpan();
                    var slashSplit = argSpan.SplitLines('/');
                    byte phase = 0;

                    int num = 0;
                    string towerSet = null;
                    string levelID = null;
                    foreach (var (slashSet, _) in slashSplit) 
                    {
                        switch (phase) 
                        {
                        case 0:
                            towerSet = slashSet.ToString();
                            break;
                        case 1:
                            levelID = slashSet.ToString();
                            break;
                        case 2:
                            var span = slashSet.SplitLines('.');
                            foreach (var (s, x) in span) 
                            {
                                if (s[0] == '0') 
                                {
                                    num = int.Parse(s[1].ToString());
                                    break;
                                }
                                num = int.Parse(s.ToString());
                                break;
                            }
                            break;
                        }

                        phase++;
                    }
                    if (towerSet == null)
                    {
                        Logger.Error("[Quick Start] Couldn't quick start as TowerSet is missing");
                        continue;
                    }
                    if (levelID == null)
                    {
                        Logger.Error("[Quick Start] Couldn't quick start as LevelID is missing");
                        continue;
                    }

                    RiseCore.Events.OnPostInitialize += () => {
                        TowerRegistry.PlayDarkWorld(towerSet, towerSet + "/" + levelID, DarkWorldDifficulties.Legendary, num);
                    };
                }
                catch (Exception ex) 
                {
                    Logger.Error("[Quick Start] Couldn't quick start as the passed arguments is invalid");
                    Logger.Error(ex);
                }
                
                break;
            }
            cursor++;
        }

        ApplicationArgs = compiledArgs.ToArray();
    }

    public static ModuleMetadata ParseMetadata(string dir, string path)  
    {
        if (Path.GetExtension(path) == ".json") 
        {
            using var jfs = File.OpenRead(path);
            return ParseMetadataWithJson(dir, jfs);
        }
        using var fs = File.OpenRead(path);
        return ParseMetadataWithHJson(dir, fs);
    }

    public static ModuleMetadata ParseMetadataWithJson(string dir, string path)  
    {
        using var fs = File.OpenRead(path);
        return ParseMetadataWithJson(dir, fs);
    }

    public static ModuleMetadata ParseMetadataWithJson(string dirPath, Stream path, bool zip = false) 
    {
        var json = JsonTextReader.FromStream(path);
        var metadata = json.Convert<ModuleMetadata>();
        if (FortRiseVersion < metadata.FortRiseVersion) 
        {
            Logger.Error($"Mod Name: {metadata.Name} has a higher version of FortRise required {metadata.FortRiseVersion}. Your FortRise version: {FortRiseVersion}");
            return null;
        }
        string zipPath = "";
        if (!zip) 
        {
            metadata.PathDirectory = dirPath;
        }
        else 
        {
            zipPath = dirPath;
            dirPath = Path.GetDirectoryName(dirPath);
            metadata.PathZip = zipPath;
        }

        return metadata; 
    }

    public static ModuleMetadata ParseMetadataWithHJson(string dir, string path)  
    {
        using var fs = File.OpenRead(path);
        return ParseMetadataWithHJson(dir, fs);
    }

    public static ModuleMetadata ParseMetadataWithHJson(string dirPath, Stream path, bool zip = false) 
    {
        var hjson = Hjson.HjsonValue.Load(path);
        var metadata = ModuleMetadata.HJsonToMetadata(hjson);
        if (FortRiseVersion < metadata.FortRiseVersion) 
        {
            Logger.Error($"Mod Name: {metadata.Name} has a higher version of FortRise required {metadata.FortRiseVersion}. Your FortRise version: {FortRiseVersion}");
            return null;
        }
        string zipPath = "";
        if (!zip) 
        {
            metadata.PathDirectory = dirPath;
        }
        else 
        {
            zipPath = dirPath;
            dirPath = Path.GetDirectoryName(dirPath);
            metadata.PathZip = zipPath;
        }

        return metadata; 
    }

    /// <summary>
    /// Ask the mod loader to restart the game. If it isn't possible to restart, the call will be ignored.
    /// </summary>
    public static void AskForRestart(FortModule module) 
    {
        if (CantRestart) 
        {
            Logger.Warning($"[RESTART] {module.Name} asked for restart. But it was rejected as the game is not ready yet.");
            return;
        }
        
        var moduleName = module.Name;
        Logger.Info($"[RESTART] {module.Name} asked for restart. Restarting the game...");
        WillRestart = true;
        Engine.Instance.Exit();
    } 

    internal static void InternalRestart() 
    {
        WillRestart = true;
        Engine.Instance.Exit();
    }

    internal static void RunTowerFallProcess(string towerFallPath, string[] args) 
    {
        bool noIntroFound = false;
        var sb = new StringBuilder();
        foreach (var arg in args) 
        {
            sb.Append(arg + " ");
            if (arg is "-nointro" or "nointro") 
            {
                noIntroFound = true;
            }
        }
        if (!noIntroFound)
            sb.Append("-nointro");    
        var process = new Process();
        process.StartInfo.FileName = towerFallPath;
        process.StartInfo.Arguments = sb.ToString();

        process.Start();
    }

    // Generated at patch-time
    [PatchFlags]
    internal static void Flags() {}

    public static readonly HashAlgorithm ChecksumHasher = XXHash64.Create();

    public static byte[] GetChecksum(string path) 
    {
        using var fs = File.OpenRead(path);
        return ChecksumHasher.ComputeHash(fs);
    }

    public static byte[] GetChecksum(ModuleMetadata meta) 
    {
        return GetChecksum(meta.DLL);
    }

    // https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Everest/Everest.cs
    public static byte[] GetChecksum(ref Stream stream) 
    {
        if (!stream.CanSeek) 
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            stream.Dispose();
            stream = ms;
            stream.Seek(0, SeekOrigin.Begin);
        }

        long pos = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        byte[] hash = ChecksumHasher.ComputeHash(stream);
        stream.Seek(pos, SeekOrigin.Begin);
        return hash;
    }


    internal static void LogAllTypes() 
    {
        Logger.Info(InternalMods.Count + " total of mods loaded");
    }

    internal static void Initialize() 
    {
        foreach (var fortModule in InternalFortModules) 
        {
            fortModule.Initialize();
            if (fortModule is ITowerPatcher patcher)
                patcher.PatchTower(new OnTower(fortModule));
            RiseCore.Events.Invoke_OnModInitialized(fortModule);
        }

        Relinker.Modder.Dispose();
        Relinker.Modder = null;
    }

    internal static void ModuleEnd() 
    {
        foreach (var t in InternalFortModules) 
        {
            t.InternalUnload();
        }
    }

    internal static void ModsAfterLoad() 
    {
        foreach (var mod in InternalFortModules) 
        {
            mod.AfterLoad();
        }
    }

    internal static void RegisterMods() 
    {
        foreach (var mod in InternalFortModules) 
        {
            TowerFall.Loader.Message = ("Registering " + mod.Name + " Features").ToUpperInvariant();
            mod.Register();
        }
    }

    internal static void WriteBlacklist(JsonValue ctx, string path) 
    {
        JsonTextWriter.WriteToFile(path, ctx);
    }

    internal static void Register(this FortModule module) 
    {
        if (module is NoModule)
            return;

        module.LoadContent();
        module.Enabled = true;
        if (module is LuaModule)
            return;
        try 
        {
        foreach (var type in module.GetType().Assembly.GetTypes()) 
        {
            if (type is null)
                continue;
            
            // TODO rewrite
            foreach (var attrib in type.GetCustomAttributes<CustomRoundLogicAttribute>()) 
            {
                if (attrib is null)
                    continue;

                string name = attrib.Name;
                RoundLogicLoader loader = null;
                ConstructorInfo ctor;
                MethodInfo info;
                string overriden = attrib.OverrideFunction ?? "Create";
                info = type.GetMethod(overriden);
                if (info == null || !info.IsStatic)
                {
                    Logger.Error($"[Register] [{module.Meta.Name}] No `static RoundLogicIdentifier Create()` method found on this RoundLogic {name}, ignored.");
                    continue;
                }
                var identifier = (RoundLogicInfo)info.Invoke(null, Array.Empty<object>());
                var legacyGameMode = new LegacyCustomGamemode(name, identifier);
                ctor = type.GetConstructor(new Type[] { typeof(Session), typeof(bool) });
                if (ctor != null) 
                {
                    loader = (x, y) => {
                        var roundLogic = (CustomVersusRoundLogic)ctor.Invoke(new object[] { x, y });
                        return roundLogic;
                    };
                    goto Loaded;
                }
                ctor = type.GetConstructor(new Type[] { typeof(Session) });
                if (ctor != null) 
                {
                    loader = (x, y) => {
                        var roundLogic = (CustomVersusRoundLogic)ctor.Invoke(new object[] { x });
                        return roundLogic;
                    };
                    goto Loaded;
                }
                Loaded:

                legacyGameMode.LegacyLoader = loader;
                legacyGameMode.coinSprite = legacyGameMode.CoinSprite();
                GameModeRegistry.AddToLegacyVersus(name, legacyGameMode);
            }
            GameModeRegistry.Register(type, module);
            foreach (CustomEnemyAttribute attrib in type.GetCustomAttributes<CustomEnemyAttribute>()) 
            {
                if (attrib is null)
                    continue;
                EntityRegistry.AddEnemy(module, type, attrib.Names);
            }
            foreach (var clea in type.GetCustomAttributes<CustomLevelEntityAttribute>()) 
            {
                if (clea is null)
                    continue;
                foreach (var name in clea.Names) 
                {
                    string id;
                    string methodName = string.Empty;
                    string[] split = name.Split('=');
                    if (split.Length == 1) 
                    {
                        id = split[0];
                    }
                    else if (split.Length == 2) 
                    {
                        id = split[0];
                        methodName = split[1];
                    }
                    else 
                    {
                        Logger.Error($"[Loader] [{module.Meta.Name}] Invalid syntax of custom entity ID: {name}, {type.FullName}");
                        continue;
                    }
                    id = id.Trim();
                    methodName = methodName?.Trim();

                    ConstructorInfo ctor;
                    MethodInfo info;
                    LevelEntityLoader loader = null;
                    info = type.GetMethod(methodName, new Type[] { typeof(XmlElement) });
                    if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(LevelEntity))) 
                    {
                        loader = (xml, _, _) => {
                            var invoked = (LevelEntity)info.Invoke(null, new object[] {
                               xml 
                            });
                            return invoked;
                        };
                        goto Loaded;
                    }

                    info = type.GetMethod(methodName, new Type[] { typeof(XmlElement), typeof(Vector2) });
                    if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(LevelEntity))) 
                    {
                        loader = (xml, pos, _) => {
                            var invoked = (LevelEntity)info.Invoke(null, new object[] {
                                xml, pos
                            });
                            return invoked;
                        };
                        goto Loaded;
                    }

                    info = type.GetMethod(methodName, new Type[] { typeof(XmlElement), typeof(Vector2), typeof(Vector2[]) });
                    if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(LevelEntity))) 
                    {
                        loader = (xml, pos, nodes) => {
                            var invoked = (LevelEntity)info.Invoke(null, new object[] {
                                xml, pos, nodes
                            });
                            return invoked;
                        };
                        goto Loaded;
                    }

                    ctor = type.GetConstructor(new Type[] { typeof(XmlElement) });
                    if (ctor != null) 
                    {
                        loader = (x, _, _) => 
                        {
                            var invoked = (LevelEntity)ctor.Invoke(new object[] { x });
                            return invoked;
                        };
                        goto Loaded;
                    }
                    ctor = type.GetConstructor(new Type[] { typeof(XmlElement), typeof(Vector2) });
                    if (ctor != null) 
                    {
                        loader = (x, pos, _) => 
                        {
                            var invoked = (LevelEntity)ctor.Invoke(new object[] { x, pos});
                            return invoked;
                        };
                        goto Loaded;
                    }
                    ctor = type.GetConstructor(new Type[] { typeof(XmlElement), typeof(Vector2), typeof(Vector2[]) });
                    if (ctor != null) 
                    {
                        loader = (x, pos, nodes) => 
                        {
                            var invoked = (LevelEntity)ctor.Invoke(new object[] { x, pos, nodes });
                            return invoked;
                        };
                        goto Loaded;
                    }
                    Loaded:
                    LevelEntityLoader[name] = loader;
                }
            }

            FortRise.ArrowsRegistry.Register(type, module);
            FortRise.TowerPatchRegistry.Register(type, module);
            FortRise.PickupsRegistry.Register(type, module);
            foreach (var dwBoss in type.GetCustomAttributes<CustomDarkWorldBossAttribute>()) 
            {
                if (dwBoss is null)
                    continue;
                var bossName = dwBoss.BossName;

                ConstructorInfo ctor;
                DarkWorldBossLoader loader = null;
                ctor = type.GetConstructor(new Type[] { typeof(int) });
                if (ctor != null) 
                {
                    loader = diff => 
                    {
                        var invoked = (DarkWorldBoss)ctor.Invoke(new object[] { diff });
                        return invoked;
                    };
                    goto Loaded;
                }
                Loaded:
                DarkWorldBossLoader[bossName] = loader;
            }
        }
        }
        catch (Exception e) 
        {
            Logger.Error(e.ToString());
        }

        module.AfterLoad();
    }

    internal static void Unregister(this FortModule module) 
    {
        module.InternalUnload();
    }


    internal static IConsole ConsoleAttachment() 
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
        {
            return new WindowConsole();
        }
        return null;
    }

    public static void LogDetours(Logger.LogLevel level = Logger.LogLevel.Debug) 
    {
        List<string> detours = DetourLogs;
        if (detours.Count == 0)
            return;

        DetourLogs = new List<string>();

        foreach (string line in detours) 
        {
            Logger.Log(line, level);
        }
    }

    /// <summary>
    /// Checks if a <see cref="FortRise.FortModule"/> exists or been loaded. This checks for a Fort name, not a metadata name.
    /// </summary>
    /// <param name="modName">A Fort name, not a metadata name</param>
    /// <returns>true if found, else false</returns>
    public static bool IsModExists(string modName) 
    {
        foreach (var module in InternalFortModules) 
        {
            if (module.Name == modName) 
            {
                return true;
            }
        }
        return false;
    }

    public static ArrowTypes GetArrowID(string name) => RiseCore.ArrowsRegistry[name].Types;
}