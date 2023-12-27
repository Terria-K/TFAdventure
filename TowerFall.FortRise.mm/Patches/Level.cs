using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{
    public class patch_Level : Level
    {
        private Layer bgGameLayer;
        private Layer gameLayer;
        private RenderTarget2D foregroundRenderTarget;
        private FileSystemWatcher levelWatcher;
        private List<ShaderFilter> ActiveShaders;

        private bool reload;
        public XmlElement XML
        {
            [MonoModIgnore]
            get => throw null;
            [MonoModIgnore]
            private set => throw null;
        }
        public static bool DebugMode;
        public patch_Level(Session session, XmlElement xml) : base(session, xml)
        {
        }

        public extern void orig_ctor(Session session, XmlElement xml);

        [MonoModConstructor]
        public void ctor(Session session, XmlElement xml) 
        {
            orig_ctor(session, xml);
            ActiveShaders = new();
            if (!RiseCore.DebugMode || session.GetLevelSet() == "TowerFall") 
                return;
            
            var levelSystem = session.MatchSettings.LevelSystem;

            string levelPath = null;
            switch (levelSystem) 
            {
            case VersusLevelSystem versus:
                levelPath = versus.GetLastLevel();
                break;
            case QuestLevelSystem quest:
                levelPath = quest.QuestTowerData.Path;
                break;
            case DarkWorldLevelSystem darkWorld:
                var file = darkWorld.DarkWorldTowerData
                    [session.MatchSettings.DarkWorldDifficulty][session.RoundIndex + darkWorld.GetStartLevel()].File; 
                levelPath = darkWorld.DarkWorldTowerData.Levels[file];
                break;
            case TrialsLevelSystem trials:
                levelPath = trials.TrialsLevelData.Path;
                break;
            }

            if (RiseCore.ResourceTree.TreeMap.TryGetValue(levelPath, out var res)) 
            {
                var fullPath = Path.GetDirectoryName(res.FullPath);
                // we need it to be in the folder. If it's in the zip, don't watch
                if (!Directory.Exists(fullPath))
                    return;
                levelWatcher = new FileSystemWatcher(fullPath, "*.oel");
                levelWatcher.EnableRaisingEvents = true;
                levelWatcher.Changed += OnFileChanged;
            }
        }

        public extern void orig_LoadEntity(XmlElement e);

        public void LoadEntity(XmlElement e) 
        {
            var name = e.Name;
            if (FortRise.RiseCore.LevelEntityLoader.TryGetValue(name, out var val)) 
            {
                Add(val(e, e.Position(), e.Nodes()));
                return;
            }
            orig_LoadEntity(e);
        }

        [MonoModReplace]
        public void CoreRender(RenderTarget2D canvas) 
        {
            Engine.Instance.GraphicsDevice.SetRenderTarget(foregroundRenderTarget);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
            bgGameLayer.Render();
            gameLayer.Render();
            if (Foreground != null && Foreground.Visible)
            {
                Foreground.Render();
            }

            Engine.Instance.GraphicsDevice.SetRenderTarget(canvas);
            if (Background != null && Background.Visible)
            {
                Background.Render();
            }
            if (LightingLayer.DarkenForLightning)
            {
                TFGame.LightingAlpha.SetValue(Math.Min(1f, LightingLayer.Alpha * 1.5f));
            }
            else
            {
                TFGame.LightingAlpha.SetValue(LightingLayer.Alpha);
            }
            foreach (var shaders in ActiveShaders) 
            {
                shaders.BeforeRender(canvas);
            }

            Engine.Instance.GraphicsDevice.Textures[1] = LightingLayer.Canvas.Texture2D;
            Draw.SpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, TFGame.LightingEffect, Matrix.Identity);
            Draw.SpriteBatch.Draw(foregroundRenderTarget, Vector2.Zero, Color.White);
            foreach (var shader in ActiveShaders) 
            {
                shader.Render(canvas);
            }
            Draw.SpriteBatch.End();
            foreach (var shader in ActiveShaders) 
            {
                shader.AfterRender(canvas);
            }
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            if (Camera.Origin == Vector2.Zero)
            {
                if (Camera.X < 0f)
                {
                    Draw.Rect(0f, -2f, 2f, 244f, Color.Black);
                }
                else if (Camera.X > 0f)
                {
                    Draw.Rect(318f, -2f, 2f, 244f, Color.Black);
                }
                if (Camera.Y < 0f)
                {
                    Draw.Rect(-2f, 0f, 324f, 2f, Color.Black);
                }
                else if (Camera.Y > 0f)
                {
                    Draw.Rect(-2f, 238f, 324f, 2f, Color.Black);
                }
            }
            Draw.SpriteBatch.End();
        }

        [MonoModIgnore]
        [PreFixing("FortRise.RiseCore/Events", "System.Void Invoke_OnLevelEntered()", true)]
        public extern override void Begin();

        [MonoModIgnore]
        [PostFixing("FortRise.RiseCore/Events", "System.Void Invoke_OnLevelExited()", true)]
        public extern void orig_End();

        public override void End() 
        {
            orig_End();
            if (levelWatcher != null) 
            {
                levelWatcher.Changed -= OnFileChanged;
                levelWatcher.Dispose();
            }
        }

        [MonoModIgnore]
        [PostFixing("TowerFall.Level", "System.Void DebugModeRender()")]
        public extern override void Render();

        public void DebugModeRender() 
        {
            if (DebugMode) 
            {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Lerp(Matrix.Identity, Camera.Matrix, 1f));
                foreach (var entity in Layers[0].Entities) 
                {
                    entity.DebugRender();
                }
                Draw.SpriteBatch.End();
            }
        }


        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // This event sometimes called twice.
            if (reload)
                return;    
            try 
            {
                using var fullPath = File.OpenRead(e.FullPath);
                var xml = patch_Calc.LoadXML(fullPath)["level"];

                // Let's replace the old one
                bool[,] solidsBitData = Calc.GetBitData(xml["Solids"].InnerText, 32, 24);
                bool[,] bgBitData = Calc.GetBitData(xml["BG"].InnerText, 32, 24);
                int[,] bgTileData = Calc.ReadCSVIntGrid(xml["BGTiles"].InnerText, 32, 24);
                int[,] solidTilesData = Calc.ReadCSVIntGrid(xml["SolidTiles"].InnerText, 32, 24);

                XML = xml;
                HotReload(solidsBitData, bgBitData, bgTileData, solidTilesData);

                Logger.Info($"[Level][Reload] Hot Reloaded {fullPath}");
            }
            catch (UnauthorizedAccessException) 
            {
                Thread.Sleep(500);
            }
            catch (IOException) 
            {
                Thread.Sleep(500);
            }
            catch (Exception x)
            {
                Logger.Error($"[Level][Reload] Unexpected error while trying to hot reload.");
                Logger.Error(x.ToString());
            }
        }

        public void HotReload(bool[,] solids, bool[,] bg, int[,] bgTiles, int[,] solidTiles) 
        {
            (BGTiles as patch_LevelBGTiles).Replace(bg, solids, bgTiles);
            (Tiles as patch_LevelTiles).Replace(solids, solidTiles);
            reload = true;

            foreach (var entity in Layers[-3].Entities)
            {
                if (entity is not LevelEntity)
                    continue;

                Remove(entity);
            }

            foreach (var entity in Layers[0].Entities)
            {
                if (entity is not LevelEntity)
                    continue;
                
                if (entity is Actor or LevelTiles or QuestSpawnPortal) 
                {
                    if (entity is not Lantern and not Orb)
                        continue;
                }

                Remove(entity);
            }

            foreach (XmlElement xmlElement2 in XML["Entities"])
            {
                LoadEntity(xmlElement2);
            }
        }

        public override void PreRender()
        {
            if (reload)
            {
                (BGTiles as patch_LevelBGTiles).ReloadTiles();
                (Tiles as patch_LevelTiles).ReloadTiles();
                reload = false;
            }
            base.PreRender();
        }

        public void Reload(XmlElement xml, int width, int height) 
        {
            XML = xml;
            bool[,] solidsBitData = Calc.GetBitData(this.XML["Solids"].InnerText, width, height);
            bool[,] bgBitData = Calc.GetBitData(this.XML["BG"].InnerText, width, height);
            int[,] overwriteData = Calc.ReadCSVIntGrid(this.XML["BGTiles"].InnerText, width, height);
            foreach (var entity in Layers[0].Entities) 
            {
                if (entity is LevelEntity and not (TowerFall.Player or Enemy or TreasureChest or Arrow))
                    continue;
                
                Remove(entity);
            }

            Add<LevelTiles>(Tiles = new LevelTiles(this.XML, solidsBitData));
            Add<LevelBGTiles>(BGTiles = new LevelBGTiles(this.XML, bgBitData, solidsBitData, overwriteData));
            foreach (XmlElement xmlElement2 in this.XML["Entities"])
            {
                LoadEntity(xmlElement2);
            }
            if (xml["Entities"].GetElementsByTagName("BlueSwitchBlock").Count > 0 || xml["Entities"].GetElementsByTagName("RedSwitchBlock").Count > 0)
            {
                Add<SwitchBlockControl>(new SwitchBlockControl(this.Session));
            }
            else if (this.Session.MatchSettings.Variants.DarkPortals)
            {
                Add<DarkPortalsVariantSequence>(new DarkPortalsVariantSequence());
            }
        }

        [PatchLevelHandlePausing]
        [MonoModIgnore]
        public extern void HandlePausing();

        public void Activate(ShaderFilter filter) 
        {
            ActiveShaders.Add(filter);
            filter.Activated(new ShaderFilter.LevelRenderData { 
                ForegroundRenderTarget = foregroundRenderTarget,
                BGTiles = BGTiles,
                SolidTiles = Tiles,
                Level = this
            });
        }

        public void Deactivate(ShaderFilter filter) 
        {
            ActiveShaders.Remove(filter);
            filter.Deactivated();
        }
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchLevelHandlePausing))]
    internal class PatchLevelHandlePausing : Attribute {}

    internal static partial class MonoModRules 
    {
        public static void PatchLevelHandlePausing(ILContext ctx, CustomAttribute attrib) 
        {
            var get_NoAutoPause = ctx.Module.GetType("FortRise.RiseCore").FindMethod("get_NoAutoPause");
            var cursor = new ILCursor(ctx);
            ILLabel label = null;

            cursor.GotoNext(instr => instr.MatchCallOrCallvirt(
                "Microsoft.Xna.Framework.Game", "get_IsActive"));
            cursor.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out label));

            cursor.Emit(OpCodes.Call, get_NoAutoPause);
            cursor.Emit(OpCodes.Brtrue_S, label);
        }
    }
}