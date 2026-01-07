using System;
using Fantasy.Async;
using Fantasy.Event;

namespace Fantasy
{
    public sealed class OnLoginComplete_Init_Event : AsyncEventSystem<OnLoginComplete>
    {
        protected override async FTask Handler(OnLoginComplete self)
        {
            var scene = self.Scene;
            // 挂载地图组件。
            scene.AddComponent<MapComponent>().Initialize("Starter Assets/Maps/Map01");
            // 挂载Unit管理组件
            scene.AddComponent<UnitManageComponent>();
            // 通知服务器初始化资源完成，可以接收服务器推送的消息
            scene.Session.C2M_InitComplete();
            await FTask.CompletedTask;
        }
    }
}