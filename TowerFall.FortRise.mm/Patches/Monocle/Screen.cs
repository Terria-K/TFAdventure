using System;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Monocle 
{
    public class patch_Screen : Screen
    {
        private Viewport viewport;
        private float scale;
        public patch_Screen(Engine engine, int width, int height, float scale) : base(engine, width, height, scale)
        {
        }

        [MonoModIgnore]
        [MonoModConstructor]
        [PatchScreenCtor]
        public extern void ctor(Engine engine, int width, int height, float scale);

        [MonoModIgnore]
        [PatchScreenResize]
        public extern void Resize(int width, int height, float scale);


        [MonoModReplace]
        private void SetWindowSize(int width, int height, bool init = false)
		{
			this.Graphics.IsFullScreen = false;
			this.Graphics.PreferredBackBufferWidth = width;
			this.Graphics.PreferredBackBufferHeight = height;
			if (!init)
			{
				this.Graphics.ApplyChanges();
			}
			this.viewport.Width = width;
			this.viewport.Height = this.ScaledHeight;
			this.viewport.X = 0;
			this.viewport.Y = (height - this.ScaledHeight) / 2;
			this.DrawRect.X = width / 2 - this.ScaledWidth / 2;
			this.UpdatePadRects();
			this.Matrix = Microsoft.Xna.Framework.Matrix.CreateScale(this.scale) * Microsoft.Xna.Framework.Matrix.CreateTranslation((float)this.DrawRect.X, 0f, 0f);
		}

        [MonoModIgnore]
        [ObsoletePatch("For compability sake, please use SetWindowSize(int, int, bool) with the third argument being `false`")]
        private extern void SetWindowSize(int width, int height);

        [MonoModIgnore]
        private extern void UpdatePadRects();
    }
}

namespace MonoMod 
{
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchScreenResize))]
    internal class PatchScreenResize : Attribute {}

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchScreenCtor))]
    internal class PatchScreenCtor : Attribute {}


    internal static partial class MonoModRules 
    {
        public static void PatchScreenResize(ILContext ctx, CustomAttribute attrib) 
        {
            var height = ctx.Method.DeclaringType.FindField("height");
            var cursor = new ILCursor(ctx);

            cursor.GotoNext(MoveType.After, instr => instr.MatchStfld("Monocle.Screen", "width"));
            if (cursor.TryGotoNext(instr => instr.MatchStfld("Monocle.Screen", "width"))) 
            {
                cursor.Next.Operand = height;
            }
        }

        public static void PatchScreenCtor(ILContext ctx, CustomAttribute attrib) 
        {
            var SetWindowSize = ctx.Method.DeclaringType.FindMethod("System.Void SetWindowSize(System.Int32,System.Int32,System.Boolean)");
            var cursor = new ILCursor(ctx);

            if (cursor.TryGotoNext(instr => instr.MatchCallOrCallvirt("Monocle.Screen", "System.Void SetWindowSize(System.Int32,System.Int32)"))) 
            {
                cursor.Next.Operand = SetWindowSize;
                cursor.Emit(OpCodes.Ldc_I4_1);
            }
        }
    }
}