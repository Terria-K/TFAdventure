using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace TowerFall 
{
    internal static class NativeMethods 
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDefaultDllDirectories(int directoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void AddDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectory(string lpPathName);
    }

public partial class patch_TFGame : TFGame
{
    public static bool SoundLoaded;
    private const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
    private static string FILENAME;

        public static patch_Atlas FortRiseMenuAtlas;
        public static bool Loaded
        {
            [MonoModIgnore]
            get => throw null;
            [MonoModIgnore]
            set => throw null;
        }



        private bool noIntro;
        public patch_TFGame(bool noIntro) : base(noIntro)
        {
        }

        public extern void orig_ctor(bool noIntro);

        [MonoModConstructor]
        public void ctor(bool noIntro) 
        {
            orig_ctor(noIntro || RiseCore.NoIntro);

            FortRise.RiseCore.ModuleStart();
            if (!RiseCore.NoIntro && !noIntro)
                this.noIntro = RiseCore.DebugMode;
        }

        [MonoModReplace]
        public static void Main(string[] args) 
        {
            var towerFallPath = typeof(TFGame).Assembly.Location;
            bool vanillaLaunch = false;
            foreach (var arg in args) 
            {
                if (arg == "--vanilla")
                {
                    vanillaLaunch = true;
                    break;
                }
            }

            if (vanillaLaunch) 
            {
                ThreadStart start = () => {
                    try 
                    {
                        AppDomain.CurrentDomain.ExecuteAssembly("fortOrig/TowerFall.exe");
                    }
                    catch (Exception e) 
                    {
                        Console.WriteLine(e.ToString());
                        Console.WriteLine(e.StackTrace);
                    }
                };
                Thread thread = new Thread(start);
                thread.Start();
                thread.Join();
                goto Exit;
            }
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) 
            {
                try 
                {
                    NativeMethods.SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                    NativeMethods.AddDllDirectory(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        Environment.Is64BitProcess ? "x64" : "x86"
                    ));
                }
                catch 
                {
                    NativeMethods.SetDllDirectory(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        Environment.Is64BitProcess ? "x64" : "x86"
                    ));
                }
            }

            if (!FortRise.RiseCore.Start()) 
            {
                SDL2.SDL.SDL_ShowSimpleMessageBox(
                    SDL2.SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, 
                    "CONTENT NOT FOUND",
                    "TowerFall Content cannot be found elsewhere.", IntPtr.Zero);
                return;
            }
            RiseCore.ParseArgs(args);
            var patchFile = "PatchVersion.txt";
            if (!File.Exists(patchFile)) 
            {
                patchFile = Path.Combine(RiseCore.GameRootPath, "PacthVersion.txt");
            }
            if (File.Exists(patchFile)) 
            {
                try 
                {
                    using var fs = File.OpenRead(patchFile);
                    using TextReader reader = new StreamReader(fs);
                    string readed;
                    while ((readed = reader.ReadLine()) != null) 
                    {
                        if (readed.Contains("Installer")) 
                        {
                            var version = readed.Split(':')[1].Trim();
                            RiseCore.FortRiseVersion = new System.Version(version);
                        }
                    }
                }
                catch 
                {
                    Logger.Log("Unable to load PatchVersion.txt, Unknown FortRise version will be sent.", Logger.LogLevel.Warning);
                    Logger.Log("Please report this bug", Logger.LogLevel.Warning);
                    RiseCore.DebugMode = true;
                }
            }
            if (RiseCore.DebugMode) 
            {
                // var detourModManager = new DetourModManager();
                // detourModManager.OnILHook += (assembly, source, dest) => 
                // {
                //     object obj = dest.Target;
                //     RiseCore.DetourLogs.Add($"ILHook from {assembly.GetName().Name}: {source.GetID()} :: {dest.Method?.GetID() ?? "??"}{(obj == null ? "" : $"(object: {obj})")}");
                // };
                // detourModManager.OnHook += (assembly, source, dest, obj) => 
                // {
                //     RiseCore.DetourLogs.Add($"Hook from {assembly.GetName().Name}: {source.GetID()} :: {dest.GetID()}{(obj == null ? "" : $"(object: {obj})")}");
                // };

                // detourModManager.OnDetour += (assembly, source, dest) => 
                // {
                //     RiseCore.DetourLogs.Add($"Detour from {assembly.GetName().Name}: {source.GetID()} :: {dest.GetID()}");
                // };
                
                try 
                {
                    Logger.AttachConsole(RiseCore.ConsoleAttachment());
                }
                catch (Exception e) 
                {
                    Logger.Error("Failed to attach console.");
                    Logger.Error(e.ToString());
                }
            }


            try 
            {
                InnerMain(args);

                if (RiseCore.WillRestart) 
                {
                    RiseCore.RunTowerFallProcess(towerFallPath, args);
                }
            }
            catch (Exception e) 
            {
                Logger.Error(e.ToString());
                Logger.Error(e.StackTrace);
            }

            Exit:
            Logger.DetachConsole();
            Logger.WriteToFile("fortRiseLog.txt");
            Environment.Exit(0);
        }

        [MonoModIfFlag("Steamworks")]
        [MonoModPatch("TryInit")]
        [MonoModOriginalName("TryInit")]
        public static bool TryInit_Steam() 
        {
            return SteamClient.Init();
        }

        [MonoModIfFlag("NoLauncher")]
        [MonoModPatch("TryInit")]
        [MonoModOriginalName("TryInit")]
        public static bool TryInit_NoLauncher() 
        {
            return true;
        }

        [MonoModIgnore]
        public static extern bool TryInit();

        public static void InnerMain(string[] args) 
        {
            bool noIntro = false;
            bool loadLog = false;
            foreach (string text in args)
            {
                if (text.ToLowerInvariant() == "nointro" || text.ToLowerInvariant() == "-nointro")
                {
                    noIntro = true;
                }
                else if (text.ToLowerInvariant() == "loadlog" || text.ToLowerInvariant() == "-loadlog")
                {
                    loadLog = true;
                }
                else if (text.ToLowerInvariant() == "noquit" || text.ToLowerInvariant() == "-noquit")
                {
                    MainMenu.NoQuit = true;
                }
                else if (text.ToLowerInvariant() == "nogamepads" || text.ToLowerInvariant() == "-nogamepads")
                {
                    MainMenu.NoGamepads = true;
                }
                else if (text.ToLowerInvariant() == "nogamepadupdates" || text.ToLowerInvariant() == "-nogamepadupdates")
                {
                    MainMenu.NoGamepadUpdates = true;
                }
            }
            if (loadLog)
            {
                TFGame.StartLoadLog();
            }
            TFGame.WriteLineToLoadLog("Initializing Steam...");
            if (!TryInit())
                return;
            TFGame.WriteLineToLoadLog("Initializing game window...");

            try
            {
                using (patch_TFGame tfgame = new patch_TFGame(noIntro))
                {
                    TFGame.WriteLineToLoadLog("Starting game...");
                    tfgame.InternalRun();
                }
            }
            catch (NoSuitableGraphicsDeviceException)
            {
                SDL2.SDL.SDL_ShowSimpleMessageBox(SDL2.SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "SAVE THIS MESSAGE!", "No suitable graphics card found!", IntPtr.Zero);
            }
            catch (Exception ex)
            {
                TFGame.Log(ex, false);
                TFGame.OpenLog();
            }
        }

        [MonoModIgnore]
        [MonoModLinkTo("Monocle.Engine", "InternalRun")]
        private extern void InternalRun();

        protected extern void orig_LoadContent();

        protected override void LoadContent()
        {
            FortRiseMenuAtlas = AtlasExt.CreateAtlasFromEmbedded("Content\\Atlas\\menuatlas.xml", "Content\\Atlas\\menuatlas.png");
            foreach (var mods in RiseCore.InternalMods) 
            {
                mods.Content.LoadResources();
            }
        }

        protected extern void orig_Initialize();

        protected override void Initialize() 
        {
            orig_LoadContent();
            FortRise.RiseCore.ModsAfterLoad();
            FortRise.RiseCore.Events.Invoke_OnPreInitialize();
            patch_Arrow.ExtendArrows();
            patch_TreasureSpawner.ExtendTreasures();
            orig_Initialize();
            FortRise.RiseCore.LogAllTypes();
        }

        protected extern void orig_UnloadContent();

        protected override void UnloadContent()
        {
            FortRise.RiseCore.ModuleEnd();
            orig_UnloadContent();
        }

        protected extern void orig_Update(GameTime gameTime);

        protected override void Update(GameTime gameTime)
        {
            RiseCore.Events.Invoke_BeforeUpdate(gameTime);
            RiseCore.Events.Invoke_Update(gameTime);
            orig_Update(gameTime);
            RiseCore.Events.Invoke_AfterUpdate(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            RiseCore.Events.Invoke_BeforeRender(Monocle.Draw.SpriteBatch);
            RiseCore.Events.Invoke_Render(Monocle.Draw.SpriteBatch);
            base.Draw(gameTime);
            RiseCore.Events.Invoke_AfterRender(Monocle.Draw.SpriteBatch);
        }

        [MonoModReplace]
        public static void Load()
        {
            TaskHelper.Run("loading data", () =>
            {
                try
                {
                    Loader.Message = "LOADING";
                    Logger.Log("[LOAD] --- LOADING DATA ---");

                    FortRise.RiseCore.RegisterMods();
                    Loader.Message = "INITIALIZING INPUT";
                    Logger.Log("[LOAD] ...Input");
                    TFGame.WriteLineToLoadLog("Initializing Input...");
                    patch_XGamepadInput.Init();
                    PlayerInput.AssignInputs();
                    for (int i = 0; i < 4; i++)
                    {
                        TFGame.Characters[i] = i;
                    }
                    Loader.Message = "INITIALIZING ARCHER DATA";
                    Logger.Log("[LOAD] ...Archer Data");
                    ArcherData.Initialize();
                    Loader.Message = "INITIALIZING LEVEL DATA";
                    Logger.Log("[LOAD] ...Level Data");
                    GameData.Load();
                    Loader.Message = "INITIALIZING DEFAULT SESSION";
                    TFGame.WriteLineToLoadLog("Initialize Default Sessions...");
                    MainMenu.VersusMatchSettings = MatchSettings.GetDefaultVersus();
                    MainMenu.TrialsMatchSettings = MatchSettings.GetDefaultTrials();
                    MainMenu.QuestMatchSettings = MatchSettings.GetDefaultQuest();
                    MainMenu.DarkWorldMatchSettings = MatchSettings.GetDefaultDarkWorld();
                    Loader.Message = "LOADING VERSUS AWARDS AND TIPS";
                    Logger.Log("[LOAD] ...Awards and Tips");
                    TFGame.WriteLineToLoadLog("Loading Versus Awards...");
                    VersusAwards.Initialize();
                    TFGame.WriteLineToLoadLog("Loading Versus Tips...");
                    GameTips.Initialize();
                    Loader.Message = "VERIFYING SAVE DATA";
                    Logger.Log("[LOAD] ...Save Data");
                    TFGame.WriteLineToLoadLog("Verifying Save Data...");
                    SaveData.Instance.Verify();
                    SessionStats.Initialize();
                    Loader.Message = "CACHING ENTITIES";
                    Logger.Log("[LOAD] ...Entity Caching");
                    TFGame.WriteLineToLoadLog("Initializing Entity Caching...");
                    Arrow.Initialize();
                    Cache.Init<CatchShine>();
                    Cache.Init<LightFade>();
                    Cache.Init<Explosion>();
                    Cache.Init<PlayerBreath>();
                    Cache.Init<ShockCircle>();
                    Cache.Init<SmallShock>();
                    Cache.Init<QuestScorePopup>();
                    Cache.Init<EnemyAttack>();
                    Cache.Init<Prism>();
                    Cache.Init<PrismVanish>();
                    Cache.Init<PrismParticle>();
                    Cache.Init<CrumbleWallChunk>();
                    Cache.Init<AmaranthShot>();
                    Cache.Init<BrambleSpore>();
                    Cache.Init<CyclopsShot>();
                    Cache.Init<CataclysmBullet>();
                    Cache.Init<WorkshopPortal>();
                    Loader.Message = "LOADING PARTICLES AND LIGHTING";
                    Logger.Log("[LOAD] ...Particles and Lighting");
                    TFGame.WriteLineToLoadLog("Initializing Particle Systems...");
                    Particles.Initialize();
                    TFGame.WriteLineToLoadLog("Initializing Lighting Systems...");
                    LightingLayer.Initialize();

                    Loader.Message = "LOADING SHADERS";
                    Logger.Log("[LOAD] ...Shaders (1/2)");
                    TFGame.WriteLineToLoadLog("Initializing Shaders (1 of 2)...");
                    ScreenEffects.Initialize();
                    Logger.Log("[LOAD] ...Shaders (2/2)");
                    TFGame.WriteLineToLoadLog("Initializing Shaders (2 of 2)...");
                    TFGame.LoadLightingEffect();
                    Logger.Log("[LOAD] --- LOADING COMPLETE ---");
                    TFGame.WriteLineToLoadLog("Loading Complete!");

                    Loader.Message = "INITIALIZING MODS";
                    FortRise.RiseCore.Initialize();

                    TFGame.GameLoaded = true;
                    if (TaskHelper.WaitForAll()) 
                    {
                        Loader.Message = "WAITING FOR OTHER TASK TO COMPLETE";
                    }
                }
                catch (Exception ex)
                {
                    TFGame.Log(ex, true);
                    TFGame.OpenLog();
                    Engine.Instance.Exit();
                }
            });

            if (RiseCore.DumpResources)
                TaskHelper.RunAsync("dumping assets", RiseCore.ResourceTree.DumpAll);

            TaskHelper.Run("loading sfx", () => 
            {
                try 
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    Logger.Log("[LOAD] ...Music");
                    TFGame.WriteLineToLoadLog("Loading Music...");
                    patch_Music.Initialize();
                    patch_Audio.InitMusicSystems();

                    foreach (var mods in RiseCore.InternalMods) 
                    {
                        mods.Content.LoadAudio();
                    }

                    Logger.Info($"[LOAD] -- MUSIC LOADING: {watch.ElapsedMilliseconds} ms --");

                    watch = Stopwatch.StartNew();
                    if (!Sounds.Loaded)
                    {
                        Logger.Log("[LOAD] ...SFX" );
                        TFGame.WriteLineToLoadLog("Loading Sounds...");
                        Sounds.Load();
                    }

                    SoundLoaded = true;
                    Logger.Info($"[LOAD] -- SOUND LOADING: {watch.ElapsedMilliseconds} ms --");
                    watch.Stop();
                }
                catch (Exception ex)
                {
                    TFGame.Log(ex, true);
                    TFGame.OpenLog();
                    Engine.Instance.Exit();
                }
            });
        }

        [MonoModReplace]
        public static IEnumerator MainMenuLoadWait()
        {
            while (TaskHelper.WaitForAll())
            {
                yield return 0;
            }
            foreach (var gameMode in GameModeRegistry.VersusGameModes) 
            {
                gameMode.InitializeSoundsInternal();
            }

            FortRise.RiseCore.Events.Invoke_OnPostInitialize();
            Loader.Message = "";
            if (SaveData.NewDataCreated && MainMenu.LoadError == null)
            {
                Saver saver = new Saver(true);
                Engine.Instance.Scene.Add(saver);
                saver.CanHandleError = true;
                while (!saver.Finished)
                {
                    yield return 0;
                }
                saver = null;
            }
            MainMenu.PlayMenuMusic();
            Engine.ConsoleEnabled = SaveData.Instance.Options.DevConsole;
            if (SaveData.Instance.Unlocks.Ascension)
            {
                (Engine.Instance.Scene as MainMenu).Background.AscensionTransition();
            }
            patch_TFGame.Loaded = true;
            yield break;
        }
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchTFGameMain))]
    internal class PatchTFGameMain : Attribute {}
    internal static partial class MonoModRules 
    {
        public static void PatchTFGameMain(ILContext ctx, CustomAttribute attrib) 
        {
            var InternalRun = ctx.Module.GetType("Monocle.Engine").FindMethod("System.Void InternalRun()");
            var cursor = new ILCursor(ctx);

            cursor.GotoNext(instr => instr.MatchCallOrCallvirt("Microsoft.Xna.Framework.Game", "Run"));
            cursor.Next.Operand = InternalRun;
        }
    }
}
