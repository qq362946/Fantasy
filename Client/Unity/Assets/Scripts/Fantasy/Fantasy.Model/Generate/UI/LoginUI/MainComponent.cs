/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;
using Fantasy.Core;

namespace Fantasy
{
    public partial class MainComponent : UIComponent
    {
        public override string PackageName => "LoginUI";
        public override string ComponentName => "MainComponent";
        public override string ConfigBundleName => "loginui";
        public override string ResourceBundleName => "loginui";

        public GButton StoreButton;
        public GButton SetUPButton;
        public GTextField PlayerName;
        public GTextField PlayerPower;
        public GTextField PlayerDiamond;
        public GTextField PlayerGold;
        public GButton PowerButton;
        public GButton DiamondButton;
        public GButton GoldButton;
        public const string URL = "ui://69hhdwjqtt1v3i";

        public override void OnCreate()
        {
            StoreButton = (GButton)GComponent.GetChild("StoreButton");
            SetUPButton = (GButton)GComponent.GetChild("SetUPButton");
            PlayerName = (GTextField)GComponent.GetChild("PlayerName");
            PlayerPower = (GTextField)GComponent.GetChild("PlayerPower");
            PlayerDiamond = (GTextField)GComponent.GetChild("PlayerDiamond");
            PlayerGold = (GTextField)GComponent.GetChild("PlayerGold");
            PowerButton = (GButton)GComponent.GetChild("PowerButton");
            DiamondButton = (GButton)GComponent.GetChild("DiamondButton");
            GoldButton = (GButton)GComponent.GetChild("GoldButton");
        }
    }
}