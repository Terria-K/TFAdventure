using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace FortRise;

public class BowButton : TextContainer.ButtonText
{
    private SineWave sine;
    public override int LineOffset => 24;
    public float TextWidth;
    public BowButton(string text) : base(text)
    {
        TextWidth = TFGame.Font.MeasureString(text).X * 2;
    }

    public override void Added(TextContainer container)
    {
        base.Added(container);
        container.Add(sine = new SineWave(60));
    }

    public override void Render(Vector2 position, bool selected)
    {
        Color color = (base.Selected ? OptionsButton.SelectedColor : OptionsButton.NotSelectedColor);
        Draw.OutlineTextCentered(TFGame.Font, Text, position, color, 2f);
        if (selected) 
        {
            Draw.TextureCentered(
                TFGame.MenuAtlas["bowSelection"], position + new Vector2(-TextWidth - 3f * this.sine.Value, 0f), Color.White);
			Draw.TextureCentered(
                TFGame.MenuAtlas["bowSelection"], position + new Vector2(TextWidth + 3f * this.sine.Value, 0f), Color.White, 
                new Vector2(-1f, 1f), 0f);
        }
    }
}