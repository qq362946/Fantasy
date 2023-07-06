using Fantasy.Core;

namespace Fantasy
{
	public partial class LobbyUI : UI
	{
		public override string AssetName { get; protected set; } = "LobbyUI";
		public override string BundleName { get; protected set; } = "lobbyui";
		public override UILayer Layer { get; protected set; } = UILayer.BaseRoot;


		public override void Initialize()
		{
			var referenceComponent = GameObject.GetComponent<FantasyUI>();
		}
	}
}
