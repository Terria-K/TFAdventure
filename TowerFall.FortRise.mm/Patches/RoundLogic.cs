using System;
using FortRise;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace TowerFall 
{
    public class patch_RoundLogic : RoundLogic
    {
        public patch_RoundLogic(Session session, bool canHaveMiasma) : base(session, canHaveMiasma)
        {
        }

        public extern static RoundLogic orig_GetRoundLogic(patch_Session session);

        public static RoundLogic GetRoundLogic(patch_Session session) 
        {
            if (session.MatchSettings.IsCustom && GameModeRegistry.TryGetGameMode(session.MatchSettings.CurrentModeName, out var mode)) 
            {
                return mode.CreateRoundLogic(session);
            }
            return orig_GetRoundLogic(session);
        }

        [MonoModIgnore]
        [PatchRoundLogicOnLevelLoadFinish]
        public extern override void OnLevelLoadFinish();
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchRoundLogicOnLevelLoadFinish))]
    public class PatchRoundLogicOnLevelLoadFinish: Attribute {}

    internal static partial class MonoModRules 
    {
        public static void PatchRoundLogicOnLevelLoadFinish(ILContext ctx, CustomAttribute attrib) 
        {
            var OnLevelLoaded = ctx.Module.GetType("FortRise.RiseCore/Events").FindMethod("System.Void Invoke_OnLevelLoaded(TowerFall.RoundLogic)");
            var cursor = new ILCursor(ctx);
            ILLabel label = null;

            cursor.GotoNext(instr => instr.MatchBrtrue(out label));
            cursor.GotoNext(MoveType.After, instr => instr.MatchStsfld("TowerFall.SessionStats", "RoundsPlayed"));

            cursor.MarkLabel(label);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, OnLevelLoaded);
        }
    }
}