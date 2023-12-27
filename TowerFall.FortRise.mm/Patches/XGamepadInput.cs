using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using FortRise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod;

namespace TowerFall;

public class patch_XGamepadInput : XGamepadInput
{
    private Subtexture icon;
    private Subtexture iconMove;
    private Subtexture iconJump;
    private Subtexture iconShoot;
    private Subtexture iconAltShoot;
    private Subtexture iconDodge;
    private Subtexture iconStart;
    private Subtexture iconSkipReplay;
    private Subtexture iconConfirm;
    private Subtexture iconBack;
    private Subtexture iconAlt;
    private Subtexture iconAlt2;


    private string id;
    private string name;
    private static Dictionary<string, XmlElement> ButtonIconMap;
    private static Dictionary<string, XmlElement> ControllerInfoMap;
    public patch_XGamepadInput(int xGamepadID) : base(xGamepadID)
    {
    }

    [MonoModReplace]
    [MonoModConstructor]
    public void ctor(int xGamepadID) 
    {
        XGamepadIndex = xGamepadID;
        XGamepad = MInput.XGamepads[xGamepadID];
        id = GamePad.GetGUIDEXT((PlayerIndex)xGamepadID);
        XmlElement xmlElement = ControllerInfoMap.TryGetValue(id, out var val) ? val : ControllerInfoMap["null"];
        name = xmlElement["name"].InnerText.ToUpper(CultureInfo.InvariantCulture);
        var text = xmlElement.ChildText("buttonset", "default");

        xmlElement = ButtonIconMap.ContainsKey(text) ? ButtonIconMap[text] : null;
        icon = ReadPlayerIcon(xmlElement, xGamepadID);
        iconMove = ReadButtonIcon(xmlElement, "move");
        iconJump = ReadButtonIcon(xmlElement, "jump");
        iconShoot = ReadButtonIcon(xmlElement, "shoot");
        iconAltShoot = ReadButtonIcon(xmlElement, "altShoot");
        iconDodge = ReadButtonIcon(xmlElement, "dodge");
        iconStart = ReadButtonIcon(xmlElement, "start");
        iconConfirm = ReadButtonIcon(xmlElement, "confirm");
        iconBack = ReadButtonIcon(xmlElement, "back");
        iconAlt = ReadButtonIcon(xmlElement, "alt");
        iconAlt2 = ReadButtonIcon(xmlElement, "alt2");
        iconSkipReplay = ReadButtonIcon(xmlElement, "skip_replay");
    }

