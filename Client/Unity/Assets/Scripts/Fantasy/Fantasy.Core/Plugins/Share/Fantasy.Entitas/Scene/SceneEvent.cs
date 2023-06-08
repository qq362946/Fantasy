#if FANTASY_SERVER
using System;
using Fantasy.Core.Network;
namespace Fantasy
{
    public struct OnCreateScene
    {
        public readonly SceneConfigInfo SceneInfo;
        public readonly Action<Session> OnSetNetworkComplete;

        public OnCreateScene(SceneConfigInfo sceneInfo, Action<Session> onSetNetworkComplete)
        {
            SceneInfo = sceneInfo;
            OnSetNetworkComplete = onSetNetworkComplete;
        }
    }
}
#endif
