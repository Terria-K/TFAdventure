using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace FortRise;

public class TextContainer : MenuItem
{
    private List<Item> items = new();
    public int Selection = -1;
    public float Height;
    public float CurrentPositionY;
    public int ToX;
    public bool WithFade;
    public Color FadeColor = Color.Black * 0.5f;
    public Action BackAction;

    public IReadOnlyList<Item> Items => items;

    public TextContainer(int x) : base(new Vector2(x - 320, 0))
    {
        ToX = x;
    }

    public Item Current 
    {
        get 
        {
            if (items.Count <= 0 || Selection < 0) 
            {
                return null;
            }
            return items[Selection];
        }
        set => Selection = items.IndexOf(value);
    }

    public float ScrollTargetY 
    {
        get 
        {
            float num = (float)(240 - 50) - this.Height; 
            float num2 = 50 + Height;
            return patch_Calc.Clamp((float)(240 / 2) + this.Height - this.GetYOffsetOf(this.Current), num, num2);
        }
    }
    public float GetYOffsetOf(TextContainer.Item item)
    {
        if (item == null)
            return 0f;
        
        float num = 0f;
        foreach (var item2 in this.items)
        {
            if (item.Visible)
            {
                num += item2.LineOffset;
            }
            if (item2 == item)
                break;
            
        }
        return num - item.LineOffset * 0.5f;
    }

    public TextContainer Add(Item item) 
    {
        items.Add(item);
        item.Index = items.Count;
        item.Container = this;
        Add(item.ValueWiggler = Wiggler.Create(25, 3f));
        Add(item.SelectedWiggler = Wiggler.Create(25, 3f));
        if (Selection == -1) 
        {
            MoveItem(1);
        }
        item.Added(this);
        Height = 0;
        foreach (TextContainer.Item item3 in this.items)
        {
            if (item3.Visible)
            {
                Height += item3.LineOffset;
            }
        }
        return this;
    }

    public void MoveItem(int index) 
    {
        if (Current != null)
            Current.Selected = false;
        
        int hoverables = 0;
        foreach (var item in items) 
        {
            if (item.IsHoverable)
                hoverables++;
        }

        do 
        {
            Selection += index;
            if (hoverables > 2) 
            {
                if (Selection < 0) 
                {
                    Selection = items.Count - 1;
                }
                else if (Selection >= items.Count) 
                {
                    Selection = 0;
                }
            }
            else if (Selection < 0 || Selection > items.Count - 1)
            {
                break;
            }
        }
        while (!Current.IsHoverable);

        Selection = Calc.Clamp(this.Selection, 0, this.items.Count - 1);

        CurrentPositionY = Position.Y;
        Current.Selected = true;
        Current.SelectedWiggler.Start();

    }

    public override void Update()
    {
        base.Update();
        if (Selected) 
        {
            if (MenuInput.Up) 
            {
                Sounds.ui_move1.Play();
                MoveItem(-1);
            }
            else if (MenuInput.Down) 
            {
                Sounds.ui_move1.Play();
                MoveItem(1);
            }
            else if (MenuInput.Back) 
            {
                BackAction?.Invoke();
            }
            if (Current != null) 
            {
                if (MenuInput.Left) 
                {
                    Current.LeftPressed();
                }
                else if (MenuInput.Right) 
                {
                    Current.RightPressed();
                }
                if (MenuInput.Confirm) 
                {
                    Current.OnConfirm?.Invoke();
                    Current.ConfirmPressed();
                }
            }
        }

        foreach (var item in items) 
        {
            item.OnUpdate?.Invoke();
            item.Update();
        }

        // if (Height > (240 - 50)) 
        // {
        Position.Y = Position.Y + (ScrollTargetY - Position.Y) * (1f - (float)Math.Pow(0.009999999776482582, (double)Engine.TimeMult));
        //     return;
        // }
        // Position.Y = 160;
    }

    public override void Render()
    {
        if (WithFade)
            Draw.Rect(0f, 0f, 320f, 240f, FadeColor);
        base.Render();
        
        var position = Position - new Vector2(0, Height);

        foreach (var item in items) 
        {
            if (item.Visible) 
            {
                item.Render(position + new Vector2(0, item.SelectedWiggler.Value * 4f), Selected && Current == item);
                position.Y += item.LineOffset;
            }
        }
    }

