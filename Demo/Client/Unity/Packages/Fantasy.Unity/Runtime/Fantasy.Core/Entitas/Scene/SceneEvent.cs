#if FANTASY_NET
using System;
using Fantasy.Core.Network;
namespace Fantasy
{
    public struct OnCreateScene
    {
        public readonly Scene Scene;

        public OnCreateScene(Scene scene)
        {
            Scene = scene;
        }
    }
}
#endif
