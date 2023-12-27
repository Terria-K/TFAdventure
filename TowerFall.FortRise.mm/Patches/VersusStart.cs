using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall
{
    public class patch_VersusStart : VersusStart
    {
        private bool tweeningOut;
        private Session session;
        public patch_VersusStart(Session session) : base(session)
        {
        }

        [MonoModIgnore]
        [PatchVersusStartSessionIntroSequence]
        private extern IEnumerator SessionIntroSequence();

        private OutlineText CustomModeText(object displayField) 
        {
            var customGameMode = (session.MatchSettings as patch_MatchSettings).CurrentCustomGameMode;
            if (customGameMode == null)
                return null;
            var outlineText = new OutlineText(TFGame.Font, customGameMode.Name.ToUpperInvariant());
            outlineText.Color = Color.Transparent;
            outlineText.OutlineColor = Color.Transparent;
            displayField.DynSetData("modeColor", customGameMode.NameColor);
            Add(outlineText);
            return outlineText;
        }

        [MonoModReplace]
        private IEnumerator VariantsSequence(Subtexture[] variants)
        {
            var variantImage = new Entity(this.Position, base.LayerIndex) {
                Depth = base.Depth - 1
            };

            Scene.Add<Entity>(variantImage);
            var vector = new Vector2(0f, 50f);
            var positions = new Vector2[variants.Length];
            var images = new OutlineImage[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                int x = i % 14;
                int y = i / 14;
                float num3 = (float)(-(Math.Min(14, variants.Length - y * 14) - 1) * 18 / 2);
                positions[i] = vector + new Vector2(num3 + (x * 18), (y * 18));
            }
            yield return 5;
            int index = 0;
            for (int i = 0; i < variants.Length; i = index + 1)
            {
                yield return 1;
                var image = new OutlineImage(variants[i]);
                image.CenterOrigin();
                image.Position = positions[i];
                image.Scale = Vector2.Zero;
                variantImage.Add(image);
                images[i] = image;
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 12, true);
                tween.OnUpdate = t =>
                {
                    image.Scale = Vector2.One * t.Eased;
                };
                Add(tween);
                index = i;
            }
            while (!this.tweeningOut)
                yield return null;

            var coroutine = new Coroutine(VariantTweenOutSequence(variants, images, index, variantImage));
            variantImage.Add(coroutine);
        }

        private IEnumerator VariantTweenOutSequence(Subtexture[] variants, OutlineImage[] images, int index, Entity variantImage) 
        {
            var subIndex = index;
            for (int i = 0; i < variants.Length; i = subIndex + 1)
            {
                OutlineImage image = images[i];
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackIn, 8, true);
                tween.OnUpdate = t => {
                    image.Scale = Vector2.One * (1f - t.Eased);
                };
                variantImage.Add(tween);
                yield return 1;
                subIndex = i;
            }
            yield return 10;
            variantImage.RemoveSelf();
        }
    }
}


namespace MonoMod
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchVersusStartSessionIntroSequence))]
    internal class PatchVersusStartSessionIntroSequence : Attribute {}
    internal static partial class MonoModRules 
    {
        public static void PatchVersusStartSessionIntroSequence(MethodDefinition method, CustomAttribute attrib) 
        {
            var moveNext = method.GetEnumeratorMoveNext();
            new ILContext(moveNext).Invoke(ctx => {
                var localsName = IsWindows ? "<>8__1" : "CS$<>8__localsa";
                var this8__1 = ctx.Method.DeclaringType.FindField(localsName);
                var this__4 = ctx.Method.DeclaringType.FindField("<>4__this");
                var CustomModeText = this__4.FieldType.Resolve().FindMethod("Monocle.OutlineText CustomModeText(System.Object)");
                var cursor = new ILCursor(ctx);

                cursor.GotoNext(instr => instr.MatchLdnull());
                cursor.GotoNext(instr => instr.MatchLdnull());

                // There is 4 ldnull in Linux/OSX, so we do this to correctly patch it
                if (!IsWindows) 
                {
                    cursor.GotoNext(instr => instr.MatchLdnull());
                    cursor.GotoNext(instr => instr.MatchLdnull());
                }

                // Let's replace null with 'this'
                cursor.Next.OpCode = OpCodes.Ldarg_0;
                cursor.GotoNext();
                cursor.Emit(OpCodes.Ldfld, this__4);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, this8__1);
                cursor.Emit(OpCodes.Call, CustomModeText);
            });
        }
    }
}