using System;
using System.Reflection;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;
using TowerFall;

namespace Monocle;

public class patch_Engine : Engine
{
    private static FieldInfo fieldGameRunApplication = typeof(Game).GetField("RunApplication", BindingFlags.Instance | BindingFlags.NonPublic);
    private static MethodInfo methodGameRunLoop = typeof(Game).GetMethod("RunLoop", BindingFlags.Instance | BindingFlags.NonPublic);
    private static MethodInfo methodGameAfterLoop = typeof(Game).GetMethod("AfterLoop", BindingFlags.Instance | BindingFlags.NonPublic);
    public Commands Commands { [MonoModIgnore] get => null; [MonoModIgnore] private set => throw new System.Exception(value.ToString()); }
    
    public patch_Engine(int width, int height, float scale, string windowTitle) : base(width, height, scale, windowTitle)
    {
    }

    [MonoModLinkTo("Microsoft.Xna.Framework.Game", "System.Void Initialize()")]
    protected void base_Initialize() 
    {
        base.Initialize();
    }

    [MonoModReplace]
    protected override void Initialize() 
    {
        base_Initialize();
        this.Graphics.DeviceReset += this.OnGraphicsReset;
        this.Graphics.DeviceCreated += this.OnGraphicsCreated;
        patch_MInput.Initialize();
        this.Commands = new Commands();
    }

    public void InternalRun() 
    {
        var end = false;
        while (true) 
        {
            try 
            {
                if (!end) 
                {
                    base.Run();
                    break;
                }
                methodGameRunLoop.Invoke(this, Array.Empty<object>());
                EndRun();
                methodGameAfterLoop.Invoke(this, Array.Empty<object>());
            }
            catch (Exception e) 
            {
                if (Instance.Scene == null || RiseCore.NoErrorScene) 
                {
                    TFGame.Log(e, false);
                    TFGame.OpenLog();
                    break;
                }
                if ((bool)fieldGameRunApplication.GetValue(this)) 
                {
                    ErrorSceneBuilder.HandleErrorScene(e);
                    end = true;
                }
                Logger.Error(e.ToString());
                continue;
            }
            break;
        }
    }
}

internal static class ErrorSceneBuilder 
{
    public static void HandleErrorScene(Exception ex) 
    {
        var errorScene = new ErrorScene(ex);
        errorScene.UpdateEntityLists();
        Engine.Instance.Scene = errorScene;
        errorScene.Begin();
    }
}

internal class ErrorScene : Scene 
{
    private Exception ex;
    private string[] lines;
    private bool isOpened;
    public ErrorScene(Exception ex) 
    {
        TFGame.Log(ex, false);
        this.ex = ex;
        lines = ex.ToString().ToUpperInvariant().Split('\n');
        SetLayer(-3, new Layer());
        SetLayer(-2, new Layer());
        Engine.Instance.Screen.ClearColor = Color.Black;
    }

    public override void Begin()
    {
        base.Begin();

        Add(new TowerFall.MenuBackground());
    }

    public override void Update()
    {
        if (MenuInput.Confirm && !isOpened) 
        {
            isOpened = true;
            var uiModal = new UIModal(-2);
            uiModal.SetTitle("CONTINUE");
            uiModal.AddItem("Continue", () => {
                Engine.Instance.Scene = new MainMenu(MainMenu.MenuState.PressStart);
            });
            uiModal.AddItem("Open Log", () => {
                isOpened = false;
                TFGame.OpenLog();
            });
            uiModal.AddItem("Reload", () => {
                TFGame.Load();
                Engine.Instance.Scene = new MainMenu(MainMenu.MenuState.Loading);
            });
            uiModal.AddItem("Quit", () => {
                Engine.Instance.Exit();
            });
            uiModal.OnBack = () => {
                uiModal.RemoveSelf();
                isOpened = false;
            };
            uiModal.AutoClose = true;
            Add(uiModal);
        }
        base.Update();
    }

    public override void Render()
    {
        base.Render();
        if (isOpened)   
            return;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

        const string UNEXPECTED = "UNEXPECTED ERROR OCCURED";
        Draw.OutlineTextCentered(TFGame.Font, UNEXPECTED, new Vector2(320/2, 20));

        const string REPORT = "PLEASE REPORT THIS IN THE TOWERFALL DISCORD SERVER";
        Draw.OutlineTextCentered(TFGame.Font, REPORT, new Vector2(320/2, 40));

        Draw.SpriteBatch.End();

        Draw.SpriteBatch.Begin();

        var pos = 0;
        for (int i = 0; i < lines.Length; i++) 
        {
            var line = lines[i];
            Draw.OutlineTextJustify(TFGame.Font, line, new Vector2(320, 60 + pos), Color.White, Color.Black, new Vector2(1, 0f), 1f);
            pos += 12;
        }
        
        Draw.SpriteBatch.End();
    }
}