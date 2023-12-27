using System;
using System.Collections;
using System.Collections.Generic;
using FortRise;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{
    public class patch_DarkWorldControl : DarkWorldControl 
    {
        private List<QuestSpawnPortal> allPortals;
		private List<QuestSpawnPortal> groundPortals;
		private List<QuestSpawnPortal> airPortals;
		private List<QuestSpawnPortal> nodePortals;

        private List<Pickups> midwayTreasure;
        private bool bossMode;
        [PostPatchEnableTempVariant]
        public static void ActivateTempVariants(Level level, patch_DarkWorldTowerData.patch_LevelData levelData) 
        {
            patch_MatchVariants matchVariant = (level.Session.MatchSettings.Variants as patch_MatchVariants);
            if (levelData.ActiveVariant.CustomVariants != null) 
            {
                foreach (var variant in matchVariant.CustomVariants) 
                {
                    var key = variant.Key;
                    if (!levelData.ActiveVariant.CustomVariants.TryGetValue(key, out var val)) 
                    {
                        continue;
                    }
                    if (!variant.Value && val) 
                    {
                        TempVariantHolder.TempCustom[key] = true;
                        matchVariant.CustomVariants[key].Value = true;
                    }
                }
            }

            JumpLanding();
        }


        [PostPatchDisableTempVariant]
        public static void DisableTempVariants(Level level) 
        {
            patch_MatchVariants matchVariant = (level.Session.MatchSettings.Variants as patch_MatchVariants);
            var modified = new List<string>();
            foreach (var variant in TempVariantHolder.TempCustom) 
            {
                var variantKey = variant.Key;
                var variantVal = variant.Value;
                if (variantVal) 
                {
                    modified.Add(variantKey);
                    matchVariant.GetCustomVariant(variantKey).Value = false;
                }
            }
            foreach (var modifiedVariant in modified) 
            {
                TempVariantHolder.TempCustom[modifiedVariant] = false;
            }
            JumpLanding();
        }

        // Doing this will prevent the instruction jumping on a void
        private static void JumpLanding() {}

        [PatchDarkWorldControlLevelSequence]
        private extern IEnumerator orig_LevelSequence();

        [MonoModReplace]
        private void StartMusic()
        {
            if (bossMode)
                return;
            
            var levelSession = Level.Session;
            var themeMusic = Level.Session.MatchSettings.LevelSystem.Theme.Music;
            levelSession.SongTimer = 0;
            Music.Play(themeMusic);
        }

        private IEnumerator LevelSequence() 
        {
            patch_DarkWorldControl.DisableTempVariants(Level);
            var matchSettings = Level.Session.MatchSettings;

            patch_DarkWorldLevelSystem darkWorld = matchSettings.LevelSystem as patch_DarkWorldLevelSystem;
            var levelData = darkWorld.GetLevelData(matchSettings.DarkWorldDifficulty, Level.Session.RoundIndex);
            patch_DarkWorldControl.ActivateTempVariants(Level, levelData);

            if (matchSettings.Variants.SlowTime)
            {
                Level.OrbLogic.DoTimeOrb(delay: true);
            }
            if (matchSettings.Variants.AlwaysLava)
            {
                Level.OrbLogic.DoLavaVariant();
            }
            if (matchSettings.Variants.AlwaysScrolling)
            {
                Level.OrbLogic.StartScroll();
            }

            yield return orig_LevelSequence();
        }

        private void DoBossLevelSetup(int bossID, List<Pickups> treasure) 
        {
            //FIXME Will probably not do this soon
            var levelData = (Level.Session.MatchSettings.LevelSystem as DarkWorldLevelSystem).GetLevelData(base.Level.Session.MatchSettings.DarkWorldDifficulty, base.Level.Session.RoundIndex) as patch_DarkWorldTowerData.patch_LevelData;

            if (!string.IsNullOrEmpty(levelData.CustomBossName)) 
            {
                if (RiseCore.DarkWorldBossLoader.TryGetValue(levelData.CustomBossName, out var val)) 
                {
                    this.midwayTreasure = treasure;
                    int darkWorldDifficulty = (int)base.Level.Session.MatchSettings.DarkWorldDifficulty;
                    Level.Add(val(darkWorldDifficulty));
                    return;
                }
                Logger.Error($"Failed to spawn boss type name: {levelData.CustomBossName}. Falling back to Amaranth Boss");
            }
            orig_DoBossLevelSetup(bossID, treasure);
        }

        private extern void orig_DoBossLevelSetup(int bossID, List<Pickups> treasure);

        [PatchDarkWorldControlStartMatchSequence]
        [MonoModIgnore]
        private extern IEnumerator StartMatchSequence();

        private void FixedEmptyPortal() 
        {
            // allPortals isn't required, as it will not be empty anyway..
            FillIfNeeded(groundPortals, nameof(groundPortals));
            FillIfNeeded(nodePortals, nameof(nodePortals));
            FillIfNeeded(airPortals, nameof(airPortals));

            void FillIfNeeded(List<QuestSpawnPortal> portals, string portalName) 
            {
                if (portals.Count != 0)
                    return;
                
                var towerData = (Level.Session.MatchSettings.LevelSystem as DarkWorldLevelSystem).DarkWorldTowerData;
                var id = towerData.GetLevelID();
                Logger.Warning($"[DARK WORLD][{id}] does not have {portalName}.");
                portals.AddRange(allPortals);
            }
        }

        private void PlayBossMusic() 
        {
            foreach (var layer in Scene.Layers.Values) 
            {
                var boss = layer.GetFirst<patch_DarkWorldBoss>();
                if (boss is not null) 
                {
                    Music.Play(boss.BossMusic);
                    return;
                }
            }
        }
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDarkWorldControlStartMatchSequence))]
    public class PatchDarkWorldControlStartMatchSequence : Attribute {}

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDarkWorldControlLevelSequence))]
    public class PatchDarkWorldControlLevelSequence : Attribute {}

    internal static partial class MonoModRules 
    {
        public static void PatchDarkWorldControlLevelSequence(MethodDefinition method, CustomAttribute attrib) 
        {
            MethodDefinition complete = method.GetEnumeratorMoveNext();
            new ILContext(complete).Invoke(ctx => {
                var f__4this = ctx.Method.DeclaringType.FindField("<>4__this");
                var FixedEmptyPortal = f__4this.FieldType.Resolve().FindMethod("System.Void FixedEmptyPortal()");
                var PlayBossMusic = f__4this.FieldType.Resolve().FindMethod("System.Void PlayBossMusic()");
                var cursor = new ILCursor(ctx);
                ILLabel val = null;

                // It's BrFalse in Windows while BrTrue in Linux or MacOS
                Func<Instruction, bool> brFalseOrTrue;
                if (IsWindows)
                    brFalseOrTrue = instr => instr.MatchBrfalse(out val);
                else
                    brFalseOrTrue = instr => instr.MatchBrtrue(out val);

                cursor.GotoNext(brFalseOrTrue,
                    instr => instr.MatchLdstr("DarkBoss"));
                
                cursor.GotoNext();
                cursor.RemoveRange(2);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, f__4this);
                cursor.Emit(OpCodes.Call, PlayBossMusic);

                cursor.MarkLabel(val);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, f__4this);
                cursor.Emit(OpCodes.Call, FixedEmptyPortal);
            });
        }

        public static void PatchDarkWorldControlStartMatchSequence(MethodDefinition method, CustomAttribute attrib) 
        {
            MethodDefinition complete = method.GetEnumeratorMoveNext();
            new ILContext(complete).Invoke(ctx => {
                var f__4this = ctx.Method.DeclaringType.FindField("<>4__this");
                var HUD = ctx.Module.GetType("TowerFall.HUD");
                var get_Level = HUD.FindMethod("TowerFall.Level get_Level()");
                var Level = ctx.Module.GetType("TowerFall.Level");
                var get_Session = Level.FindMethod("TowerFall.Session get_Session()");
                var Session = ctx.Module.GetType("TowerFall.Session");
                var GetLevelSet = ctx.Module.GetType("TowerFall.SessionExt").FindMethod("System.String GetLevelSet(TowerFall.Session)");

                var MapButton = ctx.Module.GetType("TowerFall.MapButton");
                var InitDarkWorldStartLevelGraphics = MapButton.FindMethod("Monocle.Image[] InitDarkWorldStartLevelGraphics(System.Int32,System.String)");
                var cursor = new ILCursor(ctx);

                cursor.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt("TowerFall.MapButton", "InitDarkWorldStartLevelGraphics"));

                cursor.Next.Operand = InitDarkWorldStartLevelGraphics;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, f__4this);
                cursor.Emit(OpCodes.Callvirt, get_Level);
                cursor.Emit(OpCodes.Callvirt, get_Session);
                cursor.Emit(OpCodes.Call, GetLevelSet);
            });
        }
    }
}