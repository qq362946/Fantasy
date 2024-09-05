using System;
using System.IO;
// ReSharper disable UnassignedField.Global
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy
{
    public abstract class ANetworkMessageScheduler
    {
        protected readonly Scene Scene;
        protected readonly MessageDispatcherComponent MessageDispatcherComponent;
        protected readonly NetworkMessagingComponent NetworkMessagingComponent;
        protected ANetworkMessageScheduler(Scene scene)
        {
            Scene = scene;
            MessageDispatcherComponent = scene.MessageDispatcherComponent;
            NetworkMessagingComponent = scene.NetworkMessagingComponent;
        }
        public abstract void Scheduler(Session session, APackInfo packInfo);
    }
}