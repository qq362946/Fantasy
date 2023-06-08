/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;
using Fantasy.Core;
using UnityEngine;

namespace Fantasy
{
    public partial class LoginComponent : UIComponent
    {
        public override string PackageName => "LoginUI";
        public override string ComponentName => "LoginComponent";
        public override string ConfigBundleName => "loginui";
        public override string ResourceBundleName => "loginui";

        public GImage LoginBG;
        public GImage Login_Title;
        public GImage CadPa;
        public GButton PlayButton;
        public const string URL = "ui://69hhdwjqnrl90";

        public override void OnCreate()
        {
            LoginBG = (GImage)GComponent.GetChild("LoginBG");
            Login_Title = (GImage)GComponent.GetChild("Login_Title");
            CadPa = (GImage)GComponent.GetChild("CadPa");
            PlayButton = (GButton)GComponent.GetChild("PlayButton");
        }
    }
}