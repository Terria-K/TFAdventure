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
    internal static long PickupLoaderCount = 21;
    public static Dictionary<string, EnemyLoader> EnemyLoader = new();
    public static Dictionary<string, DarkWorldBossLoader> DarkWorldBossLoader = new();
    public static Dictionary<string, LevelEntityLoader> LevelEntityLoader = new();
    public static Dictionary<string, RoundLogicLoader> RoundLogicLoader = new();
    public static Dictionary<string, RoundLogicInfo> RoundLogicIdentifiers = new();

    // Extending enums this way. Please inform me if there's a better way to do this.
    public static Dictionary<Pickups, PickupLoader> PickupLoader = new();
    [Obsolete("Use RiseCore.ArrowsRegistry instead.")]
    public static Dictionary<string, ArrowTypes> ArrowsID = new();
    public static Dictionary<string, ArrowObject> ArrowsRegistry = new();
    public static Dictionary<string, Pickups> PickupID = new();
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

    internal static bool Restart;

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

    internal static void ModuleStart() 
    {
        RiseCore.Flags();
        GameChecksum = GetChecksum(typeof(TFGame).Assembly.Location).ToHexadecimalString();
        GameRootPath = Path.GetDirectoryName(typeof(TFGame).Assembly.Location);
        if (!Directory.Exists("Mods"))
            Directory.CreateDirectory("Mods");

        new NoModule(new ModuleMetadata() {
            Name = "FortRise",
            Version = FortRiseVersion
        }).Register();

        AdventureModule = new AdventureModule();
        AdventureModule.Register();

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

            foreach (var mod in InternalFortModules) 
            {
                var meta = mod.Meta;

                if (meta == null)
                    continue;

                if (string.IsNullOrEmpty(meta.PathDirectory))
                    continue;
                
                var path = name.Name + ".dll";
                if (!string.IsNullOrEmpty(meta.DLL)) 
                {
                    var pathDirectory = Path.GetDirectoryName(meta.DLL);
                    path = Path.Combine(pathDirectory, path).Replace('\\', '/');
                    if (!string.IsNullOrEmpty(pathDirectory))
                        path = path.Substring(pathDirectory.Length + 1);
                }

                var depPath = Path.Combine(meta.PathDirectory, path);

                if (!File.Exists(depPath))
                    continue;
                using var fs = File.OpenRead(depPath);
                if (fs != null)
                    return Relinker.GetRelinkedAssembly(meta, Path.GetFullPath(Path.Combine(meta.PathDirectory, path)), fs);
                
            }
            return null;
        };
        Loader.InitializeMods();
        Logger.Info("[RESOURCE] Initializng resources...");
        RiseCore.ResourceTree.Initialize();
    }

    public static void ParseArgs(string[] args) 
    {
        foreach (var arg in args) 
        {
            switch (arg) 
            {
            case "--debug":
                DebugMode = true;
                break;
            case "--verbose":
                Logger.Verbosity = Logger.LogLevel.Assert;
                break;
            }
        }
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
        var dll = metadata.DLL;
        if (!zip) 
        {
            metadata.DLL = dll is not null ? Path.GetFullPath(Path.Combine(dirPath, dll)) : string.Empty;
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

    public static void AskForRestart() 
    {
        Restart = true;
        Engine.Instance.Exit();
    }

    public static void RunTowerFallProcess(string towerFallPath, string[] args) 
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

    public static readonly HashAlgorithm ChecksumHasher = MD5.Create();

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
        Lua.Initialize();
        foreach (var t in InternalFortModules) 
        {
            t.Initialize();
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

    internal static void WriteBlacklist(JsonValue ctx, string path) 
    {
        JsonTextWriter.WriteToFile(path, ctx);
    }

    internal static void Register(this FortModule module) 
    {
        if (module is NoModule)
            return;

        // Everything is registered, so no need to register it again.
        if (InternalFortModules.Contains(module)) 
        {
            module.LoadContent();
            module.InternalLoad();
            return;
        }

        lock (InternalFortModules) 
            InternalFortModules.Add(module);
        
        module.LoadContent();
        module.InternalLoad();

        module.Enabled = true;
        try 
        {

        foreach (var type in module.GetType().Assembly.GetTypes()) 
        {
            if (type is null)
                continue;
            
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
                RoundLogicIdentifiers.Add(name, identifier);
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
                if (identifier.RoundType == RoundLogicType.FFA)
                    CustomVersusRoundLogic.LookUpModes.Add(name, patch_Modes.LastManStanding);
                else if (identifier.RoundType == RoundLogicType.TeamDeatchmatch)
                    CustomVersusRoundLogic.LookUpModes.Add(name, patch_Modes.TeamDeathmatch);
                else
                    CustomVersusRoundLogic.LookUpModes.Add(name, patch_Modes.HeadHunters);

                CustomVersusRoundLogic.VersusModes.Add(name);
                RoundLogicLoader[name] = loader;
                
            }
            foreach (CustomEnemyAttribute attrib in type.GetCustomAttributes<CustomEnemyAttribute>()) 
            {
                if (attrib is null)
                    continue;
                foreach (var name in attrib.Names) 
                {
                    string id;
                    string methodName = null;
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
                    EnemyLoader loader = null;

                    info = type.GetMethod(methodName, new Type[] { typeof(Vector2), typeof(Facing) });
                    if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(Enemy))) 
                    {
                        loader = (position, facing, _) => {
                            var invoked = (patch_Enemy)info.Invoke(null, new object[] {
                                position, facing
                            });
                            invoked.Load();
                            return invoked;
                        };
                        goto Loaded;
                    }

                    info = type.GetMethod(methodName, new Type[] { typeof(Vector2), typeof(Facing), typeof(Vector2[]) });
                    if (info != null && info.IsStatic && info.ReturnType.IsCompatible(typeof(Enemy))) 
                    {
                        loader = (position, facing, nodes) => {
                            var invoked = (patch_Enemy)info.Invoke(null, new object[] {
                                position, facing, nodes
                            });
                            invoked.Load();
                            return invoked;
                        };
                        goto Loaded;
                    }

                    ctor = type.GetConstructor(
                        new Type[] { typeof(Vector2), typeof(Facing) }
                    );
                    if (ctor != null) 
                    {
                        loader = (position, facing, _) => {
                            var invoked = (patch_Enemy)ctor.Invoke(new object[] {
                                position,
                                facing
                            });
                            invoked.Load();

                            return invoked;

                        };
                        goto Loaded;
                    }

                    ctor = type.GetConstructor(
                        new Type[] { typeof(Vector2), typeof(Facing), typeof(Vector2[]) }
                    );
                    if (ctor != null) 
                    {
                        loader = (position, facing, nodes) => {
                            var invoked = (patch_Enemy)ctor.Invoke(new object[] {
                                position,
                                facing,
                                nodes
                            });
                            invoked.Load();

                            return invoked;

                        };
                        goto Loaded;
                    }
                    Loaded:
                    EnemyLoader[id] = loader;
                }
            }
            foreach (var clea in type.GetCustomAttributes<CustomLevelEntityAttribute>()) 
            {
                if (clea is null)
                    continue;
                foreach (var name in clea.Names) 
                {
                    string id;
                    string methodName = null;
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
            foreach (var arrow in type.GetCustomAttributes<CustomArrowsAttribute>()) 
            {
                const int offset = 11;
                if (arrow is null)
                    continue;
                var name = arrow.Name;
                var graphicFn = arrow.GraphicPickupInitializer ?? "CreateGraphicPickup";
                var stride = (ArrowTypes)offset + ArrowsRegistry.Count;
                MethodInfo graphic = type.GetMethod(graphicFn);

                ConstructorInfo ctor = type.GetConstructor(Array.Empty<Type>());
                ArrowLoader loader = null;
                if (ctor != null) 
                {
                    loader = () => 
                    {
                        var invoked = (patch_Arrow)ctor.Invoke(Array.Empty<object>());
                        invoked.ArrowType = stride;
                        return invoked;
                    };
                }
                ArrowInfoLoader infoLoader = null;
                if (graphic == null || !graphic.IsStatic)
                {
                    Logger.Log($"[Loader] [{module.Meta.Name}] No `static ArrowInfo CreateGraphicPickup()` method found on this Arrow {name}, falling back to normal arrow graphics.");
                    infoLoader = () => {
                        return ArrowInfo.Create(new Image(TFGame.Atlas["arrows/arrow"]));
                    };
                }
                else 
                {
                    infoLoader = () => {
                        var identifier = (ArrowInfo)graphic.Invoke(null, Array.Empty<object>());
                        if (string.IsNullOrEmpty(identifier.Name))
                            identifier.Name = name;
                        return identifier;
                    };
                }

                ArrowsID[name] = stride;
                ArrowsRegistry[name] = new ArrowObject() 
                {
                    Types = stride,
                    SpawnType = TreasureChest.Types.Normal,
                    InfoLoader = infoLoader,
                    PickupType = (Pickups)PickupLoaderCount
                };
                ArrowNameMap[stride] = name;
                Arrows[stride] = loader;
                PickupID[name] = (Pickups)PickupLoaderCount;
                PickupLoader[(Pickups)PickupLoaderCount] 
                    = (pos, targetPos, _) => new ArrowTypePickup(pos, targetPos, stride);
                PickupLoaderCount++;
            }
            foreach (var pickup in type.GetCustomAttributes<CustomPickupAttribute>()) 
            {
                if (pickup is null)
                    continue;
                var pickupName = pickup.Name;
                var stride = PickupLoaderCount;
                ConstructorInfo ctor = type.GetConstructor(new Type[2] { typeof(Vector2), typeof(Vector2) });
                PickupLoader loader = null;

                if (ctor != null) 
                {
                    loader = (pos, targetPos, idx) => 
                    {
                        var custom = (Pickup)ctor.Invoke(new object[2] { pos, targetPos });
                        if (custom is CustomOrbPickup customOrb) 
                        {
                            var info = customOrb.CreateInfo();
                            customOrb.Sprite = info.Sprite;
                            customOrb.LightColor = info.Color.Invert();
                            customOrb.Collider = info.Hitbox;
                            customOrb.Border.Color = info.Color;
                            customOrb.Sprite.Play(0);
                            customOrb.Add(customOrb.Sprite);
                        }

                        return custom;
                    };
                }

                PickupID[pickupName] = (Pickups)stride;
                PickupLoader[(Pickups)PickupLoaderCount] = loader;
                PickupLoaderCount++;
            }
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
            Logger.Log(e.ToString());
        }
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
}