    [MonoModReplace]
    public static void Init() 
    {
        ButtonIconMap = new Dictionary<string, XmlElement>();
        ControllerInfoMap = new Dictionary<string, XmlElement>();
        var iconConfig = Path.Combine(TFGame.OSSaveDir, "GamepadIcon_Config.xml");
        var nameConfig = Path.Combine(TFGame.OSSaveDir, "GamepadName_Config.xml");
        if (!File.Exists(iconConfig)) 
        {
            using var sw = new StreamWriter(iconConfig, false);
            sw.WriteLine("<!-- GamepadIcon_Config.xml -->\n<buttonlists>\n\t<!-- Xbox 360 -->\n\t<buttonlist>\n\t\t<name>XB360</name>\n\t\t<player>xb360/player</player>\n\t\t<move>xb360/stick</move>\n\t\t<jump>xb360/a</jump>\n\t\t<shoot>xb360/x</shoot>\n\t\t<altShoot>xb360/b</altShoot>\n\t\t<dodge>xb360/rt</dodge>\n\t\t<start>xb360/start</start>\n\t\t<confirm>xb360/a</confirm>\n\t\t<back>xb360/b</back>\n\t\t<alt>xb360/rt</alt>\n\t\t<alt2>xb360/lt</alt2>\n\t\t<skip_replay>xb360/start</skip_replay>\n\t</buttonlist>\n\t<!-- DualShock 4 -->\n\t<buttonlist>\n\t\t<name>DS4</name>\n\t\t<player>dualshock/player</player>\n\t\t<move>dualshock/dpad</move>\n\t\t<jump>dualshock/x</jump>\n\t\t<shoot>dualshock/square</shoot>\n\t\t<altShoot>dualshock/circle</altShoot>\n\t\t<dodge>dualshock/r2</dodge>\n\t\t<start>dualshock/x</start>\n\t\t<confirm>dualshock/x</confirm>\n\t\t<back>dualshock/circle</back>\n\t\t<alt>dualshock/r2</alt>\n\t\t<alt2>dualshock/l2</alt2>\n\t\t<skip_replay>dualshock/triangle</skip_replay>\n\t</buttonlist>\n\t<!-- DualShock 3 -->\n\t<buttonlist>\n\t\t<name>DS3</name>\n\t\t<player>dualshock/player</player>\n\t\t<move>dualshock/dpad</move>\n\t\t<jump>dualshock/x</jump>\n\t\t<shoot>dualshock/square</shoot>\n\t\t<altShoot>dualshock/circle</altShoot>\n\t\t<dodge>dualshock/r2</dodge>\n\t\t<start>dualshock/psButton</start>\n\t\t<confirm>dualshock/x</confirm>\n\t\t<back>dualshock/circle</back>\n\t\t<alt>dualshock/r2</alt>\n\t\t<alt2>dualshock/l2</alt2>\n\t\t<skip_replay>dualshock/psButton</skip_replay>\n\t</buttonlist>\n\t<!-- DualShock 2 -->\n\t<buttonlist>\n\t\t<name>DS2</name>\n\t\t<player>dualshock/player</player>\n\t\t<move>dualshock/dpad</move>\n\t\t<jump>dualshock/x</jump>\n\t\t<shoot>dualshock/square</shoot>\n\t\t<altShoot>dualshock/circle</altShoot>\n\t\t<dodge>dualshock/r2</dodge>\n\t\t<start>dualshock/start</start>\n\t\t<confirm>dualshock/x</confirm>\n\t\t<back>dualshock/circle</back>\n\t\t<alt>dualshock/r2</alt>\n\t\t<alt2>dualshock/l2</alt2>\n\t\t<skip_replay>dualshock/start</skip_replay>\n\t</buttonlist>\n\t<!-- SNES -->\n\t<buttonlist>\n\t\t<name>SNES</name>\n\t\t<player>snes/player</player>\n\t\t<move>snes/dpad</move>\n\t\t<jump>snes/b</jump>\n\t\t<shoot>snes/y</shoot>\n\t\t<altShoot>snes/a</altShoot>\n\t\t<dodge>snes/r</dodge>\n\t\t<start>snes/start</start>\n\t\t<confirm>snes/b</confirm>\n\t\t<back>snes/a</back>\n\t\t<alt>snes/r</alt>\n\t\t<alt2>snes/l</alt2>\n\t\t<skip_replay>snes/start</skip_replay>\n\t</buttonlist>\n\t<!-- NES -->\n\t<buttonlist>\n\t\t<name>NES</name>\n\t\t<player>snes/player</player>\n\t\t<move>snes/dpad</move>\n\t\t<jump>snes/a</jump>\n\t\t<shoot>snes/b</shoot>\n\t\t<dodge>snes/select</dodge>\n\t\t<start>snes/start</start>\n\t\t<confirm>snes/a</confirm>\n\t\t<back>snes/b</back>\n\t\t<alt>snes/select</alt>\n\t\t<alt2>noButton</alt2>\n\t\t<skip_replay>snes/start</skip_replay>\n\t</buttonlist>\n\t<!-- Fighting Stick 3 -->\n\t<buttonlist>\n\t\t<name>FightingStick3</name>\n\t\t<player>dualshock/player</player>\n\t\t<move>dualshock/stick</move>\n\t\t<jump>dualshock/circle</jump>\n\t\t<shoot>dualshock/x</shoot>\n\t\t<dodge>dualshock/l2</dodge>\n\t\t<start>dualshock/start</start>\n\t\t<confirm>dualshock/circle</confirm>\n\t\t<back>dualshock/x</back>\n\t\t<alt>xb360/rt</alt>\n\t\t<alt2>xb360/lt</alt2>\n\t\t<skip_replay>dualshock/start</skip_replay>\n\t</buttonlist>\n\t<!-- Logitech Rumblepad 2 -->\n\t<buttonlist>\n\t\t<name>Rumblepad2</name>\n\t\t<player>generic/player</player>\n\t\t<move>generic/stick</move>\n\t\t<jump>generic/2</jump>\n\t\t<shoot>generic/1</shoot>\n\t\t<altShoot>generic/3</altShoot>\n\t\t<dodge>generic/shoulder6</dodge>\n\t\t<start>generic/start</start>\n\t\t<confirm>generic/2</confirm>\n\t\t<back>generic/3</back>\n\t\t<alt>generic/shoulder5</alt>\n\t\t<alt2>generic/shoulder6</alt2>\n\t\t<skip_replay>generic/start</skip_replay>\n\t</buttonlist>\n\t<!-- Logitech Dual Action -->\n\t<buttonlist>\n\t\t<name>DualAction</name>\n\t\t<player>generic/player</player>\n\t\t<move>generic/dpad</move>\n\t\t<jump>generic/2</jump>\n\t\t<shoot>generic/1</shoot>\n\t\t<altShoot>generic/3</altShoot>\n\t\t<dodge>generic/shoulder6</dodge>\n\t\t<start>generic/start</start>\n\t\t<confirm>generic/2</confirm>\n\t\t<back>generic/3</back>\n\t\t<alt>generic/shoulder6</alt>\n\t\t<alt2>generic/shoulder5</alt2>\n\t\t<skip_replay>generic/start</skip_replay>\n\t</buttonlist>\n\t<!-- Users can add buttonlists here. -->\n</buttonlists>\n");
        }
        if (!File.Exists(nameConfig)) 
        {
            using var sw = new StreamWriter(nameConfig, false);
            sw.WriteLine("<!-- GamepadName_Config.xml -->\n<controllers>\n\t<!-- Unknown devices -->\n\t<controller>\n\t\t<name>???</name>\n\t\t<guid>null</guid>\n\t\t<buttonset>XB360</buttonset>\n\t</controller>\n\t<!-- XInput devices -->\n\t<controller>\n\t\t<name>XInput</name>\n\t\t<guid>xinput</guid>\n\t\t<buttonset>XB360</buttonset>\n\t</controller>\n\t<!-- Xbox 360 -->\n\t<controller>\n\t\t<name>Xbox 360</name>\n\t\t<guid>5e048e02</guid>\n\t\t<guid>5e04a102</guid>\n\t\t<guid>380726b7</guid>\n\t\t<guid>5e041907</guid>\n\t\t<guid>5e049102</guid>\n\t\t<buttonset>XB360</buttonset>\n\t</controller>\n\t<!-- F310 -->\n\t<controller>\n\t\t<name>F310</name>\n\t\t<guid>6d041dc2</guid>\n\t\t<buttonset>XB360</buttonset>\n\t</controller>\n\t<!-- DualShock 4 -->\n\t<controller>\n\t\t<name>DualShock 4</name>\n\t\t<guid>4c05c405</guid>\n\t\t<buttonset>DS4</buttonset>\n\t</controller>\n\t<!-- DualShock 3 -->\n\t<controller>\n\t\t<name>DualShock 3</name>\n\t\t<guid>c0110055</guid>\n\t\t<buttonset>DS3</buttonset>\n\t</controller>\n\t<!-- DualShock 2 -->\n\t<controller>\n\t\t<name>DualShock 2</name>\n\t\t<guid>10080300</guid>\n\t\t<buttonset>DS2</buttonset>\n\t</controller>\n\t<!-- SNES -->\n\t<controller>\n\t\t<name>SNES</name>\n\t\t<guid>8f0e1303</guid>\n\t\t<buttonset>SNES</buttonset>\n\t</controller>\n\t<!-- NES -->\n\t<controller>\n\t\t<name>NES</name>\n\t\t<guid>bd12150d</guid>\n\t\t<buttonset>NES</buttonset>\n\t</controller>\n\t<!-- A.N.N.E -->\n\t<controller>\n\t\t<name>A.N.N.E.</name>\n\t\t<guid>79001100</guid>\n\t\t<buttonset>SNES</buttonset>\n\t</controller>\n\t<!-- iBuffalo -->\n\t<controller>\n\t\t<name>iBuffalo</name>\n\t\t<guid>83056020</guid>\n\t\t<buttonset>SNES</buttonset>\n\t</controller>\n\t<!-- Fighting Stick 3 -->\n\t<controller>\n\t\t<name>Fighting Stick 3</name>\n\t\t<guid>0d0f1000</guid>\n\t\t<buttonset>FS3</buttonset>\n\t</controller>\n\t<!-- Logitech Rumblepad 2 -->\n\t<controller>\n\t\t<name>Logitech Cordless Rumblepad 2</name>\n\t\t<guid>6d0419c2</guid>\n\t\t<buttonset>Rumblepad2</buttonset>\n\t</controller>\n\t<!-- Logitech Dual Action -->\n\t<controller>\n\t\t<name>Dual Action</name>\n\t\t<guid>6d0416c2</guid>\n\t\t<buttonset>DualAction</buttonset>\n\t</controller>\n\t<!-- Users can add controllers here. -->\n</controllers>");
        }
        var buttonLists = Calc.LoadXML(iconConfig)["buttonlists"].GetElementsByTagName("buttonlist");
        var controllers = Calc.LoadXML(nameConfig)["controllers"].GetElementsByTagName("controller");
        foreach (XmlElement button in buttonLists) 
        {
            try 
            {
                ButtonIconMap.Add(button.GetElementsByTagName("name")[0].InnerText, button);
            }
            catch 
            {
                Logger.Error("[XGamepadInput] Button List parsed error!");
            }
        }

        foreach (XmlElement controller in controllers) 
        {
            try 
            {
                var guid = controller.GetElementsByTagName("guid");
                foreach (XmlElement id in guid) 
                {
                    ControllerInfoMap.Add(id.InnerText, controller);
                }
            }
            catch 
            {
                Logger.Error("[XGamepadInput] Controllers parsed error!");
            }
        }
    }

    [MonoModReplace]
    private static Subtexture ReadPlayerIcon(XmlElement config, int id) 
    {
        var text = (id + 1).ToString();
        if (config == null || config["player"] == null)
            return TFGame.MenuAtlas["controls/generic/player" + text];
    
        var text2 = "controls/" + config["player"].InnerText;
        if (TFGame.MenuAtlas.Contains(text2))
            return TFGame.MenuAtlas[text2];
        var text3 = "controls/generic/player" + text;
        if (!TFGame.MenuAtlas.Contains(text3))
            // prevents potential crashes
            return TFGame.MenuAtlas["controls/generic/player1"];
        return TFGame.MenuAtlas["controls/generic/player" + text];
    }

    [MonoModReplace]
    private static Subtexture ReadButtonIcon(XmlElement config, string name) 
    {
        if (config == null || config[name] == null)
            return TFGame.MenuAtlas["controls/unknownButton"];
        
        var text = "controls/" + config[name].InnerText;
        if (!TFGame.MenuAtlas.Contains(text))
            return TFGame.MenuAtlas["controls/unknownButton"];
        return TFGame.MenuAtlas[text];
    }
}