    public override void TweenIn()
    {
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackInOut, 30, true);
        var posX = Position.X;
        tween.OnUpdate = t => 
        {
            Position.X = MathHelper.Lerp(posX, ToX, t.Eased);
        };
        Add(tween);
    }

    public override void TweenOut()
    {
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackInOut, 20, true);
        var posX = Position.X;
        var posXEnd = posX - 320;
        tween.OnUpdate = t => 
        {
            Position.X = MathHelper.Lerp(posX, posXEnd, t.Eased);
        };
        Add(tween);
    }

    protected override void OnSelect()
    {
    }

    protected override void OnDeselect()
    {
    }

    protected override void OnConfirm()
    {
    }

    public abstract class Item 
    {
        public Action OnConfirm;
        public Action OnExit;
        public Action OnPressed;
        public Action OnAltPressed;
        public Action OnUpdate;
        public Wiggler ValueWiggler;
        public Wiggler SelectedWiggler;
        public bool Selected;
        public int Index;
        public virtual int LineOffset => 12;
        public virtual bool IsHoverable => true;

        public bool Visible = true;

        public TextContainer Container;

        public Item Confirm(Action onConfirm) 
        {
            OnConfirm = onConfirm;
            return this;
        }

        public Item Exit(Action onExit) 
        {
            OnExit = onExit;
            return this;
        }

        public Item Pressed(Action onPressed) 
        {
            OnPressed = onPressed;
            return this;
        }

        public Item AltPressed(Action onAltPressed) 
        {
            OnAltPressed = onAltPressed;
            return this;
        }

        public virtual void Added(TextContainer container) {}
        public virtual void Update() {}
        public virtual void Render(Vector2 position, bool selected) {}

        public virtual void LeftPressed() {}
        public virtual void RightPressed() {}
        public virtual void ConfirmPressed() {}
    }

    public abstract class Option<T> : Item 
    {
        public string Text;
        public Action<T> OnValueChanged;
        public T Value;
        private SineWave sine;

        public Color SelectedColor = OptionsButton.SelectedColor;
        public Color NotSelectedColor = OptionsButton.NotSelectedColor;
        public bool Interactable = true;

        public override bool IsHoverable => true;

        protected Image LeftArrow;
        protected Image RightArrow;


        public int WiggleDir;

        public abstract bool CanLeft { get; }
        public abstract bool CanRight { get; }

        public Option(string text) 
        {
            Text = text.ToUpperInvariant();
            RightArrow = new Image(TFGame.MenuAtlas["portraits/arrow"]);
            RightArrow.CenterOrigin();
            RightArrow.Visible = false;

            LeftArrow = new Image(TFGame.MenuAtlas["portraits/arrow"]);
            LeftArrow.CenterOrigin();
            LeftArrow.FlipX = true;
            LeftArrow.Visible = false;
        }

        public override void Added(TextContainer container)
        {
            container.Add(sine = new SineWave(120));
        }

        public Option<T> Change(Action<T> onChanged) 
        {
            OnValueChanged = onChanged;
            return this;
        }

        public override sealed void LeftPressed()
        {
            if (!CanLeft || !Interactable)
                return;
            
            OptionLeft();
        }

        public override sealed void RightPressed()
        {
            if (!CanRight || !Interactable)
                return;
            
            OptionRight();
        }

        public abstract void OptionLeft();
        public abstract void OptionRight();

        public override void ConfirmPressed()
        {

        }

        public override void Render(Vector2 position, bool selected)
        {
            Vector2 vector = new Vector2(30f + 2f * this.ValueWiggler.Value * (float)this.WiggleDir, 0f);
            Color color = (base.Selected ? SelectedColor : NotSelectedColor);
            Draw.OutlineTextJustify(TFGame.Font, Text, position + new Vector2(-5f, 0f) + new Vector2(5f * this.SelectedWiggler.Value, 0f), color, Color.Black, new Vector2(1f, 0.5f), 1f);

            if (Selected) 
            {
                LeftArrow.Position = position + vector + Vector2.UnitX * (-20f + -3f * sine.Value + ((WiggleDir == -1) ? ValueWiggler.Value * -2f : 0f));
                RightArrow.Position = position + vector + Vector2.UnitX * (20f + 3f * sine.Value + ((WiggleDir == 1) ? ValueWiggler.Value * 2f : 0f));
                LeftArrow.Render();
                RightArrow.Render();
            }

            RenderValue(ref position, ref vector, ref color);
        }

        public virtual void RenderValue(ref Vector2 position, ref Vector2 vector, ref Color color) {}
    }

    public class Number : Option<int>
    {
        public int Min;
        public int Max;
        public Number(string text, int start, int min = 0, int max = 10) : base(text)
        {
            Value = start;
            Min = min;
            Max = max;
        }

        public override bool CanLeft => Value > Min;

        public override bool CanRight => Value < Max;

        public override void OptionLeft()
        {
            Value--;
            OnValueChanged?.Invoke(Value);
            Sounds.ui_move1.Play();
        }

        public override void OptionRight()
        {
            Value++;
            OnValueChanged?.Invoke(Value);
            Sounds.ui_move1.Play();
        }

        public override void RenderValue(ref Vector2 position, ref Vector2 vector, ref Color color)
        {
            Draw.OutlineTextJustify(TFGame.Font, Value.ToString(), position + vector, color, Color.Black, Vector2.One * 0.5f, 1f);
        }
    }

    public class Toggleable : Option<bool>
    {
        public override bool CanLeft => Value; 
        public override bool CanRight => !Value;

        public Toggleable(string text, bool start) : base(text)
        {
            Value = start;
        }

        public override void ConfirmPressed()
        {
            if (!Interactable)
                return;
            Value = !Value;
            WiggleDir = Value ? 1 : -1;
            OnValueChanged?.Invoke(Value);
            ValueWiggler.Start();
            if (Value)
                Sounds.ui_subclickOn.Play();
            else
                Sounds.ui_subclickOff.Play();
        }

        public override void RenderValue(ref Vector2 position, ref Vector2 vector, ref Color color)
        {
            if (Value)
            {
                Draw.OutlineTextureCentered(TFGame.MenuAtlas["optionOn"], position + vector, color);
                LeftArrow.Color = Color.White * 1f;
                RightArrow.Color = Color.White * 0.3f;
            }
            else 
            {
                Draw.OutlineTextureCentered(TFGame.MenuAtlas["optionOff"], position + vector, color);
                RightArrow.Color = Color.White * 1f;
                LeftArrow.Color = Color.White * 0.3f;
            }
        }

        public override void OptionLeft()
        {
            WiggleDir = -1;
            Value = false;
            OnValueChanged?.Invoke(false);
            ValueWiggler.Start();
            Sounds.ui_subclickOff.Play();
        }

        public override void OptionRight()
        {
            WiggleDir = 1;
            Value = true;
            OnValueChanged?.Invoke(true);
            ValueWiggler.Start();
            Sounds.ui_subclickOn.Play();
        }
    }

    public class SelectionOption : Option<(string, int)>
    {
        public string[] Options;
        public SelectionOption(string text, string[] options) : base(text)
        {
            Options = options.Select(x => x.ToUpperInvariant()).ToArray();
        }

        public SelectionOption(string text, string[] options, int start) : base(text)
        {
            Options = options.Select(x => x.ToUpperInvariant()).ToArray();
            Value.Item2 = start;
            Value.Item1 = Options[Value.Item2];
        }

        public override bool CanLeft => Value.Item2 > 0;

        public override bool CanRight => Value.Item2 < Options.Length - 1;

        public override void OptionLeft()
        {
            WiggleDir = -1;
            Value.Item2--;
            Value.Item1 = Options[Value.Item2]; 
            OnValueChanged?.Invoke(Value);
            ValueWiggler.Start();
            Sounds.ui_subclickOff.Play();
        }

        public override void OptionRight()
        {
            WiggleDir = -1;
            Value.Item2++;
            Value.Item1 = Options[Value.Item2]; 
            OnValueChanged?.Invoke(Value);
            ValueWiggler.Start();
            Sounds.ui_subclickOff.Play();
        }

        public override void RenderValue(ref Vector2 position, ref Vector2 vector, ref Color color)
        {
            Draw.OutlineTextJustify(TFGame.Font, Options[Value.Item2], position + vector, color, Color.Black, Vector2.One * 0.5f, 1f);
        }
    }

    public class HeaderText : Item 
    {
        public string Text;
        public float Scale = 2f;
        public override bool IsHoverable => false;

        public override int LineOffset => 12 * (int)Scale;

        public HeaderText(string text) 
        {
            Text = text.ToUpperInvariant();
        }

        public override void Render(Vector2 position, bool selected)
        {
            Draw.OutlineTextCentered(TFGame.Font, Text, position + new Vector2(-5f, 0f) + new Vector2(5f * SelectedWiggler.Value, 0f), Color.White, Color.Black, Scale);
        }
    }

    public class ImageHeader : Item 
    {
        public Subtexture Texture;
        public Vector2 Scale = new Vector2(1);
        public override bool IsHoverable => false;
        public override int LineOffset => Texture.Height * (int)Scale.Y;

        public ImageHeader(Subtexture texture) 
        {
            Texture = texture;
        }

        public override void Render(Vector2 position, bool selected)
        {
            Draw.OutlineTextureCentered(Texture, position + new Vector2(-5f, 0f) + new Vector2(5f, 0f), Color.White, Scale);
        }
    }

    public class ButtonText : Item 
    {
        public string Text;
        public Action OnPressed;

        public ButtonText(string text) 
        {
            Text = text.ToUpperInvariant();
        }

        public void Pressed(Action onPressed) 
        {
            OnPressed = onPressed;
        }

        public override void ConfirmPressed()
        {
            OnPressed?.Invoke();
            Sounds.ui_click.Play();
        }

        public override void Render(Vector2 position, bool selected)
        {
            Color color = (base.Selected ? OptionsButton.SelectedColor : OptionsButton.NotSelectedColor);
            Draw.OutlineTextCentered(TFGame.Font, Text, position + new Vector2(-5f, 0f) + new Vector2(5f * SelectedWiggler.Value, 0f), color, Color.Black, 1f);
        }
    }
}