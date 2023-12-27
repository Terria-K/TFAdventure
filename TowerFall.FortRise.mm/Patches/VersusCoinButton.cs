using Microsoft.Xna.Framework;
using MonoMod;
using FortRise;
using Monocle;

namespace TowerFall;

public class patch_VersusCoinButton : VersusCoinButton
{
    private Sprite<int> coinSprite;
    private Sprite<int> customSprite;
    private Sprite<int> skullSprite;
    private static string[] LengthNames;

    private bool IsCustomizedGoal => patch_MainMenu.VersusMatchSettings.MatchLength == patch_MatchSettings.patch_MatchLengths.Custom;
    public patch_VersusCoinButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom)
    {
    }

    public extern void orig_ctor(Vector2 position, Vector2 tweenFrom);

    [MonoModConstructor]
    public void ctor(Vector2 position, Vector2 tweenFrom)
    {
        orig_ctor(position, tweenFrom);
        patch_VersusModeButton.ModeSwitch += Switch;
    }

    [MonoModConstructor]
    public static void cctor() 
    {
        LengthNames = new string[5] {"INSTANT MATCH", "QUICK MATCH", "STANDARD MATCH", "EPIC MATCH", "CUSTOM MATCH"};
    }

    public override void Removed()
    {
        base.Removed();
        patch_VersusModeButton.ModeSwitch -= Switch;
    }

    private void Switch() 
    {
        if (customSprite != null && customSprite.Scene != null) 
        {
            customSprite.RemoveSelf();
            customSprite = null;
        }
        if (patch_MainMenu.VersusMatchSettings.IsCustom) 
        {
            var gameMode = patch_MainMenu.VersusMatchSettings.CurrentCustomGameMode;
            if (gameMode != null) 
            {
                customSprite = gameMode.coinSprite;
                Add(customSprite);
            }
        }
    }


    [MonoModLinkTo("TowerFall.BorderButton", "System.Void Update()")]
    [MonoModIgnore]
    public void base_Update() 
    {
        base.Update();
    }

    [MonoModLinkTo("TowerFall.BorderButton", "System.Void OnConfirm()")]
    [MonoModIgnore]
    public void base_OnConfirm() 
    {
        base.OnConfirm();
    }

    [MonoModLinkTo("TowerFall.BorderButton", "System.Void Render()")]
    [MonoModIgnore]
    public void base_Render() 
    {
        base.Render();
    }

    [MonoModReplace]
    public override void Update()
    {
        base_Update();
        this.Visible = MainMenu.VersusMatchSettings.CanPlayThisMode;
        if (base.Selected)
        {
            if (MenuInput.Confirm) 
            {
                Sounds.ui_click.Play(160f, 1f);
                patch_MainMenu.VersusMatchSettings.MatchLength = patch_MatchSettings.patch_MatchLengths.Custom;
                base_OnConfirm();
                this.UpdateSides();
                var customMatch = new UIVersusCustomMatch(patch_MatchSettings.CustomGoal);
                customMatch.OnBack = () => {
                    base.Selected = true;
                    patch_MatchSettings.CustomGoal = customMatch.Count;
                };
                base.Selected = false;
                Scene.Add(customMatch);
                return;
            }
            if (MenuInput.Right && MainMenu.VersusMatchSettings.MatchLength < MatchSettings.MatchLengths.Epic)
            {
                Sounds.ui_move2.Play(160f, 1f);
                MainMenu.VersusMatchSettings.MatchLength++;
                base_OnConfirm();
                this.UpdateSides();
                return;
            }
            if (MenuInput.Left && MainMenu.VersusMatchSettings.MatchLength > MatchSettings.MatchLengths.Instant)
            {
                Sounds.ui_move2.Play(160f, 1f);
                MainMenu.VersusMatchSettings.MatchLength--;
                base_OnConfirm();
                this.UpdateSides();
            }
        }
    }


    [MonoModIgnore]
    private extern void UpdateSides();

    public override void Render()
    {
        Sprite<int> sprite;
        if (MainMenu.VersusMatchSettings.Mode == Modes.HeadHunters || MainMenu.VersusMatchSettings.Mode == Modes.Warlord)
        {
            sprite = skullSprite;
            skullSprite.Visible = true;
            coinSprite.Visible = false;
            if (customSprite != null)
                customSprite.Visible = false;
        }
        else if (!patch_MainMenu.VersusMatchSettings.IsCustom || customSprite == null)
        {
            sprite = coinSprite;
            skullSprite.Visible = false;
            coinSprite.Visible = true;
            if (customSprite != null)
                customSprite.Visible = false;
        }
        else 
        {
            skullSprite.Visible = false;
            coinSprite.Visible = false;
            customSprite.Visible = true;
            sprite = customSprite;
        }
        
        string text = "PLAY TO: ";
        string text2 = " x " + MainMenu.VersusMatchSettings.GoalScore.ToString();
        float x = TFGame.Font.MeasureString(text).X;
        float x2 = TFGame.Font.MeasureString(text2).X;
        float num = x + sprite.Width + x2;
        Draw.OutlineTextCentered(TFGame.Font, LengthNames[(int)MainMenu.VersusMatchSettings.MatchLength], Position + new Vector2(0f, -6f), DrawColor, 2f);
        Draw.OutlineTextCentered(TFGame.Font, text, Position + new Vector2(-num / 2f + x / 2f, 6f), DrawColor, 1f);
        Draw.OutlineTextCentered(TFGame.Font, text2, Position + new Vector2(num / 2f - x2 / 2f, 6f), DrawColor, 1f);
        sprite.X = -num / 2f + x + sprite.Width / 2f;
        sprite.Y = 6f;
        sprite.DrawOutline(1);
        base_Render();
    }
}