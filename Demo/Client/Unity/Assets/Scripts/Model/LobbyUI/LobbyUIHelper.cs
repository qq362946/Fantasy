namespace Fantasy.Model
{
    public sealed class LobbyUIAwakeSystem : AwakeSystem<LobbyUI>
    {
        protected override void Awake(LobbyUI self)
        {
            self.ShowLog();
        }
    }

    public static class LobbyUIHelper
    {
        public static void ShowLog(this LobbyUI self)
        {
            Log.Info("进入了游戏大厅");
        }
    }
}