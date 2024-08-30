namespace Fantasy
{
    internal class M2G_StartGameHandler : Route<Scene, M2G_StartGame>
    {
        protected override async FTask Run(Scene scene, M2G_StartGame message)
        {
            var session = scene.GetEntity<Session>(message.ClientID);
            if (session != null)
            {
                session.Send(new G2C_StartGame() { });
            }
            else
            {
                var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new G2M_RemoveClient() { ClientID = message.ClientID });
            }

            await FTask.CompletedTask;
        }
    }
}
