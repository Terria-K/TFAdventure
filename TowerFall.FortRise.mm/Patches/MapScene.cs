using System;
using System.Collections;
using System.Collections.Generic;
using FortRise.Adventure;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{
    public class patch_MapScene : MapScene
    {
        private static int lastRandomVersusTower;
        private Tween cameraTween;
        private bool adventureLevels;
        private float crashDelay;
        private Counter counterDelay;
        public AdventureType CurrentAdventureType;
        public bool MapPaused;
        public string LevelSet;
        public patch_MapRenderer Renderer;

        public patch_MapScene(MainMenu.RollcallModes mode) : base(mode)
        {
        }

        private void InitializeCustoms() 
        {
            var counterHolder = new Entity();
            counterDelay = new Counter();
            counterHolder.Add(counterDelay);
            Add(counterHolder);
            crashDelay = 10;
        }

        [MonoModConstructor]
        [MonoModReplace]
        public static void cctor() {}

        internal static void FixedStatic() 
        {
            lastRandomVersusTower = -1;
            MapScene.NoRandomStates = new bool[GameData.VersusTowers.Count];
        }

        [MonoModIgnore]
        [PatchMapSceneBegin]
        [FixClampCamera]
        [PreFixing("TowerFall.MapScene", "System.Void InitializeCustoms()")]
        public extern void orig_Begin();

        public override void Begin() 
        {
            orig_Begin();
            if (!this.IsOfficialLevelSet()) 
            {
                var entity = new Entity();
                Alarm.Set(entity, 10, () => {
                    var startingID = CurrentAdventureType switch 
                    {
                        AdventureType.Quest => MainMenu.QuestMatchSettings.LevelSystem.ID.X,
                        AdventureType.DarkWorld => MainMenu.DarkWorldMatchSettings.LevelSystem.ID.X,
                        AdventureType.Trials => MainMenu.TrialsMatchSettings.LevelSystem.ID.X,
                        AdventureType.Versus => MainMenu.VersusMatchSettings.LevelSystem.ID.X,
                        _ => 0
                    };
                    GotoAdventure(CurrentAdventureType, startingID + 1);
                    entity.RemoveSelf();
                });
                Add(entity);
            }
            FortRise.RiseCore.Events.Invoke_OnMapBegin(this);
        }


        [MonoModReplace]
        private void StartSession() 
        {
            var session = new Session(MainMenu.CurrentMatchSettings);
            session.SetLevelSet(LevelSet);
            session.StartGame();
        }

        [MonoModReplace]
        [MonoModPatch("DarkWorldIntroSequence")] // This is a hack to fix System.TypeLoadException on MacOS and Linux
        private IEnumerator DarkWorldIntroSequence_Patch() 
        {
            int num = 0;
            for (int i = 0; i < Buttons.Count; i = num + 1) 
            {
                if (Buttons[i] is not DarkWorldMapButton)
                   continue;
                if (SaveData.Instance.DarkWorld.ShouldRevealTower(Buttons[i].Data.ID.X)) 
                {
                    Music.Stop();
                    yield return Buttons[i].UnlockSequence(false);
                }
                num = i;
            }
            yield break;
        }

        [MonoModReplace]
        private IEnumerator QuestIntroSequence()
        {
            int num = 0;
            for (int i = 0; i < this.Buttons.Count; i = num + 1)
            {
                if (Buttons[i] is not QuestMapButton)
                   continue;
                if (SaveData.Instance.Quest.ShouldRevealTower(this.Buttons[i].Data.ID.X))
                {
                    Music.Stop();
                    yield return this.Buttons[i].UnlockSequence(true);
                }
                num = i;
            }
            yield break;
        }

        

        private void InitAdventureMap(AdventureType adventureType) 
        {
            CurrentAdventureType = adventureType;
            Buttons.Add(new AdventureCategoryButton(adventureType));
            if (adventureType == AdventureType.Versus) 
            {
                Buttons.Add(new AdventureChaoticRandomSelect());
            }
        }

        private void InitAdventureMap(List<MapButton[]> list) 
        {
            CurrentAdventureType = AdventureType.Trials;
            var adv = new AdventureCategoryButton(CurrentAdventureType);
            Buttons.Add(adv);
            list.Add(new MapButton[] { adv, adv, adv });
        }

        public void InitAdventure(int id) 
        {
            counterDelay.Set(20);
            Add(new AdventureListLoader(this, id));
        }

        public void GotoAdventure(AdventureType type, int id = 0) 
        {
            adventureLevels = true;
            WorkshopLevels = true;
            TweenOutAllButtonsAndRemove();
            Buttons.Clear();
            InitAdventure(id);
        }

        public extern void orig_ExitWorkshop();

        public void ExitWorkshop() 
        {
            if (adventureLevels)
                ExitAdventure();
            else
                orig_ExitWorkshop();
        }

        public void ExitAdventure(int id = 1) 
        {
            adventureLevels = false;
            WorkshopLevels = false;
            TweenOutAllButtonsAndRemove();
            LevelSet = "TowerFall";
            Renderer.ChangeLevelSet(LevelSet);
            Buttons.Clear();
            switch (CurrentAdventureType) 
            {
            case AdventureType.Quest:
                Buttons.Add(new AdventureCategoryButton(CurrentAdventureType));
                for (int i = 0; i < GameData.QuestLevels.Length; i++)
                {
                    if (SaveData.Instance.Unlocks.GetQuestTowerUnlocked(i))
                    {
                        this.Buttons.Add(new QuestMapButton(GameData.QuestLevels[i]));
                    }
                }
                break;
            case AdventureType.DarkWorld:
                Buttons.Add(new AdventureCategoryButton(CurrentAdventureType));
                for (int j = 0; j < GameData.DarkWorldTowers.Count; j++)
                {
                    if (SaveData.Instance.Unlocks.GetDarkWorldTowerUnlocked(j))
                    {
                        Buttons.Add(new DarkWorldMapButton(GameData.DarkWorldTowers[j]));
                    }
                }
                break;
            case AdventureType.Versus:
                Buttons.Add(new AdventureCategoryButton(CurrentAdventureType));
                Buttons.Add(new AdventureChaoticRandomSelect());
                InitVersusButtons();
                break;
            case AdventureType.Trials:
                List<MapButton[]> list = new List<MapButton[]>();
				this.InitAdventureMap(list);
				for (int k = 0; k < GameData.VersusTowers.Count; k++)
				{
					if (SaveData.Instance.Unlocks.GetTowerUnlocked(k))
					{
						var array = new MapButton[GameData.TrialsLevels.GetLength(1)];
						for (int l = 0; l < array.Length; l++)
						{
							array[l] = new TrialsMapButton(GameData.TrialsLevels[k, l]);
							Buttons.Add(array[l]);
						}
						for (int m = 0; m < array.Length; m++)
						{
							if (m > 0)
							{
								array[m].UpButton = array[m - 1];
							}
							if (m < array.Length - 1)
							{
								array[m].DownButton = array[m + 1];
							}
						}
						list.Add(array);
					}
				}
				for (int n = 0; n < list.Count; n++)
				{
					if (n > 0)
					{
						for (int num3 = 0; num3 < list[n].Length; num3++)
						{
							list[n][num3].LeftButton = list[n - 1][num3];
						}
					}
					if (n < list.Count - 1)
					{
						for (int num4 = 0; num4 < list[n].Length; num4++)
						{
							list[n][num4].RightButton = list[n + 1][num4];
						}
					}
					for (int num5 = 0; num5 < list[n].Length; num5++)
					{
						list[n][num5].MapXIndex = n;
					}
				}
                break;
            }

            if (CurrentAdventureType != AdventureType.Trials)
                this.LinkButtonsList();
            if (id >= Buttons.Count)
                id = Buttons.Count;
            InitButtons(Buttons[0]);
            foreach (var button in Buttons)
                Add(button);
            ScrollToButton(Selection);
        }

        [MonoModIgnore]
        private extern void InitVersusButtons();


        [MonoModLinkTo("Monocle.Scene", "System.Void Update()")]
        [MonoModIgnore]
        public void base_Update() 
        {
            base.Update();
        }

        [FixClampCamera]
        private extern void orig_Update();

        public override void Update()
        {
            if (MapPaused) 
            {
                base_Update();
                return;
            }
            if (!ScrollMode && !MatchStarting && Mode == MainMenu.RollcallModes.DarkWorld && crashDelay <= 0) 
            {

            }
            orig_Update();
            if (crashDelay > 0)
                crashDelay--;
        }

        public void TweenOutAllButtonsAndRemove() 
        {
            foreach (var mapButton in Buttons) 
            {
                (mapButton as patch_MapButton).TweenOutAndRemoved();
            }
        }

        public void TweenOutAllButtonsAndRemoveExcept(MapButton button) 
        {
            foreach (var mapButton in Buttons) 
            {
                if (button == mapButton)
                    continue;    
                (mapButton as patch_MapButton).TweenOutAndRemoved();
            }
        }

        public static Vector2 FixedClampCamera(Vector2 position, patch_MapScene map) 
        {
			return new Vector2(
                MathHelper.Clamp(position.X, 160f, map.Renderer.GetInstanceWidth() - 160), 
                MathHelper.Clamp(position.Y, 120f, map.Renderer.GetInstanceHeight() - 120));
        }

        [MonoModReplace]
        public void ScrollToPosition(Vector2 position) 
        {
            Vector2 start = Camera.Position;
            cameraTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 30, true);
            cameraTween.OnUpdate = t =>
            {
                Camera.Position = Vector2.Lerp(start, FixedClampCamera(position, this), t.Eased);
            };
        }

        [MonoModPatch("<>c")]
        public class GetRandomVersusTower_c 
        {
            [MonoModPatch("<GetRandomVersusTower>b__39_0")]
            [MonoModReplace]
            internal bool GetRandomVersusTowerb__39_0(MapButton b)
            {
                return !(b is VersusMapButton or AdventureMapButton);
            }

            [MonoModPatch("<GetRandomVersusTower>b__39_1")]
            [MonoModReplace]
            internal bool GetRandomVersusTowerb__39_1(MapButton b)
            {
                return b is VersusMapButton && !(b as VersusMapButton).NoRandom;
            }

            [MonoModPatch("<GetRandomVersusTower>b__39_2")]
            [MonoModReplace]
            internal bool GetRandomVersusTowerb__39_2(MapButton b)
            {
                if (b is not VersusMapButton)
                    return false;
                return (b as VersusMapButton).NoRandom;
            }
        }
    }

    public static class MapSceneExt 
    {
        public static void SetLevelSet(this MapScene mapScene, string levelSet) 
        {
            ((patch_MapScene)mapScene).LevelSet = levelSet;
        }

        public static string GetLevelSet(this MapScene mapScene) 
        {
            return ((patch_MapScene)mapScene).LevelSet ?? "TowerFall";
        }

        public static bool IsOfficialLevelSet(this MapScene mapScene) 
        {
            return ((patch_MapScene)mapScene).GetLevelSet() == "TowerFall";
        }

        public static AdventureType GetCurrentAdventureType(this MapScene mapScene) 
        {
            return ((patch_MapScene)mapScene).CurrentAdventureType;
        }
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchMapSceneBegin))]
    internal class PatchMapSceneBegin : Attribute {}

    [MonoModCustomMethodAttribute(nameof(MonoModRules.FixClampCamera))]
    internal class FixClampCamera : Attribute {}

    internal static partial class MonoModRules 
    {
        public static void FixClampCamera(ILContext ctx, CustomAttribute attrib) 
        {
            var method = ctx.Method.DeclaringType.FindMethod("Microsoft.Xna.Framework.Vector2 FixedClampCamera(Microsoft.Xna.Framework.Vector2,TowerFall.MapScene)");

            var cursor = new ILCursor(ctx);

            cursor.GotoNext(instr => instr.MatchCall("TowerFall.MapScene", "ClampCamera"));
            cursor.Next.Operand = method;

            cursor.Emit(OpCodes.Ldarg_0);
        }

        public static void PatchMapSceneBegin(ILContext ctx, CustomAttribute attrib) 
        {
            var method = ctx.Method.DeclaringType.FindMethod("System.Void InitAdventureMap(FortRise.Adventure.AdventureType)");
            var methodWithList = 
                ctx.Method.DeclaringType.FindMethod(
                    "System.Void InitAdventureMap(System.Collections.Generic.List`1<TowerFall.MapButton[]>)");

            ILCursor cursor = new ILCursor(ctx);
            cursor.GotoNext(instr => instr.MatchCallOrCallvirt("TowerFall.MapScene", "System.Void InitVersusButtons()"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_3);
            cursor.Emit(OpCodes.Call, method);

            if (!IsWindows)
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(0));

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(0));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Call, method);
            cursor.GotoNext();
            if (!IsWindows) 
            {
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(0));
                cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(0));
            }

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(0));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Call, method);

            int location;
            if (IsWindows)
                location = 4;
            else
                location = 2;

            cursor.GotoNext(MoveType.After, 
                instr => instr.MatchNewobj("System.Collections.Generic.List`1<TowerFall.MapButton[]>"),
                instr => instr.MatchStloc(location));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_S, ctx.Body.Variables[location]);
            cursor.Emit(OpCodes.Call, methodWithList);
        }
    }
}