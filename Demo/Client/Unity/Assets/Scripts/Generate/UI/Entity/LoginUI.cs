using Fantasy.Core;

namespace Fantasy
{
	public partial class LoginUI : UI
	{
		public override string AssetName { get; protected set; } = "LoginUI";
		public override string BundleName { get; protected set; } = "loginui";
		public override UILayer Layer { get; protected set; } = UILayer.BaseRoot;

		public UnityEngine.UI.Button LoginButton;
		public UnityEngine.UI.InputField Password;
		public UnityEngine.UI.InputField UserName;

		public override void Initialize()
		{
			var referenceComponent = GameObject.GetComponent<FantasyUI>();
			LoginButton = referenceComponent.GetReference<UnityEngine.UI.Button>("LoginButton");
			Password = referenceComponent.GetReference<UnityEngine.UI.InputField>("Password");
			UserName = referenceComponent.GetReference<UnityEngine.UI.InputField>("UserName");
		}
	}
}
