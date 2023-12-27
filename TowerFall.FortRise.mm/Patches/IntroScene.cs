using System;
using System.Collections;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{
    public class patch_IntroScene : IntroScene 
    {
        [MonoModReplace]
        private Subtexture GetLetterSub(int letterIndex) 
        {
            if (patch_TFGame.FortRiseMenuAtlas.Contains("mmg/" + letterIndex))
                return patch_TFGame.FortRiseMenuAtlas["mmg/" + letterIndex];
            return TFGame.MenuAtlas["mmg/" + letterIndex];
        }

        [MonoModIgnore]
        [PatchIntroSceneSequence]
        private extern IEnumerator Sequence();
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchIntroSceneSequence))]
    internal class PatchIntroSceneSequence : Attribute {}

    internal static partial class MonoModRules 
    {

        public static void PatchIntroSceneSequence(MethodDefinition method, CustomAttribute attrib) 
        {
            var complete = method.GetEnumeratorMoveNext();

            new ILContext(complete).Invoke(ctx => {
                var ORIGINAL_LOAD_PREFIX = ctx.Module.GetType("Monocle.Audio")
                    .FindField("ORIGINAL_LOAD_PREFIX");
                var cursor = new ILCursor(ctx);

                if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdsfld("Monocle.Audio", "LOAD_PREFIX"))) 
                {
                    cursor.Next.Operand = ORIGINAL_LOAD_PREFIX;
                }
            });
        }
    }
}