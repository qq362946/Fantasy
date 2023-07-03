using Fantasy.Core;

namespace Fantasy.Model
{
    public class OnAppStart_Init :AsyncEventSystem<OnAppStart>
    {
        public override async FTask Handler(OnAppStart self)
        {
            var scene = self.ClientScene;
            // 添加UI组件
            var uiComponent = scene.AddComponent<UIComponent>();
            // 初始化UI组件
            uiComponent.Initialize(1920, 1080, addAudioListener: false);
            // 添加音效管理组件
            scene.AddComponent<AudioManageComponent>();
            // 显示LoginUI
            uiComponent.AddComponent<LoginUI>();
            await FTask.CompletedTask;
        }
    }
}