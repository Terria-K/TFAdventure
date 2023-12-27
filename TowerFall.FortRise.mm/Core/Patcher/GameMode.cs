using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace FortRise;


public abstract class CustomGameMode 
{
    public string ID { get; internal set; }
    internal TowerFall.Modes GameModeInternal;

    public string Name 
    { 
        get => name ?? StringUtils.SeparateCases(GetType().Name); 
        set => name = value; 
    }
    private string name;
    public Color NameColor { get => nameColor; set => nameColor = value; }
    private Color nameColor = Color.White;
    public Subtexture Icon 
    { 
        get => icon; 
        set => icon = value; 
    }
    private Subtexture icon = TFGame.MenuAtlas["gameModes/lastManStanding"];
    public GameModeType ModeType { get => type; set => type = value; }
    private GameModeType type;

    public bool TeamMode { get => teamMode; set => teamMode = value; }
    private bool teamMode;

    public int CoinOffset { get => coinOffset; set => coinOffset = value; }
    private int coinOffset = 10;

    public SFX EarnedCoinSound { get => earnedCoinSound; set => earnedCoinSound = value; }
    private SFX earnedCoinSound = Sounds.sfx_multiCoinEarned;

    public SFX LoseCoinSound { get => loseCoinSound; set => loseCoinSound = value; }
    private SFX loseCoinSound = Sounds.sfx_multiSkullNegative;
    internal Sprite<int> coinSprite;

    internal void InitializeSoundsInternal() 
    {
        earnedCoinSound = Sounds.sfx_multiCoinEarned;
        loseCoinSound = Sounds.sfx_multiSkullNegative;
        InitializeSounds();
    }

    public CustomGameMode() 
    {
        ID = GetType().Name;
    }

    /// <summary>
    /// Initialize the fields you need to initialize such as Name, Icon and ModeType. 
    /// <br/>
    /// For initializing SFX, <see cref="FortRise.CustomGameMode.InitializeSounds"/>.
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// Initialize your SFX here such as <see cref="FortRise.CustomGameMode.EarnedCoinSound"/> 
    /// and <see cref="FortRise.CustomGameMode.LoseCoinSound"/>.
    /// </summary>
    public abstract void InitializeSounds();

    /// <summary>
    /// Initialize your game mode state here
    /// <param name="session">A game session that you can access if you need to modify some parts of it</param>
    /// </summary>
    public virtual void StartGame(Session session) {}

    /// <summary>
    /// Instantiate your round logic here to create your own logic for your gamemode.
    /// You can either use the vanilla round logic or derived atleast one of it with its different behaviour.
    /// <param name="session">A game session needed to be passed for round logic</param>
    /// </summary>
    public abstract RoundLogic CreateRoundLogic(Session session);
    
    /// <summary>
    /// Instatiate your own level system here to create a custom level system.
    /// <param name="levelData">A data needed to be passed for level systems</param>
    /// </summary>
    public virtual LevelSystem GetLevelSystem(LevelData levelData) 
    {
        if (ModeType == GameModeType.Versus) 
        {
            return new VersusLevelSystem(levelData as VersusTowerData);
        }
        return new QuestLevelSystem(levelData as QuestLevelData);
    }

    /// <summary>
    /// A sprite to used for points system. Default is the coin (Last Man Standing or TDM).
    /// <br/>
    /// To change it to skull, override this function and return UseSkullSprite()
    /// </summary>
    public virtual Sprite<int> CoinSprite() 
    {
        return VersusCoinButton.GetCoinSprite();
    }

    /// <summary>
    /// A ready to use skull sprite with offset adjusted as well.
    /// </summary>
    public Sprite<int> UseSkullSprite() 
    {
        CoinOffset = 12;
        return DeathSkull.GetSprite();
    }
}


public enum GameModeType { Versus, CoOp }