using System;
using System.Collections;
using System.Collections.Generic;
using FortRise;
using FortRise.Adventure;
using Microsoft.Xna.Framework.Input;
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
            // foreach (var (contaning, mapRenderer) in patch_GameData.AdventureWorldMapRenderer) 
            // {
            //     var entity = new Entity(-1);
            //     if (!contaning) 
            //         continue;
            //     entity.Add(mapRenderer);
            //     Add(entity);
            //     mapRenderer.Visible = false;
            // }
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
                        _ => 0
                    };
                    GotoAdventure(CurrentAdventureType, startingID + 1);
                    entity.RemoveSelf();
                });
                Add(entity);
            }
        }


        [MonoModReplace]
        private void StartSession() 
        {
            var session = new Session(MainMenu.CurrentMatchSettings);
            session.SetLevelSet(LevelSet);
            session.StartGame();
        }

        [MonoModReplace]
        private IEnumerator DarkWorldIntroSequence() 
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
            Buttons.Add(new AdventureCategoryButton(CurrentAdventureType));
            switch (CurrentAdventureType) 
            {
            case AdventureType.Quest:
                for (int i = 0; i < GameData.QuestLevels.Length; i++)
                {
                    if (SaveData.Instance.Unlocks.GetQuestTowerUnlocked(i))
                    {
                        this.Buttons.Add(new QuestMapButton(GameData.QuestLevels[i]));
                    }
                }
                break;
            case AdventureType.DarkWorld:
                for (int j = 0; j < GameData.DarkWorldTowers.Count; j++)
                {
                    if (SaveData.Instance.Unlocks.GetDarkWorldTowerUnlocked(j))
                    {
                        Buttons.Add(new DarkWorldMapButton(GameData.DarkWorldTowers[j]));
                    }
                }
                break;
            case AdventureType.Versus:
                Buttons.Add(new AdventureChaoticRandomSelect());
                InitVersusButtons();
                break;
            }

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

    internal static partial class MonoModRules 
    {

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

            cursor.GotoNext(MoveType.Before, instr => instr.MatchLdcI4(0));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Call, method);
            cursor.GotoNext();
            cursor.GotoNext(MoveType.Before, instr => instr.MatchLdcI4(0));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Call, method);

            cursor.GotoNext(MoveType.After, 
                instr => instr.MatchNewobj("System.Collections.Generic.List`1<TowerFall.MapButton[]>"),
                instr => instr.MatchStloc(4));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_S, ctx.Body.Variables[4]);
            cursor.Emit(OpCodes.Call, methodWithList);
        }
    }
}