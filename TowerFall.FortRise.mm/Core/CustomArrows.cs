using System;
using Monocle;
using FortRise;
using TowerFall;
using Microsoft.Xna.Framework;

namespace FortRise 
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CustomArrowsAttribute : Attribute 
    {
        public string Name;
        public string GraphicPickupInitializer;
        public float Chance = 1f;

        public CustomArrowsAttribute(string name, string graphicPickupFn = null) 
        {
            Name = name;
            GraphicPickupInitializer = graphicPickupFn;
        }

        public CustomArrowsAttribute(string name, float chance, string graphicPickupFn = null) 
        {
            Name = name;
            GraphicPickupInitializer = graphicPickupFn;
            Chance = chance;
        }
    }

    internal abstract class CustomArrowPickup : Pickup
    {
        public ArrowTypes ArrowType { get => arrowType; set => arrowType = value; }
		public GraphicsComponent Graphic { get => graphic; set => graphic = value; }
        public SFX PickupSound { get => pickupSound; set => pickupSound = value; }

        private ArrowTypes arrowType;
        private GraphicsComponent graphic;
        private SFX pickupSound;


        public CustomArrowPickup(Vector2 position, Vector2 targetPosition) : base(position, targetPosition) 
        {
            Tag(GameTags.PlayerCollectible);
        }

        public abstract void Initialize();

        public virtual void Defaults() 
        {
            arrowType = ArrowTypes.Normal;
            Initialize();

            Collider ??= new Hitbox(16f, 16f, -8f, -8f);

            graphic ??= new Monocle.Image(TFGame.Atlas["pickups/arrowPickup"], null);
            graphic.CenterOrigin();
            pickupSound ??= Sounds.pu_plus2Arrows;
            Add(Graphic);
        }

        public virtual void PlaySound() 
        {
            pickupSound.Play(base.X, 1f);
        }

        public override void DoPlayerCollect(Player player)
        {
            player.CollectArrows(new ArrowTypes[] { arrowType, arrowType });
            PlaySound();
        }

        public override void OnPlayerCollide(Player player)
        {
			if (player.CollectArrows(new ArrowTypes[] { arrowType, arrowType }))
			{
				DoCollectStats(player.PlayerIndex);
				RemoveSelf();
				Color first = Arrow.Colors[(int)arrowType];
				Color second = Arrow.ColorsB[(int)arrowType];
				Level.Add(new FloatText(Position + new Vector2(0f, -10f), Arrow.Names[(int)arrowType], first, second, 1f));
				Level.Add(new FloatText(Position + new Vector2(0f, -3f), "ARROWS", first, second, 1f));
				Level.Add(Cache.Create<LightFade>().Init(this));
				PlaySound();
			}
        }

        public override void Update()
        {
            base.Update();
            graphic.Position = DrawOffset;
        }

        public override void Render()
        {
            DrawGlow();
            graphic.DrawOutline(1);
            base.Render();
        }

        public override void TweenUpdate(float t)
        {
            base.TweenUpdate(t);
            graphic.Scale = Vector2.One * t;
        }
    }
}


namespace TowerFall 
{
    public class ArrowObject
    {
        public ArrowTypes Types;
        public PickupObject PickupType;
        public ArrowInfoLoader InfoLoader;
    }


    public struct ArrowInfo 
    {
        public string Color = "F7EAC3";
        public string ColorB = "FFFFFF";
        public string Name = "";
        public SFX PickupSound;
        internal Image Simple;
        internal Subtexture HUD;
        internal Sprite<int> Animated;
        internal byte Type;

        [Obsolete("Use ArrowInfo.Create or ArrowInfo.CreateAnimated instead")]
        public ArrowInfo()
        {
            Simple = null;
            Animated = null;
            HUD = null;
            Type = 0;
            PickupSound = null;
        }
    #pragma warning disable CS0618

        public static ArrowInfo Create(Image simple) 
        {
            return new ArrowInfo { Simple = simple, Type = 0 };
        }

        public static ArrowInfo Create(Image simple, SFX pickupSound) 
        {
            return new ArrowInfo { Simple = simple, Type = 0, PickupSound = pickupSound };
        }

        public static ArrowInfo CreateAnimated(Sprite<int> animated) 
        {
            return new ArrowInfo { Animated = animated, Type = 1 };
        }

        public static ArrowInfo CreateAnimated(Sprite<int> animated, SFX pickupSound) 
        {
            return new ArrowInfo { Animated = animated, Type = 1, PickupSound = pickupSound};
        }

        public static ArrowInfo Create(Image simple, Subtexture hud) 
        {
            return new ArrowInfo { Simple = simple, Type = 0, HUD = hud };
        }

        public static ArrowInfo CreateAnimated(Sprite<int> animated, Subtexture hud) 
        {
            return new ArrowInfo { Animated = animated, Type = 1, HUD = hud };
        }

        public static ArrowInfo Create(Image simple, Subtexture hud, SFX pickupSound) 
        {
            return new ArrowInfo { Simple = simple, Type = 0, HUD = hud, PickupSound = pickupSound };
        }

        public static ArrowInfo CreateAnimated(Sprite<int> animated, Subtexture hud, SFX pickupSound) 
        {
            return new ArrowInfo { Animated = animated, Type = 1, HUD = hud, PickupSound = pickupSound};
        }
    }
}
