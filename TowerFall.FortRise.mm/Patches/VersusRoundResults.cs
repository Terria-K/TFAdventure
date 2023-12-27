using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{

    public class patch_VersusRoundResults : VersusRoundResults
    {
        private Session session;
        public patch_VersusRoundResults(Session session, List<EventLog> events) : base(session, events)
        {
        }

        [MonoModReplace]
        private void PlayGainSound()
        {
            if (session.MatchSettings.Mode == Modes.HeadHunters)
            {
                Sounds.sfx_multiSkullEarned.Play(160f, 1f);
                return;
            }
            if ((session.MatchSettings as patch_MatchSettings).IsCustom) 
            {
                var gameMode = (session.MatchSettings as patch_MatchSettings).CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    gameMode.EarnedCoinSound.Play(160f, 1f);
                    return;
                }
            }
            Sounds.sfx_multiCoinEarned.Play(160f, 1f);
        }

        [MonoModReplace]
        private void LosePoint(Sprite<int> point)
        {
            point.Stop();
            point.CurrentFrame = 0;
            point.Color = DeathSkull.SuicideColor * 0.35f;
            Wiggler wiggler = Wiggler.Create(30, 3f, null, v =>
            {
                point.Scale = Vector2.One * (1f + v * 0.3f);
            }, true, true);
            Add(wiggler);

            if ((session.MatchSettings as patch_MatchSettings).IsCustom) 
            {
                var gameMode = (session.MatchSettings as patch_MatchSettings).CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    gameMode.LoseCoinSound.Play(160f, 1f);
                    return;
                }
            }
            Sounds.sfx_multiSkullNegative.Play(160f, 1f);
        }

        [MonoModReplace]
        private void LosePointOverflow(Text overflowText, int totalScore)
        {
            if (totalScore <= this.session.MatchSettings.GoalScore)
            {
                overflowText.Color = DeathSkull.SuicideColor;
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 30, true);
                tween.OnUpdate = t =>
                {
                    overflowText.Scale = Vector2.One * (1f - t.Eased);
                };
                Add(tween);
            }
            else
            {
                overflowText.Color = DeathSkull.SuicideColor;
                overflowText.DrawText = "+" + (totalScore - session.MatchSettings.GoalScore);
                Wiggler wiggler = Wiggler.Create(30, 3f, null, v =>
                {
                    overflowText.Scale = Vector2.One * (1f + 0.3f * v);
                }, true, true);
                Add(wiggler);
            }
            if ((session.MatchSettings as patch_MatchSettings).IsCustom) 
            {
                var gameMode = (session.MatchSettings as patch_MatchSettings).CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    gameMode.LoseCoinSound.Play(160f, 1f);
                    return;
                }
            }
            Sounds.sfx_multiSkullNegative.Play(160f, 1f);
        }

        private Sprite<int> GetCustomSpriteOrNot() 
        {
            if (patch_MainMenu.VersusMatchSettings.IsCustom) 
            {
                var gameMode = patch_MainMenu.VersusMatchSettings.CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    return gameMode.CoinSprite();
                }
            }
            return VersusCoinButton.GetCoinSprite();
        }

        private SFX GetCustomSoundOrNot() 
        {
            if (patch_MainMenu.VersusMatchSettings.IsCustom) 
            {
                var gameMode = patch_MainMenu.VersusMatchSettings.CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    return gameMode.EarnedCoinSound;
                }
            }
            return Sounds.sfx_multiCoinEarned;
        }

        private int GetCustomOffsetOrNot() 
        {
            if (patch_MainMenu.VersusMatchSettings.IsCustom) 
            {
                var gameMode = patch_MainMenu.VersusMatchSettings.CurrentCustomGameMode;
                if (gameMode != null) 
                {
                    return gameMode.CoinOffset;
                }
            }
            return 10;
        }

        [MonoModIgnore]
        [PatchVersusRoundResultsSequence]
        private extern IEnumerator Sequence(List<EventLog> events);
    }
}

namespace MonoMod
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchVersusRoundResultsSequence))]
    internal class PatchVersusRoundResultsSequence : Attribute {}


    internal static partial class MonoModRules 
    {
        public static void PatchVersusRoundResultsSequence(MethodDefinition method, CustomAttribute attrib) 
        {
            var moveNext = method.GetEnumeratorMoveNext();
            new ILContext(moveNext).Invoke(ctx => {
                var this__4 = ctx.Method.DeclaringType.FindField("<>4__this");
                var GetCustomSpriteOrNot = this__4.FieldType.Resolve().FindMethod("Monocle.Sprite`1<System.Int32> GetCustomSpriteOrNot()");
                var GetCustomOffsetOrNot = this__4.FieldType.Resolve().FindMethod("System.Int32 GetCustomOffsetOrNot()");
                var cursor = new ILCursor(ctx);

                cursor.GotoNext(instr => instr.MatchLdftn("TowerFall.VersusCoinButton", "GetCoinSprite"));
                cursor.Prev.OpCode = OpCodes.Ldarg_0;
                cursor.Next.Operand = GetCustomSpriteOrNot;
                cursor.Emit(OpCodes.Ldfld, this__4);

                cursor.GotoNext(instr => instr.MatchLdcI4(10));
                cursor.Next.OpCode = OpCodes.Call;
                cursor.Next.Operand = GetCustomOffsetOrNot;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, this__4);
            });
        }
    }
}